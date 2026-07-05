using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Zapiski.Pro.MiniApp.Models;
using Zapiski.Pro.MiniApp.Services;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Zapiski.Pro.ClassMiniApp.Services;

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
            app.MapPost("/api/master/{key}/clients", (HttpRequest httpRequest, string key, MiniAppAddMasterClientRequest request) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new
                        {
                            success = false,
                            message = "Откройте раздел из Telegram"
                        }, statusCode: StatusCodes.Status401Unauthorized);
                    var result = masterService.AddClient(key, telegramId, request);
                    if (!result.Success)
                    {
                        return Results.BadRequest(result);
                    }
                    return Results.Ok(result);
                }
            );

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

            app.MapGet("/api/master/{key}/bookings", (HttpRequest httpRequest, string key) =>
            {
                if (!TryGetTelegramId(httpRequest, out var telegramId))
                    return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                        statusCode: StatusCodes.Status401Unauthorized);

                var bookings = masterService.GetBookings(key, telegramId);

                if (bookings == null)
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Мастер не найден"
                    });

                return Results.Ok(bookings);
            });

            app.MapGet("/api/master/{key}/schedule", (HttpRequest httpRequest, string key) =>
            {
                if (!TryGetTelegramId(httpRequest, out var telegramId))
                    return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                        statusCode: StatusCodes.Status401Unauthorized);

                var schedule = masterService.GetSchedule(key, telegramId);

                if (schedule == null)
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Мастер не найден"
                    });

                return Results.Ok(schedule);
            });

            app.MapPut("/api/master/{key}/schedule/{day:int}",
                (HttpRequest httpRequest, string key, int day, MiniAppUpdateScheduleDayRequest request) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = masterService.UpdateScheduleDay(key, telegramId, day, request);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapGet("/api/master/{key}/schedule-mode", (HttpRequest httpRequest, string key) =>
            {
                if (!TryGetTelegramId(httpRequest, out var telegramId))
                    return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                        statusCode: StatusCodes.Status401Unauthorized);

                var mode = masterService.GetScheduleMode(key, telegramId);

                if (mode == null)
                    return Results.NotFound(new { success = false, message = "Мастер не найден" });

                return Results.Ok(mode);
            });

            app.MapPut("/api/master/{key}/schedule-mode",
                (HttpRequest httpRequest, string key, MiniAppUpdateScheduleModeRequest request) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = masterService.UpdateScheduleMode(key, telegramId, request);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapGet("/api/master/{key}/manual-slots", (HttpRequest httpRequest, string key, string date) =>
            {
                if (!TryGetTelegramId(httpRequest, out var telegramId))
                    return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                        statusCode: StatusCodes.Status401Unauthorized);

                var slots = masterService.GetManualSlots(key, telegramId, date);

                if (slots == null)
                    return Results.NotFound(new { success = false, message = "Мастер не найден" });

                return Results.Ok(slots);
            });

            app.MapPost("/api/master/{key}/manual-slots",
                (HttpRequest httpRequest, string key, MiniAppCreateManualSlotRequest request) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = masterService.CreateManualSlot(key, telegramId, request);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapDelete("/api/master/{key}/manual-slots/{slotId:int}",
                (HttpRequest httpRequest, string key, int slotId) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = masterService.DeleteManualSlot(key, telegramId, slotId);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapDelete("/api/master/{key}/manual-slots", (HttpRequest httpRequest, string key, string date) =>
            {
                if (!TryGetTelegramId(httpRequest, out var telegramId))
                    return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                        statusCode: StatusCodes.Status401Unauthorized);

                var result = masterService.ClearManualSlotsDay(key, telegramId, date);

                if (!result.Success)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            });

            app.MapPost("/api/master/{key}/time-blocks",
                (HttpRequest httpRequest, string key, MiniAppCreateTimeBlockRequest request) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = masterService.CreateTimeBlock(key, telegramId, request);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapDelete("/api/master/{key}/time-blocks/{blockId:int}",
                (HttpRequest httpRequest, string key, int blockId) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = masterService.DeleteTimeBlock(key, telegramId, blockId);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapPost("/api/master/{key}/bookings/{bookingId:int}/accept",
                async (HttpRequest httpRequest, string key, int bookingId) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = await masterService.AcceptBooking(key, telegramId, bookingId);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapPost("/api/master/{key}/bookings/{bookingId:int}/cancel",
                async (HttpRequest httpRequest, string key, int bookingId) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = await masterService.CancelBooking(key, telegramId, bookingId);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapPost("/api/master/{key}/bookings/{bookingId:int}/payment-accept",
                async (HttpRequest httpRequest, string key, int bookingId) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = await masterService.AcceptPayment(key, telegramId, bookingId);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapPost("/api/master/{key}/bookings/{bookingId:int}/payment-reject",
                async (HttpRequest httpRequest, string key, int bookingId) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = await masterService.RejectPayment(key, telegramId, bookingId);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
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

            app.MapPut("/api/master/{key}/profile",
                (HttpRequest httpRequest, string key, MiniAppUpdateMasterProfileRequest request) =>
                {
                    var telegramHeader = httpRequest.Headers["X-Telegram-Id"].ToString();

                    if (!long.TryParse(telegramHeader, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте профиль из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = masterService.UpdateProfile(key, telegramId, request);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapGet("/api/master/{key}/addresses", (string key) =>
            {
                var addresses = masterService.GetAddresses(key);

                if (addresses == null)
                    return Results.NotFound(new { success = false, message = "Мастер не найден" });

                return Results.Ok(addresses);
            });

            app.MapPost("/api/master/{key}/addresses",
                (HttpRequest httpRequest, string key, MiniAppMasterAddressRequest request) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте профиль из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = masterService.CreateAddress(key, telegramId, request);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapDelete("/api/master/{key}/addresses/{addressId:int}",
                (HttpRequest httpRequest, string key, int addressId) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте профиль из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = masterService.DeleteAddress(key, telegramId, addressId);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapPost("/api/master/{key}/services", (string key, MiniAppCreateMasterServiceRequest request) =>
            {
                var result = masterService.CreateService(key, request);

                if (!result.Success)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            });

            app.MapPut("/api/master/{key}/services/{serviceId:int}",
                (string key, int serviceId, MiniAppCreateMasterServiceRequest request) =>
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

            app.MapPost("/api/master/{key}/broadcast",
                async (HttpRequest httpRequest, string key, MiniAppMasterBroadcastRequest request) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);

                    var result = await masterService.SendBroadcast(key, telegramId, request);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });
            app.MapPost("/api/master/{key}/avatar", async (HttpRequest httpRequest, string key, IFormFile file) =>
            {
                if (!TryGetTelegramId(httpRequest, out var telegramId))
                    return Results.Json(new { success = false, message = "Откройте профиль из Telegram" },
                        statusCode: StatusCodes.Status401Unauthorized);

                var master = masterService.GetMasterProfile(key);

                if (master == null)
                    return Results.NotFound(new { success = false, message = "Мастер не найден" });

                if (master.TelegramId != telegramId)
                    return Results.Forbid();
                try
                {
                    var imageService = new CloudinaryImageService();
                    var avatarUrl = await imageService.UploadMasterAvatar(master.Id, file);
                    var result = masterService.UpdateAvatarUrl(key, telegramId, avatarUrl);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);

                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        message = ex.Message
                    });
                }

            }).DisableAntiforgery();
        }

        private static bool TryGetTelegramId(HttpRequest httpRequest, out long telegramId)
        {
            var telegramHeader = httpRequest.Headers["X-Telegram-Id"].ToString();
            return long.TryParse(telegramHeader, out telegramId);
            
        }
    }
}
