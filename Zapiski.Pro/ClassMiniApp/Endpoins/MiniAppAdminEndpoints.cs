using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Telegram.Bot.Types;
using Zapiski.Pro.MiniApp.Models;
using Zapiski.Pro.MiniApp.Services;

namespace Zapiski.Pro.MiniApp.Endpoints;

public static class MiniAppAdminEndpoints
{
    public static void MapMiniAppAdminEndpoints(
        this WebApplication app,
        MiniAppAdminService adminService)
    {
       

        app.MapGet("/api/admin/masters", async () =>
        {
            var result =  adminService.GetMasters();
            return Results.Ok(result);
        });

        app.MapGet("/api/admin/users", async () =>
        {
            var result =  adminService.GetUsers();
            return Results.Ok(result);
        });
        app.MapPost("/api/admin/masters", async (
     HttpContext context,
     MiniAppCreateMasterRequest request) =>
        {
            if (!IsAdmin(context, adminService))
                return Results.Unauthorized();

            var result = await adminService.CreateMaster(request);

            if (!result.Success)
                return Results.BadRequest(result);

            return Results.Ok(result);
        });
        app.MapDelete("/api/admin/masters/{id:int}", async (
     HttpContext context,
     int id) =>
        {
            if (!IsAdmin(context, adminService))
                return Results.Unauthorized();

            var result = await adminService.DeleteMaster(id);

            if (!result.Success)
                return Results.BadRequest(result);

            return Results.Ok(result);
        });

        app.MapGet("/api/admin/stats", (HttpContext context) =>
        {
            if (!IsAdmin(context, adminService))
                return Results.Unauthorized();

            var result = adminService.GetStats();
            return Results.Ok(result);
        });
        static bool IsAdmin(HttpContext context, MiniAppAdminService adminService)
        {
            var telegramIdText = context.Request.Headers["X-Telegram-Id"].FirstOrDefault();

            if (!long.TryParse(telegramIdText, out var telegramId))
                return false;

            return adminService.IsAdmin(telegramId);
        }
    }
}