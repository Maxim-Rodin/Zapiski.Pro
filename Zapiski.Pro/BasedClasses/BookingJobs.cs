using Hangfire;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Zapisi.Pro;

namespace Zapiski.Pro.BasedClasses
{
    public static class BookingJobs
    {
        public static ITelegramBotClient BotClient { get; set; }
        public static DbHelper Db { get; set; }

        // 🔥 TEST MODE
        public static bool TestMode = false;

        // ─────────────────────────────
        // HELP: delay helper
        // ─────────────────────────────
        private static TimeSpan GetDelay(DateTime target, int testMinutes = 1)
        {
            if (TestMode)
                return TimeSpan.FromMinutes(testMinutes);

            var delay = target - DateTime.Now;
            return delay.TotalSeconds > 0 ? delay : TimeSpan.Zero;
        }

        // ─────────────────────────────
        // AUTO COMPLETE
        // ─────────────────────────────
        public static async Task AutoCompleteBooking(int bookingId, long clientId, string? serviceName)
        {
            try
            {
                var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🏠 В меню",
                    "client:menu"
                )
            }
        });
                var status = Db.ExecuteScalar($@"
                    SELECT ""Status"" FROM ""Bookings""
                    WHERE ""idBooking"" = {bookingId}
                ")?.ToString();

                if (status != "confirmed")
                    return;

                Db.ExecuteNonQuery($@"
                    UPDATE ""Bookings""
                    SET ""Status"" = 'completed'
                    WHERE ""idBooking"" = {bookingId}
                ");

                await BotClient.SendMessage(
                    clientId,
                    $"✨ Спасибо за визит!\n\n" +
                    $"💼 Услуга: {serviceName}\n\n" +
                    $"Будем рады видеть вас снова 💙", replyMarkup:keyboard
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoComplete ERROR] {ex.Message}");
            }
        }

        // ─────────────────────────────
        // SCHEDULER
        // ─────────────────────────────
        public static void ScheduleAllReminders(
            int bookingId,
            long clientId,
            DateTime appointmentTime,
            string serviceName,
            int durationMinutes)
        {
            // ───── 24h ─────
            BackgroundJob.Schedule(
                () => SendPreDayReminder(bookingId),
                GetDelay(appointmentTime.AddDays(-1))
            );

            // ───── 2h ─────
            BackgroundJob.Schedule(
                () => SendSimpleReminder(clientId, appointmentTime, serviceName, "2 часа"),
                GetDelay(appointmentTime.AddHours(-2))
            );

            // ───── auto complete ─────
            BackgroundJob.Schedule(
                () => AutoCompleteBooking(bookingId, clientId, serviceName),
                GetDelay(appointmentTime.AddMinutes(durationMinutes))
            );
        }

        // ─────────────────────────────
        // SIMPLE REMINDER
        // ─────────────────────────────
        public static async Task SendSimpleReminder(
            long clientId,
            DateTime appointmentTime,
            string serviceName,
            string label)
        {
            try
            {
                var activeBooking = Db.ExecuteQuery($@"
                    SELECT 1
                    FROM ""Bookings"" b
                    JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                    JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                    WHERE u.""TelegrammId"" = {clientId}
                    AND b.""Date"" = '{appointmentTime:yyyy-MM-dd}'
                    AND b.""Time"" = '{appointmentTime:HH:mm:ss}'
                    AND s.""Name"" = @serviceName
                    AND b.""Status"" = 'confirmed'
                    LIMIT 1
                ", new Npgsql.NpgsqlParameter("serviceName", serviceName));

                if (activeBooking.Rows.Count == 0)
                    return;

                var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🏠 В меню",
                    "client:menu"
                )
            }
        });
                await BotClient.SendMessage(
                    clientId,
                    $"⏰ Напоминание!\n\n" +
                    $"Через {label} у вас запись\n\n" +
                    $"💼 Услуга: {serviceName}\n" +
                    $"📅 Дата: {appointmentTime:dd.MM.yyyy}\n" +
                    $"⏰ Время: {appointmentTime:HH:mm}", replyMarkup: keyboard
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Reminder ERROR] {ex.Message}");
            }
        }

        // ─────────────────────────────
        // 24H REMINDER WITH BUTTONS
        // ─────────────────────────────
        public static async Task SendPreDayReminder(int bookingId)
        {
            try
            {
                var booking = Db.ExecuteQuery($@"
                    SELECT 
                        b.""Status"",
                        b.""Date"",
                        b.""Time"",
                        s.""Name"" AS ""ServiceName"",
                        u.""TelegrammId"" AS ""ClientTelegramId""
                    FROM ""Bookings"" b
                    JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                    JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                    WHERE b.""idBooking"" = {bookingId}
                ");

                if (booking.Rows.Count == 0)
                    return;

                var row = booking.Rows[0];

                if (row["Status"].ToString() != "confirmed")
                    return;

                long clientId = Convert.ToInt64(row["ClientTelegramId"]);
                string serviceName = row["ServiceName"].ToString();

                DateOnly date = (DateOnly)row["Date"];
                TimeOnly time = (TimeOnly)row["Time"];

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Да, приду", $"client:preconfirm_yes:{bookingId}"),
                        InlineKeyboardButton.WithCallbackData("❌ Нет, отменить", $"client:preconfirm_no:{bookingId}")
                    }
                });

                await BotClient.SendMessage(
                    clientId,
                    $"📅 Напоминание\n\n" +
                    $"Завтра у вас запись:\n\n" +
                    $"💼 Услуга: {serviceName}\n" +
                    $"📅 Дата: {date:dd.MM.yyyy}\n" +
                    $"⏰ Время: {time:HH:mm}\n\n" +
                    $"Вы придёте?",
                    replyMarkup: keyboard
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PreDayReminder ERROR] {ex.Message}");
            }
        }

        public static void RestoreReminders()
        {
            try
            {
                var rows = Db.ExecuteQuery(@"
            SELECT
                b.""idBooking"",
                b.""Date"",
                b.""Time"",
                u.""TelegrammId"",
                s.""Name"" AS ""ServiceName"",
                s.""Duration""
            FROM ""Bookings"" b
            JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
            JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
            WHERE b.""Status"" = 'confirmed'
        ");

                foreach (System.Data.DataRow row in rows.Rows)
                {
                    int bookingId = Convert.ToInt32(row["idBooking"]);

                    long clientId = Convert.ToInt64(row["TelegrammId"]);

                    DateOnly date = (DateOnly)row["Date"];
                    TimeOnly time = (TimeOnly)row["Time"];

                    DateTime appointmentTime = date.ToDateTime(time);

                    string serviceName = row["ServiceName"].ToString();

                    int duration = Convert.ToInt32(row["Duration"]);

                    // только будущие записи
                    if (appointmentTime > DateTime.Now)
                    {
                        ScheduleAllReminders(
                            bookingId,
                            clientId,
                            appointmentTime,
                            serviceName,
                            duration
                        );

                        Console.WriteLine($"[RESTORE] booking {bookingId}");
                    }
                }

                Console.WriteLine("[RESTORE] DONE");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RESTORE ERROR] {ex.Message}");
            }
        }
    }
}
