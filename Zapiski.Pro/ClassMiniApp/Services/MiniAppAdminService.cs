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

            repository.CreateMaster(request.TelegramId, key);

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