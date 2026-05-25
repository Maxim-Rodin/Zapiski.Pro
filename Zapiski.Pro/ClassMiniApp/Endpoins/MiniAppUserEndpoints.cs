using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Zapiski.Pro.ClassMiniApp.Services;

namespace Zapiski.Pro.MiniApp.Endpoints
{
    public static class MiniAppUserEndpoints
    {
        public static void MapMiniAppUserEndpoints(
            this WebApplication app,
            MiniAppUserService userService)
        {
            app.MapGet("/api/user/{telegramId:long}/dashboard", (long telegramId, HttpContext context) =>
            {
                if (!long.TryParse(context.Request.Headers["X-Telegram-Id"], out var currentTelegramId))
                {
                    return Results.Unauthorized();
                }

                if (currentTelegramId != telegramId)
                {
                    return Results.Forbid();
                }

                var dashboard = userService.GetDashboard(telegramId);

                if (dashboard == null)
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Пользователь не найден"
                    });

                return Results.Ok(dashboard);
            });
        }
    }
}
