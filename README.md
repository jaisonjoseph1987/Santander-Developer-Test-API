# Hacker News Best Stories API

An ASP.NET Core Web API that fetches the best stories from Hacker News and returns the Top N stories sorted by score, optimized for large datasets.

> **API Source:** `https://hacker-news.firebaseio.com/v0/beststories.json`

---

## How to Run the Application

### Prerequisites
****- .NET 8 SDK or later
- Visual Studio 2022 / VS Code
- Internet access for Firebase API****

### 1. Clone the Repository

git clone https://github.com/jaisonjoseph1987/Santander-Developer-Test-API
cd Santander-Developer-Test-API


### 2. Restore & Run

dotnet restore
dotnet run --project Santander-Developer-Test-API

Or open `Santander-Developer-Test-API.sln` in Visual Studio and press F5.

### 3. Access Swagger
Once running, open:
```
https://localhost:7243/swagger

### 4. Test the Endpoint
```
GET /api/BestStories?n=10
```
Example:
```
GET https://localhost:7243/api/BestStories?n=10
```

**Sample Response:**
```json
[
  {
    "id": 12345,
    "title": "Show HN: My new project",
    "url": "https://example.com",
    "score": 450,
    "by": "johndoe",
    "commentCount": 120
  }
]
```

---

## Design Decisions

### 1. Why `static class` for Constants (ClsCommonVariables)?
The task asked about `sealed vs static`. We used `static class` because `BaseStoryId` is a shared constant value. `sealed class` is for closed type hierarchies (e.g., Result pattern), not for holding URLs.

### 2. Handling 1M Records
- **Chunking:** `allIds.Chunk(1000)` - Splits 1M into 1000 chunks to avoid memory spike
- **Controlled Parallelism:** `MaxDegreeOfParallelism = 50` - Never 1M parallel HTTP calls. Prevents socket exhaustion and Firebase rate-limiting.
- **PriorityQueue:** Instead of sorting 1M items `O(M log M)`, we keep only Top N using a min-heap `O(M log N)` - much more efficient for memory and CPU.

### 3. Zero-Allocation JSON Parsing
Used `Utf8JsonReader` instead of `JsonSerializer.Deserialize<Item>` for each story. We only parse fields we need (`id, score, title, url, by, descendants, kids`) - faster for 1M calls.

---

## Caching Strategy

This is the most critical part for performance and to avoid hitting Firebase limits.

We implemented 3-level caching using `IMemoryCache`:

| Level | Key | Duration | Purpose |
|-------|-----|----------|---------|
| **L1** | `best_ids` | 10 min | Caches list of IDs. Saves 1 API call. |
| **L2** | `item_{id}` | 30 min | Caches individual story details. **Most important.** Saves up to 1M calls on repeat requests. |
| **L3** | `top_stories_{n}` | 2 min | Caches final sorted Top N result. Returns in <10ms. |

**Important Fix for `SizeLimit`:**
When `options.SizeLimit` is set in `Program.cs`, every cache entry MUST specify `Size`. 
```csharp
e.Size = 1; // Required for GetOrCreateAsync
options.SetSize(1); // Required for Set()
```
If not, you get: `System.InvalidOperationException: Cache entry must specify a value for Size`

**Registration in Program.cs:**
```csharp
builder.Services.AddMemoryCache(options => { options.SizeLimit = 100_000; });
builder.Services.AddHttpClient<HackerService>();
```

---

##  Assumptions Made

1. **Best Stories list is not real-time:** It's acceptable to cache IDs for 10 minutes. Hacker News best list doesn't change every second.
2. **Story details are immutable (except score/comments):** Title, URL, author never change, so we can cache for 30 mins. Score can be slightly stale.
3. **Firebase has rate limits:** We must throttle requests (50 parallel + 10s timeout per request). Not handling this would cause 429 errors for 1M case.
4. **Top N is small (10, 20, 50):** Using PriorityQueue makes sense because N << M (1M). If N was close to M, sorting would be better.
5. **Comment count:** Used `descendants` if available, else `kids.Length` as fallback, as per HN API spec.

---

##  Enhancements / Changes Given More Time

Given more time, I would implement:

Distributed Cache (Redis)
`IMemoryCache` is in-memory per instance. For scaling, replace with `IDistributedCache` + Redis so all instances share cache.



---

##  Project Structure
```
/Controllers
  - BestStoriesController.cs
/Services
  - HackerService.cs
/Models
  - Item.cs, StoryResponse.cs
/Common
  - ClsCommonVariables.cs (static class for constants)
Program.cs
```

---

## Author
Jaison Joseph - Santander Developer Coding Test Submission
