using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Zapiski.Pro.MiniApp.Models;
using Zapiski.Pro.MiniApp.Repositories;

namespace Zapiski.Pro.MiniApp.Services
{
    public class MiniAppAdminService
    {
        private readonly MiniAppAdminRepository repository;
        private readonly ITelegramBotClient botClient;

        public MiniAppAdminService(
            MiniAppAdminRepository repository,
            ITelegramBotClient botClient)
        {
            this.repository = repository;
            this.botClient = botClient;
        }

        public MiniAppAdminStatsDto GetStats()
        {
            return repository.GetStats();
        }

        public List<MiniAppMasterDto> GetMasters()
        {
            return repository.GetMasters();
        }

        public List<MiniAppUserDto> GetUsers()
        {
            return repository.GetUsers();
        }

        public async Task<MiniAppActionResultDto> CreateMaster(
            MiniAppCreateMasterRequest request)
        {
            if (request.TelegramId <= 0)
            {
                return MiniAppActionResultDto.Fail("Некорректный Telegram ID");
            }

            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return MiniAppActionResultDto.Fail("Ключ мастера не указан");
            }

            var key = request.Key.Trim();

            if (!repository.UserExistsByTelegramId(request.TelegramId))
            {
                return MiniAppActionResultDto.Fail("Пользователь не найден");
            }

            if (repository.MasterKeyExists(key))
            {
                return MiniAppActionResultDto.Fail("Такой ключ уже существует");
            }

            if (request.SubscriptionMonths != 0 && request.SubscriptionMonths != 1 &&
                request.SubscriptionMonths != 3 && request.SubscriptionMonths != 12)
            {
                return MiniAppActionResultDto.Fail("Можно выдать подписку только на 1, 3 или 12 месяцев");
            }

            repository.CreateMaster(request.TelegramId, key, request.IsFounder, request.SubscriptionMonths);

            var masterId = repository.GetMasterIdByKey(key);

            repository.CreateDefaultSchedule(masterId);
            try
            {
                await botClient.SendMessage(
                    request.TelegramId,
                    "🎉 Поздравляем! Вы назначены мастером в Zapisi.Pro.",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "👤 Открыть профиль",
                    $"master:master_panel:{key}"
                )
            }
                    })
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MiniApp CreateMaster Notify ERROR] {ex.Message}");
            }

            return MiniAppActionResultDto.Ok("Мастер создан");
        }

        public MiniAppActionResultDto GrantSubscription(int masterId, MiniAppAdminGrantSubscriptionRequest request)
        {
            if (masterId <= 0)
            {
                return MiniAppActionResultDto.Fail("Некорректный ID мастера");
            }

            if (request.SubscriptionMonths != 0 && request.SubscriptionMonths != 1 &&
                request.SubscriptionMonths != 3 && request.SubscriptionMonths != 12)
            {
                return MiniAppActionResultDto.Fail("Можно выдать подписку только на 1, 3 или 12 месяцев");
            }

            var master = repository.GetMasterById(masterId);

            if (master == null)
            {
                return MiniAppActionResultDto.Fail("Мастер не найден");
            }

            repository.UpdateMasterSubscription(masterId, request.IsFounder, request.SubscriptionMonths);

            if (request.IsFounder)
                return MiniAppActionResultDto.Ok("Мастеру выдан доступ первого мастера");

            if (request.SubscriptionMonths > 0)
                return MiniAppActionResultDto.Ok($"Подписка продлена на {request.SubscriptionMonths} мес.");

            return MiniAppActionResultDto.Ok("Доступ мастера обновлён");
        }

        public async Task<MiniAppActionResultDto> DeleteMaster(int masterId)
        {
            if (masterId <= 0)
            {
                return MiniAppActionResultDto.Fail("Некорректный ID мастера");
            }

            var master = repository.GetMasterById(masterId);

            if (master == null)
            {
                return MiniAppActionResultDto.Fail("Мастер не найден");
            }

            repository.DeleteMaster(masterId, master.TelegramId);

            await botClient.SendMessage(
                master.TelegramId,
                "⚠️ Поддержка вашего профиля мастера в Zapisi.Pro была прекращена.\n\n" +
                "Если это ошибка — свяжитесь с поддержкой."
            );

            return MiniAppActionResultDto.Ok("Мастер удалён");
        }

        public bool IsAdmin(long telegramId)
        {
            return repository.IsAdmin(telegramId);
        }
    }
}
