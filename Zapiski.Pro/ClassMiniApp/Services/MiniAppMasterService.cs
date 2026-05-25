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

        public List<MiniAppMasterServiceDto>? GetServices(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (repository.GetMasterByKey(key.Trim()) == null)
                return null;

            return repository.GetServices(key.Trim());
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
    }
}
