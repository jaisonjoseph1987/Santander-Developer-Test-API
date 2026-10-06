using Microsoft.Extensions.Caching.Memory;
using Santander_Developer_Test_API.Models;
using Santander_Developer_Test_API.Shared;
using System.Text.Json;

namespace Santander_Developer_Test_API.Services
{
    /// <summary>
    /// Author: Jaison Joseph
    /// GetBestStoriesAsync - Fetching data from HackerNews
    /// </summary>
    public class HackerService : IHackerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memmorycache;

        public HackerService(HttpClient http, IMemoryCache memoryCache)
        {
            _httpClient = http;
            _memmorycache = memoryCache;
        }
        public async Task<List<StoryResponse>> GetBestStoriesAsync(int n)
        {
            // Level 1 - Fetch All Ids and aloow to save in cache memmory
            var allIds = await _memmorycache.GetOrCreateAsync("best_ids", async e => {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                e.SlidingExpiration = TimeSpan.FromMinutes(2);
                e.Size = 1;
                return await _httpClient.GetFromJsonAsync<int[]>(ClsCommonVariables.BaseStoryId);
            });

            if (allIds == null) return new();

            // Create Chunks for large number of data handling 
            const int CHUNK_SIZE = 1000;
            const int PARALLELISM = 50;

            var topQueue = new PriorityQueue<Item, int>();
            var lockObj = new object();

            foreach (var chunk in allIds.Chunk(CHUNK_SIZE))
            {
                await Parallel.ForEachAsync(chunk,
                    new ParallelOptions { MaxDegreeOfParallelism = PARALLELISM },
                    async (id, ct) =>
                    {
                        try
                        {
                            // Level 2 Caches: Individual story details 
                            var cacheKey = $"item_{id}";
                            Item item;

                            if (!_memmorycache.TryGetValue(cacheKey, out item))
                            {
                                // Add retry + timeout 
                                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                                cts.CancelAfter(TimeSpan.FromSeconds(10));

                                var bytes = await _httpClient.GetByteArrayAsync(
                                    ClsCommonVariables.BaseStoryIdDetails + id.ToString() + ".json", cts.Token);

                                item = ParseStoryResponse(bytes);

                                // Title, URL, By never change. Score changes slowly.
                                // So we can cache for 30 mins
                                var options = new MemoryCacheEntryOptions()
                                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                                    .SetSize(1);

                                _memmorycache.Set(cacheKey, item, options);
                            }

                            if (item.id == 0) return;

                            lock (lockObj)
                            {
                                if (topQueue.Count < n)
                                    topQueue.Enqueue(item, item.score);
                                else if (item.score > topQueue.Peek().score)
                                {
                                    topQueue.Dequeue();
                                    topQueue.Enqueue(item, item.score);
                                }
                            }
                        }
                        catch { }
                    });

            }

            return topQueue.UnorderedItems
                .Select(x => x.Element)
                .OrderByDescending(x => x.score)
                .Select(x => new StoryResponse
                {
                    id = x.id,
                    title = x.title,
                    url = x.url,
                    score = x.score,
                    by = x.by,
                    commentCount = x.descendants ?? x.kids?.Length ?? 0
                }).ToList();
        }
        private static Item ParseStoryResponse(byte[] bytes)
        {
            var reader = new Utf8JsonReader(bytes);
            var item = new Item();

            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName) continue;

                if (reader.ValueTextEquals("id"))
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.Number) item.id = reader.GetInt32();
                }
                else if (reader.ValueTextEquals("score"))
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.Number) item.score = reader.GetInt32();
                }
                else if (reader.ValueTextEquals("title"))
                {
                    reader.Read();
                    item.title = reader.GetString() ?? "";
                }
                else if (reader.ValueTextEquals("url"))
                {
                    reader.Read();
                    item.url = reader.GetString() ?? "";
                }
                else if (reader.ValueTextEquals("by"))
                {
                    reader.Read();
                    item.by = reader.GetString() ?? "";
                }
                else if (reader.ValueTextEquals("descendants"))
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.Number) item.descendants = reader.GetInt32();
                }
                else if (reader.ValueTextEquals("kids"))
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        item.kids = JsonSerializer.Deserialize<int[]>(ref reader);
                    }
                }
            }
            return item;
        }
    }
}
