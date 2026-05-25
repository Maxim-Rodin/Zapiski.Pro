using System.Data;
using Zapisi.Pro;
using Zapiski.Pro.MiniApp.Models;

namespace Zapiski.Pro.MiniApp.Repositories
{
    public class MiniAppMasterRepository
    {
        private readonly DbHelper db;

        public MiniAppMasterRepository(DbHelper db)
        {
            this.db = db;
        }

        public MiniAppMasterProfileDto? GetMasterByKey(string key)
        {
            var safeKey = key.Replace("'", "''");

            var dt = db.ExecuteQuery($@"
                SELECT
                    m.""idMaster"",
                    m.""Key"",
                    u.""TelegrammId"",
                    u.""UserName""
                FROM ""Masters"" m
                JOIN ""Users"" u ON u.""idUser"" = m.""UserId""
                WHERE m.""Key"" = '{safeKey}'
                LIMIT 1
            ");

            if (dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];

            return new MiniAppMasterProfileDto
            {
                Id = Convert.ToInt32(row["idMaster"]),
                Key = row["Key"].ToString(),
                TelegramId = Convert.ToInt64(row["TelegrammId"]),
                Username = row["UserName"]?.ToString()
            };
        }

        public List<MiniAppMasterClientDto> GetClients(string key)
        {
            var safeKey = key.Replace("'", "''");

            var dt = db.ExecuteQuery($@"
                SELECT
                    u.""idUser"",
                    u.""TelegrammId"",
                    u.""UserName"",
                    COUNT(b.""idBooking"") AS ""BookingsCount"",
                    latest.""Date"" AS ""LastDate"",
                    latest.""Time"" AS ""LastTime"",
                    latest.""Status"" AS ""LastStatus""
                FROM ""Masters"" m
                JOIN ""Bookings"" b ON b.""MasterId"" = m.""idMaster""
                JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                LEFT JOIN LATERAL (
                    SELECT
                        lb.""Date"",
                        lb.""Time"",
                        lb.""Status""
                    FROM ""Bookings"" lb
                    WHERE lb.""MasterId"" = m.""idMaster""
                    AND lb.""UserId"" = u.""idUser""
                    ORDER BY lb.""Date"" DESC, lb.""Time"" DESC
                    LIMIT 1
                ) latest ON true
                WHERE m.""Key"" = '{safeKey}'
                GROUP BY
                    u.""idUser"",
                    u.""TelegrammId"",
                    u.""UserName"",
                    latest.""Date"",
                    latest.""Time"",
                    latest.""Status""
                ORDER BY latest.""Date"" DESC NULLS LAST, latest.""Time"" DESC NULLS LAST, u.""idUser"" DESC
            ");

            var clients = new List<MiniAppMasterClientDto>();

            foreach (DataRow row in dt.Rows)
            {
                clients.Add(new MiniAppMasterClientDto
                {
                    Id = Convert.ToInt32(row["idUser"]),
                    TelegramId = Convert.ToInt64(row["TelegrammId"]),
                    Username = row["UserName"]?.ToString(),
                    BookingsCount = Convert.ToInt32(row["BookingsCount"]),
                    LastBookingAt = FormatBookingDateTime(row["LastDate"], row["LastTime"]),
                    LastStatus = row["LastStatus"] == DBNull.Value ? "inactive" : row["LastStatus"]?.ToString()
                });
            }

            return clients;
        }

        public MiniAppMasterStatsDto GetStats(string key)
        {
            var safeKey = key.Replace("'", "''");

            return new MiniAppMasterStatsDto
            {
                Clients = Convert.ToInt32(db.ExecuteScalar($@"
                    SELECT COUNT(DISTINCT b.""UserId"")
                    FROM ""Bookings"" b
                    JOIN ""Masters"" m ON m.""idMaster"" = b.""MasterId""
                    WHERE m.""Key"" = '{safeKey}'
                ")),
                ActiveBookings = Convert.ToInt32(db.ExecuteScalar($@"
                    SELECT COUNT(*)
                    FROM ""Bookings"" b
                    JOIN ""Masters"" m ON m.""idMaster"" = b.""MasterId""
                    WHERE m.""Key"" = '{safeKey}'
                    AND b.""Status"" NOT IN ('cancelled', 'completed')
                ")),
                Services = Convert.ToInt32(db.ExecuteScalar($@"
                    SELECT COUNT(*)
                    FROM ""Services"" s
                    JOIN ""Masters"" m ON m.""idMaster"" = s.""MasterId""
                    WHERE m.""Key"" = '{safeKey}'
                "))
            };
        }

        private static string? FormatBookingDateTime(object dateValue, object timeValue)
        {
            if (dateValue == DBNull.Value || timeValue == DBNull.Value)
                return null;

            var date = (DateOnly)dateValue;
            var time = TimeSpan.Parse(timeValue.ToString());

            return $"{date:dd.MM.yyyy} {time:hh\\:mm}";
        }
    }
}
