using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Zapiski.Pro.ClassMiniApp.Services;

public static class TelegramWebAppAuth
{
    private const string InitDataHeader = "X-Telegram-Init-Data";
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    public static bool TryGetTelegramId(HttpRequest request, string botToken, out long telegramId)
    {
        telegramId = 0;
        var initData = request.Headers[InitDataHeader].ToString();

        if (string.IsNullOrWhiteSpace(initData) || string.IsNullOrWhiteSpace(botToken))
            return false;

        var parameters = QueryHelpers.ParseQuery(initData);

        if (!parameters.TryGetValue("hash", out var hashValue) ||
            !parameters.TryGetValue("auth_date", out var authDateValue) ||
            !parameters.TryGetValue("user", out var userValue))
            return false;

        var receivedHashText = hashValue.ToString();
        if (receivedHashText.Length != 64)
            return false;

        byte[] receivedHash;
        try
        {
            receivedHash = Convert.FromHexString(receivedHashText);
        }
        catch (FormatException)
        {
            return false;
        }

        var dataCheckString = string.Join(
            "\n",
            parameters
                .Where(item => !string.Equals(item.Key, "hash", StringComparison.Ordinal))
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => $"{item.Key}={item.Value}"));

        using var secretHmac = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData"));
        var secretKey = secretHmac.ComputeHash(Encoding.UTF8.GetBytes(botToken));
        using var dataHmac = new HMACSHA256(secretKey);
        var calculatedHash = dataHmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));

        if (!CryptographicOperations.FixedTimeEquals(calculatedHash, receivedHash))
            return false;

        if (!long.TryParse(authDateValue.ToString(), out var authDateUnix))
            return false;

        DateTimeOffset authDate;
        try
        {
            authDate = DateTimeOffset.FromUnixTimeSeconds(authDateUnix);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        var age = DateTimeOffset.UtcNow - authDate;
        if (age < TimeSpan.FromMinutes(-5) || age > MaxAge)
            return false;

        try
        {
            using var userJson = JsonDocument.Parse(userValue.ToString());
            return userJson.RootElement.TryGetProperty("id", out var idElement) &&
                   idElement.TryGetInt64(out telegramId) &&
                   telegramId > 0;
        }
        catch (JsonException)
        {
            telegramId = 0;
            return false;
        }
    }
}
