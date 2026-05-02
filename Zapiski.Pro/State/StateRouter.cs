using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zapisi.Pro.CallBacks;

namespace Zapisi.Pro.State
{
    internal class StateRouter
    {
        private readonly AdminHandler adminService;
        private readonly MasterHandler materService;
        private readonly StateService stateService;
        private readonly ITelegramBotClient botClient;
        private readonly ScheduleService scheduleService;
        private readonly UserHandler userService;
        private readonly DbHelper db ;

        public StateRouter(AdminHandler adminService,MasterHandler masterService ,StateService stateService ,ITelegramBotClient botClient, ScheduleService scheduleService ,UserHandler userService)
        {
            var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            DotNetEnv.Env.Load(envPath);
            var host = Environment.GetEnvironmentVariable("DB_HOST");
            db = new DbHelper($"Host={host};Port=5432;Username=postgres;Password=admin;Database=Zapisi.Pro");
            this.adminService = adminService;
            this.materService = masterService;
            this.stateService = stateService;
            this.botClient = botClient;
            this.scheduleService = scheduleService;
            this.userService = userService;
            

        }

        public async Task Handle(string state, Message message)
        {
            Console.WriteLine("STATE = " + state);
            var parts = state.Split(':');

            var action = parts.Length > 0 ? parts[0] : null;
            var key = parts.Length > 1 ? parts[1] : null;
            var title = parts.Length > 2 ? parts[2] : null;
            var price = parts.Length > 3 ? parts[3] : null;
            var btn = InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:master_edit:{key}");
            switch (action)
            {
                case "waiting_broadcast_text":
                    await adminService.SendBroadcast(message);
                    break;
                case "waiting_master_id":
                    await adminService.SetMaster(message);
                    break;
                case "waiting_master_key":
                    await adminService.CreateMasterWithKey(message);
                    break;
                case "waiting_Dmaster_id":
                    await adminService.DeletMasterWithId(message);
                    break;
                case "waiting_description":
                    await materService.SaveDescription(message, key);
                    break;

                case "waiting_contacts":
                    await materService.SaveContacts(message, key);
                    break;
                case "waiting_name":
                    await materService.SaveName(message,key);
                    break;
                case "schedule_time":
                    await scheduleService.SaveTime(message, state);
                    break;

                case "waiting_service_title":
                     key = parts[1];
                     title = message.Text;
                    

                    stateService.SetState(message.Chat.Id, $"waiting_service_price:{key}:{title}");

                    await botClient.SendMessage(message.Chat.Id, "💰 Введите цену:",replyMarkup:btn);
                   
                    break;
                case "booking_date":
                    await userService.GetSheduleByDate(message, state);
                    break;



                case "waiting_service_price":
                    key = parts[1];
                    title = parts[2];
                    

                    if (!int.TryParse(message.Text, out int priceValue))
                    {
                        await botClient.SendMessage(message.Chat.Id, "❌ Введите корректную цену", replyMarkup: btn);
                        return;
                    }

                    stateService.SetState(message.Chat.Id, $"waiting_service_duration:{key}:{title}:{priceValue}");

                    await botClient.SendMessage(message.Chat.Id, "⏱ Введите длительность (в минутах):");
                    break;
                case "waiting_service_duration":
                    key = parts[1];
                    title = parts[2];
                    
                    var priceV = int.Parse(parts[3]);

                    if (!int.TryParse(message.Text, out int duration))
                    {
                        await botClient.SendMessage(message.Chat.Id, "❌ Введите число минут", replyMarkup: btn);
                        return;
                    }
                    if (duration <= 0)
                    {
                        await botClient.SendMessage(message.Chat.Id, "❌ Длительность должна быть больше 0 минут");
                        return;
                    }
                    var service = new ServiceHandler();
                    service.AddService(key, title, priceV, duration);

                    stateService.ClearState(message.Chat.Id);

                    await botClient.SendMessage(message.Chat.Id, "✅ Услуга добавлена", replyMarkup: btn);

                    break;

                case "edit_title":
                    {
                        var serviceId = parts[2];

                        db.ExecuteNonQuery($@"
                            UPDATE ""Services""
                            SET ""Title"" = '{message.Text}'
                            WHERE ""idService"" = {serviceId}
                        ");

                        await botClient.SendMessage(message.Chat.Id, "✅ Название обновлено", replyMarkup: btn);
                        break;
                    }

                case "edit_price":
                    {
                        var serviceId = parts[2];

                        db.ExecuteNonQuery($@"
                                UPDATE ""Services""
                                SET ""Price"" = {message.Text}
                                WHERE ""idService"" = {serviceId}
                            ");

                        await botClient.SendMessage(message.Chat.Id, "✅ Цена обновлена",replyMarkup:btn);
                        break;
                    }

                case "edit_duration":
                    {
                        var serviceId = parts[2];

                        db.ExecuteNonQuery($@"
                                UPDATE ""Services""
                                SET ""Duration"" = {message.Text}
                                WHERE ""idService"" = {serviceId}
                            ");

                        await botClient.SendMessage(message.Chat.Id, "✅ Длительность обновлена", replyMarkup: btn);
                        break;
                    }

            }
            
        }
    }
}
