using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static async Task AutoCompleteBooking(int bookingId, long clientId, string? serviceName)
        {
            try
            {
                // Проверяем, что запись всё ещё в статусе confirmed (не отменена)
                var currentStatus = Db.ExecuteScalar($@"
            SELECT ""Status"" FROM ""Bookings"" 
            WHERE ""idBooking"" = {bookingId}
        ").ToString();

                if (currentStatus == "confirmed")
                {
                    // Обновляем статус на completed
                    Db.ExecuteNonQuery($@"
                UPDATE ""Bookings""
                SET ""Status"" = 'completed'
                WHERE ""idBooking"" = {bookingId}
            ");

                    // Отправляем клиенту благодарность
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🏠 В меню", "client:menu")
                }
            });

                    await BotClient.SendMessage(
                        clientId,
                        $"✨ Спасибо за визит!\n\n" +
                        $"Надеемся, вам понравилась услуга «{serviceName}» 😊\n\n" +
                        $"Будем рады видеть вас снова!",
                        replyMarkup: keyboard
                    );

                    Console.WriteLine($"[AutoComplete] Запись {bookingId} автоматически завершена");
                }
                else
                {
                    Console.WriteLine($"[AutoComplete] Запись {bookingId} уже имеет статус {currentStatus}, завершение не требуется");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoComplete] Ошибка при завершении записи {bookingId}: {ex.Message}");
            }
        }

    }
}
