using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Zapiski.Pro.MiniApp.Models;
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

            app.MapGet("/api/master/{key}/clients", (string key) =>
            {
                var clients = masterService.GetClients(key);

                if (clients == null)
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Мастер не найден"
                    });

                return Results.Ok(clients);
            });

            app.MapGet("/api/master/{key}/stats", (string key) =>
            {
                var stats = masterService.GetStats(key);

                if (stats == null)
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Мастер не найден"
                    });

                return Results.Ok(stats);
            });

            app.MapGet("/api/master/{key}/services", (string key) =>
            {
                var services = masterService.GetServices(key);

                if (services == null)
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Мастер не найден"
                    });

                return Results.Ok(services);
            });

            app.MapPost("/api/master/{key}/services", (string key, MiniAppCreateMasterServiceRequest request) =>
            {
                var result = masterService.CreateService(key, request);

                if (!result.Success)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            });

            app.MapPut("/api/master/{key}/services/{serviceId:int}", (string key, int serviceId, MiniAppCreateMasterServiceRequest request) =>
            {
                var result = masterService.UpdateService(key, serviceId, request);

                if (!result.Success)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            });

            app.MapDelete("/api/master/{key}/services/{serviceId:int}", (string key, int serviceId) =>
            {
                var result = masterService.DeleteService(key, serviceId);

                if (!result.Success)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            });
        }
    }
}
