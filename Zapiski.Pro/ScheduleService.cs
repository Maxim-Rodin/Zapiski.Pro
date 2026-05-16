using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zapisi.Pro.CallBacks;
using Zapisi.Pro.State;

namespace Zapisi.Pro
{
    internal class ScheduleService
    {
        private readonly ITelegramBotClient botClient;
        private readonly DbHelper db;
        
        private readonly StateService stateService;

        public ScheduleService(ITelegramBotClient botClient, DbHelper db, StateService stateService)
        {
            this.botClient = botClient;
            this.db = db;
            this.stateService = stateService;
        }
        public async Task SetDayOff(CallbackQuery query, CallBackData data)
        {
            var key = data.Id;
            var day = int.Parse(data.SubAction);

            var masterId = db.GetMasterIdByKey(key);

            db.ExecuteNonQuery($@"
                UPDATE ""MasterSchedule""
                SET ""IsActive"" = false
                WHERE ""MasterId"" = {masterId}
                AND ""DayOfWeek"" = {day}
            ");
            var keyboard = new InlineKeyboardMarkup(new[]
                 {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:schedule_day:{key}:{day}")
                    }
                });
            await botClient.EditMessageText(
                       query.Message.Chat.Id,
                       query.Message.MessageId,
                       "✅ Готово! День теперь выходной",
                       replyMarkup: keyboard
                   );
        }

        public async Task SetDayOn(CallbackQuery query, CallBackData data)
        {
            var key = data.Id;
            var day = int.Parse(data.SubAction);

            var masterId = db.GetMasterIdByKey(key);

            db.ExecuteNonQuery($@"
                        UPDATE ""MasterSchedule""
                        SET ""IsActive"" = true
                        WHERE ""MasterId"" = {masterId}
                        AND ""DayOfWeek"" = {day}
                    ");
            var keyboard = new InlineKeyboardMarkup(new[]
                 {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:schedule_edit:{key}")
                    }
                });


            await botClient.EditMessageText(
                        query.Message.Chat.Id,
                        query.Message.MessageId,
                        "✅ Готово! День теперь рабочий",
                        replyMarkup: keyboard
                    );
        }

        public async Task StartSetTime(CallbackQuery query, CallBackData data)
        {
            var key = data.Id;
            var day = data.SubAction;

            stateService.SetState(
                query.From.Id,
                $"schedule_time:{key}:{day}"
            );

            await botClient.EditMessageText(
                query.Message.Chat.Id,
                query.Message.MessageId,
                "Введите время: 10:00-18:00"
            );
        }
        public async Task SaveTime(Message message, string state)
        {
            Console.WriteLine("INPUT RAW = [" + message.Text + "]");
            Console.WriteLine("STATE RAW = [" + state + "]");
            var parts = state?.Split(':');

            var key = parts[1];
            var day = int.Parse(parts[2]);
            var btn = InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"master:schedule_edit:{key}");
            var input = message.Text.Trim();
            Console.WriteLine("INPUT old = [" + input + "]");
            input = input.Replace("–", "-").Replace("—", "-").Replace(" ", ""); ;
            Console.WriteLine("INPUT CLEAN = [" + input + "]");

            if (!input.Contains("-"))
            {
                await botClient.SendMessage(message.Chat.Id,
                    "❌ Формат: 10:00-18:00", replyMarkup: btn);
                return;
            }

            var times = input.Split('-');

            if (times.Length != 2)
            {
                await botClient.SendMessage(message.Chat.Id,
                    "❌ Неверный формат. Пример: 10:00-18:00", replyMarkup: btn);
                return;
            }


            if (!TimeSpan.TryParse(times[0], out var start) ||
        !TimeSpan.TryParse(times[1], out var end))
            {
                await botClient.SendMessage(message.Chat.Id,
                    "❌ Время введено неверно",
                    replyMarkup: btn);
                return;
            }


            if (start >= end)
            {
                await botClient.SendMessage(message.Chat.Id,
                    "❌ Время начала должно быть раньше конца", replyMarkup: btn);
                return;
            }

            
            var masterId =db.GetMasterIdByKey(key);

            db.ExecuteNonQuery($@"
                        UPDATE ""MasterSchedule""
                        SET ""StartTime"" = '{start}',
                            ""EndTime"" = '{end}',
                            ""IsActive"" = true
                        WHERE ""MasterId"" = {masterId}
                        AND ""DayOfWeek"" = {day}
                    ");

            stateService.ClearState(message.Chat.Id);

            await botClient.SendMessage(message.Chat.Id, "✅ Время обновлено", replyMarkup: btn);
        }
        
    }

}
