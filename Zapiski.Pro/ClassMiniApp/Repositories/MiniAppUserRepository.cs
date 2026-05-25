using System.Data;
using Zapisi.Pro;
using Zapiski.Pro.ClassMiniApp.Models;

namespace Zapiski.Pro.ClassMiniApp.Repositories
{
    public class MiniAppUserRepository
    {
        private readonly DbHelper db;

        public MiniAppUserRepository(DbHelper db)
        {
            this.db = db;
        }

        public MiniAppUserDashboardDto? GetDashboard(long telegramId)
        {
            var userTable = db.ExecuteQuery($@"
                SELECT ""idUser"", ""TelegrammId"", ""UserName""
                FROM ""Users""
                WHERE ""TelegrammId"" = {telegramId}
                LIMIT 1
            ");

            if (userTable.Rows.Count == 0)
                return null;

            var userRow = userTable.Rows[0];
            var userId = Convert.ToInt32(userRow["idUser"]);

            return new MiniAppUserDashboardDto
            {
                Profile = new MiniAppUserProfileDto
                {
                    Id = userId,
                    TelegramId = Convert.ToInt64(userRow["TelegrammId"]),
                    Username = userRow["UserName"]?.ToString()
                },
                Roles = GetRoles(userId),
                Bookings = GetBookings(userId),
                Masters = GetMasters(userId)
            };
        }

        private MiniAppUserRoleDto GetRoles(int userId)
        {
            var userTable = db.ExecuteQuery($@"
                SELECT ""Role""
                FROM ""Users""
                WHERE ""idUser"" = {userId}
                LIMIT 1
            ");

            var role = userTable.Rows.Count == 0
                ? "client"
                : userTable.Rows[0]["Role"]?.ToString();

            var masterTable = db.ExecuteQuery($@"
                SELECT ""Key""
                FROM ""Masters""
                WHERE ""UserId"" = {userId}
                LIMIT 1
            ");

            return new MiniAppUserRoleDto
            {
                IsAdmin = role == "admin",
                IsMaster = masterTable.Rows.Count > 0,
                MasterKey = masterTable.Rows.Count > 0
                    ? masterTable.Rows[0]["Key"]?.ToString()
                    : null
            };
        }

        private List<MiniAppUserBookingDto> GetBookings(int userId)
        {
            var table = db.ExecuteQuery($@"
                SELECT
                    b.""idBooking"",
                    b.""Date"",
                    b.""Time"",
                    b.""Status"",
                    s.""Name"" AS ""ServiceName"",
                    m.""Key"" AS ""MasterKey"",
                    mu.""UserName"" AS ""MasterUsername""
                FROM ""Bookings"" b
                JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                JOIN ""Masters"" m ON m.""idMaster"" = b.""MasterId""
                JOIN ""Users"" mu ON mu.""idUser"" = m.""UserId""
                WHERE b.""UserId"" = {userId}
                AND b.""Status"" != 'cancelled'
                ORDER BY b.""Date"" DESC, b.""Time"" DESC
            ");

            var bookings = new List<MiniAppUserBookingDto>();

            foreach (DataRow row in table.Rows)
            {
                bookings.Add(new MiniAppUserBookingDto
                {
                    Id = Convert.ToInt32(row["idBooking"]),
                    ServiceName = row["ServiceName"]?.ToString(),
                    MasterKey = row["MasterKey"]?.ToString(),
                    MasterUsername = row["MasterUsername"]?.ToString(),
                    DateTime = FormatBookingDateTime(row["Date"], row["Time"]),
                    Status = row["Status"]?.ToString()
                });
            }

            return bookings;
        }

        private List<MiniAppUserMasterDto> GetMasters(int userId)
        {
            var table = db.ExecuteQuery($@"
                SELECT
                    m.""idMaster"",
                    m.""Key"",
                    mu.""UserName"",
                    COUNT(b.""idBooking"") AS ""BookingsCount""
                FROM ""Bookings"" b
                JOIN ""Masters"" m ON m.""idMaster"" = b.""MasterId""
                JOIN ""Users"" mu ON mu.""idUser"" = m.""UserId""
                WHERE b.""UserId"" = {userId}
                GROUP BY m.""idMaster"", m.""Key"", mu.""UserName""
                ORDER BY COUNT(b.""idBooking"") DESC, m.""idMaster"" DESC
            ");

            var masters = new List<MiniAppUserMasterDto>();

            foreach (DataRow row in table.Rows)
            {
                masters.Add(new MiniAppUserMasterDto
                {
                    Id = Convert.ToInt32(row["idMaster"]),
                    Key = row["Key"]?.ToString(),
                    Username = row["UserName"]?.ToString(),
                    BookingsCount = Convert.ToInt32(row["BookingsCount"])
                });
            }

            return masters;
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
