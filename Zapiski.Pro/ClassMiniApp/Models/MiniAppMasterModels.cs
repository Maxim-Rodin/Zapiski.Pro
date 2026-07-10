namespace Zapiski.Pro.MiniApp.Models
{
    public class MiniAppMasterProfileDto
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PaymentDetails { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class MiniAppMasterClientDto
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; }
        public int BookingsCount { get; set; }
        public string LastBookingAt { get; set; }
        public string LastStatus { get; set; }
    }

    public class MiniAppMasterStatsDto
    {
        public int Clients { get; set; }
        public int ActiveBookings { get; set; }
        public int Services { get; set; }
    }

    public class MiniAppMasterBookingDto
    {
        public int Id { get; set; }
        public long ClientTelegramId { get; set; }
        public string ClientUsername { get; set; }
        public string ServiceName { get; set; }
        public string Address { get; set; } = string.Empty;
        public string DateTime { get; set; }
        public string Status { get; set; }
        public int Price { get; set; }
        public int PrepaymentPercent { get; set; }
        public int PrepaymentAmount { get; set; }
        public bool IsManualBlock { get; set; }
    }

    public class MiniAppMasterAddressDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class MiniAppMasterAddressRequest
    {
        public string Title { get; set; }
        public string Address { get; set; }
    }

    public class MiniAppCreateTimeBlockRequest
    {
        public string Title { get; set; }
        public string Date { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class MiniAppMasterScheduleDayDto
    {
        public int DayOfWeek { get; set; }
        public string DayName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class MiniAppMasterScheduleModeDto
    {
        public string Mode { get; set; } = "stable";
    }

    public class MiniAppUpdateScheduleModeRequest
    {
        public string Mode { get; set; }
    }

    public class MiniAppManualSlotDto
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class MiniAppCreateManualSlotRequest
    {
        public string Date { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class MiniAppUpdateScheduleDayRequest
    {
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class MiniAppMasterServiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public bool IsVariablePrice { get; set; }
        public int? MaxPrice { get; set; }
        public int Duration { get; set; }
        public int PrepaymentPercent { get; set; }
        public int PrepaymentAmount { get; set; }
        public int? AddressId { get; set; }
        public string AddressTitle { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class MiniAppCreateMasterServiceRequest
    {
        public string Name { get; set; }
        public int Price { get; set; }
        public bool IsVariablePrice { get; set; }
        public int? MaxPrice { get; set; }
        public int Duration { get; set; }
        public int PrepaymentPercent { get; set; }
        public int? AddressId { get; set; }
    }

    public class MiniAppUpdateMasterProfileRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string PaymentDetails { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class MiniAppMasterBroadcastRequest
    {
        public string Title { get; set; }
        public string Text { get; set; }
    }

    public class MiniAppMasterActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class MiniAppMasterAvatarResult : MiniAppMasterActionResult
    {
        public string AvatarUrl { get; set; }= string .Empty;
    }

    public class MiniAppAddMasterClientRequest
    {
        public string Search { get; set; } = string.Empty;
    }

    public class MiniAppPortfolioPhotoDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
    public class MiniAppPortfolioPhotoInternalDto : MiniAppPortfolioPhotoDto
    {
        public string PublicId { get; set; } = string.Empty;
    }

    public class MiniAppReorderPortfolioRequest
    {
        public List<int> PhotoIds { get; set; } = new();
    }

    public class MiniAppPortfolioPhotoResult : MiniAppMasterActionResult
    {
        public MiniAppPortfolioPhotoDto? Photo { get; set; }
    }
}
