namespace MyVexBooks.Models.DTOs
{
    public class SubscriptionDTO
    {
        public string Endpoint { get; set; } = null!;
        public Keys Keys { get; set; } = null!;
    }

    public class Keys
    {
        public string P256dh { get; set; } = null!;
        public string Auth { get; set; } = null!;
    }
}
