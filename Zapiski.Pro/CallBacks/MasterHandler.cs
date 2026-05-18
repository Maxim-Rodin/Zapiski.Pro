using Hangfire;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
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



        public string Entity => "master";

        public MasterHandler(ITelegramBotClient botClient, ScheduleService scheduleService)
        {
            var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            DotNetEnv.Env.Load(envPath);
            var host = Environment.GetEnvironmentVariable("DB_HOST");
            var user = Environment.GetEnvironmentVariable("DB_USER");
            var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");
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

                case "profile_edit":
                    await ShowEditMenu(query, data.Id);
                    break;

                case "master_records":
                    await ShowRecords(query, data.Id);
                    break;
                case "edit_description":
                    await StartEditDescription(query, data.Id);
                    break;
                case "edit_contacts":
                    await StartEditContacts(query, data.Id);
                    break;
                case "edit_name":
                    await StartEditName(query, data.Id);
                    break;
                case "master_edit":
                    await ShowMasterEditMenu(query, data.Id);
                    break;
                case "master_add_Service":
                    await StartAddService(query, data.Id);
                    break;
                case "master_edit_Service":
                    await StartEditContacts(query, data.Id);
                    break;
                case "master_delet_Service":
                    await StartDeletService(query, data.Id);
                    break;
                case "profile_services":
                    int page = data.SubAction != null ? int.Parse(data.SubAction) : 0;
                    await ShowServices(query, data.Id, page);

                    break;
                case "profile_services_delete":
                    await ConfirmDeleteService(query, data);
                    break;
                case "delete_service_confirm":
                    await DeleteService(query, data);
                    break;
                case "master_edit_service":
                    await StartEditService(query, data.Id);
                    break;
                case "edit_service_select":
                    await ShowEditOptions(query, data);
                    break;
                case "schedule":
                    await ShowSchedule(query, data.Id);
                    break;
                case "schedule_edit":
                    await ShowDays(query, data.Id);
                    break;
                case "schedule_day":
                    var day = int.Parse(data.SubAction);
                    await ShowDay(query, data.Id, day);
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
                case "show_records":
                    await ShowRecords(query, data.Id);
                    break;

                case "edit_title":
                    var btn = InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_edit:{data.Id}");
                    stateService.SetState(query.From.Id, $"edit_title:{data.Id}:{data.SubAction}");
                    await botClient.EditMessageText(query.Message.Chat.Id, query.Message.MessageId, "Введите новое название:", replyMarkup: btn);
                    break;

                case "edit_price":
                    btn = InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_edit:{data.Id}");
                    stateService.SetState(query.From.Id, $"edit_price:{data.Id}:{data.SubAction}");
                    await botClient.EditMessageText(query.Message.Chat.Id, query.Message.MessageId, "Введите новую цену:", replyMarkup: btn);
                    break;

                case "edit_duration":
                    btn = InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_edit:{data.Id}");
                    stateService.SetState(query.From.Id, $"edit_duration:{data.Id}:{data.SubAction}");
                    await botClient.EditMessageText(query.Message.Chat.Id, query.Message.MessageId, "Введите новую длительность:", replyMarkup: btn);
                    break;
                case "booking_accept":
                    await AcceptBooking(query, data); break;

                case "booking_cancel":
                    await CancelBooking(query, data); break;


            }

        }
        

        public async Task ShowMenu(CallbackQuery query, string key) //главное меню мастера  
        {
            var chatId = query.Message.Chat.Id;
            var keyboard = new InlineKeyboardMarkup(new[]
             {
                         new[]
                {
                    InlineKeyboardButton.WithCallbackData("👁 Мой профиль", $"master:master_profile:{key}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"master:master_edit:{key}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📅 Мои записи", $"master:master_records:{key}")
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
                InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"master:profile_edit:{key}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💼 Услуги", $"master:profile_services:{key}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📞 Контакты", $"master:profile_contacts:{key}")
            },
            new[]
             {
                 InlineKeyboardButton.WithCallbackData("📅 Записи",$"master:show_records:{key}")
             },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_panel:{key}")
            },

        });
            }
            else
            {
                keyboard = new InlineKeyboardMarkup(new[]
                {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💼 Услуги", $"master:profile_services:{key}")
            }
           
        });
            }

            return (text, keyboard);
        }
        public async Task ShowMasterEditMenu(CallbackQuery query, string key)//меню где происходит добавление удаление редактироание услуг
        {
            var chatId = query.Message.Chat.Id;
            stateService.ClearState(chatId);
            var keyboard = new InlineKeyboardMarkup(new[]
             {
                         new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Добавить услугу", $"master:master_add_Service:{key}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✏️ Редактировать услугу", $"master:master_edit_service:{key}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("❌ Удалить услугу", $"master:master_delet_Service:{key}")
                },
                new[]
                 {
                     InlineKeyboardButton.WithCallbackData("⏰ График",$"master:schedule:{key}")
                 },
                 new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_panel:{key}")
                        }
            }

              );

            await botClient.SendMessage(chatId, $"👤 Редактор услуг \n\nВыберите действие:", replyMarkup: keyboard);



        }


        public async Task ShowRecords(CallbackQuery query, string key) //показать записи
        {
            var masterId = db.GetMasterIdByKey(key);

            var chatId = query.Message.Chat.Id;

            var messageId = query.Message.MessageId;
            var btn = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_profile:{key}")
                }
            });


            var records = db.ExecuteQuery($@"
                SELECT r.""idBooking"", r.""Date"",r.""Time"",r.""Status"", u.""TelegrammId"", u.""UserName"" , o.""Name""
                FROM ""Bookings"" r
                JOIN ""Users"" u ON u.""idUser"" = r.""UserId""
                JOIN ""Services"" o ON o.""idService"" = r.""ServiceId""
                WHERE r.""MasterId"" = {masterId}
                AND r.""Status"" != 'cancelled'
                AND r.""Status"" != 'completed'
                ORDER BY r.""Date"" ");
            
            if (records.Rows.Count == 0)
            {
                await botClient.SendMessage(chatId, "📅 Нет записей.",replyMarkup: btn);
                return;
            }

            string text = "📅 Ваши записи:\n\n";

            foreach (var row in records.Rows) 
            { 
                var record = (DataRow)row;
                var date = (DateOnly)record["Date"];
                var time = record["Time"].ToString();
                var username = record["UserName"] != DBNull.Value ? record["UserName"].ToString() : "Пользователь удалён";
                var serviceName =  record["Name"] != DBNull.Value ? record["Name"].ToString() : "Услуга удалена";
               
                string status = record["Status"] != DBNull.Value ? record["Status"].ToString() : "unknown";
                string statusIcon =
                           status == "confirmed" ? "✅" :
                           status == "pending" ? "⏳" :
                           "❌";
                text += $" {statusIcon}👤 @{username} — {date}-{time} - {serviceName} \n";
                var buttons = new List<InlineKeyboardButton[]>();

                // 🟡 если ожидает
                if (status == "pending")
                {
                    buttons.Add(new[]
                    {
                InlineKeyboardButton.WithCallbackData("✅ Принять", $"master:booking_accept:{key}:{record["idBooking"]}"),
                InlineKeyboardButton.WithCallbackData("❌ Отменить", $"master:booking_cancel:{key}:{record["idBooking"]}")
            });
                }
                // 🟢 если уже подтверждена
                else if (status == "confirmed")
                {
                    buttons.Add(new[]
                    {
                InlineKeyboardButton.WithCallbackData("❌ Отменить", $"master:booking_cancel:{key} : {record["idBooking"]}")
            });
                }

                buttons.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_profile:{key}")
        });

                var keyboard = new InlineKeyboardMarkup(buttons);

                await botClient.SendMessage(chatId, text, replyMarkup: keyboard);

            }
           

            
        }
        public async Task ShowEditMenu(CallbackQuery query, string key)
        {
            var chatId = query.Message.Chat.Id;
            var messageId = query.Message.MessageId;
            stateService.ClearState(chatId);


            var keyboard = new InlineKeyboardMarkup(new[]
            {
                    new[] { InlineKeyboardButton.WithCallbackData("👤 Имя", $"master:edit_name:{key}") },
                    new[] { InlineKeyboardButton.WithCallbackData("📝 Описание", $"master:edit_description:{key}") },
                    new[] { InlineKeyboardButton.WithCallbackData("📞 Контакты", $"master:edit_contacts:{key}") },
                    new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", $"master:master_panel:{key}") }
                });

            await botClient.EditMessageText(
                chatId,
                messageId,
                "✏️ Что хотите изменить?",
                replyMarkup: keyboard
            );
        }

        public async Task StartEditDescription(CallbackQuery query, string key) //наало редактировани описани
        {



            stateService.SetState(query.From.Id, $"waiting_description:{key}");

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                "📝 Введите описание:"
            );
        }
        public async Task StartEditContacts(CallbackQuery query, string key) //начало редактирования контактов
        {


            stateService.SetState(query.From.Id, $"waiting_contacts:{key}");

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                "📞 Введите контакты:"
            );
        }
        public async Task SaveDescription(Message message, string key)
        {
            var keyboard = new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", $"master:master_records:{key}") });

            string sql = $@"
                            UPDATE ""Masters""
                            SET ""Description"" = '{message.Text}'
                            WHERE ""Key"" = '{key}'";

            db.ExecuteQuery(sql);
            stateService.ClearState(message.Chat.Id);
            await botClient.SendMessage(message.Chat.Id, "✅ Описание сохранено", replyMarkup: keyboard);
        }
        public async Task SaveContacts(Message message, string key)// сохраняем контакты 
        {
            var keyboard = new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", $"master:master_records:{key}") });

            string sql = $@"
                            UPDATE ""Masters""
                            SET ""Contacts"" = '{message.Text}'
                            WHERE ""Key"" = '{key}'";

            db.ExecuteQuery(sql);
            stateService.ClearState(message.Chat.Id);
            await botClient.SendMessage(message.Chat.Id, "✅ Контакты сохранено", replyMarkup: keyboard);

        }

        public async Task StartEditName(CallbackQuery query, string key)//начинаем менять имя
        {

            stateService.SetState(query.From.Id, $"waiting_name:{key}");
            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                "👤 Введите имя мастера:"
            );


        }
        public async Task SaveName(Message message, string key) // сохранение имени мастера в бд
        {
            if (message.Text.Length > 50)
            {
                await botClient.SendMessage(message.Chat.Id, "❌ Имя слишком длинное");
                return;
            }
            var keyboard = new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", $"master:master_records:{key}") });

            string sql = $@"
                        UPDATE ""Masters""
                        SET ""Name"" = '{message.Text}'
                        WHERE ""Key"" = '{key}'";

            db.ExecuteNonQuery(sql);
            stateService.ClearState(message.Chat.Id);
            await botClient.SendMessage(
                               message.Chat.Id,
                               "✅ Имя сохранено", replyMarkup: keyboard
                           );

        }
        public async Task StartAddService(CallbackQuery query, string key)// начало добавления услуги
        {
            var keyboard = new InlineKeyboardMarkup(new[]
       {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_edit:{key}")
                }
            });

            stateService.SetState(query.From.Id, $"waiting_service_title:{key}");
            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                "Введите название услуги:", replyMarkup: keyboard
            );
        }
        public async Task StartRedactService(CallbackQuery query, string key) // начать редактировать услугу
        {
            var keyboard = new InlineKeyboardMarkup(new[]
       {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_edit:{key}")
                }
            });

            stateService.SetState(query.From.Id, $"waiting_service_title:{key}");
            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                "Введите id услуги которую хотите изменить:", replyMarkup: keyboard
            );
        }
        public async Task StartDeletService(CallbackQuery query, string key, int page = 0) // начало удаления услуги
        {
            Console.WriteLine($"PAGE: {page}");
            var keyboard = new List<InlineKeyboardButton[]>();
            var chatId = query.Message.Chat.Id;
            var messageId = query.Message.MessageId;

            var services = db.GetByMasterKey(key);

            int pageSize = 4;
            int start = page * pageSize;

            var pageItems = services.Skip(start).Take(pageSize).ToList();


            var text = "💼 Менеджер удаления услуг\n\n";
            for (int i = 0; i < pageItems.Count; i++)
            {
                var s = pageItems[i];

                text += $"{start + i + 1}. {s["Name"]} — {s["Price"]}₴ — {s["Duration"]} мин\n";
            }
            text += "\nВыберите услугу для удвления по ID:";



            var idsRow = new List<InlineKeyboardButton>();


            for (int i = 0; i < pageItems.Count; i++)
            {
                var s = pageItems[i];
                idsRow.Add(
                    InlineKeyboardButton.WithCallbackData(
                        $"{start + i + 1}",
                        $"master:profile_services_delete:{key}:{s["idService"]}"
                    )
                );
            }

            keyboard.Add(idsRow.ToArray());

            var nav = new List<InlineKeyboardButton>();

            if (page > 0)
                nav.Add(InlineKeyboardButton.WithCallbackData(
                    "⬅️ Назад",
                    $"master:profile_services:{key}:{page - 1}"
                ));

            if (start + pageSize < services.Count)
                nav.Add(InlineKeyboardButton.WithCallbackData(
                    "➡️ Далее",
                    $"master:profile_services:{key}:{page + 1}"
                ));

            if (nav.Count > 0)
                keyboard.Add(nav.ToArray());
            keyboard.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "👤 В профиль",
                        $"master:master_profile:{key}"
                    )
                });

            await botClient.EditMessageText(
                chatId,
                messageId,
                text,
                replyMarkup: new InlineKeyboardMarkup(keyboard)
            );
        }



        public async Task ShowServices(CallbackQuery query, string key, int page = 0)
        {
            Console.WriteLine($"PAGE: {page}");
            var keyboard = new List<InlineKeyboardButton[]>();
            var chatId = query.Message.Chat.Id;
            var messageId = query.Message.MessageId;

            var services = db.GetByMasterKey(key);

            int pageSize = 4;
            int start = page * pageSize;

            var pageItems = services.Skip(start).Take(pageSize).ToList();


            var text = "💼 Услуги мастера\n\n";
            for (int i = 0; i < pageItems.Count; i++)
            {
                var s = pageItems[i];

                text += $"{start + i + 1}. {s["Name"]} — {s["Price"]}₽ — {s["Duration"]} мин\n";
            }
            text += "\nВыберите услугу по ID:";



            var idsRow = new List<InlineKeyboardButton>();


            for (int i = 0; i < pageItems.Count; i++)
            {
                var s = pageItems[i];
                idsRow.Add(
                    InlineKeyboardButton.WithCallbackData(
                        $"{start + i + 1}",
                        $"client:profile_services:{key}:{s["idService"]}"
                    )
                );
            }

            keyboard.Add(idsRow.ToArray());

            var nav = new List<InlineKeyboardButton>();

            if (page > 0)
                nav.Add(InlineKeyboardButton.WithCallbackData(
                    "⬅️ Назад",
                    $"master:profile_services:{key}:{page - 1}"
                ));

            if (start + pageSize < services.Count)
                nav.Add(InlineKeyboardButton.WithCallbackData(
                    "➡️ Далее",
                    $"master:profile_services:{key}:{page + 1}"
                ));

            if (nav.Count > 0)
                keyboard.Add(nav.ToArray());
            keyboard.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "👤 В профиль",
                        $"master:master_profile:{key}"
                    )
                });

            await botClient.EditMessageText(
                chatId,
                messageId,
                text,
                replyMarkup: new InlineKeyboardMarkup(keyboard)
            );
        }
       
        public async Task ConfirmDeleteService(CallbackQuery query, CallBackData data)
        {
            var serviceId = data.SubAction;
            var key = data.Id;
            string check = $@" 
                SELECT COUNT(*)
                FROM ""Bookings""
                WHERE ""ServiceId"" = {serviceId}"; // проверка есть ли связанные с услгой записи
            
            var countOfBookings = int.Parse(db.ExecuteScalar(check).ToString());
            if (countOfBookings > 0)
            {
                await botClient.EditMessageText(
               query.Message.Chat.Id,
               query.Message.MessageId,
               $"❗Нельзя удалить услугу, так как есть связанные с ней записи - {countOfBookings}шт\n Отмените записи потом переходите к удалению", replyMarkup: new InlineKeyboardMarkup(new[]
               {
                    new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_edit:{key}") }
               })
           );
            }
            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "✅ Да",
                $"master:delete_service_confirm:{key}:{serviceId}"
            ),
            InlineKeyboardButton.WithCallbackData(
                "❌ Нет",
                $"master:master_edit:{key}"
            )
                    }
                });

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                "❗ Вы уверены, что хотите удалить услугу?",
                replyMarkup: keyboard
            );
        }


        public async Task DeleteService(CallbackQuery query, CallBackData data)
        {
            var serviceid = Convert.ToInt32(data.SubAction);

            serviceHandler.DeleteService(serviceid);

            var keyboard = new InlineKeyboardMarkup(new[]
       {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_edit:{data.Id}")
                }
            });

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                "✅ Услуга удалена", replyMarkup: keyboard
            );
        }


        public async Task StartEditService(CallbackQuery query, string key, int page = 0)
        {
            Console.WriteLine($"PAGE: {page}");
            var keyboard = new List<InlineKeyboardButton[]>();
            var chatId = query.Message.Chat.Id;
            var messageId = query.Message.MessageId;

            var services = db.GetByMasterKey(key);

            int pageSize = 4;
            int start = page * pageSize;

            var pageItems = services.Skip(start).Take(pageSize).ToList();


            var text = "💼 Услуги мастера\n\n";
            for (int i = 0; i < pageItems.Count; i++)
            {
                var s = pageItems[i];

                text += $"{start + i + 1}. {s["Name"]} — {s["Price"]}₴ — {s["Duration"]} мин\n";
            }
            text += "\nВыберите услугу по ID:";



            var idsRow = new List<InlineKeyboardButton>();


            for (int i = 0; i < pageItems.Count; i++)
            {
                var s = pageItems[i];
                idsRow.Add(
                    InlineKeyboardButton.WithCallbackData(
                        $"{start + i + 1}",
                        $"master:edit_service_select:{key}:{s["idService"]}"
                    )
                );
            }

            keyboard.Add(idsRow.ToArray());

            var nav = new List<InlineKeyboardButton>();

            if (page > 0)
                nav.Add(InlineKeyboardButton.WithCallbackData(
                    "⬅️ Назад",
                    $"master:profile_services:{key}:{page - 1}"
                ));

            if (start + pageSize < services.Count)
                nav.Add(InlineKeyboardButton.WithCallbackData(
                    "➡️ Далее",
                    $"master:profile_services:{key}:{page + 1}"
                ));

            if (nav.Count > 0)
                keyboard.Add(nav.ToArray());
            keyboard.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "👤 В профиль",
                        $"master:master_profile:{key}"
                    )
                });

            await botClient.EditMessageText(
                chatId,
                messageId,
                text,
                replyMarkup: new InlineKeyboardMarkup(keyboard)
            );
        }


        public async Task ShowEditOptions(CallbackQuery query, CallBackData data)
        {
            stateService.ClearState(query.Message.Chat.Id);
            var key = data.Id;
            var serviceId = data.SubAction;
            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "✏️ Название",
                $"master:edit_title:{key}:{serviceId}"
            )
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "💰 Цена",
                $"master:edit_price:{key}:{serviceId}"
            )
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "⏱ Длительность",
                $"master:edit_duration:{key}:{serviceId}"
            )
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "⬅️ Назад",
                $"master:edit_service:{key}"
            )
        }
    });

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                "Что хотите изменить?",
                replyMarkup: keyboard
            );
        }

        public async Task ShowSchedule(CallbackQuery query, string key)//показать график работы
        {
            var masterId =db.GetMasterIdByKey(key);
            var rows = db.ExecuteQuery($@"
                SELECT * FROM ""MasterSchedule""
                WHERE ""MasterId"" = {masterId}
                ORDER BY ""DayOfWeek""
            ");

            if (rows.Rows.Count == 0)
            {

                db.CreateDefaultSchedule(masterId);
                rows = db.ExecuteQuery($@"
                    SELECT * FROM ""MasterSchedule""
                    WHERE ""MasterId"" = {masterId}
                    ORDER BY ""DayOfWeek""
                ");
            }
            string[] days = { "", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            var text = "⏰ Ваш график\n\n";
            foreach (DataRow r in rows.Rows)
            {
                int day = (int)r["DayOfWeek"];
                bool active = (bool)r["IsActive"];

                var start = r["StartTime"] == DBNull.Value
                     ? "-"
                     : ((TimeOnly)r["StartTime"]).ToString(@"hh\:mm");

                var end = r["EndTime"] == DBNull.Value
                    ? "-"
                    : ((TimeOnly)r["EndTime"]).ToString(@"hh\:mm");

                if (active)
                    text += $"{days[day]} — {start}-{end}\n";
                else
                    text += $"{days[day]} — ❌ выходной\n";
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                    new[] { InlineKeyboardButton.WithCallbackData("✏️ Изменить", $"master:schedule_edit:{key}") },
                    new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_edit:{key}") }
                });

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                text,
                replyMarkup: keyboard
            );
        }

        public async Task ShowDays(CallbackQuery query, string key)//показать дни недели для редактирования
        {
            stateService.ClearState(query.Message.Chat.Id); 
            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Пн", $"master:schedule_day:{key}:1"),
            InlineKeyboardButton.WithCallbackData("Вт", $"master:schedule_day:{key}:2"),
            InlineKeyboardButton.WithCallbackData("Ср", $"master:schedule_day:{key}:3"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Чт", $"master:schedule_day:{key}:4"),
            InlineKeyboardButton.WithCallbackData("Пт", $"master:schedule_day:{key}:5"),
            InlineKeyboardButton.WithCallbackData("Сб", $"master:schedule_day:{key}:6"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Вс", $"master:schedule_day:{key}:7"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:schedule:{key}")
        }
    });

            await botClient.EditMessageText(query.Message.Chat.Id, query.Message.MessageId, "Выберите день:", replyMarkup: keyboard);
        }

       
        public async Task ShowDay(CallbackQuery query, string key, int day)
        {
            var masterId = db.GetMasterIdByKey(key);

            var row = db.ExecuteQuery($@"
                        SELECT * FROM ""MasterSchedule""
                        WHERE ""MasterId"" = {masterId}
                        AND ""DayOfWeek"" = {day}
                    ").Rows[0];

            string[] days = { "", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };

            bool active = (bool)row["IsActive"];

            var keyboard = new List<InlineKeyboardButton[]>();

            string text = $"📅 {days[day]}\n\n";

            if (active)
            {
                var start = row["StartTime"] == DBNull.Value
                     ? "-"
                     : ((TimeOnly)row["StartTime"]).ToString(@"hh\:mm");

                var end = row["EndTime"] == DBNull.Value
                    ? "-"
                    : ((TimeOnly)row["EndTime"]).ToString(@"hh\:mm");

                text += $"⏰ {start} – {end}\n";

                keyboard.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData(
                "✏️ Изменить время",
                $"master:schedule_set:{key}:{day}"
            )
        });

                keyboard.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData(
                "❌ Сделать выходным",
                $"master:schedule_off:{key}:{day}"
            )
        });
            }
            else
            {
                text += "❌ Выходной\n";

                keyboard.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData(
                "✅ Сделать рабочим",
                $"master:schedule_on:{key}:{day}"
            )
        });
            }

            keyboard.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData(
            "⬅️ Назад",
            $"master:schedule_edit:{key}"
        )
    });

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                text,
                replyMarkup: new InlineKeyboardMarkup(keyboard)
            );
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
                                s.""Name"" as ""ServiceName"",
                                s.""Duration""
                            FROM ""Bookings"" b
                            JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                            JOIN ""Services"" s ON s.""idService"" = b.""ServiceId""
                            WHERE b.""idBooking"" = {bookingId}
                        ").Rows[0];

            long clientId = Convert.ToInt64(booking["TelegrammId"]);
            DateOnly date = (DateOnly)booking["Date"];
            TimeOnly time = (TimeOnly)booking["Time"];
            DateTime appointmentTime = date.ToDateTime(time);
            string serviceName = booking["ServiceName"].ToString();
            int durationMinutes = Convert.ToInt32(booking["Duration"]);



            await botClient.SendMessage(clientId, "✅ Ваша запись подтверждена", replyMarkup: clientKeyboard);


            await botClient.EditMessageText(
                     query.Message.Chat.Id,
                     query.Message.MessageId,
                     "✅ Запись подтверждена"
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

            db.ExecuteNonQuery($@"
                        UPDATE ""Bookings""
                        SET ""Status"" = 'cancelled'
                        WHERE ""idBooking"" = {bookingId}
                    ");

            var user = db.ExecuteQuery($@"
                SELECT u.""TelegrammId""
                FROM ""Bookings"" b
                JOIN ""Users"" u ON u.""idUser"" = b.""UserId""
                WHERE b.""idBooking"" = {bookingId}
            ").Rows[0];

            long clientId = Convert.ToInt64(user["TelegrammId"]);

            await botClient.SendMessage(
                clientId,
                $"❌ Ваша запись отменена мастером",replyMarkup:clientKeyboard
            );

            await botClient.EditMessageText(
                     query.Message.Chat.Id,
                     query.Message.MessageId,
                     "❌ Запись отменена"
                 );
        }

    }
}
