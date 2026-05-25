using Npgsql;
using System;
using System.Data;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zapisi.Pro.CallBacks;
using Zapisi.Pro.State;
using static System.Net.Mime.MediaTypeNames;

namespace Zapisi.Pro
{
    internal class AdminHandler : ICallbackHandler
    {
        
        private readonly ITelegramBotClient botClient;
        private readonly StateService stateService = new StateService();
        private readonly DbHelper db;
        public string Entity => "admin";


        public AdminHandler(ITelegramBotClient botClient)
        {
            this.botClient = botClient;
            var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
            DotNetEnv.Env.Load(envPath);
            var host = Environment.GetEnvironmentVariable("DB_HOST");
            var user = Environment.GetEnvironmentVariable("DB_USER");
            var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");
            db = new DbHelper($"Host={host};Port=5432;Username={user};Password={pass};Database=Zapisi.Pro;SSL Mode=Disable;Trust Server Certificate=true;");
            this.botClient = botClient;
        }
        public async Task Handle(CallBackData data, CallbackQuery query)
        {

            stateService.ClearState(query.Message.Chat.Id);
            var chatId = query.Message.Chat.Id;

            switch (data.Action)
            {
                case "users":
                    await ShowUsers(query) ;
                    break;

                case "masters":
                    await ShowMasters(query);
                    break;
                case "menu":
                    var stateService = new StateService();
                    stateService.ClearState(query.Message.Chat.Id);
                    await ShowMenu(query) ; break;
                case "make_master":
                    await HendlerMakeMaster(query);break;
                case "delet_master":
                    await HendlerDeletMaster(query);
                    break;
                case "broadcast":

                    await StartBroadcost(query);  break;  
            }
        }
        public async Task ShowMasters(CallbackQuery query) // метод для просмотра мастеров 
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", "admin:menu")
                }

            });
            var chatId = query.Message.Chat.Id;
            var messageId = query.Message.MessageId;
            string sql = @"Select ""UserName"" ,""TelegrammId"" From public.""Users"" Where ""Role"" = 'master' ";
            DataTable dt =  db.ExecuteQuery(sql);
            if (dt.Rows.Count == 0)
            {
                await botClient.EditMessageText(chatId:chatId,messageId:messageId, "❌ Мастеров нет" ,replyMarkup: keyboard); return;
            }
            else
            {
                string text = "💇 Список Мастеров \n\n";
                foreach (DataRow row in dt.Rows)
                {
                    text += $"* {row["UserName"]} | {row["TelegrammId"]}\n";
                }
                await botClient.EditMessageText(
                       chatId: chatId,
                       messageId: messageId,
                       text: text,
                       replyMarkup: keyboard
                   );
            }
        }
        public async Task ShowUsers(CallbackQuery query) // метод для просмотра пользоваетелей
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", "admin:menu")
                }
            });
            var chatId = query.Message.Chat.Id;
            var messageId = query.Message.MessageId;
            string sql = @"Select ""UserName"" , ""Role"" ,""TelegrammId""  From public.""Users"" ";

            DataTable dt = db.ExecuteQuery(sql);

            if (dt.Rows.Count == 0)
            {
                await botClient.SendMessage(chatId, "❌ Пользователей нет"); return;

            }
            else
            {
                string text = "👤 Список пользователей \n\n";
                foreach (DataRow row in dt.Rows) 
                {
                    text += $"* {row["UserName"]} | {row["Role"]} | {row["TelegrammId"]}\n";
                }
                await botClient.EditMessageText(
                       chatId: chatId,
                       messageId: messageId,
                       text: text,
                       replyMarkup: keyboard
                   );
            }
        }
        public async Task ShowMenu(CallbackQuery query)//главное меню админа 
        {
            var chatId = query.Message.Chat.Id;
            var messageId = query.Message.MessageId;
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                   InlineKeyboardButton.WithCallbackData("👤 Пользователи", "admin:users"),
                    InlineKeyboardButton.WithCallbackData("💇 Мастера", "admin:masters"),
                    InlineKeyboardButton.WithCallbackData("📣 Рассылка","admin:broadcast"),
                    InlineKeyboardButton.WithWebApp("🛠 Admin Panel",new WebAppInfo{Url = "https://app-zapisi-pro.site/"})

                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Назначить мастера", "admin:make_master"),
                    InlineKeyboardButton.WithCallbackData("❌ Удалить мастера","admin:delet_master")
                }
            }
            );
            await botClient.EditMessageText(
                       chatId: chatId,
                       messageId: messageId,
                       text: "👑 Админ-панель",
                       replyMarkup: keyboard
                   ); ;
        }
        public async Task ShowMenu(long chatId) //главное меню админа перегруженный 
        {
            var keyboard = new InlineKeyboardMarkup(new[]
             {
                new[]
                {
                   InlineKeyboardButton.WithCallbackData("👤 Пользователи", "admin:users"),
                    InlineKeyboardButton.WithCallbackData("💇 Мастера", "admin:masters")

                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Назначить мастера", "admin:make_master"),
                    InlineKeyboardButton.WithCallbackData("❌ Удалить мастера","admin:delet_master")
                }
            }
             );

            await botClient.SendMessage(chatId, "👑 Админ-панель", replyMarkup: keyboard);
        }
        async public Task HendlerMakeMaster(CallbackQuery query) 
        { 
            var chatId = query.Message.Chat.Id;
            var messageId = query.Message.MessageId;
           
            var stateService = new StateService();
            stateService.SetState(chatId, "waiting_master_id");
            var keyboard = new InlineKeyboardMarkup(new[]
             {
                    new[]
                {
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", "admin:menu")
                }
            });
            await botClient.EditMessageText(chatId: chatId, messageId: messageId, "✏️ Введите TelegramId пользователя, которого хотите назначить мастером:",replyMarkup:keyboard);

        }
        async public Task HendlerDeletMaster(CallbackQuery query)
        {
            var chatId = query.Message.Chat.Id;
            var messageId = query.Message.MessageId;

            var stateService = new StateService();
            stateService.SetState(chatId, "waiting_Dmaster_id");
            var keyboard = new InlineKeyboardMarkup(new[]
             {
                    new[]
                {
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", "admin:menu")
                }
            });
            await botClient.EditMessageText(chatId: chatId, messageId: messageId, "✏️ Введите TelegramId мастера , которого хотите  убрать:", replyMarkup: keyboard);


        }
        public async Task SetMaster(Message message)//назначение мастера
        {
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ В главное меню", "admin:menu")
                    }
                });
            var stateService = new StateService();
            var userService = new UserService();

            if (!long.TryParse(message.Text, out long telegramID))
            {
                await botClient.SendMessage(message.Chat.Id, "❌ Неверный ID",replyMarkup:keyboard);
                return;
            }

            if (!userService.ExistsByTelegramId(telegramID))
            {
                await botClient.SendMessage(message.Chat.Id, "❌ Пользователь не найден",replyMarkup:keyboard);
                return;
            }




            stateService.SetData(message.Chat.Id, telegramID.ToString());
            stateService.SetState(message.Chat.Id, "waiting_master_key");

            await botClient.SendMessage(message.Chat.Id, "🔑 Введите ключ мастера:", replyMarkup: keyboard);
        }
        public async Task CreateMasterWithKey(Message message)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ В главное меню", "admin:menu")
                    }
                });
            var stateService = new StateService();
            var userService = new UserService();

            var key = message.Text;
            var exists = db.ExecuteQuery($@"
                    SELECT 1
                    FROM ""Masters""
                    WHERE ""Key"" = '{key}'
                    LIMIT 1
                ");

            if (exists.Rows.Count > 0)
            {
                await botClient.SendMessage(message.Chat.Id, "❌ Такой ключ уже существует");
                return;
            }
            var telegramId = long.Parse(stateService.GetData(message.Chat.Id));

            string sql = $@"
                            INSERT INTO public.""Masters"" (""UserId"", ""Key"")
                            VALUES (
                                (SELECT ""idUser"" FROM public.""Users"" WHERE ""TelegrammId"" = {telegramId}),
                                '{key}'
                            );

                            UPDATE public.""Users""
                            SET ""Role"" = 'master'
                            WHERE ""TelegrammId"" = {telegramId};
                        ";


            db.ExecuteNonQuery(sql);

            stateService.ClearState(message.Chat.Id);
            var masterId = GetMasterIdByKey(key);
            CreateDefaultSchedule(masterId);

            await botClient.SendMessage(message.Chat.Id, "✅ Мастер создан",replyMarkup:keyboard);
            await botClient.SendMessage(
                    chatId: telegramId,
                    text: "🎉 Поздравляем! Вы назначены мастером.\nДоступ в личный кабинет в разработке.", replyMarkup: new InlineKeyboardMarkup(new[]
                 {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("👤 Личный кабинет", $"master:master_panel:{key}")
                            }
                        })
                );
        }
         public async Task DeletMasterWithId(Message message)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ В главное меню", "admin:menu")
                    }
                });
            var stateService = new StateService();
            var userService = new UserService();

            if (!long.TryParse(message.Text, out long telegramID))
            {
                await botClient.SendMessage(message.Chat.Id, "❌ Неверный ID", replyMarkup: keyboard);
                return;
            }

            if (!userService.ExistsByTelegramId(telegramID))
            {
                await botClient.SendMessage(message.Chat.Id, "❌ Пользователь не найден", replyMarkup: keyboard);
                return;
            }
            string sqlDeleteMaster = $@"
                DELETE FROM public.""Masters""
                WHERE ""UserId"" = (
                    SELECT ""idUser"" FROM public.""Users""
                    WHERE ""TelegrammId"" = {telegramID}
                )";
            db.ExecuteNonQuery(sqlDeleteMaster);

            string sqlUpdateRole = $@"
                UPDATE public.""Users""
                SET ""Role"" = 'client'
                WHERE ""TelegrammId"" = {telegramID}";

            db.ExecuteNonQuery(sqlUpdateRole);

            stateService.ClearState(message.Chat.Id);

            await botClient.SendMessage(
                   telegramID,
                   "❌ Вы больше не являетесь мастером"
               );
            await botClient.SendMessage(
                    message.Chat.Id,
                    "✅ Мастер удалён",
                    replyMarkup: keyboard
                );
        }
        public async Task StartBroadcost(CallbackQuery query)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ Назад", "admin:menu")
                    }
                });
            var stateService = new StateService();
            stateService.SetState(query.Message.Chat.Id, "waiting_broadcast_text");
            await botClient.EditMessageText(chatId: query.Message.Chat.Id, messageId: query.Message.Id, "Ввведите текст для рассылки",replyMarkup:keyboard);
        }
        public async Task SendBroadcast(Message message) 
        {
            var text = message.Text;

            var users = db.ExecuteQuery(@"
                                    SELECT 
                                        u.""TelegrammId"",
                                        m.""Key""
                                    FROM ""Masters"" m
                                    JOIN ""Users"" u ON u.""idUser"" = m.""UserId""
                                ");

            foreach (DataRow row in users.Rows)
            {
                try
                {

                    long chatId = Convert.ToInt64(row["TelegrammId"]);
                    string key = row["Key"].ToString();
                    var back = new InlineKeyboardMarkup(new[]
                         {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(
                                        "👤 В профиль",
                                        $"master:master_panel:{key}"
                                    )
                                }
                            });

                    await botClient.SendMessage(
                        chatId,
                        "📣 Обновление от админа:\n\n" + text ,replyMarkup:back
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки: {ex.Message}");
                }
            }
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ Назад", "admin:menu")
                    }
                });

            await botClient.SendMessage(message.Chat.Id, "✅ Рассылка завершена", replyMarkup: keyboard);


        }
        public void CreateDefaultSchedule(long masterId)
        {
            for (int day = 1; day <= 7; day++)
            {
                bool isWorkDay = day <= 5; // Пн–Пт

                db.ExecuteNonQuery($@"
            INSERT INTO ""MasterSchedule""
            (""MasterId"", ""DayOfWeek"", ""StartTime"", ""EndTime"", ""IsActive"")
            VALUES
            ({masterId}, {day},
            '09:00', '18:00',
            {isWorkDay})
        ");
            }
        }
        private int GetMasterIdByKey(string key)
        {
            var table = db.ExecuteQuery($@"
        SELECT ""idMaster""
        FROM ""Masters""
        WHERE ""Key"" = '{key}'
        LIMIT 1
    ");

            return Convert.ToInt32(table.Rows[0]["idMaster"]);
        }
    }
}
