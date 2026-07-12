using Zapiski.Pro.ClassMiniApp.Models;
using Zapiski.Pro.ClassMiniApp.Repositories;

namespace Zapiski.Pro.ClassMiniApp.Services
{
    public class MiniAppUserService
    {
        private readonly MiniAppUserRepository repository;

        public MiniAppUserService(MiniAppUserRepository repository)
        {
            this.repository = repository;
        }

        public MiniAppUserDashboardDto? GetDashboard(long telegramId)
        {
            if (telegramId <= 0)
                return null;

            return repository.GetDashboard(telegramId);
        }

        public MiniAppBecomeMasterResult BecomeMaster(long telegramId, MiniAppBecomeMasterRequest request)
        {
            if (telegramId <= 0)
            {
                return new MiniAppBecomeMasterResult
                {
                    Success = false,
                    Message = "Откройте регистрацию из Telegram"
                };
            }

            return repository.BecomeMaster(telegramId, request.Key);
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
