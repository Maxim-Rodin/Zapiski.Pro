namespace Zapiski.Pro.MiniApp.Models
{
    public class MiniAppAdminStatsDto
    {
        public int Users { get; set; }
        public int Masters { get; set; }
        public int Bookings { get; set; }
        public int Payments { get; set; }
    }

    public class MiniAppMasterDto
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
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
