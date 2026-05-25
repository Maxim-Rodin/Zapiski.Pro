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
    }
}
