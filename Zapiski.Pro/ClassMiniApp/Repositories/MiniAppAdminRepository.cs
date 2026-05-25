using System.Data;
using Zapisi.Pro;
using Zapiski.Pro.MiniApp.Models;

namespace Zapiski.Pro.MiniApp.Repositories
{
    public class MiniAppAdminRepository
    {
        private readonly DbHelper db;

        public MiniAppAdminRepository(DbHelper db)
        {
            this.db = db;
        }

        public MiniAppAdminStatsDto GetStats()
        {
            return new MiniAppAdminStatsDto
            {
                Users = Convert.ToInt32(db.ExecuteScalar(@"SELECT COUNT(*) FROM ""Users""")),
                Masters = Convert.ToInt32(db.ExecuteScalar(@"SELECT COUNT(*) FROM ""Masters""")),
                Bookings = Convert.ToInt32(db.ExecuteScalar(@"SELECT COUNT(*) FROM ""Bookings""")),
                Payments = Convert.ToInt32(db.ExecuteScalar(@"
                    SELECT COUNT(*) 
                    FROM ""Bookings""
                    WHERE ""Status"" = 'waiting_payment_confirm'
                "))
            };
        }

        public List<MiniAppMasterDto> GetMasters()
        {
            var dt = db.ExecuteQuery(@"
                SELECT 
                    m.""idMaster"",
                    m.""Key"",
                    u.""TelegrammId"",
                    u.""UserName""
                FROM ""Masters"" m
                JOIN ""Users"" u ON u.""idUser"" = m.""UserId""
                ORDER BY m.""idMaster"" DESC
            ");

            var masters = new List<MiniAppMasterDto>();

            foreach (DataRow row in dt.Rows)
            {
                masters.Add(new MiniAppMasterDto
                {
                    Id = Convert.ToInt32(row["idMaster"]),
                    Key = row["Key"].ToString(),
                    TelegramId = Convert.ToInt64(row["TelegrammId"]),
                    Username = row["UserName"]?.ToString()
                });
            }

            return masters;
        }

        public List<MiniAppUserDto> GetUsers()
        {
            var dt = db.ExecuteQuery(@"
                SELECT 
                    u.""idUser"",
                    u.""TelegrammId"",
                    u.""UserName"",
                    COUNT(b.""idBooking"") AS ""BookingsCount""
                FROM ""Users"" u
                LEFT JOIN ""Bookings"" b ON b.""UserId"" = u.""idUser""
                GROUP BY u.""idUser"", u.""TelegrammId"", u.""UserName""
                ORDER BY u.""idUser"" DESC
            ");

            var users = new List<MiniAppUserDto>();

            foreach (DataRow row in dt.Rows)
            {
                users.Add(new MiniAppUserDto
                {
                    Id = Convert.ToInt32(row["idUser"]),
                    TelegramId = Convert.ToInt64(row["TelegrammId"]),
                    Username = row["UserName"]?.ToString(),
                    BookingsCount = Convert.ToInt32(row["BookingsCount"])
                });
            }

            return users;
        }

        public bool UserExistsByTelegramId(long telegramId)
        {
            var dt = db.ExecuteQuery($@"
                SELECT 1
                FROM ""Users""
                WHERE ""TelegrammId"" = {telegramId}
                LIMIT 1
            ");

            return dt.Rows.Count > 0;
        }

        public bool MasterKeyExists(string key)
        {
            var safeKey = key.Replace("'", "''");

            var dt = db.ExecuteQuery($@"
                SELECT 1
                FROM ""Masters""
                WHERE ""Key"" = '{safeKey}'
                LIMIT 1
            ");

            return dt.Rows.Count > 0;
        }

        public int GetMasterIdByKey(string key)
        {
            var safeKey = key.Replace("'", "''");

            var result = db.ExecuteScalar($@"
                SELECT ""idMaster""
                FROM ""Masters""
                WHERE ""Key"" = '{safeKey}'
            ");

            return Convert.ToInt32(result);
        }

        public MiniAppMasterDto? GetMasterById(int masterId)
        {
            var dt = db.ExecuteQuery($@"
                SELECT 
                    m.""idMaster"",
                    m.""Key"",
                    u.""TelegrammId"",
                    u.""UserName""
                FROM ""Masters"" m
                JOIN ""Users"" u ON u.""idUser"" = m.""UserId""
                WHERE m.""idMaster"" = {masterId}
            ");

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];

            return new MiniAppMasterDto
            {
                Id = Convert.ToInt32(row["idMaster"]),
                Key = row["Key"].ToString(),
                TelegramId = Convert.ToInt64(row["TelegrammId"]),
                Username = row["UserName"]?.ToString()
            };
        }

        public void CreateMaster(long telegramId, string key)
        {
            var safeKey = key.Replace("'", "''");

            db.ExecuteNonQuery($@"
                INSERT INTO public.""Masters"" (""UserId"", ""Key"")
                VALUES (
                    (SELECT ""idUser"" FROM public.""Users"" WHERE ""TelegrammId"" = {telegramId}),
                    '{safeKey}'
                );

                UPDATE public.""Users""
                SET ""Role"" = 'master'
                WHERE ""TelegrammId"" = {telegramId};
            ");
        }

        public void DeleteMaster(int masterId, long telegramId)
        {
            db.ExecuteNonQuery($@"
                DELETE FROM ""Masters""
                WHERE ""idMaster"" = {masterId};

                UPDATE ""Users""
                SET ""Role"" = 'client'
                WHERE ""TelegrammId"" = {telegramId};
            ");
        }

        public void CreateDefaultSchedule(int masterId)
        {
            for (int day = 1; day <= 7; day++)
            {
                db.ExecuteNonQuery($@"
            INSERT INTO ""MasterSchedule""
            (
                ""MasterId"",
                ""DayOfWeek"",
                ""StartTime"",
                ""EndTime"",
                ""IsActive""
            )
            VALUES
            (
                {masterId},
                {day},
                '09:00:00',
                '18:00:00',
                false
            )
        ");
            }
        }

        public bool IsAdmin(long telegramId)
        {
            var result = db.ExecuteScalar($@"
        SELECT ""Role""
        FROM ""Users""
        WHERE ""TelegrammId"" = {telegramId}
        LIMIT 1
    ");

            return result != null && result.ToString() == "admin";
        }
    }
}
