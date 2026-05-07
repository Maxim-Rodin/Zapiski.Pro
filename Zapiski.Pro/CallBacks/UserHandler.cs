using Npgsql.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zapisi.Pro.State;

namespace Zapisi.Pro.CallBacks
{
    internal class UserHandler : ICallbackHandler
    {
        private readonly ITelegramBotClient botClient;

        private readonly DbHelper db ;
        private readonly StateService stateService = new StateService();

        public string Entity => "client";

        public UserHandler(ITelegramBotClient botClient)
        {
            var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
            DotNetEnv.Env.Load(envPath);
            var host = Environment.GetEnvironmentVariable("DB_HOST");
            var user = Environment.GetEnvironmentVariable("DB_USER");
            var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");
            db = new DbHelper($"Host={host};Port=5432;Username={user};Password={pass};Database=Zapisi.Pro");
            this.botClient = botClient;
        }
        public async Task Handle(CallBackData data,CallbackQuery query)
        {
            var chatId = query.Message.Chat.Id;
            stateService.ClearState(chatId);

            switch (data.Action)
            {
                case "menu":
                    await ShowMenu(chatId);
                        break;
                case "my_records":
                    await ShowMyRecords(query);
                    break;
                case "profile_services":
                    await StartBooking(query, data);
                    break;
                case "book_time":
                    await BookTime(query, data); break;
                case "cancel":
                    await CancelBookingByClient(query, data); break;
            }
            return;
        }
        public async Task ShowMenu(long chatId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
     {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("📅 Записаться", "client:book")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("📖 Мои записи", "client:my_records")
        }
    });

            await botClient.SendMessage(chatId, "👤 Главное меню", replyMarkup: keyboard);
        }

        public async Task ShowMyRecords(CallbackQuery query)
        {
            var chatId = query.Message.Chat.Id;
            var telegramId = query.From.Id;

            var rows = db.ExecuteQuery($@"
        SELECT r.""idBooking"", r.""Date"", r.""Time"", r.""Status"",
               s.""Name"" AS ServiceName,
               m.""Key"" AS MasterKey
        FROM ""Bookings"" r
        JOIN ""Services"" s ON s.""idService"" = r.""ServiceId""
        JOIN ""Masters"" m ON m.""idMaster"" = r.""MasterId""
        JOIN ""Users"" u ON u.""idUser"" = r.""UserId""
        WHERE u.""TelegrammId"" = {telegramId}
        AND r.""Status"" != 'cancelled'
        ORDER BY r.""Date"" DESC, r.""Time"" DESC
    ");

            if (rows.Rows.Count == 0)
            {
                await botClient.SendMessage(chatId, "📭 У вас нет записей");
                return;
            }

            // удалим старое сообщение с кнопки
            await botClient.DeleteMessage(chatId, query.Message.MessageId);

            foreach (DataRow r in rows.Rows)
            {
                int bookingId = (int)r["idBooking"];
                string status = r["Status"].ToString();

                var date = (DateOnly)r["Date"];
                var time = TimeSpan.Parse(r["Time"].ToString());

                string icon =
                    status == "confirmed" ? "✅" :
                    status == "pending" ? "⏳" :
                    "❌";

                string text =
                    $"{icon} {r["ServiceName"]}\n" +
                    $"📅 {date:dd.MM.yyyy} {time}\n" +
                    $"🔑 {r["MasterKey"]}";

                var buttons = new List<InlineKeyboardButton[]>();

               
                    buttons.Add(new[]
                    {
                InlineKeyboardButton.WithCallbackData(
                    "❌ Отменить",
                    $"client:cancel:{bookingId}"
                )
            });
                

                buttons.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", "client:menu")
        });

                var keyboard = new InlineKeyboardMarkup(buttons);

                await botClient.SendMessage(chatId, text, replyMarkup: keyboard);
            }
        }
        public async Task CancelBookingByClient(CallbackQuery query, CallBackData data)
        {
            int bookingId = int.Parse(data.Id);

            // ─────────────────────────────
            // 1. получаем всю инфу
            // ─────────────────────────────
            var row = db.ExecuteQuery($@"
        SELECT b.*, 
               u.""UserName"", u.""TelegrammId"",
               s.""Name"" AS ServiceName,
               m.""Key"" AS MasterKey,
               mu.""TelegrammId"" AS MasterTelegramId
        FROM ""Bookings"" b
        JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
        JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
        JOIN ""Masters"" m ON m.""idMaster"" = b.""MasterId""s
        JOIN ""Users"" mu ON mu.""idUser"" = m.""UserId""
        WHERE b.""idBooking"" = {bookingId}
    ");

            if (row.Rows.Count == 0)
            {
                await botClient.AnswerCallbackQuery(query.Id, "❌ Запись не найдена");
                return;
            }

            var record = row.Rows[0];

            string username = record["Username"]?.ToString() ?? "без username";
            string service = record["ServiceName"].ToString();
            string masterKey = record["MasterKey"].ToString();

            var date = (DateOnly)record["Date"];
            var time = TimeSpan.Parse(record["Time"].ToString());

            long masterTelegramId = Convert.ToInt64(record["MasterTelegramId"]);

            // ─────────────────────────────
            // 2. отменяем
            // ─────────────────────────────
            db.ExecuteNonQuery($@"
        UPDATE ""Bookings""
        SET ""Status"" = 'cancelled'
        WHERE ""idBooking"" = {bookingId}
    ");

            // ─────────────────────────────
            // 3. клиенту
            // ─────────────────────────────
            var clientKeyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🏠 В меню", "client:menu")
        }
    });

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                $"❌ Вы отменили запись\n\n" +
                $"💼 {service}\n" +
                $"📅 {date:dd.MM.yyyy} {time}",
                replyMarkup: clientKeyboard
            );

            await botClient.AnswerCallbackQuery(query.Id);

            // ─────────────────────────────
            // 4. мастеру уведомление
            // ─────────────────────────────
            var masterKeyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "👤 Открыть профиль",
                $"master:master_profile:{masterKey}"
            )
        }
    });

            await botClient.SendMessage(
                masterTelegramId,
                $"❌ Клиент отменил запись\n\n" +
                $"👤 @{username}\n" +
                $"💼 {service}\n" +
                $"📅 {date:dd.MM.yyyy} {time}",
                replyMarkup: masterKeyboard
            );
        }
        public async Task StartBooking(CallbackQuery query, CallBackData data)
        {
            var chatId = query.Message.Chat.Id;
            var key = data.Id;

            stateService.SetState(chatId, $"booking_date:{key}:{data.SubAction}");

            var masterId = db.GetMasterIdByKey(key);

            var rows = db.ExecuteQuery($@"
        SELECT *
        FROM ""MasterSchedule""
        WHERE ""MasterId"" = {masterId}
        ORDER BY ""DayOfWeek""
    ");

            string[] days = { "", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };

            var text = "📅 График мастера\n\n";

            foreach (DataRow r in rows.Rows)
            {
                bool active = (bool)r["IsActive"];

                if (!active)
                    continue;

                if (r["StartTime"] == DBNull.Value ||
                    r["EndTime"] == DBNull.Value)
                    continue;

                int day = (int)r["DayOfWeek"];

                var start = ((TimeOnly)r["StartTime"]).ToString(@"HH\:mm");
                var end = ((TimeOnly)r["EndTime"]).ToString(@"HH\:mm");

                text += $"{days[day]} — {start}-{end}\n";
            }

            text += "\n📅 Введите дату для записи в формате ДД.MM.ГГГГ";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "⬅️ Назад",
                $"master:master_profile:{key}"
            )
        }
    });

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                text,
                replyMarkup: keyboard
            );
        }
        public async Task GetSheduleByDate(Message message, string state)
        {
            var chatId = message.Chat.Id;
            var parts = state.Split(':');
            var key = parts[1];
            var serviceId = int.Parse(parts[2]);
            var btn = new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_profile:{key}") });
            if (!DateTime.TryParseExact(
                                    message.Text.Trim(),
                                    "dd.MM.yyyy",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out DateTime date))
            {
                await botClient.SendMessage(message.Chat.Id, "❌ Формат: 25.04.2026");
                return;
            }
            int day;

            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    day = 1;
                    break;
                case DayOfWeek.Tuesday:
                    day = 2;
                    break;
                case DayOfWeek.Wednesday:
                    day = 3;
                    break;
                case DayOfWeek.Thursday:
                    day = 4;
                    break;
                case DayOfWeek.Friday:
                    day = 5;
                    break;
                case DayOfWeek.Saturday:
                    day = 6;
                    break;
                case DayOfWeek.Sunday:
                    day = 7;
                    break;
                default:
                    day = 0;
                    break;
            }
            var masterId = db.GetMasterIdByKey(key);
            var schedule = db.ExecuteQuery($@"
                            SELECT *
                            FROM ""MasterSchedule""
                            WHERE ""MasterId"" = {masterId}
                            AND ""DayOfWeek"" = {day}
                        ");
            Console.WriteLine($"хначение полученного ссообщения- {message.Text} , айди услуги -{serviceId} значение day - {day}  ");
            Console.WriteLine("ROWS COUNT = " + schedule.Rows.Count);
            /// Проверяем, есть ли запись в расписании и активен ли мастер в этот день///
            if (schedule.Rows.Count == 0 || !(bool)schedule.Rows[0]["IsActive"])
            {
                
                await botClient.SendMessage(message.Chat.Id, "❌ У мастера выходной", replyMarkup: btn);
                return;
            }
            
            var serviceTable = db.ExecuteQuery($@"
                                SELECT * FROM ""Services""
                                WHERE ""idService"" = {serviceId}
                            ");

            if (serviceTable.Rows.Count == 0)
            {
                await botClient.SendMessage(chatId, "❌ Услуга не найдена", replyMarkup: btn);
                return;
            }

            var service = serviceTable.Rows[0];

            int duration = (int)service["Duration"];
            /// Генерируем слоты под эту услгу///
             
            var start = TimeSpan.Parse(schedule.Rows[0]["StartTime"].ToString());
            var end = TimeSpan.Parse(schedule.Rows[0]["EndTime"].ToString());

            var slots = new List<TimeSpan>();

            for (var t = start; t + TimeSpan.FromMinutes(duration) <= end; t += TimeSpan.FromMinutes(duration))
            {
                slots.Add(t);
            }

            /// Получаем уже занятые слоты на эту дату ///
            var busy = db.ExecuteQuery($@"
                                        SELECT ""Time""
                                        FROM ""Bookings""
                                        WHERE ""MasterId"" = {masterId}
                                        AND ""Date"" = '{date:yyyy-MM-dd}'
                                    ");

            var busyTimes = new HashSet<TimeSpan>();

            foreach (DataRow r in busy.Rows)
            {
                busyTimes.Add(TimeSpan.Parse(r["Time"].ToString()));
            }

            /// Формируем клавиатуру со слотами ///

            var buttons = new List<InlineKeyboardButton[]>();

            for (var t = start; t + TimeSpan.FromMinutes(duration) <= end; t += TimeSpan.FromMinutes(duration))
            {
                bool isBusy = busyTimes.Contains(t);

                buttons.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData(
                isBusy ? $"❌ {t:hh\\:mm}" : $"✅ {t:hh\\:mm}",
                isBusy ? "busy" : $"client:book_time:{key}:{serviceId}:{date:yyyy-MM-dd}:{t:hh\\-mm\\-ss}"
                                )
                            });
            }
            buttons.Add(new[]
                   {
                        InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_profile:{key}")
                    });
            await botClient.SendMessage(
                            chatId,
                            $"📅 {date:dd.MM.yyyy}\n⏰ Выберите время:",
                            replyMarkup: new InlineKeyboardMarkup(buttons)
                        );
        }

        public async Task BookTime(CallbackQuery query, CallBackData data)
        {
            var clientKeyboard = new InlineKeyboardMarkup(new[]
          {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🏠 В меню", "client:menu")
        }
    });

            var chatId = query.Message.Chat.Id;
            

            var key = data.Id;
            
            var serviceId = data.SubAction;
           
            var date = DateTime.Parse(data.Extra);
            var timeParts = data.Ultra.Split('-');
          

            var times = new TimeSpan(
                int.Parse(timeParts[0]),
                int.Parse(timeParts[1]),
                timeParts.Length > 2 ? int.Parse(timeParts[2]) : 0
            );
            
            Console.WriteLine($"ключ - {key} айди услуги{serviceId} дата -{date} время {times.ToString()}");
            var masterId = db.GetMasterIdByKey(key);
           
            // ─────────────────────────────
            // 1. проверка занятости
            // ─────────────────────────────
            var time = times.ToString(@"hh\:mm\:ss");
           

            var check = db.ExecuteQuery($@"
        SELECT * FROM ""Bookings""
        WHERE ""MasterId"" = {masterId}
        AND ""Date"" = '{date:yyyy-MM-dd}'
        AND ""Time"" = '{time}'
        AND ""Status"" IN ('pending','confirmed')
    ");
           

            if (check.Rows.Count > 0)
            {
                await botClient.AnswerCallbackQuery(query.Id, "❌ Уже занято выберите другую дату");
                return;
            }
            Console.WriteLine("step10");
            Console.WriteLine($"полученный id мастера - {masterId} , дата - {date} , время - {time}");
            // ─────────────────────────────
            // 2. клиент
            // ─────────────────────────────
            var userService = new UserService();
            

            if (!userService.ExistsByTelegramId(query.From.Id))
            {
                userService.CreateUser(query.From.Id, query.From.Username ?? "unknown");
            }
            Console.WriteLine("step12");
            var user = db.ExecuteQuery($@"
        SELECT u.*
        FROM ""Users"" u
        WHERE u.""TelegrammId"" = {query.From.Id}
    ").Rows[0];
           
            var userTable = db.ExecuteQuery($@"
                SELECT ""idUser""
                FROM ""Users""
                WHERE ""TelegrammId"" = {query.From.Id}
            ");
            

            if (userTable.Rows.Count == 0)
            {
                await botClient.SendMessage(chatId, "❌ Пользователь не найден");
                return;
            }
          

            var userId = (int)userTable.Rows[0]["idUser"];
            string username = query.From.Username ?? "no_username";
            Console.WriteLine("step16");
            // ─────────────────────────────
            // 3. создаём запись
            // ─────────────────────────────
            var bookingTable = db.ExecuteQuery($@"
        INSERT INTO ""Bookings""
        (""MasterId"", ""ServiceId"", ""UserId"",  ""Date"", ""Time"", ""Status"")
        VALUES
        ({masterId}, {serviceId}, {userId},'{date:yyyy-MM-dd}', '{time}', 'pending')
         RETURNING ""idBooking"";
    ");
           

            // ─────────────────────────────
            // 4. клиенту
            // ─────────────────────────────
            await botClient.SendMessage(chatId,
                "✅ Запись создана!\n⏳ Ожидает подтверждения мастера" ,replyMarkup:clientKeyboard);

            // ─────────────────────────────
            // 5. мастеру уведомление
            // ─────────────────────────────
            var service = db.ExecuteQuery($@"
                        SELECT * FROM ""Services""
                        WHERE ""idService"" = {serviceId}
                    ").Rows[0];

            var masterUser = db.ExecuteQuery($@"
                        SELECT u.""TelegrammId""
                        FROM ""Users"" u
                        JOIN ""Masters"" m ON m.""UserId"" = u.""idUser""
                        WHERE m.""idMaster"" = {masterId}
                    ").Rows[0];

            long masterTelegramId = (long)masterUser["TelegrammId"];
            int bookingId = Convert.ToInt32(bookingTable.Rows[0]["idBooking"]);
            Console.WriteLine($"MASTER TELEGRAM ID = {masterTelegramId}");
            var timeString = $"{times.Hours}-{times.Minutes}-{times.Seconds}";
            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("✅ Принять", $"master:booking_accept:{key}:{bookingId}"),
            InlineKeyboardButton.WithCallbackData("❌ Отмена", $"master:booking_cancel:{key}:{bookingId}")
        }
    });

            await botClient.SendMessage(
                masterTelegramId,
                $"📥 Новая запись!\n\n" +
                $"👤 @{username}\n" +
                $"💼 {service["Name"]}\n" +
                $"📅 {date:dd.MM.yyyy}\n" +
                $"⏰ {time}\n\n" +
                $"Подтвердить?",
                replyMarkup: keyboard
            );
        }

        public async Task CancelBookingByUser(
    CallbackQuery query,
    CallBackData data)
        {
            int bookingId = int.Parse(data.SubAction);

            db.ExecuteNonQuery($@"
                                    UPDATE ""Bookings""
                                    SET ""Status"" = 'cancelled'
                                    WHERE ""idBooking"" = {bookingId}
                                ");

            var booking = db.ExecuteQuery($@"
                                        SELECT 
                                            b.""MasterId"",
                                            u.""UserName""
                                        FROM ""Bookings"" b
                                        JOIN ""Users"" u 
                                            ON u.""idUser"" = b.""UserId""
                                        WHERE b.""idBooking"" = {bookingId}
                                    ").Rows[0];

            int masterId = Convert.ToInt32(booking["MasterId"]);

            string userName = booking["Name"].ToString();

            long masterTelegramId = db.GetMasterTelegramId(masterId);

            await botClient.SendMessage(
                masterTelegramId,
                $"❌ Клиент {userName} отменил запись"
            );

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                "❌ Вы отменили запись"
            );
        }
    }
}
