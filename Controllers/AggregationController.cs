using System;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Remoting;
using System.Text.Json;
using APIAggregator.Models;
using APIAggregator.Services;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;

namespace APIAggregator.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class AggregationController : Controller {

        private readonly IApiService _apiService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AggregationController> _logger;

        public AggregationController(IApiService apiService, IConfiguration configuration, ILogger<AggregationController> logger) {
            _apiService = apiService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("GetAggregatedData")]
        public async Task<ActionResult> GetAggregatedData(AggregatorRequest request) {

            _logger.LogInformation("GetAggregatedData aggregation");
            try 
            {
                var apiSection = _configuration.GetSection("APIs");
                var apiConfigs = new Dictionary<string, ApiConfig>();
                apiSection.Bind(apiConfigs);

                var tasks = new List<Task>();

                var results = new Dictionary<string, string>();

                #region Get APIs DATA 
                foreach (var api in apiConfigs) 
                {
                    if (string.IsNullOrEmpty(api.Value.Url))
                        continue;
                    tasks.Add(Task.Run(async () =>
                    {
                        
                        try {
                            var json = await _apiService.GetRawJson(api.Value.Url);
                            results[api.Key] = json;
                        } catch (Exception ex) {
                            _logger.LogWarning(ex, "Fallback: Failed to get data from {ApiName}", api.Key);
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                #endregion

                #region Data Filtering/Sorting
                var finalResult = new Dictionary<string, object?>();
                foreach (var api in results) {

                    if (string.IsNullOrEmpty(api.Value)) {
                        finalResult[api.Key] = null;
                        continue;
                    }

                    try 
                    {
                        using var doc = JsonDocument.Parse(api.Value);
                        var root = doc.RootElement;
                        var path = apiConfigs.GetValueOrDefault(api.Key);

                        var array = ExtractArrayFromPath(root, path?.ArrayPath);

                        if (array is not null) {
                            var filtered = ApplyFilter(array, request);
                            var sorted = ApplySort(filtered, request.SortBy, request.SortOrder);
                            finalResult[api.Key] = sorted.ToArray();
                            _logger.LogInformation("Filtered and sorted results for {ApiName}", api.Key);
                        } else {
                            finalResult[api.Key] = root;
                            _logger.LogInformation("Returned full root element for {ApiName} (no array path)", api.Key);
                        }

                    } 
                    catch (Exception ex) 
                    {
                        _logger.LogError(ex, "Error processing JSON for {ApiName}", api.Key);
                        finalResult[api.Key] = null;
                    }
                }
                #endregion
                return Ok(finalResult);

            } catch (Exception ex) 
            {
                _logger.LogError(ex, "Error on GetAggregatedData call");
                return BadRequest();

            }
        }

        private JsonElement[]? ExtractArrayFromPath(JsonElement root, string? path) {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            JsonElement current = root;

            foreach (var segment in segments) {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                    return null;
            }

            if (current.ValueKind == JsonValueKind.Array)
                return current.EnumerateArray().ToArray();

            return null;
        }
        private IEnumerable<JsonElement> ApplyFilter(IEnumerable<JsonElement> elements, AggregatorRequest query) {
            if (string.IsNullOrWhiteSpace(query.FilterBy) || string.IsNullOrWhiteSpace(query.FilterValue))
                return elements;

            return elements.Where(el =>
            {
                if (el.TryGetProperty(query.FilterBy, out var val)) {
                    return val.ToString()?.Contains(query.FilterValue, StringComparison.OrdinalIgnoreCase) ?? false;
                }
                return false;
            });
        }
        private IEnumerable<JsonElement> ApplySort(IEnumerable<JsonElement> elements, string? sortBy, string? sortOrder) {
            if (string.IsNullOrWhiteSpace(sortBy)) return elements;

            var sorted = elements
                .Where(e => e.TryGetProperty(sortBy, out _))
                .Select(e => new {
                    Element = e,
                    Value = e.GetProperty(sortBy).ToString()
                });

            sorted = (sortOrder?.ToLower()) switch {
                "desc" => sorted.OrderByDescending(x => x.Value),
                _ => sorted.OrderBy(x => x.Value)
            };

            return sorted.Select(x => x.Element);
        }
    }
}
