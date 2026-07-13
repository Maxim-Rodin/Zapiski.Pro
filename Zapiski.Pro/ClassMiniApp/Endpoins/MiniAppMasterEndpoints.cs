using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
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
            MiniAppMasterService masterService,
            YooKassaPaymentService yooKassaPaymentService)
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
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

                var stats = masterService.GetStats(key);

                if (stats == null)
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Мастер не найден"
                    });

                return Results.Ok(stats);
            });

            app.MapGet("/api/master/{key}/analytics", (string key, string? from, string? to) =>
            {
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

                var analytics = masterService.GetAnalytics(key, from, to);

                if (analytics == null)
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Мастер не найден"
                    });

                return Results.Ok(analytics);
            });

            app.MapGet("/api/master/{key}/subscription", (string key) =>
            {
                var subscription = masterService.GetSubscription(key);

                if (subscription == null)
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Мастер не найден"
                    });

                return Results.Ok(subscription);
            });

            app.MapPost("/api/master/{key}/subscription/payments", async (
                HttpRequest httpRequest,
                string key,
                MiniAppCreateSubscriptionPaymentRequest request) =>
            {
                if (!TryGetTelegramId(httpRequest, out var telegramId))
                    return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                        statusCode: StatusCodes.Status401Unauthorized);

                var result = await yooKassaPaymentService.CreateSubscriptionPayment(key, telegramId, request);

                if (!result.Success)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            });

            app.MapGet("/api/master/{key}/subscription/payments/{paymentToken}", (
                HttpRequest httpRequest,
                string key,
                string paymentToken) =>
            {
                if (!TryGetTelegramId(httpRequest, out var telegramId))
                    return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                        statusCode: StatusCodes.Status401Unauthorized);

                var payment = yooKassaPaymentService.GetSubscriptionPaymentStatus(key, telegramId, paymentToken);

                if (payment == null)
                    return Results.NotFound(new { success = false, message = "Платеж не найден" });

                return Results.Ok(payment);
            });

            app.MapPost("/api/payments/yookassa/webhook", async (HttpRequest httpRequest) =>
            {
                using var document = await JsonDocument.ParseAsync(httpRequest.Body);
                var result = await yooKassaPaymentService.ProcessWebhook(document.RootElement.Clone());

                if (!result.Success)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            });

            app.MapGet("/api/master/{key}/bookings", (HttpRequest httpRequest, string key) =>
            {
                if (!TryGetTelegramId(httpRequest, out var telegramId))
                    return Results.Json(new { success = false, message = "Откройте раздел из Telegram" },
                        statusCode: StatusCodes.Status401Unauthorized);
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

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
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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

            app.MapGet("/api/master/{key}/portfolio", (string key) =>
            {
                return Results.Ok(masterService.GetPortfolioPhotos(key));
            });

            app.MapPost("/api/master/{key}/portfolio", async (HttpRequest httpRequest, string key, IFormFile file) =>
            {
                if (!TryGetTelegramId(httpRequest, out var telegramId))
                    return Results.Json(new { success = false, message = "Откройте профиль из Telegram" },
                        statusCode: StatusCodes.Status401Unauthorized);
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

                var result = await masterService.UploadPortfolioPhoto(key, telegramId, file);

                if (!result.Success)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            }).DisableAntiforgery();

            app.MapDelete("/api/master/{key}/portfolio/{photoId:int}",
                async (HttpRequest httpRequest, string key, int photoId) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте профиль из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

                    var result = await masterService.DeletePortfolioPhoto(key, telegramId, photoId);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapPut("/api/master/{key}/portfolio/reorder",
                (HttpRequest httpRequest, string key, MiniAppReorderPortfolioRequest request) =>
                {
                    if (!TryGetTelegramId(httpRequest, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте профиль из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

                    var result = masterService.ReorderPortfolioPhotos(key, telegramId, request);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapPut("/api/master/{key}/profile",
                (HttpRequest httpRequest, string key, MiniAppUpdateMasterProfileRequest request) =>
                {
                    var telegramHeader = httpRequest.Headers["X-Telegram-Id"].ToString();

                    if (!long.TryParse(telegramHeader, out var telegramId))
                        return Results.Json(new { success = false, message = "Откройте профиль из Telegram" },
                            statusCode: StatusCodes.Status401Unauthorized);
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

                    var result = masterService.DeleteAddress(key, telegramId, addressId);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapPost("/api/master/{key}/services", (string key, MiniAppCreateMasterServiceRequest request) =>
            {
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

                var result = masterService.CreateService(key, request);

                if (!result.Success)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            });

            app.MapPut("/api/master/{key}/services/{serviceId:int}",
                (string key, int serviceId, MiniAppCreateMasterServiceRequest request) =>
                {
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

                    var result = masterService.UpdateService(key, serviceId, request);

                    if (!result.Success)
                        return Results.BadRequest(result);

                    return Results.Ok(result);
                });

            app.MapDelete("/api/master/{key}/services/{serviceId:int}", (string key, int serviceId) =>
            {
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

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
                    var accessError = EnsureMasterAccess(masterService, key);
                    if (accessError != null)
                        return accessError;

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
                var accessError = EnsureMasterAccess(masterService, key);
                if (accessError != null)
                    return accessError;

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

        private static IResult? EnsureMasterAccess(MiniAppMasterService masterService, string key)
        {
            var subscription = masterService.GetSubscription(key);

            if (subscription == null)
                return Results.NotFound(new { success = false, message = "Мастер не найден" });

            if (subscription.HasAccess)
                return null;

            return Results.Json(new
            {
                success = false,
                message = "Доступ закончился. Оформите подписку, чтобы продолжить пользоваться мастер-панелью."
            }, statusCode: StatusCodes.Status402PaymentRequired);
        }
    }
}
