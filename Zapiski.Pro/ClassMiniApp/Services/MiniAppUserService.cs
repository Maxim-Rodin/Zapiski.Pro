using Zapiski.Pro.ClassMiniApp.Models;
using Zapiski.Pro.ClassMiniApp.Repositories;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Zapiski.Pro.ClassMiniApp.Services
{
    public class MiniAppUserService
    {
        private readonly MiniAppUserRepository repository;
        private readonly ITelegramBotClient botClient;

        public MiniAppUserService(
            MiniAppUserRepository repository,
            ITelegramBotClient botClient)
        {
            this.repository = repository;
            this.botClient = botClient;
        }

        public MiniAppUserDashboardDto? GetDashboard(long telegramId)
        {
            if (telegramId <= 0)
                return null;

            return repository.GetDashboard(telegramId);
        }

        public MiniAppPersonalDataConsentDto GetPersonalDataConsent(long telegramId)
        {
            if (telegramId <= 0)
                return new MiniAppPersonalDataConsentDto();

            return repository.GetPersonalDataConsent(telegramId);
        }

        public MiniAppPersonalDataConsentDto AcceptPersonalDataConsent(long telegramId)
        {
            if (telegramId <= 0)
                return new MiniAppPersonalDataConsentDto();

            return repository.AcceptPersonalDataConsent(telegramId);
        }

        public Task<MiniAppBecomeMasterResult> BecomeMaster(long telegramId, MiniAppBecomeMasterRequest request)
        {
            if (telegramId <= 0)
            {
                return Task.FromResult(new MiniAppBecomeMasterResult
                {
                    Success = false,
                    Message = "Откройте регистрацию из Telegram"
                });
            }

            var registrationSource = string.Equals(request.Source, "landing", StringComparison.OrdinalIgnoreCase)
                ? "landing"
                : "direct";

            var result = repository.BecomeMaster(telegramId, request.Key, registrationSource);

            if (!result.Success || !result.Created || string.IsNullOrWhiteSpace(result.MasterKey))
                return Task.FromResult(result);

            _ = SendBecomeMasterNotification(telegramId, result.MasterKey);
            return Task.FromResult(result);
        }

        private async Task SendBecomeMasterNotification(long telegramId, string masterKey)
        {
            try
            {
                var miniAppUrl = Environment.GetEnvironmentVariable("MINIAPP_URL")
                    ?? "https://app-zapisi-pro.site";
                var masterPanelUrl =
                    $"{miniAppUrl.TrimEnd('/')}/master/{Uri.EscapeDataString(masterKey)}/onboarding";

                await botClient.SendMessage(
                    telegramId,
                    "🎉 Поздравляем, вы стали мастером Zapisi.Pro!\n\n" +
                    "Мастер-профиль создан, а пробный период на 30 дней уже активирован.\n" +
                    "Продолжите короткую настройку: заполните профиль, добавьте услуги и рабочее время.",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithWebApp(
                                "🚀 Продолжить настройку",
                                new WebAppInfo(masterPanelUrl)
                            )
                        }
                    })
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BecomeMaster Notify ERROR] {ex.Message}");
            }
        }

        public MiniAppMasterKeyAvailabilityDto CheckMasterKey(string key)
        {
            return repository.CheckMasterKey(key);
        }

        public async Task<bool> CancelBooking(long telegramId, int bookingId)
        {
            if (telegramId <= 0 || bookingId <= 0)
                return false;

            return await repository.CancelBooking(telegramId, bookingId);
        }

        public List<MiniAppBookingSlotDto> GetBookingSlots(string masterKey, int serviceId, string date)
        {
            if (string.IsNullOrWhiteSpace(masterKey) || serviceId <= 0 || string.IsNullOrWhiteSpace(date))
                return new List<MiniAppBookingSlotDto>();

            return repository.GetBookingSlots(masterKey.Trim(), serviceId, date.Trim());
        }

        public async Task<MiniAppCreateBookingResult> CreateBooking(long telegramId, MiniAppCreateBookingRequest request)
        {
            if (telegramId <= 0)
                return new MiniAppCreateBookingResult { Success = false, Message = "Откройте запись из Telegram" };

            return await repository.CreateBooking(telegramId, request);
        }

        public async Task<bool> MarkBookingPaid(long telegramId, int bookingId)
        {
            if (telegramId <= 0 || bookingId <= 0)
                return false;

            return await repository.MarkBookingPaid(telegramId, bookingId);
        }
    }
}
