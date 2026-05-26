namespace Zapiski.Pro.ClassMiniApp.Models
{
    public class MiniAppUserDashboardDto
    {
        public MiniAppUserProfileDto Profile { get; set; }
        public MiniAppUserRoleDto Roles { get; set; }
        public List<MiniAppUserBookingDto> Bookings { get; set; } = new();
        public List<MiniAppUserMasterDto> Masters { get; set; } = new();
    }

    public class MiniAppUserRoleDto
    {
        public bool IsAdmin { get; set; }
        public bool IsMaster { get; set; }
        public string MasterKey { get; set; }
    }

    public class MiniAppUserProfileDto
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; }
    }

    public class MiniAppUserBookingDto
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }
        public string MasterKey { get; set; }
        public string MasterUsername { get; set; }
        public string DateTime { get; set; }
        public string Status { get; set; }
    }

    public class MiniAppUserMasterDto
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Username { get; set; }
        public int BookingsCount { get; set; }
    }

    public class MiniAppBookingSlotDto
    {
        public string Time { get; set; }
        public bool IsBusy { get; set; }
    }

    public class MiniAppCreateBookingRequest
    {
        public string MasterKey { get; set; }
        public int ServiceId { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Username { get; set; }
    }

    public class MiniAppCreateBookingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int BookingId { get; set; }
        public string Status { get; set; }
        public string ServiceName { get; set; }
        public int Price { get; set; }
        public int PrepaymentPercent { get; set; }
        public int PrepaymentAmount { get; set; }
        public string PaymentDetails { get; set; }
    }
}
