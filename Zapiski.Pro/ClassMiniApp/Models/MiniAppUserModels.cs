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
}
