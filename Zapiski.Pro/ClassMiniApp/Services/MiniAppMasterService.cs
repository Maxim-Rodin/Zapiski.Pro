using Microsoft.AspNetCore.Http;
using Zapiski.Pro.ClassMiniApp.Services;
using Zapiski.Pro.MiniApp.Models;
using Zapiski.Pro.MiniApp.Repositories;

namespace Zapiski.Pro.MiniApp.Services
{
    public class MiniAppMasterService
    {
        private readonly MiniAppMasterRepository repository;
        private readonly CloudinaryImageService imageService;

        public MiniAppMasterService(MiniAppMasterRepository repository, CloudinaryImageService imageService)
        {
            this.repository = repository;
            this.imageService = imageService;
        }

        public MiniAppMasterProfileDto? GetMasterProfile(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            return repository.GetMasterByKey(key.Trim());
        }

        public List<MiniAppMasterClientDto>? GetClients(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (repository.GetMasterByKey(key.Trim()) == null)
                return null;

            return repository.GetClients(key.Trim());
        }

        public MiniAppMasterStatsDto? GetStats(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (repository.GetMasterByKey(key.Trim()) == null)
                return null;

            return repository.GetStats(key.Trim());
        }

        public List<MiniAppMasterScheduleDayDto>? GetSchedule(string key, long telegramId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (repository.GetMasterByKey(key.Trim()) == null)
                return null;

            return repository.GetSchedule(key.Trim(), telegramId);
        }

        public MiniAppMasterActionResult UpdateScheduleDay(string key, long telegramId, int day, MiniAppUpdateScheduleDayRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.UpdateScheduleDay(key.Trim(), telegramId, day, request);
        }

        public MiniAppMasterScheduleModeDto? GetScheduleMode(string key, long telegramId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (repository.GetMasterByKey(key.Trim()) == null)
                return null;

            return repository.GetScheduleMode(key.Trim(), telegramId);
        }

        public MiniAppMasterActionResult UpdateScheduleMode(string key, long telegramId, MiniAppUpdateScheduleModeRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.UpdateScheduleMode(key.Trim(), telegramId, request);
        }

        public List<MiniAppManualSlotDto>? GetManualSlots(string key, long telegramId, string date)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (repository.GetMasterByKey(key.Trim()) == null)
                return null;

            return repository.GetManualSlots(key.Trim(), telegramId, date);
        }

        public MiniAppMasterActionResult CreateManualSlot(string key, long telegramId, MiniAppCreateManualSlotRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.CreateManualSlot(key.Trim(), telegramId, request);
        }

        public MiniAppMasterActionResult DeleteManualSlot(string key, long telegramId, int slotId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.DeleteManualSlot(key.Trim(), telegramId, slotId);
        }

        public MiniAppMasterActionResult ClearManualSlotsDay(string key, long telegramId, string date)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.ClearManualSlotsDay(key.Trim(), telegramId, date);
        }

        public List<MiniAppMasterBookingDto>? GetBookings(string key, long telegramId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (repository.GetMasterByKey(key.Trim()) == null)
                return null;

            return repository.GetBookings(key.Trim(), telegramId);
        }

        public MiniAppMasterActionResult CreateTimeBlock(string key, long telegramId, MiniAppCreateTimeBlockRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.CreateTimeBlock(key.Trim(), telegramId, request);
        }

        public MiniAppMasterActionResult DeleteTimeBlock(string key, long telegramId, int blockId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.DeleteTimeBlock(key.Trim(), telegramId, blockId);
        }

        public async Task<MiniAppMasterActionResult> AcceptBooking(string key, long telegramId, int bookingId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return await repository.AcceptBooking(key.Trim(), telegramId, bookingId);
        }

        public async Task<MiniAppMasterActionResult> CancelBooking(string key, long telegramId, int bookingId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return await repository.CancelBooking(key.Trim(), telegramId, bookingId);
        }

        public async Task<MiniAppMasterActionResult> AcceptPayment(string key, long telegramId, int bookingId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return await repository.AcceptPayment(key.Trim(), telegramId, bookingId);
        }

        public async Task<MiniAppMasterActionResult> RejectPayment(string key, long telegramId, int bookingId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return await repository.RejectPayment(key.Trim(), telegramId, bookingId);
        }

        public List<MiniAppMasterServiceDto>? GetServices(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (repository.GetMasterByKey(key.Trim()) == null)
                return null;

            return repository.GetServices(key.Trim());
        }

        public MiniAppMasterActionResult UpdateProfile(string key, long telegramId, MiniAppUpdateMasterProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.UpdateProfile(key.Trim(), telegramId, request);
        }

        public List<MiniAppMasterAddressDto>? GetAddresses(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (repository.GetMasterByKey(key.Trim()) == null)
                return null;

            return repository.GetAddresses(key.Trim());
        }

        public MiniAppMasterActionResult CreateAddress(string key, long telegramId, MiniAppMasterAddressRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.CreateAddress(key.Trim(), telegramId, request);
        }

        public MiniAppMasterActionResult DeleteAddress(string key, long telegramId, int addressId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.DeleteAddress(key.Trim(), telegramId, addressId);
        }

        public MiniAppMasterActionResult CreateService(string key, MiniAppCreateMasterServiceRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.CreateService(key.Trim(), request);
        }

        public MiniAppMasterActionResult DeleteService(string key, int serviceId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.DeleteService(key.Trim(), serviceId);
        }

        public MiniAppMasterActionResult UpdateService(string key, int serviceId, MiniAppCreateMasterServiceRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return repository.UpdateService(key.Trim(), serviceId, request);
        }

        public async Task<MiniAppMasterActionResult> SendBroadcast(string key, long telegramId, MiniAppMasterBroadcastRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return await repository.SendBroadcast(key.Trim(), telegramId, request);
        }

        public async Task<MiniAppMasterActionResult> SendPersonalBroadcast(string key, long telegramId, long clientTelegramId, MiniAppMasterBroadcastRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new MiniAppMasterActionResult { Success = false, Message = "Мастер не найден" };

            return await repository.SendPersonalBroadcast(key.Trim(), telegramId, clientTelegramId, request);
        }

        public MiniAppMasterActionResult AddClient(string key, long telegramId, MiniAppAddMasterClientRequest request) //метод прокладка для добавления ручного клиентов 
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Мастер не найден"
                };
                

            }
            if (request == null || string.IsNullOrWhiteSpace(request.Search))
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Введите username или телефон клиента"
                };

            request.Search = request.Search.Trim();

            return repository.AddClient(key.Trim(), telegramId, request);
            
        }

        public MiniAppMasterAvatarResult UpdateAvatarUrl(string key, long telegramId, string avatarUrl)//метод прокладка для проверки валидности данных
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return new MiniAppMasterAvatarResult
                {
                    Success = false,
                    Message = "Мастер не найден"
                };
            }
            if (telegramId <= 0)
            {
                return new MiniAppMasterAvatarResult
                {
                        Success = false,
                        Message = "Отркойте профиль из телеграмма"
                };
                    
            }

            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                return new MiniAppMasterAvatarResult
                {
                        Success = false,
                        Message = "Фото не загружено"
                };
            }

            return repository.UpdateAvatarUrl(key.Trim(), telegramId, avatarUrl.Trim());
            
        }
        public List<MiniAppPortfolioPhotoDto> GetPortfolioPhotos(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new List<MiniAppPortfolioPhotoDto>();

            return repository.GetPortfolioPhotos(key.Trim());
        }

        public async Task<MiniAppPortfolioPhotoResult> UploadPortfolioPhoto(string key, long telegramId, IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return new MiniAppPortfolioPhotoResult
                {
                    Success = false,
                    Message = "Мастер не найден"
                };
            }

            if (telegramId <= 0)
            {
                return new MiniAppPortfolioPhotoResult
                {
                    Success = false,
                    Message = "Откройте профиль из Telegram"
                };
            }

            if (file == null || file.Length == 0)
            {
                return new MiniAppPortfolioPhotoResult
                {
                    Success = false,
                    Message = "Фото не выбрано"
                };
            }

            var master = repository.GetMasterByKey(key.Trim());

            if (master == null)
            {
                return new MiniAppPortfolioPhotoResult
                {
                    Success = false,
                    Message = "Мастер не найден"
                };
            }

            if (master.TelegramId != telegramId)
            {
                return new MiniAppPortfolioPhotoResult
                {
                    Success = false,
                    Message = "Нет доступа к портфолио"
                };
            }

            if (repository.GetPortfolioPhotosCount(master.Id) >= 9)
            {
                return new MiniAppPortfolioPhotoResult
                {
                    Success = false,
                    Message = "Можно загрузить максимум 9 фото"
                };
            }

            try
            {
                var uploadResult = await imageService.UploadPortfolioPhoto(master.Id, file);

                var photo = repository.AddPortfolioPhoto(
                    master.Id,
                    uploadResult.ImageUrl,
                    uploadResult.PublicId);

                return new MiniAppPortfolioPhotoResult
                {
                    Success = true,
                    Message = "Фото добавлено в портфолио",
                    Photo = photo
                };
            }
            catch (Exception ex)
            {
                return new MiniAppPortfolioPhotoResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<MiniAppMasterActionResult> DeletePortfolioPhoto(string key, long telegramId, int photoId)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Мастер не найден"
                };
            }

            if (telegramId <= 0)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Откройте профиль из Telegram"
                };
            }

            if (photoId <= 0)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Фото не найдено"
                };
            }

            var master = repository.GetMasterByKey(key.Trim());

            if (master == null)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Мастер не найден"
                };
            }

            if (master.TelegramId != telegramId)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Нет доступа к портфолио"
                };
            }

            var photo = repository.GetPortfolioPhotoForMaster(master.Id, photoId);

            if (photo == null)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Фото не найдено"
                };
            }

            try
            {
                await imageService.DeletePhoto(photo.PublicId);
                repository.DeletePortfolioPhoto(master.Id, photoId);

                return new MiniAppMasterActionResult
                {
                    Success = true,
                    Message = "Фото удалено"
                };
            }
            catch (Exception ex)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public MiniAppMasterActionResult ReorderPortfolioPhotos(string key, long telegramId, MiniAppReorderPortfolioRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Мастер не найден"
                };
            }

            if (telegramId <= 0)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Откройте профиль из Telegram"
                };
            }

            if (request == null || request.PhotoIds == null || request.PhotoIds.Count == 0)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Передайте порядок фото"
                };
            }

            if (request.PhotoIds.Count > 9)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Можно сортировать максимум 9 фото"
                };
            }

            var distinctPhotoIds = request.PhotoIds.Distinct().ToList();

            if (distinctPhotoIds.Count != request.PhotoIds.Count)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "В списке есть повторяющиеся фото"
                };
            }

            var master = repository.GetMasterByKey(key.Trim());

            if (master == null)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Мастер не найден"
                };
            }

            if (master.TelegramId != telegramId)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Нет доступа к портфолио"
                };
            }

            var currentCount = repository.GetPortfolioPhotosCount(master.Id);

            if (distinctPhotoIds.Count != currentCount)
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "Передан неполный список фото"
                };
            }

            if (!repository.PortfolioPhotosBelongToMaster(master.Id, distinctPhotoIds))
            {
                return new MiniAppMasterActionResult
                {
                    Success = false,
                    Message = "В списке есть чужие или несуществующие фото"
                };
            }

            repository.UpdatePortfolioSortOrder(master.Id, distinctPhotoIds);

            return new MiniAppMasterActionResult
            {
                Success = true,
                Message = "Порядок фото обновлен"
            };
        }
    }
}
