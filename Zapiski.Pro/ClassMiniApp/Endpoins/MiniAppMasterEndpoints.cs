using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Zapiski.Pro.MiniApp.Services;

namespace Zapiski.Pro.MiniApp.Endpoints
{
    public static class MiniAppMasterEndpoints
    {
        public static void MapMiniAppMasterEndpoints(
            this WebApplication app,
            MiniAppMasterService masterService)
        {
            app.MapGet("/api/master/{key}", (string key) =>
            {
                var master = masterService.GetMasterProfile(key);

                if (master == null)
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Мастер не найден"
                    });

                return Results.Ok(master);
            });
        }
    }
}