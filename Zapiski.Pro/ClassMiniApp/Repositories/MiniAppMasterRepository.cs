using System.Data;
using Npgsql;
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

        public List<MiniAppMasterServiceDto> GetServices(string key)
        {
            var safeKey = key.Replace("'", "''");

            var dt = db.ExecuteQuery($@"
                SELECT
                    s.""idService"",
                    COALESCE(s.""Name"", 'Без названия') AS ""Name"",
                    COALESCE(s.""Price"", 0) AS ""Price"",
                    COALESCE(s.""Duration"", 0) AS ""Duration"",
                    COALESCE(s.""PrepaymentPercent"", 0) AS ""PrepaymentPercent""
                FROM ""Services"" s
                JOIN ""Masters"" m ON m.""idMaster"" = s.""MasterId""
                WHERE m.""Key"" = '{safeKey}'
                ORDER BY s.""idService"" DESC
            ");

            var services = new List<MiniAppMasterServiceDto>();

            foreach (DataRow row in dt.Rows)
            {
                var price = Convert.ToInt32(row["Price"]);
                var percent = Convert.ToInt32(row["PrepaymentPercent"]);

                services.Add(new MiniAppMasterServiceDto
                {
                    Id = Convert.ToInt32(row["idService"]),
                    Name = row["Name"]?.ToString(),
                    Price = price,
                    Duration = Convert.ToInt32(row["Duration"]),
                    PrepaymentPercent = percent,
                    PrepaymentAmount = (price * percent) / 100
                });
            }

            return services;
        }

        public MiniAppMasterActionResult CreateService(string key, MiniAppCreateMasterServiceRequest request)
        {
            var master = GetMasterByKey(key);

            if (master == null)
                return Failed("Мастер не найден");

            var name = request.Name?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return Failed("Введите название услуги");

            if (request.Price <= 0)
                return Failed("Цена должна быть больше 0");

            if (request.Duration <= 0)
                return Failed("Длительность должна быть больше 0");

            if (request.PrepaymentPercent < 0 || request.PrepaymentPercent > 100)
                return Failed("Предоплата должна быть от 0 до 100%");

            db.ExecuteNonQuery(@"
                INSERT INTO ""Services""
                    (""MasterId"", ""Name"", ""Price"", ""Duration"", ""PrepaymentPercent"")
                VALUES
                    (@masterId, @name, @price, @duration, @prepaymentPercent)
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("price", request.Price),
                new NpgsqlParameter("duration", request.Duration),
                new NpgsqlParameter("prepaymentPercent", request.PrepaymentPercent));

            return Ok("Услуга добавлена");
        }

        public MiniAppMasterActionResult UpdateService(string key, int serviceId, MiniAppCreateMasterServiceRequest request)
        {
            var master = GetMasterByKey(key);

            if (master == null)
                return Failed("Мастер не найден");

            var name = request.Name?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return Failed("Введите название услуги");

            if (request.Price <= 0)
                return Failed("Цена должна быть больше 0");

            if (request.Duration <= 0)
                return Failed("Длительность должна быть больше 0");

            if (request.PrepaymentPercent < 0 || request.PrepaymentPercent > 100)
                return Failed("Предоплата должна быть от 0 до 100%");

            var updated = db.ExecuteNonQuery(@"
                UPDATE ""Services""
                SET
                    ""Name"" = @name,
                    ""Price"" = @price,
                    ""Duration"" = @duration,
                    ""PrepaymentPercent"" = @prepaymentPercent
                WHERE ""idService"" = @serviceId
                AND ""MasterId"" = @masterId
            ",
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("price", request.Price),
                new NpgsqlParameter("duration", request.Duration),
                new NpgsqlParameter("prepaymentPercent", request.PrepaymentPercent),
                new NpgsqlParameter("serviceId", serviceId),
                new NpgsqlParameter("masterId", master.Id));

            if (updated == 0)
                return Failed("Услуга не найдена");

            return Ok("Услуга обновлена");
        }

        public MiniAppMasterActionResult DeleteService(string key, int serviceId)
        {
            var master = GetMasterByKey(key);

            if (master == null)
                return Failed("Мастер не найден");

            var bookingsCount = Convert.ToInt32(db.ExecuteScalar(@"
                SELECT COUNT(*)
                FROM ""Bookings""
                WHERE ""ServiceId"" = @serviceId
            ", new NpgsqlParameter("serviceId", serviceId)));

            if (bookingsCount > 0)
                return Failed($"Нельзя удалить услугу: есть связанные записи ({bookingsCount})");

            var deleted = db.ExecuteNonQuery(@"
                DELETE FROM ""Services""
                WHERE ""idService"" = @serviceId
                AND ""MasterId"" = @masterId
            ",
                new NpgsqlParameter("serviceId", serviceId),
                new NpgsqlParameter("masterId", master.Id));

            if (deleted == 0)
                return Failed("Услуга не найдена");

            return Ok("Услуга удалена");
        }

        private static MiniAppMasterActionResult Ok(string message)
        {
            return new MiniAppMasterActionResult { Success = true, Message = message };
        }

        private static MiniAppMasterActionResult Failed(string message)
        {
            return new MiniAppMasterActionResult { Success = false, Message = message };
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
