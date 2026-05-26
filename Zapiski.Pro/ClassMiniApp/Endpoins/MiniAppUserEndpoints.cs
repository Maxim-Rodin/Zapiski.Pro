using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Zapiski.Pro.ClassMiniApp.Models;
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

            app.MapPost("/api/user/{telegramId:long}/bookings/{bookingId:int}/cancel", async (long telegramId, int bookingId, HttpContext context) =>
            {
                if (!long.TryParse(context.Request.Headers["X-Telegram-Id"], out var currentTelegramId))
                {
                    return Results.Unauthorized();
                }

                if (currentTelegramId != telegramId)
                {
                    return Results.Forbid();
                }

                var success = await userService.CancelBooking(telegramId, bookingId);

                if (!success)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        message = "Запись не найдена или её уже нельзя отменить"
                    });
                }

                return Results.Ok(new
                {
                    success = true,
                    message = "Запись отменена"
                });
            });

            app.MapGet("/api/public/master/{key}/slots", (string key, int serviceId, string date) =>
            {
                return Results.Ok(userService.GetBookingSlots(key, serviceId, date));
            });

            app.MapPost("/api/user/{telegramId:long}/bookings", async (long telegramId, MiniAppCreateBookingRequest request, HttpContext context) =>
            {
                if (!long.TryParse(context.Request.Headers["X-Telegram-Id"], out var currentTelegramId))
                    return Results.Unauthorized();

                if (currentTelegramId != telegramId)
                    return Results.Forbid();

                var result = await userService.CreateBooking(telegramId, request);

                if (!result.Success)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            });

            app.MapPost("/api/user/{telegramId:long}/bookings/{bookingId:int}/paid", async (long telegramId, int bookingId, HttpContext context) =>
            {
                if (!long.TryParse(context.Request.Headers["X-Telegram-Id"], out var currentTelegramId))
                    return Results.Unauthorized();

                if (currentTelegramId != telegramId)
                    return Results.Forbid();

                var success = await userService.MarkBookingPaid(telegramId, bookingId);

                if (!success)
                    return Results.BadRequest(new
                    {
                        success = false,
                        message = "Запись не найдена или оплату уже нельзя подтвердить"
                    });

                return Results.Ok(new
                {
                    success = true,
                    message = "Ожидаем подтверждение оплаты от мастера"
                });
            });
        }
    }
}
