using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Zapiski.Pro.MiniApp.Models;
using Zapiski.Pro.MiniApp.Repositories;

namespace Zapiski.Pro.ClassMiniApp.Services
{
    public class YooKassaPaymentService
    {
        private const string ApiBaseUrl = "https://api.yookassa.ru/v3";
        private readonly MiniAppMasterRepository repository;
        private readonly HttpClient httpClient;
        private readonly string shopId;
        private readonly string secretKey;

        public YooKassaPaymentService(MiniAppMasterRepository repository)
        {
            this.repository = repository;
            httpClient = new HttpClient();
            shopId = Environment.GetEnvironmentVariable("YooKassa__ShopId") ?? string.Empty;
            secretKey = Environment.GetEnvironmentVariable("YooKassa__SecretKey") ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(shopId) && !string.IsNullOrWhiteSpace(secretKey))
            {
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopId}:{secretKey}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
            }
        }

        public async Task<MiniAppCreateSubscriptionPaymentResult> CreateSubscriptionPayment(
            string key,
            long telegramId,
            MiniAppCreateSubscriptionPaymentRequest request)
        {
            var master = repository.GetMasterByKey(key);

            if (string.IsNullOrWhiteSpace(shopId) || string.IsNullOrWhiteSpace(secretKey))
                return Failed("Оплата пока не настроена");

            if (master == null)
                return Failed("Мастер не найден");

            if (master.TelegramId != telegramId)
                return Failed("Нет доступа к подписке");

            var plan = repository.GetSubscriptionPlan(request.PlanCode);

            if (plan == null)
                return Failed("Некорректный тариф подписки");

            var returnUrl = BuildReturnUrl(master.Key);
            var body = new
            {
                amount = new
                {
                    value = plan.PriceRub.ToString("0.00", CultureInfo.InvariantCulture),
                    currency = "RUB"
                },
                capture = true,
                confirmation = new
                {
                    type = "redirect",
                    return_url = returnUrl
                },
                description = $"Подписка Zapisi Pro: {plan.Title}",
                metadata = new
                {
                    master_id = master.Id.ToString(CultureInfo.InvariantCulture),
                    master_key = master.Key,
                    plan_code = plan.Code,
                    months = plan.Months.ToString(CultureInfo.InvariantCulture)
                }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/payments");
            httpRequest.Headers.Add("Idempotence-Key", Guid.NewGuid().ToString());
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(httpRequest);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"YooKassa create payment error: {response.StatusCode} {responseText}");
                return Failed("ЮKassa не создала платеж. Попробуйте позже.");
            }

            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;
            var paymentId = root.GetProperty("id").GetString() ?? string.Empty;
            var confirmationUrl = root.GetProperty("confirmation").GetProperty("confirmation_url").GetString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(confirmationUrl))
                return Failed("ЮKassa вернула некорректный ответ");

            repository.CreateSubscriptionPayment(master.Id, paymentId, plan.Code, plan.Months, plan.PriceRub);

            return new MiniAppCreateSubscriptionPaymentResult
            {
                Success = true,
                Message = "Платеж создан",
                PaymentId = paymentId,
                ConfirmationUrl = confirmationUrl
            };
        }

        public async Task<MiniAppMasterActionResult> ProcessWebhook(JsonElement body)
        {
            if (!body.TryGetProperty("event", out var eventElement) ||
                eventElement.GetString() != "payment.succeeded" ||
                !body.TryGetProperty("object", out var objectElement) ||
                !objectElement.TryGetProperty("id", out var paymentIdElement))
            {
                return new MiniAppMasterActionResult { Success = true, Message = "Событие пропущено" };
            }

            var paymentId = paymentIdElement.GetString();

            if (string.IsNullOrWhiteSpace(paymentId))
                return FailedAction("Не найден id платежа");

            if (string.IsNullOrWhiteSpace(shopId) || string.IsNullOrWhiteSpace(secretKey))
                return FailedAction("Оплата пока не настроена");

            var isSucceeded = await IsPaymentSucceeded(paymentId);

            if (!isSucceeded)
                return FailedAction("Платеж еще не подтвержден ЮKassa");

            return repository.CompleteSubscriptionPayment(paymentId);
        }

        private async Task<bool> IsPaymentSucceeded(string paymentId)
        {
            using var response = await httpClient.GetAsync($"{ApiBaseUrl}/payments/{paymentId}");

            if (!response.IsSuccessStatusCode)
                return false;

            var responseText = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;

            var status = root.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : string.Empty;
            var paid = root.TryGetProperty("paid", out var paidElement) && paidElement.GetBoolean();

            return paid && status == "succeeded";
        }

        private static string BuildReturnUrl(string masterKey)
        {
            var explicitReturnUrl = Environment.GetEnvironmentVariable("YooKassa__ReturnUrl");

            if (!string.IsNullOrWhiteSpace(explicitReturnUrl))
                return explicitReturnUrl;

            var miniAppUrl = Environment.GetEnvironmentVariable("MINIAPP_URL") ?? "https://app-zapisi-pro.site";
            return $"{miniAppUrl.TrimEnd('/')}/master/{masterKey}/subscription";
        }

        private static MiniAppCreateSubscriptionPaymentResult Failed(string message)
        {
            return new MiniAppCreateSubscriptionPaymentResult
            {
                Success = false,
                Message = message
            };
        }

        private static MiniAppMasterActionResult FailedAction(string message)
        {
            return new MiniAppMasterActionResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
