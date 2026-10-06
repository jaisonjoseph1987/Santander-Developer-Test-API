using Santander_Developer_Test_API.Models;

namespace Santander_Developer_Test_API.Services
{
    public interface IHackerService
    {
        Task<List<StoryResponse>> GetBestStoriesAsync(int n);
    }
}
