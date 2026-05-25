namespace Zapiski.Pro.MiniApp.Models
{
    public class MiniAppMasterProfileDto
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; }
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

    public class MiniAppMasterServiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public int Duration { get; set; }
        public int PrepaymentPercent { get; set; }
        public int PrepaymentAmount { get; set; }
    }

    public class MiniAppCreateMasterServiceRequest
    {
        public string Name { get; set; }
        public int Price { get; set; }
        public int Duration { get; set; }
        public int PrepaymentPercent { get; set; }
    }

    public class MiniAppMasterActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
