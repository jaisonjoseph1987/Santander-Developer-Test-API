namespace Santander_Developer_Test_API.Models
{
    public class StoryResponse
    {
        public int id { get; set; }
        public string title { get; set; } = string.Empty;
        public string? url { get; set; }
        public string by { get; set; } = string.Empty;
        public string time { get; set; } = string.Empty; 
        public int score { get; set; }
        public int commentCount { get; set; }
    }
}
