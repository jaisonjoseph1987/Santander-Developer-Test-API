namespace Santander_Developer_Test_API.Models
{
    public class Item
    {
        public int id { get; set; }
        public string? title { get; set; }
        public string? url { get; set; }
        public string? by { get; set; }
        public long time { get; set; }
        public int score { get; set; }
        public int? descendants { get; set; }
        public int[]? kids { get; set; }
        public string? type { get; set; }
    }
}
