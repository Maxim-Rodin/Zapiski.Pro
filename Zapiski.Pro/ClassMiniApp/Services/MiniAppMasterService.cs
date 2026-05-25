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
    }
}
