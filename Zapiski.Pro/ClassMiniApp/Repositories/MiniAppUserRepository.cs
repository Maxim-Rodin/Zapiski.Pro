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
                $"💼 {service}\n" +
                $"📅 {date:dd.MM.yyyy} {time:hh\\:mm}",
                replyMarkup: ClientMenuKeyboard());

            await BookingJobs.BotClient.SendMessage(
                masterTelegramId,
                $"❌ Клиент отменил запись\n\n" +
                $"👤 @{username}\n" +
                $"💼 {service}\n" +
                $"📅 {date:dd.MM.yyyy} {time:hh\\:mm}",
                replyMarkup: MasterProfileKeyboard(masterKey));

            return true;
        }

        public List<MiniAppBookingSlotDto> GetBookingSlots(string masterKey, int serviceId, string dateText)
        {
            if (!DateTime.TryParse(dateText, out var date))
                return new List<MiniAppBookingSlotDto>();

            var masterId = db.GetMasterIdByKey(masterKey);
            var day = ToScheduleDay(date.DayOfWeek);

            var schedule = db.ExecuteQuery($@"
                SELECT *
                FROM ""MasterSchedule""
                WHERE ""MasterId"" = {masterId}
                AND ""DayOfWeek"" = {day}
            ");

            if (schedule.Rows.Count == 0 || !(bool)schedule.Rows[0]["IsActive"])
                return new List<MiniAppBookingSlotDto>();

            var serviceTable = db.ExecuteQuery($@"
                SELECT ""Duration""
                FROM ""Services""
                WHERE ""idService"" = {serviceId}
                AND ""MasterId"" = {masterId}
            ");

            if (serviceTable.Rows.Count == 0)
                return new List<MiniAppBookingSlotDto>();

            var duration = Convert.ToInt32(serviceTable.Rows[0]["Duration"]);
            var start = TimeSpan.Parse(schedule.Rows[0]["StartTime"].ToString());
            var end = TimeSpan.Parse(schedule.Rows[0]["EndTime"].ToString());

            var busy = db.ExecuteQuery($@"
                SELECT ""Time""
                FROM ""Bookings""
                WHERE ""MasterId"" = {masterId}
                AND ""Date"" = '{date:yyyy-MM-dd}'
                AND ""Status"" IN ('pending', 'confirmed', 'waiting_payment', 'waiting_payment_confirm')
            ");

            var busyTimes = new HashSet<TimeSpan>();

            foreach (DataRow row in busy.Rows)
                busyTimes.Add(TimeSpan.Parse(row["Time"].ToString()));

            var slots = new List<MiniAppBookingSlotDto>();

            for (var time = start; time + TimeSpan.FromMinutes(duration) <= end; time += TimeSpan.FromMinutes(duration))
            {
                slots.Add(new MiniAppBookingSlotDto
                {
                    Time = time.ToString(@"hh\:mm"),
                    IsBusy = busyTimes.Contains(time)
                });
            }

            return slots;
        }

        public async Task<MiniAppCreateBookingResult> CreateBooking(long telegramId, MiniAppCreateBookingRequest request)
        {
            if (!DateTime.TryParse(request.Date, out var date))
                return FailedBooking("Неверная дата");

            if (date.Date < DateTime.Today)
                return FailedBooking("Нельзя записаться на прошедшую дату");

            if (!TimeSpan.TryParse(request.Time, out var time))
                return FailedBooking("Неверное время");

            var masterId = db.GetMasterIdByKey(request.MasterKey);
            var day = ToScheduleDay(date.DayOfWeek);

            var schedule = db.ExecuteQuery($@"
                SELECT *
                FROM ""MasterSchedule""
                WHERE ""MasterId"" = {masterId}
                AND ""DayOfWeek"" = {day}
            ");

            if (schedule.Rows.Count == 0 || !(bool)schedule.Rows[0]["IsActive"])
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
            var duration = Convert.ToInt32(service["Duration"]);
            var start = TimeSpan.Parse(schedule.Rows[0]["StartTime"].ToString());
            var end = TimeSpan.Parse(schedule.Rows[0]["EndTime"].ToString());

            if (time < start || time + TimeSpan.FromMinutes(duration) > end)
                return FailedBooking("Это время недоступно");

            var check = db.ExecuteQuery($@"
                SELECT 1
                FROM ""Bookings""
                WHERE ""MasterId"" = {masterId}
                AND ""Date"" = '{date:yyyy-MM-dd}'
                AND ""Time"" = '{time:hh\\:mm\\:ss}'
                AND ""Status"" IN ('pending', 'confirmed', 'waiting_payment', 'waiting_payment_confirm')
                LIMIT 1
            ");

            if (check.Rows.Count > 0)
                return FailedBooking("Это время уже занято");

            var safeUsername = (request.Username ?? "unknown").Replace("'", "''");

            db.ExecuteNonQuery($@"
                INSERT INTO ""Users"" (""TelegrammId"", ""UserName"")
                VALUES ({telegramId}, '{safeUsername}')
                ON CONFLICT (""TelegrammId"") DO UPDATE
                SET ""UserName"" = EXCLUDED.""UserName""
            ");

            var userTable = db.ExecuteQuery($@"
                SELECT ""idUser"", ""UserName""
                FROM ""Users""
                WHERE ""TelegrammId"" = {telegramId}
                LIMIT 1
            ");

            if (userTable.Rows.Count == 0)
                return FailedBooking("Пользователь не найден");

            var userId = Convert.ToInt32(userTable.Rows[0]["idUser"]);
            var username = userTable.Rows[0]["UserName"]?.ToString() ?? "no_username";
            var serviceName = service["Name"]?.ToString() ?? "Услуга";
            var price = Convert.ToInt32(service["Price"]);
            var prepaymentPercent = Convert.ToInt32(service["PrepaymentPercent"]);
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

            var bookingTable = db.ExecuteQuery($@"
                INSERT INTO ""Bookings""
                    (""MasterId"", ""ServiceId"", ""UserId"", ""Date"", ""Time"", ""Status"")
                VALUES
                    ({masterId}, {request.ServiceId}, {userId}, '{date:yyyy-MM-dd}', '{time:hh\\:mm\\:ss}', '{status}')
                RETURNING ""idBooking""
            ");

            var bookingId = Convert.ToInt32(bookingTable.Rows[0]["idBooking"]);

            if (prepaymentPercent > 0)
            {
                await BookingJobs.BotClient.SendMessage(
                    telegramId,
                    $"💳 Для подтверждения записи требуется предоплата\n\n" +
                    $"💼 Услуга: {serviceName}\n" +
                    $"💰 Стоимость: {price}₽\n" +
                    $"💸 Предоплата: {prepaymentAmount}₽ ({prepaymentPercent}%)\n\n" +
                    $"📅 {date:dd.MM.yyyy}\n" +
                    $"⏰ {time:hh\\:mm}\n\n" +
                    $"Реквизиты мастера:\n\n{paymentDetails}\n\n" +
                    $"После оплаты нажмите кнопку в mini app или ниже 👇",
                    replyMarkup: PaymentKeyboard(bookingId));
            }
            else
            {
                await BookingJobs.BotClient.SendMessage(
                    telegramId,
                    "✅ Запись создана!\n⏳ Ожидает подтверждения мастера",
                    replyMarkup: ClientMenuKeyboard());

                await BookingJobs.BotClient.SendMessage(
                    masterTelegramId,
                    $"📥 Новая запись!\n\n" +
                    $"👤 @{username}\n" +
                    $"💼 {serviceName}\n" +
                    $"📅 {date:dd.MM.yyyy}\n" +
                    $"⏰ {time:hh\\:mm}\n\n" +
                    $"Подтвердить?",
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
            var serviceName = record["ServiceName"]?.ToString() ?? "Услуга";
            var price = Convert.ToInt32(record["Price"]);
            var percent = Convert.ToInt32(record["PrepaymentPercent"]);
            var prepaymentAmount = (price * percent) / 100;
            var date = (DateOnly)record["Date"];
            var time = (TimeOnly)record["Time"];

            await BookingJobs.BotClient.SendMessage(
                telegramId,
                "⏳ Ожидаем подтверждение оплаты от мастера",
                replyMarkup: ClientMenuKeyboard());

            await BookingJobs.BotClient.SendMessage(
                masterTelegramId,
                $"💸 Клиент отправил предоплату\n\n" +
                $"💼 {serviceName}\n" +
                $"💰 Сумма: {prepaymentAmount}₽\n" +
                $"📅 {date:dd.MM.yyyy}\n" +
                $"⏰ {time:HH:mm}\n\n" +
                $"Подтвердить получение?",
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
    }
}
