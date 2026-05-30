using System.Data;
using Npgsql;
using Telegram.Bot;
using Telegram.Bot.Types;
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
                    COALESCE(m.""PaymentDetails"", '') AS ""PaymentDetails"",
                    COALESCE(m.""PhoneNumber"", '') AS ""PhoneNumber"",
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
                Description = row["Description"]?.ToString(),
                PaymentDetails = row["PaymentDetails"]?.ToString(),
                PhoneNumber = row["PhoneNumber"]?.ToString()
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
            var paymentDetails = request.PaymentDetails?.Trim() ?? string.Empty;
            var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
                return Failed("Введите имя мастера");

            if (name.Length > 50)
                return Failed("Имя слишком длинное");

            if (description.Length > 1000)
                return Failed("Описание слишком длинное");

            if (paymentDetails.Length > 1000)
                return Failed("Реквизиты слишком длинные");

            if (phoneNumber.Length > 30)
                return Failed("Телефон слишком длинный");

            db.ExecuteNonQuery(@"
                UPDATE ""Masters""
                SET
                    ""Name"" = @name,
                    ""Description"" = @description,
                    ""PaymentDetails"" = @paymentDetails,
                    ""PhoneNumber"" = @phoneNumber
                WHERE ""idMaster"" = @masterId
            ",
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("description", description),
                new NpgsqlParameter("paymentDetails", string.IsNullOrWhiteSpace(paymentDetails) ? DBNull.Value : paymentDetails),
                new NpgsqlParameter("phoneNumber", string.IsNullOrWhiteSpace(phoneNumber) ? DBNull.Value : phoneNumber),
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

        public MiniAppMasterScheduleModeDto GetScheduleMode(string key, long telegramId)
        {
            var master = GetMasterByKey(key);

            if (master == null || master.TelegramId != telegramId)
                return new MiniAppMasterScheduleModeDto();

            var mode = db.ExecuteScalar(@"
                SELECT COALESCE(""ScheduleMode"", 'stable')
                FROM ""Masters""
                WHERE ""idMaster"" = @masterId
            ", new NpgsqlParameter("masterId", master.Id))?.ToString();

            return new MiniAppMasterScheduleModeDto
            {
                Mode = mode == "manual" ? "manual" : "stable"
            };
        }

        public MiniAppMasterActionResult UpdateScheduleMode(string key, long telegramId, MiniAppUpdateScheduleModeRequest request)
        {
            var master = GetMasterByKey(key);

            if (master == null || master.TelegramId != telegramId)
                return Failed("Нет доступа к этому расписанию");

            var mode = request.Mode == "manual" ? "manual" : "stable";

            db.ExecuteNonQuery(@"
                UPDATE ""Masters""
                SET ""ScheduleMode"" = @mode
                WHERE ""idMaster"" = @masterId
            ",
                new NpgsqlParameter("mode", mode),
                new NpgsqlParameter("masterId", master.Id));

            return Ok(mode == "manual" ? "Включен ручной график" : "Включен стабильный график");
        }

        public List<MiniAppManualSlotDto> GetManualSlots(string key, long telegramId, string dateText)
        {
            var master = GetMasterByKey(key);

            if (master == null || master.TelegramId != telegramId)
                return new List<MiniAppManualSlotDto>();

            if (!DateTime.TryParse(dateText, out var date))
                return new List<MiniAppManualSlotDto>();

            var table = db.ExecuteQuery(@"
                SELECT ""idSlot"", ""Date"", ""StartTime"", ""EndTime""
                FROM ""MasterManualSlots""
                WHERE ""MasterId"" = @masterId
                AND ""Date"" = @date
                AND ""IsActive"" = true
                ORDER BY ""StartTime""
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("date", date.Date));

            var slots = new List<MiniAppManualSlotDto>();

            foreach (DataRow row in table.Rows)
            {
                slots.Add(new MiniAppManualSlotDto
                {
                    Id = Convert.ToInt32(row["idSlot"]),
                    Date = ((DateOnly)row["Date"]).ToString("yyyy-MM-dd"),
                    StartTime = FormatTime(row["StartTime"]),
                    EndTime = FormatTime(row["EndTime"])
                });
            }

            return slots;
        }

        public MiniAppMasterActionResult CreateManualSlot(string key, long telegramId, MiniAppCreateManualSlotRequest request)
        {
            var master = GetMasterByKey(key);

            if (master == null || master.TelegramId != telegramId)
                return Failed("Нет доступа к этому расписанию");

            if (!DateTime.TryParse(request.Date, out var date))
                return Failed("Неверная дата");

            if (date.Date < DateTime.Now.Date)
                return Failed("Нельзя добавить слот в прошедший день");

            var startText = NormalizeTime(request.StartTime);
            var endText = NormalizeTime(request.EndTime);

            if (!TimeSpan.TryParse(startText, out var start) || !TimeSpan.TryParse(endText, out var end))
                return Failed("Время введено неверно");

            if (start >= end)
                return Failed("Время начала должно быть раньше конца");

            if (date.Date == DateTime.Now.Date && start <= DateTime.Now.TimeOfDay)
                return Failed("Нельзя добавить прошедшее время");

            var conflict = db.ExecuteQuery(@"
                SELECT 1
                FROM ""MasterManualSlots""
                WHERE ""MasterId"" = @masterId
                AND ""Date"" = @date
                AND ""IsActive"" = true
                AND ""StartTime"" < @endTime
                AND ""EndTime"" > @startTime
                LIMIT 1
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("date", date.Date),
                new NpgsqlParameter("startTime", start),
                new NpgsqlParameter("endTime", end));

            if (conflict.Rows.Count > 0)
                return Failed("Этот слот пересекается с другим слотом");

            var bookingConflict = db.ExecuteQuery(@"
                SELECT 1
                FROM ""Bookings"" b
                JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                WHERE b.""MasterId"" = @masterId
                AND b.""Date"" = @date
                AND b.""Status"" IN ('pending', 'confirmed', 'waiting_payment', 'waiting_payment_confirm')
                AND b.""Time"" < @endTime
                AND (b.""Time"" + (COALESCE(s.""Duration"", 60) * interval '1 minute')) > @startTime
                LIMIT 1
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("date", date.Date),
                new NpgsqlParameter("startTime", start),
                new NpgsqlParameter("endTime", end));

            if (bookingConflict.Rows.Count > 0)
                return Failed("В это время уже есть запись клиента");

            var blockConflict = db.ExecuteQuery(@"
                SELECT 1
                FROM ""MasterTimeBlocks""
                WHERE ""MasterId"" = @masterId
                AND ""Date"" = @date
                AND ""IsActive"" = true
                AND ""StartTime"" < @endTime
                AND ""EndTime"" > @startTime
                LIMIT 1
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("date", date.Date),
                new NpgsqlParameter("startTime", start),
                new NpgsqlParameter("endTime", end));

            if (blockConflict.Rows.Count > 0)
                return Failed("Это время уже закрыто ручной блокировкой");

            db.ExecuteNonQuery(@"
                INSERT INTO ""MasterManualSlots""
                    (""MasterId"", ""Date"", ""StartTime"", ""EndTime"")
                VALUES
                    (@masterId, @date, @startTime, @endTime)
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("date", date.Date),
                new NpgsqlParameter("startTime", start),
                new NpgsqlParameter("endTime", end));

            return Ok("Слот добавлен");
        }

        public MiniAppMasterActionResult DeleteManualSlot(string key, long telegramId, int slotId)
        {
            var master = GetMasterByKey(key);

            if (master == null || master.TelegramId != telegramId)
                return Failed("Нет доступа к этому расписанию");

            db.ExecuteNonQuery(@"
                UPDATE ""MasterManualSlots""
                SET ""IsActive"" = false
                WHERE ""idSlot"" = @slotId
                AND ""MasterId"" = @masterId
            ",
                new NpgsqlParameter("slotId", slotId),
                new NpgsqlParameter("masterId", master.Id));

            return Ok("Слот удалён");
        }

        public MiniAppMasterActionResult ClearManualSlotsDay(string key, long telegramId, string dateText)
        {
            var master = GetMasterByKey(key);

            if (master == null || master.TelegramId != telegramId)
                return Failed("Нет доступа к этому расписанию");

            if (!DateTime.TryParse(dateText, out var date))
                return Failed("Неверная дата");

            db.ExecuteNonQuery(@"
                UPDATE ""MasterManualSlots""
                SET ""IsActive"" = false
                WHERE ""MasterId"" = @masterId
                AND ""Date"" = @date
                AND ""IsActive"" = true
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("date", date.Date));

            return Ok("День очищен");
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
                ORDER BY b.""Date"" DESC, b.""Time"" DESC
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
                    PrepaymentAmount = (price * percent) / 100,
                    IsManualBlock = false
                });
            }

            var blocks = db.ExecuteQuery(@"
                SELECT
                    ""idBlock"",
                    ""Title"",
                    ""Date"",
                    ""StartTime"",
                    ""EndTime""
                FROM ""MasterTimeBlocks""
                WHERE ""MasterId"" = @masterId
                AND ""IsActive"" = true
                ORDER BY ""Date"" DESC, ""StartTime"" DESC
            ", new NpgsqlParameter("masterId", master.Id));

            foreach (DataRow row in blocks.Rows)
            {
                var start = TimeSpan.Parse(row["StartTime"].ToString());
                var end = TimeSpan.Parse(row["EndTime"].ToString());
                var title = row["Title"]?.ToString() ?? "Занято";

                bookings.Add(new MiniAppMasterBookingDto
                {
                    Id = -Convert.ToInt32(row["idBlock"]),
                    ClientTelegramId = 0,
                    ClientUsername = "Блокировка",
                    ServiceName = $"{title} ({start:hh\\:mm}-{end:hh\\:mm})",
                    DateTime = FormatBookingDateTime(row["Date"], row["StartTime"]),
                    Status = "blocked",
                    Price = 0,
                    PrepaymentPercent = 0,
                    PrepaymentAmount = 0,
                    IsManualBlock = true
                });
            }

            return bookings;
        }

        public MiniAppMasterActionResult CreateTimeBlock(string key, long telegramId, MiniAppCreateTimeBlockRequest request)
        {
            var master = GetMasterByKey(key);

            if (master == null || master.TelegramId != telegramId)
                return Failed("Мастер не найден");

            var title = request.Title?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title))
                return Failed("Введите название события");

            if (title.Length > 100)
                return Failed("Название слишком длинное");

            if (!DateTime.TryParse(request.Date, out var date))
                return Failed("Неверная дата");

            if (!TimeSpan.TryParse(request.StartTime, out var start))
                return Failed("Неверное время начала");

            if (!TimeSpan.TryParse(request.EndTime, out var end))
                return Failed("Неверное время окончания");

            if (end <= start)
                return Failed("Время окончания должно быть позже начала");

            var now = DateTime.Now;

            if (date.Date < now.Date || (date.Date == now.Date && start <= now.TimeOfDay))
                return Failed("Нельзя блокировать прошедшее время");

            var bookingConflict = db.ExecuteQuery(@"
                SELECT 1
                FROM ""Bookings"" b
                JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                WHERE b.""MasterId"" = @masterId
                AND b.""Date"" = @date
                AND b.""Status"" IN ('pending', 'confirmed', 'waiting_payment', 'waiting_payment_confirm')
                AND b.""Time"" < @endTime
                AND (b.""Time"" + (COALESCE(s.""Duration"", 60) * interval '1 minute')) > @startTime
                LIMIT 1
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("date", date.Date),
                new NpgsqlParameter("startTime", start),
                new NpgsqlParameter("endTime", end));

            if (bookingConflict.Rows.Count > 0)
                return Failed("В это время уже есть запись клиента");

            var blockConflict = db.ExecuteQuery(@"
                SELECT 1
                FROM ""MasterTimeBlocks""
                WHERE ""MasterId"" = @masterId
                AND ""Date"" = @date
                AND ""IsActive"" = true
                AND ""StartTime"" < @endTime
                AND ""EndTime"" > @startTime
                LIMIT 1
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("date", date.Date),
                new NpgsqlParameter("startTime", start),
                new NpgsqlParameter("endTime", end));

            if (blockConflict.Rows.Count > 0)
                return Failed("Это время уже заблокировано");

            db.ExecuteNonQuery(@"
                INSERT INTO ""MasterTimeBlocks""
                    (""MasterId"", ""Title"", ""Date"", ""StartTime"", ""EndTime"", ""IsActive"")
                VALUES
                    (@masterId, @title, @date, @startTime, @endTime, true)
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("title", title),
                new NpgsqlParameter("date", date.Date),
                new NpgsqlParameter("startTime", start),
                new NpgsqlParameter("endTime", end));

            return Ok("Время заблокировано");
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
            var masterProfile = GetMasterByKey(key);
            var masterName = masterProfile?.Name;
            var masterPhone = string.IsNullOrWhiteSpace(masterProfile?.PhoneNumber) ? "" : $"\n☎️ Телефон мастера: {masterProfile.PhoneNumber}";

            await BookingJobs.BotClient.SendMessage(
                clientId,
                $"✅ Запись подтверждена мастером\n\n" +
                $"👤 Мастер: {masterName ?? key}\n" +
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}" +
                $"{masterPhone}",
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
            var date = (DateOnly)booking["Date"];
            var time = (TimeOnly)booking["Time"];
            var serviceName = booking["ServiceName"]?.ToString() ?? "Услуга";
            var masterProfile = GetMasterByKey(key);
            var masterName = masterProfile?.Name;
            var masterPhone = string.IsNullOrWhiteSpace(masterProfile?.PhoneNumber) ? "" : $"\n☎️ Телефон мастера: {masterProfile.PhoneNumber}";

            await BookingJobs.BotClient.SendMessage(
                clientId,
                $"❌ Запись отменена мастером\n\n" +
                $"👤 Мастер: {masterName ?? key}\n" +
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}" +
                $"{masterPhone}\n\n" +
                $"Ждём вас в назначенное время.",
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
            var masterProfile = GetMasterByKey(key);
            var masterName = masterProfile?.Name;
            var masterPhone = string.IsNullOrWhiteSpace(masterProfile?.PhoneNumber) ? "" : $"\n☎️ Телефон мастера: {masterProfile.PhoneNumber}";

            await BookingJobs.BotClient.SendMessage(
                clientId,
                $"✅ Предоплата подтверждена мастером\n\n" +
                $"🎉 Запись успешно подтверждена\n\n" +
                $"👤 Мастер: {masterName ?? key}\n" +
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}" +
                $"{masterPhone}\n\n" +
                $"Ждём вас в назначенное время.",
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
                    b.""Date"",
                    b.""Time"",
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
            var date = (DateOnly)row["Date"];
            var time = (TimeOnly)row["Time"];
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
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}\n" +
                $"💸 Предоплата: {prepaymentAmount}₽\n\n" +
                $"Проверьте перевод и попробуйте снова.\n\n" +
                $"Реквизиты мастера:\n{paymentDetails}",
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
                    COALESCE(s.""IsVariablePrice"", false) AS ""IsVariablePrice"",
                    s.""MaxPrice"",
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
                    IsVariablePrice = Convert.ToBoolean(row["IsVariablePrice"]),
                    MaxPrice = row["MaxPrice"] == DBNull.Value ? null : Convert.ToInt32(row["MaxPrice"]),
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

            if (request.IsVariablePrice)
            {
                if (!request.MaxPrice.HasValue || request.MaxPrice.Value <= request.Price)
                    return Failed("Максимальная цена должна быть больше минимальной");
            }

            if (request.Duration <= 0)
                return Failed("Длительность должна быть больше 0");

            if (request.PrepaymentPercent < 0 || request.PrepaymentPercent > 100)
                return Failed("Предоплата должна быть от 0 до 100%");

            if (request.PrepaymentPercent > 0 && string.IsNullOrWhiteSpace(master.PaymentDetails))
                return Failed("Вы можете сделать предоплату после добавления реквизитов в профиле");

            db.ExecuteNonQuery(@"
                INSERT INTO ""Services""
                    (""MasterId"", ""Name"", ""Price"", ""MaxPrice"", ""IsVariablePrice"", ""Duration"", ""PrepaymentPercent"")
                VALUES
                    (@masterId, @name, @price, @maxPrice, @isVariablePrice, @duration, @prepaymentPercent)
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("price", request.Price),
                new NpgsqlParameter("maxPrice", request.IsVariablePrice ? request.MaxPrice!.Value : (object)DBNull.Value),
                new NpgsqlParameter("isVariablePrice", request.IsVariablePrice),
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

            if (request.IsVariablePrice)
            {
                if (!request.MaxPrice.HasValue || request.MaxPrice.Value <= request.Price)
                    return Failed("Максимальная цена должна быть больше минимальной");
            }

            if (request.Duration <= 0)
                return Failed("Длительность должна быть больше 0");

            if (request.PrepaymentPercent < 0 || request.PrepaymentPercent > 100)
                return Failed("Предоплата должна быть от 0 до 100%");

            if (request.PrepaymentPercent > 0 && string.IsNullOrWhiteSpace(master.PaymentDetails))
                return Failed("Вы можете сделать предоплату после добавления реквизитов в профиле");

            var updated = db.ExecuteNonQuery(@"
                UPDATE ""Services""
                SET
                    ""Name"" = @name,
                    ""Price"" = @price,
                    ""MaxPrice"" = @maxPrice,
                    ""IsVariablePrice"" = @isVariablePrice,
                    ""Duration"" = @duration,
                    ""PrepaymentPercent"" = @prepaymentPercent
                WHERE ""idService"" = @serviceId
                AND ""MasterId"" = @masterId
            ",
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("price", request.Price),
                new NpgsqlParameter("maxPrice", request.IsVariablePrice ? request.MaxPrice!.Value : (object)DBNull.Value),
                new NpgsqlParameter("isVariablePrice", request.IsVariablePrice),
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

        public async Task<MiniAppMasterActionResult> SendBroadcast(string key, long telegramId, MiniAppMasterBroadcastRequest request)
        {
            var master = GetMasterByKey(key);

            if (master == null)
                return Failed("Мастер не найден");

            if (master.TelegramId != telegramId)
                return Failed("Нет доступа к этому профилю");

            var title = request.Title?.Trim();
            var text = request.Text?.Trim();

            if (string.IsNullOrWhiteSpace(title))
                return Failed("Введите заголовок рассылки");

            if (string.IsNullOrWhiteSpace(text))
                return Failed("Введите текст рассылки");

            if (title.Length > 80)
                return Failed("Заголовок слишком длинный");

            if (text.Length > 900)
                return Failed("Текст слишком длинный");

            var clients = db.ExecuteQuery(@"
                SELECT DISTINCT u.""TelegrammId""
                FROM ""Bookings"" b
                JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                WHERE b.""MasterId"" = @masterId
            ", new NpgsqlParameter("masterId", master.Id));

            if (clients.Rows.Count == 0)
                return Failed("У мастера пока нет клиентов для рассылки");

            var miniAppBaseUrl = Environment.GetEnvironmentVariable("MINIAPP_URL") ?? "https://app-zapisi-pro.site";
            var profileUrl = $"{miniAppBaseUrl.TrimEnd('/')}/master/{master.Key}/public-profile";
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithWebApp("Открыть профиль мастера", new WebAppInfo(profileUrl))
                }
            });

            var sent = 0;
            var failed = 0;
            var message =
                $"📣 {title}\n\n" +
                $"{text}\n\n" +
                $"Мастер: @{master.Username}";

            foreach (DataRow row in clients.Rows)
            {
                try
                {
                    await BookingJobs.BotClient.SendMessage(
                        Convert.ToInt64(row["TelegrammId"]),
                        message,
                        replyMarkup: keyboard);

                    sent++;
                }
                catch
                {
                    failed++;
                }
            }

            return Ok(failed == 0
                ? $"Рассылка отправлена: {sent}"
                : $"Рассылка отправлена: {sent}, не доставлено: {failed}");
        }

        public async Task<MiniAppMasterActionResult> SendPersonalBroadcast(string key, long telegramId, long clientTelegramId, MiniAppMasterBroadcastRequest request)
        {
            var master = GetMasterByKey(key);

            if (master == null)
                return Failed("Мастер не найден");

            if (master.TelegramId != telegramId)
                return Failed("Нет доступа к этому профилю");

            var title = request.Title?.Trim();
            var text = request.Text?.Trim();

            if (string.IsNullOrWhiteSpace(title))
                return Failed("Введите заголовок сообщения");

            if (string.IsNullOrWhiteSpace(text))
                return Failed("Введите текст сообщения");

            if (title.Length > 80)
                return Failed("Заголовок слишком длинный");

            if (text.Length > 900)
                return Failed("Текст слишком длинный");

            var client = db.ExecuteQuery(@"
                SELECT DISTINCT u.""TelegrammId"", u.""UserName""
                FROM ""Bookings"" b
                JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                WHERE b.""MasterId"" = @masterId
                AND u.""TelegrammId"" = @clientTelegramId
                LIMIT 1
            ",
                new NpgsqlParameter("masterId", master.Id),
                new NpgsqlParameter("clientTelegramId", clientTelegramId));

            if (client.Rows.Count == 0)
                return Failed("Клиент не найден у этого мастера");

            var miniAppBaseUrl = Environment.GetEnvironmentVariable("MINIAPP_URL") ?? "https://app-zapisi-pro.site";
            var profileUrl = $"{miniAppBaseUrl.TrimEnd('/')}/master/{master.Key}/public-profile";
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithWebApp("Открыть профиль мастера", new WebAppInfo(profileUrl))
                }
            });

            var username = client.Rows[0]["UserName"]?.ToString() ?? "client";
            var message =
                $"📩 Личное сообщение от мастера\n\n" +
                $"📣 {title}\n\n" +
                $"{text}\n\n" +
                $"Мастер: @{master.Username}";

            try
            {
                await BookingJobs.BotClient.SendMessage(
                    clientTelegramId,
                    message,
                    replyMarkup: keyboard);
            }
            catch
            {
                return Failed("Не удалось доставить сообщение клиенту");
            }

            return Ok($"Сообщение отправлено @{username}");
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
                    u.""UserName"",
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
