using System.Data;
using Npgsql;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Zapisi.Pro;
using Zapiski.Pro.BasedClasses;
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
                    COALESCE(m.""Name"", '') AS ""Name"",
                    COALESCE(m.""Description"", '') AS ""Description"",
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
                Username = row["UserName"]?.ToString(),
                Name = row["Name"]?.ToString(),
                Description = row["Description"]?.ToString()
            };
        }

        public MiniAppMasterActionResult UpdateProfile(string key, long telegramId, MiniAppUpdateMasterProfileRequest request)
        {
            var master = GetMasterByKey(key);

            if (master == null)
                return Failed("Мастер не найден");

            if (master.TelegramId != telegramId)
                return Failed("Нет доступа к этому профилю");

            var name = request.Name?.Trim();
            var description = request.Description?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
                return Failed("Введите имя мастера");

            if (name.Length > 50)
                return Failed("Имя слишком длинное");

            if (description.Length > 1000)
                return Failed("Описание слишком длинное");

            db.ExecuteNonQuery(@"
                UPDATE ""Masters""
                SET
                    ""Name"" = @name,
                    ""Description"" = @description
                WHERE ""idMaster"" = @masterId
            ",
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("description", description),
                new NpgsqlParameter("masterId", master.Id));

            return Ok("Профиль обновлён");
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

        public List<MiniAppMasterScheduleDayDto> GetSchedule(string key, long telegramId)
        {
            var master = GetMasterByKey(key);

            if (master == null || master.TelegramId != telegramId)
                return new List<MiniAppMasterScheduleDayDto>();

            EnsureSchedule(master.Id);

            var table = db.ExecuteQuery(@"
                SELECT ""DayOfWeek"", ""StartTime"", ""EndTime"", ""IsActive""
                FROM ""MasterSchedule""
                WHERE ""MasterId"" = @masterId
                ORDER BY ""DayOfWeek""
            ", new NpgsqlParameter("masterId", master.Id));

            var schedule = new List<MiniAppMasterScheduleDayDto>();

            foreach (DataRow row in table.Rows)
            {
                schedule.Add(new MiniAppMasterScheduleDayDto
                {
                    DayOfWeek = Convert.ToInt32(row["DayOfWeek"]),
                    DayName = GetDayName(Convert.ToInt32(row["DayOfWeek"])),
                    StartTime = FormatTime(row["StartTime"]),
                    EndTime = FormatTime(row["EndTime"]),
                    IsActive = Convert.ToBoolean(row["IsActive"])
                });
            }

            return schedule;
        }

        public MiniAppMasterActionResult UpdateScheduleDay(string key, long telegramId, int day, MiniAppUpdateScheduleDayRequest request)
        {
            var master = GetMasterByKey(key);

            if (master == null)
                return Failed("Мастер не найден");

            if (master.TelegramId != telegramId)
                return Failed("Нет доступа к этому расписанию");

            if (day < 1 || day > 7)
                return Failed("Неверный день недели");

            var startText = NormalizeTime(request.StartTime);
            var endText = NormalizeTime(request.EndTime);

            if (!TimeSpan.TryParse(startText, out var start) || !TimeSpan.TryParse(endText, out var end))
                return Failed("Время введено неверно");

            if (start >= end)
                return Failed("Время начала должно быть раньше конца");

            EnsureSchedule(master.Id);

            db.ExecuteNonQuery(@"
                UPDATE ""MasterSchedule""
                SET
                    ""StartTime"" = @startTime,
                    ""EndTime"" = @endTime,
                    ""IsActive"" = @isActive
                WHERE ""MasterId"" = @masterId
                AND ""DayOfWeek"" = @day
            ",
                new NpgsqlParameter("startTime", start),
                new NpgsqlParameter("endTime", end),
                new NpgsqlParameter("isActive", request.IsActive),
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("day", day));

            return Ok("Расписание обновлено");
        }

        public List<MiniAppMasterBookingDto> GetBookings(string key, long telegramId)
        {
            var master = GetMasterByKey(key);

            if (master == null || master.TelegramId != telegramId)
                return new List<MiniAppMasterBookingDto>();

            var dt = db.ExecuteQuery(@"
                SELECT
                    b.""idBooking"",
                    b.""Date"",
                    b.""Time"",
                    b.""Status"",
                    u.""TelegrammId"",
                    u.""UserName"",
                    s.""Name"" AS ""ServiceName"",
                    COALESCE(s.""Price"", 0) AS ""Price"",
                    COALESCE(s.""PrepaymentPercent"", 0) AS ""PrepaymentPercent""
                FROM ""Bookings"" b
                JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                WHERE b.""MasterId"" = @masterId
                AND b.""Status"" != 'completed'
                ORDER BY b.""Date"" ASC, b.""Time"" ASC
            ", new NpgsqlParameter("masterId", master.Id));

            var bookings = new List<MiniAppMasterBookingDto>();

            foreach (DataRow row in dt.Rows)
            {
                var price = Convert.ToInt32(row["Price"]);
                var percent = Convert.ToInt32(row["PrepaymentPercent"]);

                bookings.Add(new MiniAppMasterBookingDto
                {
                    Id = Convert.ToInt32(row["idBooking"]),
                    ClientTelegramId = Convert.ToInt64(row["TelegrammId"]),
                    ClientUsername = row["UserName"]?.ToString(),
                    ServiceName = row["ServiceName"]?.ToString(),
                    DateTime = FormatBookingDateTime(row["Date"], row["Time"]),
                    Status = row["Status"]?.ToString(),
                    Price = price,
                    PrepaymentPercent = percent,
                    PrepaymentAmount = (price * percent) / 100
                });
            }

            return bookings;
        }

        public async Task<MiniAppMasterActionResult> AcceptBooking(string key, long telegramId, int bookingId)
        {
            var booking = GetBookingForAction(key, telegramId, bookingId);

            if (booking == null)
                return Failed("Запись не найдена");

            var status = booking["Status"]?.ToString();

            if (status != "pending")
                return Failed("Эту запись уже нельзя подтвердить");

            db.ExecuteNonQuery(@"
                UPDATE ""Bookings""
                SET ""Status"" = 'confirmed'
                WHERE ""idBooking"" = @bookingId
            ", new NpgsqlParameter("bookingId", bookingId));

            var clientId = Convert.ToInt64(booking["TelegrammId"]);
            var date = (DateOnly)booking["Date"];
            var time = (TimeOnly)booking["Time"];
            var appointmentTime = date.ToDateTime(time);
            var serviceName = booking["ServiceName"]?.ToString() ?? "Услуга";
            var durationMinutes = Convert.ToInt32(booking["Duration"]);

            await BookingJobs.BotClient.SendMessage(
                clientId,
                "✅ Ваша запись подтверждена",
                replyMarkup: ClientMenuKeyboard());

            BookingJobs.ScheduleAllReminders(
                bookingId,
                clientId,
                appointmentTime,
                serviceName,
                durationMinutes);

            return Ok("Запись подтверждена");
        }

        public async Task<MiniAppMasterActionResult> CancelBooking(string key, long telegramId, int bookingId)
        {
            var booking = GetBookingForAction(key, telegramId, bookingId);

            if (booking == null)
                return Failed("Запись не найдена");

            var status = booking["Status"]?.ToString();

            if (status == "cancelled" || status == "completed")
                return Failed("Эту запись уже нельзя отменить");

            db.ExecuteNonQuery(@"
                UPDATE ""Bookings""
                SET ""Status"" = 'cancelled'
                WHERE ""idBooking"" = @bookingId
            ", new NpgsqlParameter("bookingId", bookingId));

            var clientId = Convert.ToInt64(booking["TelegrammId"]);

            await BookingJobs.BotClient.SendMessage(
                clientId,
                "❌ Ваша запись отменена мастером",
                replyMarkup: ClientMenuKeyboard());

            return Ok("Запись отменена");
        }

        public async Task<MiniAppMasterActionResult> AcceptPayment(string key, long telegramId, int bookingId)
        {
            var booking = GetBookingForAction(key, telegramId, bookingId);

            if (booking == null)
                return Failed("Запись не найдена");

            if (booking["Status"]?.ToString() != "waiting_payment_confirm")
                return Failed("Эта запись не ожидает подтверждения оплаты");

            db.ExecuteNonQuery(@"
                UPDATE ""Bookings""
                SET ""Status"" = 'confirmed'
                WHERE ""idBooking"" = @bookingId
            ", new NpgsqlParameter("bookingId", bookingId));

            var clientId = Convert.ToInt64(booking["TelegrammId"]);
            var date = (DateOnly)booking["Date"];
            var time = (TimeOnly)booking["Time"];
            var appointmentTime = date.ToDateTime(time);
            var serviceName = booking["ServiceName"]?.ToString() ?? "Услуга";
            var durationMinutes = Convert.ToInt32(booking["Duration"]);

            await BookingJobs.BotClient.SendMessage(
                clientId,
                "✅ Предоплата подтверждена\n\n🎉 Запись успешно подтверждена",
                replyMarkup: ClientMenuKeyboard());

            BookingJobs.ScheduleAllReminders(
                bookingId,
                clientId,
                appointmentTime,
                serviceName,
                durationMinutes);

            return Ok("Предоплата подтверждена");
        }

        public async Task<MiniAppMasterActionResult> RejectPayment(string key, long telegramId, int bookingId)
        {
            var master = GetMasterByKey(key);

            if (master == null || master.TelegramId != telegramId)
                return Failed("Запись не найдена");

            var table = db.ExecuteQuery(@"
                SELECT
                    u.""TelegrammId"",
                    s.""Name"" AS ""ServiceName"",
                    s.""Price"",
                    s.""PrepaymentPercent"",
                    m.""PaymentDetails""
                FROM ""Bookings"" b
                JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                JOIN ""Masters"" m ON m.""idMaster"" = b.""MasterId""
                WHERE b.""idBooking"" = @bookingId
                AND b.""MasterId"" = @masterId
                AND b.""Status"" = 'waiting_payment_confirm'
                LIMIT 1
            ",
                new NpgsqlParameter("bookingId", bookingId),
                new NpgsqlParameter("masterId", master.Id));

            if (table.Rows.Count == 0)
                return Failed("Эта запись не ожидает подтверждения оплаты");

            var row = table.Rows[0];
            var clientId = Convert.ToInt64(row["TelegrammId"]);
            var serviceName = row["ServiceName"]?.ToString() ?? "Услуга";
            var price = Convert.ToInt32(row["Price"]);
            var percent = Convert.ToInt32(row["PrepaymentPercent"]);
            var prepaymentAmount = (price * percent) / 100;
            var paymentDetails = row["PaymentDetails"] != DBNull.Value
                ? row["PaymentDetails"]?.ToString()
                : "Реквизиты не указаны";

            db.ExecuteNonQuery(@"
                UPDATE ""Bookings""
                SET ""Status"" = 'waiting_payment'
                WHERE ""idBooking"" = @bookingId
            ", new NpgsqlParameter("bookingId", bookingId));

            await BookingJobs.BotClient.SendMessage(
                clientId,
                $"❌ Мастер не подтвердил оплату\n\n" +
                $"💼 {serviceName}\n" +
                $"💸 Предоплата: {prepaymentAmount}₽\n\n" +
                $"Проверьте перевод и попробуйте снова\n\n" +
                $"Реквизиты:\n{paymentDetails}",
                replyMarkup: PaymentKeyboard(bookingId));

            return Ok("Оплата отклонена");
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

        private DataRow? GetBookingForAction(string key, long telegramId, int bookingId)
        {
            var master = GetMasterByKey(key);

            if (master == null || master.TelegramId != telegramId)
                return null;

            var dt = db.ExecuteQuery(@"
                SELECT
                    b.""idBooking"",
                    b.""Date"",
                    b.""Time"",
                    b.""Status"",
                    u.""TelegrammId"",
                    s.""Name"" AS ""ServiceName"",
                    s.""Duration""
                FROM ""Bookings"" b
                JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                WHERE b.""idBooking"" = @bookingId
                AND b.""MasterId"" = @masterId
                LIMIT 1
            ",
                new NpgsqlParameter("bookingId", bookingId),
                new NpgsqlParameter("masterId", master.Id));

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        private static InlineKeyboardMarkup ClientMenuKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🏠 В меню", "client:menu")
                }
            });
        }

        private static InlineKeyboardMarkup PaymentKeyboard(int bookingId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Я оплатил", $"client:paid_booking:{bookingId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🏠 В меню", "client:menu")
                }
            });
        }

        private void EnsureSchedule(int masterId)
        {
            var count = Convert.ToInt32(db.ExecuteScalar(@"
                SELECT COUNT(*)
                FROM ""MasterSchedule""
                WHERE ""MasterId"" = @masterId
            ", new NpgsqlParameter("masterId", masterId)));

            if (count > 0)
                return;

            for (var day = 1; day <= 7; day++)
            {
                var isWorkDay = day <= 5;

                db.ExecuteNonQuery(@"
                    INSERT INTO ""MasterSchedule""
                        (""MasterId"", ""DayOfWeek"", ""StartTime"", ""EndTime"", ""IsActive"")
                    VALUES
                        (@masterId, @day, @startTime, @endTime, @isActive)
                ",
                    new NpgsqlParameter("masterId", masterId),
                    new NpgsqlParameter("day", day),
                    new NpgsqlParameter("startTime", TimeSpan.Parse("09:00")),
                    new NpgsqlParameter("endTime", TimeSpan.Parse("18:00")),
                    new NpgsqlParameter("isActive", isWorkDay));
            }
        }

        private static string FormatTime(object value)
        {
            if (value == DBNull.Value)
                return "";

            if (value is TimeOnly timeOnly)
                return timeOnly.ToString("HH:mm");

            if (TimeSpan.TryParse(value.ToString(), out var timeSpan))
                return timeSpan.ToString(@"hh\:mm");

            return "";
        }

        private static string NormalizeTime(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace(" ", "")
                .Replace("–", "-")
                .Replace("—", "-");
        }

        private static string GetDayName(int day)
        {
            return day switch
            {
                1 => "Понедельник",
                2 => "Вторник",
                3 => "Среда",
                4 => "Четверг",
                5 => "Пятница",
                6 => "Суббота",
                7 => "Воскресенье",
                _ => "День"
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
