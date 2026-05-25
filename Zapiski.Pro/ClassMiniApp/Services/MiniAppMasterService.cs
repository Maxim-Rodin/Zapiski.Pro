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
    }
}