using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zapisi.Pro.CallBacks;
using Zapiski.Pro.Services;

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
        private readonly DbHelper db;

        public StateRouter(
            AdminHandler adminService,
            MasterHandler masterService,
            StateService stateService,
            ITelegramBotClient botClient,
            ScheduleService scheduleService,
            UserHandler userService)
        {
            var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");

            EnvConfig.Load(envPath);

            var host = Environment.GetEnvironmentVariable("DB_HOST");
            var user = Environment.GetEnvironmentVariable("DB_USER");
            var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");

            db = new DbHelper(
                $"Host={host};Port=5432;Username={user};Password={pass};Database=Zapisi.Pro;SSL Mode=Disable;Trust Server Certificate=true;"
            );

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

            var backBtn = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "⬅️ Назад",
                        $"master:master_edit:{key}"
                    )
                }
            });

            switch (action)
            {
                // ─────────────────────────────
                // ADMIN
                // ─────────────────────────────

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

               

                // ─────────────────────────────
                // SCHEDULE
                // ─────────────────────────────

                case "schedule_time":
                    await scheduleService.SaveTime(message, state);
                    break;

                // ─────────────────────────────
                // BOOKING DATE
                // ─────────────────────────────

                case "booking_date":
                    await userService.GetSheduleByDate(message, state);
                    break;

                // ─────────────────────────────
                // CREATE SERVICE
                // ─────────────────────────────

                case "waiting_service_title":
                    {
                        key = parts[1];

                        title = message.Text.Trim();

                        stateService.SetState(
                            message.Chat.Id,
                            $"waiting_service_price:{key}:{title}"
                        );

                        await botClient.SendMessage(
                            message.Chat.Id,
                            "💰 Введите цену:",
                            replyMarkup: backBtn
                        );

                        break;
                    }

                case "waiting_service_price":
                    {
                        key = parts[1];
                        title = parts[2];

                        if (!int.TryParse(message.Text, out int priceValue))
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "❌ Введите корректную цену",
                                replyMarkup: backBtn
                            );

                            return;
                        }

                        if (priceValue <= 0)
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "❌ Цена должна быть больше 0",
                                replyMarkup: backBtn
                            );

                            return;
                        }

                        stateService.SetState(
                            message.Chat.Id,
                            $"waiting_service_duration:{key}:{title}:{priceValue}"
                        );

                        await botClient.SendMessage(
                            message.Chat.Id,
                            "⏱ Введите длительность в минутах:",
                            replyMarkup: backBtn
                        );

                        break;
                    }

                case "edit_prepayment":
                    {
                        key = parts[1];
                        var serviceId = parts[2];

                        if (!int.TryParse(message.Text, out int percent))
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "❌ Введите число"
                            );
                            return;
                        }

                        if (percent < 0 || percent > 100)
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "❌ Процент должен быть от 0 до 100"
                            );
                            return;
                        }

                                db.ExecuteNonQuery($@"
                UPDATE ""Services""
                SET ""PrepaymentPercent"" = {percent}
                WHERE ""idService"" = {serviceId}
            ");

                        stateService.ClearState(message.Chat.Id);

                        await botClient.SendMessage(
                            message.Chat.Id,
                            "✅ Предоплата обновлена",
                            replyMarkup: backBtn
                        );

                        break;
                    }
                case "waiting_service_duration":
                    {
                        key = parts[1];
                        title = parts[2];

                        int priceValue = int.Parse(parts[3]);

                        if (!int.TryParse(message.Text, out int duration))
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "❌ Введите число минут",
                                replyMarkup: backBtn
                            );

                            return;
                        }

                        if (duration <= 0)
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "❌ Длительность должна быть больше 0",
                                replyMarkup: backBtn
                            );

                            return;
                        }

                        stateService.SetState(
                            message.Chat.Id,
                            $"waiting_service_prepayment:{key}:{title}:{priceValue}:{duration}"
                        );

                        await botClient.SendMessage(
                            message.Chat.Id,
                            "💳 Введите процент предоплаты\n\n" +
                            "От 0 до 100\n\n" +
                            "0 — без предоплаты",
                            replyMarkup: backBtn
                        );

                        break;
                    }

                case "waiting_service_prepayment":
                    {
                        key = parts[1];
                        title = parts[2];

                        int priceValue = int.Parse(parts[3]);
                        int durationValue = int.Parse(parts[4]);

                        if (!int.TryParse(message.Text, out int percent))
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "❌ Введите число",
                                replyMarkup: backBtn
                            );

                            return;
                        }

                        if (percent < 0 || percent > 100)
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "❌ Процент должен быть от 0 до 100",
                                replyMarkup: backBtn
                            );

                            return;
                        }

                        var service = new ServiceHandler();

                        service.AddService(
                            key,
                            title,
                            priceValue,
                            durationValue,
                            percent
                        );

                        stateService.ClearState(message.Chat.Id);

                        await botClient.SendMessage(
                            message.Chat.Id,
                            "✅ Услуга добавлена",
                            replyMarkup: backBtn
                        );

                        break;
                    }

                // ─────────────────────────────
                // EDIT SERVICE
                // ─────────────────────────────

                case "edit_title":
                    {
                        var serviceId = parts[2];

                        db.ExecuteNonQuery($@"
                        UPDATE ""Services""
                        SET ""Name"" = '{message.Text.Replace("'", "''")}'
                        WHERE ""idService"" = {serviceId}
                    ");

                        stateService.ClearState(message.Chat.Id);

                        await botClient.SendMessage(
                            message.Chat.Id,
                            "✅ Название обновлено",
                            replyMarkup: backBtn
                        );

                        break;
                    }

                case "edit_price":
                    {
                        var serviceId = parts[2];

                        if (!int.TryParse(message.Text, out int newPrice))
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "❌ Введите число",
                                replyMarkup: backBtn
                            );

                            return;
                        }

                        db.ExecuteNonQuery($@"
                        UPDATE ""Services""
                        SET ""Price"" = {newPrice}
                        WHERE ""idService"" = {serviceId}
                    ");

                        stateService.ClearState(message.Chat.Id);

                        await botClient.SendMessage(
                            message.Chat.Id,
                            "✅ Цена обновлена",
                            replyMarkup: backBtn
                        );

                        break;
                    }

                case "edit_duration":
                    {
                        var serviceId = parts[2];

                        if (!int.TryParse(message.Text, out int newDuration))
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                "❌ Введите число",
                                replyMarkup: backBtn
                            );

                            return;
                        }

                        db.ExecuteNonQuery($@"
                        UPDATE ""Services""
                        SET ""Duration"" = {newDuration}
                        WHERE ""idService"" = {serviceId}
                    ");

                        stateService.ClearState(message.Chat.Id);

                        await botClient.SendMessage(
                            message.Chat.Id,
                            "✅ Длительность обновлена",
                            replyMarkup: backBtn
                        );

                        break;
                    }

                // ─────────────────────────────
                // EDIT PAYMENT DETAILS
                // ─────────────────────────────

                case "edit_payment":
                    {
                        key = parts[1];

                        db.ExecuteNonQuery($@"
                        UPDATE ""Masters""
                        SET ""PaymentDetails"" =
                        '{message.Text.Replace("'", "''")}'
                        WHERE ""Key"" = '{key}'
                    ");

                        stateService.ClearState(message.Chat.Id);

                        await botClient.SendMessage(
                            message.Chat.Id,
                            "✅ Реквизиты сохранены",
                            replyMarkup: backBtn
                        );

                        break;
                    }
            }
        }
    }
}
