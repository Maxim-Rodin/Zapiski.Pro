using Hangfire;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zapisi.Pro.State;
using Zapiski.Pro.BasedClasses;
using Zapiski.Pro.Services;
using static System.Net.Mime.MediaTypeNames;

namespace Zapisi.Pro.CallBacks
{
    internal class MasterHandler : ICallbackHandler
    {
        private readonly ITelegramBotClient botClient;
        private readonly DbHelper db;
        private readonly StateService stateService = new StateService();
        private readonly ServiceHandler serviceHandler = new ServiceHandler();
        private readonly ScheduleService scheduleService;
        private readonly string miniAppBaseUrl;



        public string Entity => "master";

        public MasterHandler(ITelegramBotClient botClient, ScheduleService scheduleService)
        {
            var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            EnvConfig.Load(envPath);
            var host = Environment.GetEnvironmentVariable("DB_HOST");
            var user = Environment.GetEnvironmentVariable("DB_USER");
            var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");
            miniAppBaseUrl = Environment.GetEnvironmentVariable("MINIAPP_URL") ?? "https://app-zapisi-pro.site";
            db = new DbHelper($"Host={host};Port=5432;Username={user};Password={pass};Database=Zapisi.Pro;SSL Mode=Disable;Trust Server Certificate=true;");

            this.botClient = botClient;
            this.scheduleService = scheduleService;
            BookingJobs.BotClient = botClient;
            BookingJobs.Db = db;
        }
        public async Task Handle( CallBackData data, CallbackQuery query)
        {
           
            var chatId = query.Message.Chat.Id;
            stateService.ClearState(chatId);

            switch (data.Action)
            {
                case "master_panel":
                    await ShowMenu(query, data.Id);
                    break;

                case "master_profile":
                    await ShowProfile(query, data.Id);
                    break;

                case "schedule_set":
                    await scheduleService.StartSetTime(query, data); 
                    break;
                case "schedule_off":
                    await scheduleService.SetDayOff(query, data);
                    break;

                case "schedule_on":
                    await scheduleService.SetDayOn(query, data);
                    break;
                case "booking_accept":
                    await AcceptBooking(query, data);
                    break;

                case "booking_cancel":
                    await CancelBooking(query, data); break;
               
                case "payment_accept":
                    await AcceptPayment(query, data);
                    break;

                case "payment_reject":
                    await RejectPayment(query, data);
                    break;
            }

        }

        public async Task AcceptPayment(
       CallbackQuery query,
       CallBackData data)
        {
            int bookingId = int.Parse(data.SubAction);

            var currentStatus = db.ExecuteScalar($@"SELECT ""Status"" FROM ""Bookings"" WHERE ""idBooking"" = {bookingId}")?.ToString();

            if (currentStatus != "waiting_payment_confirm")
            {
                await botClient.AnswerCallbackQuery(query.Id, "Запись уже обработана");
                await botClient.EditMessageText(query.Message.Chat.Id, query.Message.MessageId, "Эта запись уже обработана");
                return;
            }

            // ─────────────────────────────
            // ПОЛУЧАЕМ ДАННЫЕ
            // ─────────────────────────────

            var row = db.ExecuteQuery($@"
        SELECT
            b.""Date"",
            b.""Time"",
            u.""TelegrammId"",
            s.""Name"" AS ""ServiceName"",
            s.""Duration""
        FROM ""Bookings"" b

        JOIN ""Users"" u
            ON u.""idUser"" = b.""UserId""

        JOIN ""Services"" s
            ON s.""idService"" = b.""ServiceId""

        WHERE b.""idBooking"" = {bookingId}
    ").Rows[0];

            long clientId =
                Convert.ToInt64(row["TelegrammId"]);

            DateOnly date =
                (DateOnly)row["Date"];

            TimeOnly time =
                (TimeOnly)row["Time"];

            DateTime appointmentTime =
                date.ToDateTime(time);

            string serviceName =
                row["ServiceName"].ToString();

            int durationMinutes =
                Convert.ToInt32(row["Duration"]);

            // ─────────────────────────────
            // СТАТУС
            // ─────────────────────────────

            db.ExecuteNonQuery($@"
        UPDATE ""Bookings""
        SET ""Status"" = 'confirmed'
        WHERE ""idBooking"" = {bookingId}
    ");

            // ─────────────────────────────
            // КЛИЕНТУ
            // ─────────────────────────────

            var clientKeyboard =
                new InlineKeyboardMarkup(new[]
                {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🏠 В меню",
                    "client:menu"
                )
            }
                });

            await botClient.SendMessage(
                clientId,
                $"✅ Предоплата подтверждена мастером\n\n" +
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}\n\n" +
                $"Запись подтверждена. Ждём вас в назначенное время.",
                replyMarkup: clientKeyboard
            );

            // ─────────────────────────────
            // МАСТЕРУ
            // ─────────────────────────────

            var masterKeyboard =
                new InlineKeyboardMarkup(new[]
                {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "⬅️ Назад",
                    "master:menu"
                )
            }
                });

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                $"✅ Предоплата подтверждена\n\n" +
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}",
                replyMarkup: masterKeyboard
            );

            // ─────────────────────────────
            // ЗАПУСК НАПОМИНАНИЙ
            // ─────────────────────────────

            BookingJobs.ScheduleAllReminders(
                bookingId,
                clientId,
                appointmentTime,
                serviceName,
                durationMinutes
            );
        }

        public async Task RejectPayment(
            CallbackQuery query,
            CallBackData data)
        {
            int bookingId = int.Parse(data.SubAction);

            var currentStatus = db.ExecuteScalar($@"SELECT ""Status"" FROM ""Bookings"" WHERE ""idBooking"" = {bookingId}")?.ToString();

            if (currentStatus != "waiting_payment_confirm")
            {
                await botClient.AnswerCallbackQuery(query.Id, "Запись уже обработана");
                await botClient.EditMessageText(query.Message.Chat.Id, query.Message.MessageId, "Эта запись уже обработана");
                return;
            }

            // ─────────────────────────────
            // ДАННЫЕ
            // ─────────────────────────────

            var row = db.ExecuteQuery($@"
        SELECT
            u.""TelegrammId"",
            b.""Date"",
            b.""Time"",
            s.""Name"" AS ""ServiceName"",
            s.""Price"",
            s.""PrepaymentPercent"",
            m.""PaymentDetails"",
            m.""Key""
        FROM ""Bookings"" b

        JOIN ""Users"" u
            ON u.""idUser"" = b.""UserId""

        JOIN ""Services"" s
            ON s.""idService"" = b.""ServiceId""

        JOIN ""Masters"" m
            ON m.""idMaster"" = b.""MasterId""

        WHERE b.""idBooking"" = {bookingId}
    ").Rows[0];

            long clientId =
                Convert.ToInt64(row["TelegrammId"]);

            string serviceName =
                row["ServiceName"].ToString();

            DateOnly date =
                (DateOnly)row["Date"];

            TimeOnly time =
                (TimeOnly)row["Time"];

            int price =
                Convert.ToInt32(row["Price"]);

            int percent =
                Convert.ToInt32(row["PrepaymentPercent"]);

            int prepaymentAmount =
                (price * percent) / 100;

            string paymentDetails =
                row["PaymentDetails"] != DBNull.Value
                    ? row["PaymentDetails"].ToString()
                    : "Реквизиты не указаны";

            string masterKey =
                row["Key"].ToString();

            // ─────────────────────────────
            // ВОЗВРАЩАЕМ СТАТУС
            // ─────────────────────────────

            db.ExecuteNonQuery($@"
        UPDATE ""Bookings""
        SET ""Status"" = 'waiting_payment'
        WHERE ""idBooking"" = {bookingId}
    ");

            // ─────────────────────────────
            // МАСТЕРУ
            // ─────────────────────────────

            var masterKeyboard =
                new InlineKeyboardMarkup(new[]
                {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "⬅️ Назад",
                    $"master:master_profile:{masterKey}"
                )
            }
                });

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                $"❌ Оплата отклонена\n\n" +
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}",
                replyMarkup: masterKeyboard
            );

            // ─────────────────────────────
            // КЛИЕНТУ
            // ─────────────────────────────

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "✅ Я оплатил",
                $"client:paid_booking:{bookingId}"
            )
        },

        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "🏠 В меню",
                "client:menu"
            )
        }
    });

            await botClient.SendMessage(
                clientId,
                $"❌ Мастер не подтвердил оплату\n\n" +
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}\n" +
                $"💸 Предоплата: {prepaymentAmount}₽\n\n" +
                $"Проверьте перевод и попробуйте снова.\n\n" +
                $"Реквизиты мастера:\n{paymentDetails}",
                replyMarkup: keyboard
            );
        }

        public async Task ShowMenu(CallbackQuery query, string key) //главное меню мастера  
        {
            var chatId = query.Message.Chat.Id;
            var miniAppUrl = $"{miniAppBaseUrl.TrimEnd('/')}/master/{key}";
            var clientMiniAppUrl = $"{miniAppBaseUrl.TrimEnd('/')}/user/{query.From.Id}";
            Console.WriteLine("MASTER MINI APP URL = " + miniAppUrl);
            var keyboard = new InlineKeyboardMarkup(new[]
             {
            
                new[]
                {
                    InlineKeyboardButton.WithWebApp(
                                "👤 Мастер панель",
                                new WebAppInfo(miniAppUrl)
                            )
                },
                new[]
                {
                    InlineKeyboardButton.WithWebApp(
                                "📱 Мой клиентский кабинет",
                                new WebAppInfo(clientMiniAppUrl)
                            )
                }

            }
             );
            var link = $"https://t.me/ZapisiProBot?start={key}";
            await botClient.SendMessage(chatId, $"👤 Панель мастера\n\nВаш персональный ключ:{key}\n\n🔗 Ваша ссылка:\n{link}\n\nПоделитесь ей с клиентами 👇\n\n Выберите действие:", replyMarkup: keyboard);
        }

        public async Task ShowProfile(CallbackQuery query, string key)//перегруженный метод показа профиля мастера при нажатии на кнопку из-за наличия query, который позволяет редактировать профиль, а также показывает ссылку на профиль если владелец просматривает его
        {
            var (text, keyboard) = BuildProfile(key, query.From.Id);

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                text,
                replyMarkup: keyboard
            );
        }
        public async Task ShowProfileFromStart(long chatId, long telegramId, string key)// перегруженный метод показа профиля мастера при заходе по ссылке из-за отсутствия query
        {
            var (text, keyboard) = BuildProfile(key, telegramId);

            await botClient.SendMessage(
                chatId,
                text,
                replyMarkup: keyboard
            );
        }

        public (string text, InlineKeyboardMarkup keyboard) BuildProfile(string key, long telegramId)
        {
            string sql = $@"SELECT * FROM ""Masters"" WHERE ""Key"" = '{key}'";
            var dt = db.ExecuteQuery(sql);

            if (dt.Rows.Count == 0)
                return ("❌ Мастер не найден", null);

            var master = dt.Rows[0];

            // проверка owner
            string sqlUser = $@"
        SELECT u.""TelegrammId""
        FROM ""Users"" u
        JOIN ""Masters"" m ON m.""UserId"" = u.""idUser""
        WHERE m.""Key"" = '{key}'";

            var dtUser = db.ExecuteQuery(sqlUser);

            bool isOwner = false;

            if (dtUser.Rows.Count > 0)
            {
                isOwner = (long)dtUser.Rows[0]["TelegrammId"] == telegramId;
            }

            // текст
            string text =
                $"👤 {master["Name"] ?? "Без имени"}\n\n" +
                $"📝 Описание: {(master["Description"] ?? "Пусто")}";

            // если владелец → даём ссылку
            if (isOwner)
            {
                var link = $"https://t.me/ZapisiProBot?start={key}";
                text += $"\n\n🔗 Ваша ссылка:\n{link}";
            }

            InlineKeyboardMarkup keyboard;

            if (isOwner)
            {
                keyboard = new InlineKeyboardMarkup(new[]
                {
          
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_panel:{key}")
            },

        });
            }
            else
            {
                var miniAppProfileUrl = $"{miniAppBaseUrl.TrimEnd('/')}/master/{key}/public-profile";

                keyboard = new InlineKeyboardMarkup(new[]
                {
            new[]
            {
                InlineKeyboardButton.WithWebApp(
                    "📱 Открыть профиль",
                    new WebAppInfo(miniAppProfileUrl)
                )
            }
            
        });
            }

            return (text, keyboard);
        }
        public async Task AcceptBooking(CallbackQuery query, CallBackData data)
        {
            string key = data.Id;

            var clientKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🏠 В меню", "client:menu")
                }
            });

            int bookingId = int.Parse(data.SubAction);

            var currentStatus = db.ExecuteScalar($@"SELECT ""Status"" FROM ""Bookings"" WHERE ""idBooking"" = {bookingId}")?.ToString();

            if (currentStatus != "pending")
            {
                await botClient.AnswerCallbackQuery(query.Id, "Запись уже обработана");
                await botClient.EditMessageText(query.Message.Chat.Id, query.Message.MessageId, "Эта запись уже обработана");
                return;
            }

            db.ExecuteNonQuery($@"
                        UPDATE ""Bookings""
                        SET ""Status"" = 'confirmed'
                        WHERE ""idBooking"" = {bookingId}
                    ");

            var booking = db.ExecuteQuery($@"
                SELECT 
                    b.""Date"",
                    b.""Time"",
                    u.""TelegrammId"",
                    u.""UserName"",
                    s.""Name"" as ""ServiceName"",
                    s.""Duration""
                            FROM ""Bookings"" b
                            JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                            JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                            WHERE b.""idBooking"" = {bookingId}
                        ").Rows[0];

            long clientId = Convert.ToInt64(booking["TelegrammId"]);
            string clientUsername = booking["UserName"]?.ToString() ?? "без username";
            DateOnly date = (DateOnly)booking["Date"];
            TimeOnly time = (TimeOnly)booking["Time"];
            DateTime appointmentTime = date.ToDateTime(time);
            string serviceName = booking["ServiceName"].ToString();
            int durationMinutes = Convert.ToInt32(booking["Duration"]);

            await botClient.SendMessage(
                clientId,
                $"✅ Запись подтверждена мастером\n\n" +
                $"👤 Мастер: {key}\n" +
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}\n\n" +
                $"Ждём вас в назначенное время.",
                replyMarkup: clientKeyboard);

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                $"✅ Запись подтверждена\n\n" +
                $"👤 Клиент: @{clientUsername}\n" +
                $"🆔 Telegram ID: {clientId}\n" +
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}"
            );

            BookingJobs.ScheduleAllReminders(
                bookingId,
                clientId,
                appointmentTime,
                serviceName,
                durationMinutes
            );
        }


        public async Task CancelBooking(CallbackQuery query, CallBackData data)
        {
           
            var clientKeyboard = new InlineKeyboardMarkup(new[]
          {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🏠 В меню", "client:menu")
        }
    });
            int bookingId = int.Parse(data.SubAction);

            var currentStatus = db.ExecuteScalar($@"SELECT ""Status"" FROM ""Bookings"" WHERE ""idBooking"" = {bookingId}")?.ToString();

            if (currentStatus == "cancelled" || currentStatus == "completed")
            {
                await botClient.AnswerCallbackQuery(query.Id, "Запись уже обработана");
                await botClient.EditMessageText(query.Message.Chat.Id, query.Message.MessageId, "Эта запись уже обработана");
                return;
            }

            db.ExecuteNonQuery($@"
                        UPDATE ""Bookings""
                        SET ""Status"" = 'cancelled'
                        WHERE ""idBooking"" = {bookingId}
                    ");

            var user = db.ExecuteQuery($@"
                SELECT
                    u.""TelegrammId"",
                    u.""UserName"",
                    b.""Date"",
                    b.""Time"",
                    s.""Name"" AS ""ServiceName""
                FROM ""Bookings"" b
                JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                WHERE b.""idBooking"" = {bookingId}
            ").Rows[0];

            long clientId = Convert.ToInt64(user["TelegrammId"]);
            string clientUsername = user["UserName"]?.ToString() ?? "без username";
            DateOnly date = (DateOnly)user["Date"];
            TimeOnly time = (TimeOnly)user["Time"];
            string serviceName = user["ServiceName"]?.ToString() ?? "Услуга";

            await botClient.SendMessage(
                clientId,
                $"❌ Запись отменена мастером\n\n" +
                $"👤 Мастер: {data.Id}\n" +
                $"💼 Услуга: {serviceName}\n" +
                $"📅 Дата: {date:dd.MM.yyyy}\n" +
                $"⏰ Время: {time:HH:mm}",
                replyMarkup: clientKeyboard
            );

            await botClient.EditMessageText(
                     query.Message.Chat.Id,
                     query.Message.MessageId,
                     $"❌ Запись отменена\n\n" +
                     $"👤 Клиент: @{clientUsername}\n" +
                     $"🆔 Telegram ID: {clientId}\n" +
                     $"💼 Услуга: {serviceName}\n" +
                     $"📅 Дата: {date:dd.MM.yyyy}\n" +
                     $"⏰ Время: {time:HH:mm}"
                 );
        }

    }
}
