using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Zapisi.Pro;
using Zapiski.Pro.BasedClasses;
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
                SELECT ""idUser"", ""TelegrammId"", ""UserName"", COALESCE(""PhoneNumber"", '') AS ""PhoneNumber""
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
                    Username = userRow["UserName"]?.ToString(),
                    PhoneNumber = userRow["PhoneNumber"]?.ToString()
                },
                Roles = GetRoles(userId),
                Bookings = GetBookings(userId),
                Masters = GetMasters(userId)
            };
        }

        public async Task<bool> CancelBooking(long telegramId, int bookingId)
        {
            var row = db.ExecuteQuery($@"
                SELECT
                    b.""idBooking"",
                    b.""Date"",
                    b.""Time"",
                    b.""Status"",
                    u.""UserName"",
                    u.""TelegrammId"",
                    s.""Name"" AS ""ServiceName"",
                    m.""Key"" AS ""MasterKey"",
                    mu.""TelegrammId"" AS ""MasterTelegramId""
                FROM ""Bookings"" b
                JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                JOIN ""Masters"" m ON m.""idMaster"" = b.""MasterId""
                JOIN ""Users"" mu ON mu.""idUser"" = m.""UserId""
                WHERE b.""idBooking"" = {bookingId}
                AND u.""TelegrammId"" = {telegramId}
                LIMIT 1
            ");

            if (row.Rows.Count == 0)
                return false;

            var record = row.Rows[0];
            var status = record["Status"]?.ToString();

            if (status == "cancelled" || status == "completed")
                return false;

            db.ExecuteNonQuery($@"
                UPDATE ""Bookings""
                SET ""Status"" = 'cancelled'
                WHERE ""idBooking"" = {bookingId}
            ");

            var username = record["UserName"]?.ToString() ?? "без username";
            var service = record["ServiceName"]?.ToString() ?? "Услуга";
            var masterKey = record["MasterKey"]?.ToString() ?? "";
            var date = (DateOnly)record["Date"];
            var time = TimeSpan.Parse(record["Time"].ToString());
            var masterTelegramId = Convert.ToInt64(record["MasterTelegramId"]);

            await BookingJobs.BotClient.SendMessage(
                telegramId,
                $"❌ Вы отменили запись\n\n" +
                $"💼 Услуга: {service}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:hh\\:mm}",
                replyMarkup: ClientMenuKeyboard());

            await BookingJobs.BotClient.SendMessage(
                masterTelegramId,
                $"❌ Клиент отменил запись\n\n" +
                $"👤 Клиент: @{username}\n" +
                $"🆔 Telegram ID: {telegramId}\n" +
                $"💼 Услуга: {service}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:hh\\:mm}",
                replyMarkup: MasterProfileKeyboard(masterKey));

            return true;
        }

        public List<MiniAppBookingSlotDto> GetBookingSlots(string masterKey, int serviceId, string dateText)
        {
            if (!DateTime.TryParse(dateText, out var date))
                return new List<MiniAppBookingSlotDto>();

            var now = GetCurrentBusinessTime();

            if (date.Date < now.Date)
                return new List<MiniAppBookingSlotDto>();

            var masterId = db.GetMasterIdByKey(masterKey);
            var serviceTable = db.ExecuteQuery($@"
                SELECT ""Duration""
                FROM ""Services""
                WHERE ""idService"" = {serviceId}
                AND ""MasterId"" = {masterId}
            ");

            if (serviceTable.Rows.Count == 0)
                return new List<MiniAppBookingSlotDto>();

            var duration = serviceTable.Rows[0]["Duration"] == DBNull.Value
                ? 60
                : Convert.ToInt32(serviceTable.Rows[0]["Duration"]);
            var slotDuration = TimeSpan.FromMinutes(duration);
            var scheduleMode = db.ExecuteScalar($@"
                SELECT COALESCE(""ScheduleMode"", 'stable')
                FROM ""Masters""
                WHERE ""idMaster"" = {masterId}
            ")?.ToString() ?? "stable";

            var busyBookings = db.ExecuteQuery($@"
                SELECT b.""Time"", COALESCE(s.""Duration"", 60) AS ""Duration""
                FROM ""Bookings"" b
                JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                WHERE b.""MasterId"" = {masterId}
                AND b.""Date"" = '{date:yyyy-MM-dd}'
                AND b.""Status"" IN ('pending', 'confirmed', 'waiting_payment', 'waiting_payment_confirm')
            ");

            var busyBlocks = db.ExecuteQuery($@"
                SELECT ""StartTime"", ""EndTime""
                FROM ""MasterTimeBlocks""
                WHERE ""MasterId"" = {masterId}
                AND ""Date"" = '{date:yyyy-MM-dd}'
                AND ""IsActive"" = true
            ");

            var busyIntervals = new List<(TimeSpan Start, TimeSpan End)>();

            foreach (DataRow row in busyBookings.Rows)
            {
                var bookingStart = TimeSpan.Parse(row["Time"].ToString());
                var bookingEnd = bookingStart + TimeSpan.FromMinutes(Convert.ToInt32(row["Duration"]));
                busyIntervals.Add((bookingStart, bookingEnd));
            }

            foreach (DataRow row in busyBlocks.Rows)
            {
                busyIntervals.Add((
                    TimeSpan.Parse(row["StartTime"].ToString()),
                    TimeSpan.Parse(row["EndTime"].ToString())));
            }

            var slots = new List<MiniAppBookingSlotDto>();

            if (scheduleMode == "manual")
            {
                var manualSlots = db.ExecuteQuery($@"
                    SELECT ""StartTime"", ""EndTime""
                    FROM ""MasterManualSlots""
                    WHERE ""MasterId"" = {masterId}
                    AND ""Date"" = '{date:yyyy-MM-dd}'
                    AND ""IsActive"" = true
                    ORDER BY ""StartTime""
                ");

                foreach (DataRow row in manualSlots.Rows)
                {
                    var slotStart = TimeSpan.Parse(row["StartTime"].ToString());
                    var slotEnd = TimeSpan.Parse(row["EndTime"].ToString());

                    if (date.Date == now.Date && slotStart <= now.TimeOfDay)
                        continue;

                    if (slotStart + slotDuration > slotEnd)
                        continue;

                    var bookingEnd = slotStart + slotDuration;

                    slots.Add(new MiniAppBookingSlotDto
                    {
                        Time = slotStart.ToString(@"hh\:mm"),
                        IsBusy = busyIntervals.Any(interval => slotStart < interval.End && bookingEnd > interval.Start)
                    });
                }

                return slots;
            }

            var day = ToScheduleDay(date.DayOfWeek);

            var schedule = db.ExecuteQuery($@"
                SELECT *
                FROM ""MasterSchedule""
                WHERE ""MasterId"" = {masterId}
                AND ""DayOfWeek"" = {day}
            ");

            if (schedule.Rows.Count == 0 || !(bool)schedule.Rows[0]["IsActive"])
                return new List<MiniAppBookingSlotDto>();

            var start = TimeSpan.Parse(schedule.Rows[0]["StartTime"].ToString());
            var end = TimeSpan.Parse(schedule.Rows[0]["EndTime"].ToString());

            for (var time = start; time + slotDuration <= end; time += slotDuration)
            {
                if (date.Date == now.Date && time <= now.TimeOfDay)
                    continue;

                var slotEnd = time + slotDuration;

                slots.Add(new MiniAppBookingSlotDto
                {
                    Time = time.ToString(@"hh\:mm"),
                    IsBusy = busyIntervals.Any(interval => time < interval.End && slotEnd > interval.Start)
                });
            }

            return slots;
        }

        public async Task<MiniAppCreateBookingResult> CreateBooking(long telegramId, MiniAppCreateBookingRequest request)
        {
            if (!DateTime.TryParse(request.Date, out var date))
                return FailedBooking("Неверная дата");

            var now = GetCurrentBusinessTime();

            if (date.Date < now.Date)
                return FailedBooking("Нельзя записаться на прошедшую дату");

            if (!TimeSpan.TryParse(request.Time, out var time))
                return FailedBooking("Неверное время");

            if (date.Date == now.Date && time <= now.TimeOfDay)
                return FailedBooking("Это время уже прошло");

            var safeMasterKey = (request.MasterKey ?? string.Empty).Replace("'", "''");
            var masterTable = db.ExecuteQuery($@"
                SELECT ""idMaster"", COALESCE(""ScheduleMode"", 'stable') AS ""ScheduleMode""
                FROM ""Masters""
                WHERE ""Key"" = '{safeMasterKey}'
                LIMIT 1
            ");

            if (masterTable.Rows.Count == 0)
                return FailedBooking("Мастер не найден");

            var masterId = Convert.ToInt32(masterTable.Rows[0]["idMaster"]);
            var scheduleMode = masterTable.Rows[0]["ScheduleMode"]?.ToString() ?? "stable";
            var day = ToScheduleDay(date.DayOfWeek);

            var schedule = db.ExecuteQuery($@"
                SELECT *
                FROM ""MasterSchedule""
                WHERE ""MasterId"" = {masterId}
                AND ""DayOfWeek"" = {day}
            ");

            if (scheduleMode != "manual" && (schedule.Rows.Count == 0 || !(bool)schedule.Rows[0]["IsActive"]))
                return FailedBooking("У мастера выходной");

            var serviceTable = db.ExecuteQuery($@"
                SELECT *
                FROM ""Services""
                WHERE ""idService"" = {request.ServiceId}
                AND ""MasterId"" = {masterId}
            ");

            if (serviceTable.Rows.Count == 0)
                return FailedBooking("Услуга не найдена");

            var service = serviceTable.Rows[0];
            var duration = service["Duration"] == DBNull.Value
                ? 60
                : Convert.ToInt32(service["Duration"]);
            var start = schedule.Rows.Count == 0 ? TimeSpan.Zero : TimeSpan.Parse(schedule.Rows[0]["StartTime"].ToString());
            var end = schedule.Rows.Count == 0 ? TimeSpan.Zero : TimeSpan.Parse(schedule.Rows[0]["EndTime"].ToString());

            if (scheduleMode != "manual" && (time < start || time + TimeSpan.FromMinutes(duration) > end))
                return FailedBooking("Это время недоступно");

            var timeSql = time.ToString(@"hh\:mm\:ss");
            var timeText = time.ToString(@"hh\:mm");
            var bookingEndSql = (time + TimeSpan.FromMinutes(duration)).ToString(@"hh\:mm\:ss");

            if (scheduleMode == "manual")
            {
                var manualSlot = db.ExecuteQuery($@"
                    SELECT 1
                    FROM ""MasterManualSlots""
                    WHERE ""MasterId"" = {masterId}
                    AND ""Date"" = '{date:yyyy-MM-dd}'
                    AND ""IsActive"" = true
                    AND ""StartTime"" = '{timeSql}'::time
                    AND ""EndTime"" >= '{bookingEndSql}'::time
                    LIMIT 1
                ");

                if (manualSlot.Rows.Count == 0)
                    return FailedBooking("Это время недоступно");
            }

            var check = db.ExecuteQuery($@"
                SELECT 1
                FROM ""Bookings"" b
                JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                WHERE b.""MasterId"" = {masterId}
                AND b.""Date"" = '{date:yyyy-MM-dd}'
                AND b.""Status"" IN ('pending', 'confirmed', 'waiting_payment', 'waiting_payment_confirm')
                AND b.""Time"" < '{bookingEndSql}'::time
                AND (b.""Time"" + (COALESCE(s.""Duration"", 60) * interval '1 minute')) > '{timeSql}'::time
                LIMIT 1
            ");

            if (check.Rows.Count > 0)
                return FailedBooking("Это время уже занято");

            var blockCheck = db.ExecuteQuery($@"
                SELECT 1
                FROM ""MasterTimeBlocks""
                WHERE ""MasterId"" = {masterId}
                AND ""Date"" = '{date:yyyy-MM-dd}'
                AND ""IsActive"" = true
                AND ""StartTime"" < '{bookingEndSql}'::time
                AND ""EndTime"" > '{timeSql}'::time
                LIMIT 1
            ");

            if (blockCheck.Rows.Count > 0)
                return FailedBooking("Это время заблокировано мастером");

            var safeUsername = (request.Username ?? "unknown").Replace("'", "''");
            var safePhoneNumber = (request.PhoneNumber ?? string.Empty).Trim().Replace("'", "''");

            var existingUser = db.ExecuteQuery($@"
                SELECT ""idUser""
                FROM ""Users""
                WHERE ""TelegrammId"" = {telegramId}
                LIMIT 1
            ");

            if (existingUser.Rows.Count == 0)
            {
                db.ExecuteNonQuery($@"
                    INSERT INTO ""Users"" (""TelegrammId"", ""UserName"", ""PhoneNumber"")
                    VALUES ({telegramId}, '{safeUsername}', NULLIF('{safePhoneNumber}', ''))
                ");
            }
            else
            {
                db.ExecuteNonQuery($@"
                    UPDATE ""Users""
                    SET
                        ""UserName"" = '{safeUsername}',
                        ""PhoneNumber"" = NULLIF('{safePhoneNumber}', '')
                    WHERE ""TelegrammId"" = {telegramId}
                ");
            }

            var userTable = db.ExecuteQuery($@"
                SELECT ""idUser"", ""UserName"", COALESCE(""PhoneNumber"", '') AS ""PhoneNumber""
                FROM ""Users""
                WHERE ""TelegrammId"" = {telegramId}
                LIMIT 1
            ");

            if (userTable.Rows.Count == 0)
                return FailedBooking("Пользователь не найден");

            var userId = Convert.ToInt32(userTable.Rows[0]["idUser"]);
            var username = userTable.Rows[0]["UserName"]?.ToString() ?? "no_username";
            var clientPhone = userTable.Rows[0]["PhoneNumber"]?.ToString() ?? "";
            var serviceName = service["Name"]?.ToString() ?? "Услуга";
            var price = service["Price"] == DBNull.Value
                ? 0
                : Convert.ToInt32(service["Price"]);
            var prepaymentPercent = service["PrepaymentPercent"] == DBNull.Value
                ? 0
                : Convert.ToInt32(service["PrepaymentPercent"]);
            var prepaymentAmount = (price * prepaymentPercent) / 100;
            var status = prepaymentPercent > 0 ? "waiting_payment" : "pending";

            var masterData = db.ExecuteQuery($@"
                SELECT
                    u.""TelegrammId"",
                    m.""PaymentDetails""
                FROM ""Users"" u
                JOIN ""Masters"" m ON m.""UserId"" = u.""idUser""
                WHERE m.""idMaster"" = {masterId}
                LIMIT 1
            ").Rows[0];

            var masterTelegramId = Convert.ToInt64(masterData["TelegrammId"]);
            var paymentDetails = masterData["PaymentDetails"] != DBNull.Value
                ? masterData["PaymentDetails"]?.ToString()
                : "Реквизиты не указаны";

            var clientPhoneLine = string.IsNullOrWhiteSpace(clientPhone) ? "" : $"☎️ Телефон: {clientPhone}\n";

            if (prepaymentPercent > 0 && string.IsNullOrWhiteSpace(paymentDetails))
                return FailedBooking("Мастер еще не указал реквизиты для предоплаты");

            var bookingTable = db.ExecuteQuery($@"
                INSERT INTO ""Bookings""
                    (""MasterId"", ""ServiceId"", ""UserId"", ""Date"", ""Time"", ""Status"")
                VALUES
                    ({masterId}, {request.ServiceId}, {userId}, '{date:yyyy-MM-dd}', '{timeSql}', '{status}')
                RETURNING ""idBooking""
            ");

            var bookingId = Convert.ToInt32(bookingTable.Rows[0]["idBooking"]);

            if (prepaymentPercent > 0)
            {
                await BookingJobs.BotClient.SendMessage(
                    telegramId,
                    $"💳 Запись создана, нужна предоплата\n\n" +
                    $"💼 Услуга: {serviceName}\n" +
                    $"💰 Стоимость: {price}₽\n" +
                    $"💸 Предоплата: {prepaymentAmount}₽ ({prepaymentPercent}%)\n" +
                    $"📅 Дата: {date:dd.MM.yyyy}\n" +
                    $"⏰ Время: {timeText}\n\n" +
                    $"Реквизиты мастера:\n{paymentDetails}\n\n" +
                    $"После оплаты нажмите кнопку в mini app или ниже.",
                    replyMarkup: PaymentKeyboard(bookingId));
            }
            else
            {
                await BookingJobs.BotClient.SendMessage(
                    telegramId,
                    $"✅ Запись создана\n\n" +
                    $"💼 Услуга: {serviceName}\n" +
                    $"📅 Дата: {date:dd.MM.yyyy}\n" +
                    $"⏰ Время: {timeText}\n\n" +
                    $"⏳ Ожидайте подтверждения от мастера.",
                    replyMarkup: ClientMenuKeyboard());

                await BookingJobs.BotClient.SendMessage(
                    masterTelegramId,
                    $"📥 Новая запись от клиента\n\n" +
                    $"👤 Клиент: @{username}\n" +
                    $"🆔 Telegram ID: {telegramId}\n" +
                    $"{clientPhoneLine}" +
                    $"💼 Услуга: {serviceName}\n" +
                    $"📅 Дата: {date:dd.MM.yyyy}\n" +
                    $"⏰ Время: {timeText}\n\n" +
                    $"Подтвердить запись?",
                    replyMarkup: MasterBookingKeyboard(request.MasterKey, bookingId));
            }

            return new MiniAppCreateBookingResult
            {
                Success = true,
                Message = prepaymentPercent > 0 ? "Нужна предоплата" : "Запись создана",
                BookingId = bookingId,
                Status = status,
                ServiceName = serviceName,
                Price = price,
                PrepaymentPercent = prepaymentPercent,
                PrepaymentAmount = prepaymentAmount,
                PaymentDetails = paymentDetails
            };
        }

        public async Task<bool> MarkBookingPaid(long telegramId, int bookingId)
        {
            var row = db.ExecuteQuery($@"
                SELECT
                    b.""Date"",
                    b.""Time"",
                    b.""Status"",
                    u.""UserName"",
                    COALESCE(u.""PhoneNumber"", '') AS ""PhoneNumber"",
                    s.""Name"" AS ""ServiceName"",
                    s.""Price"",
                    s.""PrepaymentPercent"",
                    m.""Key"" AS ""MasterKey"",
                    mu.""TelegrammId"" AS ""MasterTelegramId""
                FROM ""Bookings"" b
                JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                JOIN ""Masters"" m ON m.""idMaster"" = b.""MasterId""
                JOIN ""Users"" mu ON mu.""idUser"" = m.""UserId""
                WHERE b.""idBooking"" = {bookingId}
                AND u.""TelegrammId"" = {telegramId}
                LIMIT 1
            ");

            if (row.Rows.Count == 0)
                return false;

            var record = row.Rows[0];

            if (record["Status"]?.ToString() != "waiting_payment")
                return false;

            db.ExecuteNonQuery($@"
                UPDATE ""Bookings""
                SET ""Status"" = 'waiting_payment_confirm'
                WHERE ""idBooking"" = {bookingId}
            ");

            var masterTelegramId = Convert.ToInt64(record["MasterTelegramId"]);
            var masterKey = record["MasterKey"]?.ToString() ?? "";
            var username = record["UserName"]?.ToString() ?? "unknown";
            var clientPhone = record["PhoneNumber"]?.ToString() ?? "";
            var serviceName = record["ServiceName"]?.ToString() ?? "Услуга";
            var price = Convert.ToInt32(record["Price"]);
            var percent = Convert.ToInt32(record["PrepaymentPercent"]);
            var prepaymentAmount = (price * percent) / 100;
            var date = (DateOnly)record["Date"];
            var time = (TimeOnly)record["Time"];

            await BookingJobs.BotClient.SendMessage(
                telegramId,
                $"⏳ Оплата отправлена мастеру на проверку\n\n" +
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}\n\n" +
                $"Мастер подтвердит получение оплаты.",
                replyMarkup: ClientMenuKeyboard());

            await BookingJobs.BotClient.SendMessage(
                masterTelegramId,
                $"💸 Клиент отметил предоплату\n\n" +
                $"👤 Клиент: @{username}\n" +
                $"🆔 Telegram ID: {telegramId}\n" +
                $"{(string.IsNullOrWhiteSpace(clientPhone) ? "" : $"☎️ Телефон: {clientPhone}\n")}" +
                $"💼 Услуга: {serviceName}\n" +
                $"💰 Сумма: {prepaymentAmount}₽\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}\n\n" +
                $"Проверьте поступление и подтвердите получение.",
                replyMarkup: MasterPaymentKeyboard(masterKey, bookingId));

            return true;
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

        private static InlineKeyboardMarkup MasterBookingKeyboard(string masterKey, int bookingId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Принять", $"master:booking_accept:{masterKey}:{bookingId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", $"master:booking_cancel:{masterKey}:{bookingId}")
                }
            });
        }

        private static InlineKeyboardMarkup MasterPaymentKeyboard(string masterKey, int bookingId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Деньги пришли", $"master:payment_accept:{masterKey}:{bookingId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Не пришли", $"master:payment_reject:{masterKey}:{bookingId}")
                }
            });
        }

        private static InlineKeyboardMarkup MasterProfileKeyboard(string masterKey)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("👤 Открыть профиль", $"master:master_profile:{masterKey}")
                }
            });
        }

        private static MiniAppCreateBookingResult FailedBooking(string message)
        {
            return new MiniAppCreateBookingResult
            {
                Success = false,
                Message = message
            };
        }

        private static int ToScheduleDay(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                DayOfWeek.Saturday => 6,
                DayOfWeek.Sunday => 7,
                _ => 0
            };
        }

        private static DateTime GetCurrentBusinessTime()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"));
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));
                }
                catch
                {
                    return DateTime.Now;
                }
            }
        }
    }
}
