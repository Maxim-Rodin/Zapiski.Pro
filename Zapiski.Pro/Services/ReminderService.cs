using Hangfire;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Zapisi.Pro;

namespace Zapiski.Pro.Services
{
    internal static class ReminderService
    {
        public static ITelegramBotClient BotClient;

        public static async Task SendReminder(long chatId, string text)
        {
            Console.WriteLine($"REMINDER FIRED -> {chatId}");

            try
            {
                var keyboard = new InlineKeyboardMarkup(new[]
                {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ В профиль", "client:menu")
            }
        });

                await BotClient.SendMessage(chatId, text, replyMarkup: keyboard);

                Console.WriteLine("MESSAGE SENT");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
    }
}
