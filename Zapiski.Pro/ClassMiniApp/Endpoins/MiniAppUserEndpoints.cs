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
            app.MapGet("/api/user/{telegramId:long}/personal-data-consent", (long telegramId, HttpContext context) =>
            {
                if (!long.TryParse(context.Request.Headers["X-Telegram-Id"], out var currentTelegramId))
                    return Results.Unauthorized();

                if (currentTelegramId != telegramId)
                    return Forbidden();

                return Results.Ok(userService.GetPersonalDataConsent(telegramId));
            });

            app.MapPost("/api/user/{telegramId:long}/personal-data-consent", (long telegramId, HttpContext context) =>
            {
                if (!long.TryParse(context.Request.Headers["X-Telegram-Id"], out var currentTelegramId))
                    return Results.Unauthorized();

                if (currentTelegramId != telegramId)
                    return Forbidden();

                var consent = userService.AcceptPersonalDataConsent(telegramId);

                if (!consent.Accepted)
                {
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Пользователь не найден. Сначала откройте бота."
                    });
                }

                return Results.Ok(consent);
            });

            app.MapGet("/api/user/{telegramId:long}/dashboard", (long telegramId, HttpContext context) =>
            {
                if (!long.TryParse(context.Request.Headers["X-Telegram-Id"], out var currentTelegramId))
                {
                    return Results.Unauthorized();
                }

                if (currentTelegramId != telegramId)
                {
                    return Forbidden();
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
                    return Forbidden();
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

            app.MapGet("/api/master-key/check", (string key) =>
            {
                return Results.Ok(userService.CheckMasterKey(key));
            });

            app.MapPost("/api/user/{telegramId:long}/bookings", async (long telegramId, MiniAppCreateBookingRequest request, HttpContext context) =>
            {
                if (!long.TryParse(context.Request.Headers["X-Telegram-Id"], out var currentTelegramId))
                    return Results.Unauthorized();

                if (currentTelegramId != telegramId)
                    return Forbidden();

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
                    return Forbidden();

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

            app.MapPost("/api/user/{telegramId:long}/become-master", async (long telegramId, MiniAppBecomeMasterRequest request, HttpContext context) =>
            {
                if (!long.TryParse(context.Request.Headers["X-Telegram-Id"], out var currentTelegramId))
                    return Results.Unauthorized();

                if (currentTelegramId != telegramId)
                    return Forbidden();

                var result = await userService.BecomeMaster(telegramId, request);

                if (!result.Success)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            });

            static IResult Forbidden() =>
                Results.Json(
                    new { success = false, message = "Нет доступа к этому профилю" },
                    statusCode: StatusCodes.Status403Forbidden);
        }
    }
}
