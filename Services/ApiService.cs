using APIAggregator.Models;
using Microsoft.Extensions.Caching.Memory;

namespace APIAggregator.Services {

    public class ApiService : IApiService {

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ApiService> _logger;

        public ApiService(HttpClient httpClient,IMemoryCache cache, ILogger<ApiService> logger) {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GetData(string apiUrl) {

            var cacheKey = $"Api_{apiUrl}";
            if (_cache.TryGetValue(cacheKey, out string cachedata)) {
                return cachedata;
            }

            try {
                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadAsStringAsync();

                _cache.Set(apiUrl, data, TimeSpan.FromMinutes(5));

                return data;
            } catch (Exception ex) {

                _logger.LogWarning(ex, "Fallback: Failed to fetch data from API: {Url}", apiUrl);

                if (_cache.TryGetValue(cacheKey, out string fallbackCache)) {
                    _logger.LogInformation("Last cached data for {Url}", apiUrl);
                    return fallbackCache;
                }

                throw new Exception();
            }
        }
    }
}
