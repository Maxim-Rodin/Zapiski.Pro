namespace Zapiski.Pro.MiniApp.Models
{
    public class MiniAppAdminStatsDto
    {
        public int Users { get; set; }
        public int Masters { get; set; }
        public int Bookings { get; set; }
        public int Payments { get; set; }
        public int LandingMasters { get; set; }
        public int DirectMasters { get; set; }
        public int RegistrationsLast30Days { get; set; }
        public decimal LandingSharePercent { get; set; }
    }

    public class MiniAppMasterDto
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsFounder { get; set; }
        public DateTime? TrialEndsAt { get; set; }
        public DateTime? SubscriptionEndsAt { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
        public bool HasAccess { get; set; }
        public string AccessType { get; set; } = "expired";
        public int DaysLeft { get; set; }
    }

    public class MiniAppUserDto
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; }
        public int BookingsCount { get; set; }
    }

    public class MiniAppCreateMasterRequest
    {
        public long TelegramId { get; set; }
        public string Key { get; set; }
        public bool IsFounder { get; set; }
        public int SubscriptionMonths { get; set; }
    }

    public class MiniAppAdminGrantSubscriptionRequest
    {
        public bool IsFounder { get; set; }
        public bool ChangeFounderStatus { get; set; }
        public int SubscriptionMonths { get; set; }
    }

    public class MiniAppActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static MiniAppActionResultDto Ok(string message)
        {
            return new MiniAppActionResultDto
            {
                Success = true,
                Message = message
            };
        }

        public static MiniAppActionResultDto Fail(string message)
        {
            return new MiniAppActionResultDto
            {
                Success = false,
                Message = message
            };
        }
    }
}
