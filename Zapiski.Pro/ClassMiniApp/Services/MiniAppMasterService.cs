using Zapiski.Pro.MiniApp.Models;
using Zapiski.Pro.MiniApp.Repositories;

namespace Zapiski.Pro.MiniApp.Services
{
    public class MiniAppMasterService
    {
        private readonly MiniAppMasterRepository repository;

        public MiniAppMasterService(MiniAppMasterRepository repository)
        {
            this.repository = repository;
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
    }
}
