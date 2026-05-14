using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using SmartMacroAI.Models;

namespace SmartMacroAI.Core;

/// <summary>
/// Client-side service for the SmartMacroAI community marketplace.
/// Handles uploading, browsing, downloading, and rating shared macros.
/// </summary>
public sealed class MarketplaceService
{
    private const string DefaultApiBase = "https://api.smartmacroai.com/v1";
    private static readonly string CacheDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "marketplace_cache");

    private readonly HttpClient _httpClient;
    private string _apiBase;

    public MarketplaceService(string? apiBaseUrl = null)
    {
        _apiBase = apiBaseUrl ?? DefaultApiBase;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SmartMacroAI/1.6.0");
        Directory.CreateDirectory(CacheDir);
    }

    /// <summary>
    /// Searches the marketplace for macros matching the query.
    /// </summary>
    public async Task<MarketplaceSearchResult> SearchAsync(string query, string? category = null, int page = 1, int pageSize = 20, CancellationToken token = default)
    {
        try
        {
            string url = $"{_apiBase}/macros/search?q={Uri.EscapeDataString(query)}&page={page}&size={pageSize}";
            if (!string.IsNullOrWhiteSpace(category))
                url += $"&category={Uri.EscapeDataString(category)}";

            var response = await _httpClient.GetAsync(url, token);
            if (!response.IsSuccessStatusCode)
            {
                return new MarketplaceSearchResult { Error = $"Server returned {response.StatusCode}" };
            }

            var result = await response.Content.ReadFromJsonAsync<MarketplaceSearchResult>(cancellationToken: token);
            return result ?? new MarketplaceSearchResult { Error = "Empty response" };
        }
        catch (HttpRequestException ex)
        {
            return new MarketplaceSearchResult { Error = $"Connection failed: {ex.Message}", IsOffline = true };
        }
        catch (TaskCanceledException)
        {
            return new MarketplaceSearchResult { Error = "Request timed out", IsOffline = true };
        }
        catch (Exception ex)
        {
            return new MarketplaceSearchResult { Error = ex.Message };
        }
    }

    /// <summary>
    /// Downloads a macro from the marketplace and saves it locally.
    /// </summary>
    public async Task<(bool Success, string? LocalPath, string? Error)> DownloadAsync(string macroId, CancellationToken token = default)
    {
        try
        {
            string url = $"{_apiBase}/macros/{macroId}/download";
            var response = await _httpClient.GetAsync(url, token);

            if (!response.IsSuccessStatusCode)
                return (false, null, $"Download failed: {response.StatusCode}");

            string json = await response.Content.ReadAsStringAsync(token);

            // Validate JSON structure
            var script = JsonSerializer.Deserialize<MacroScript>(json);
            if (script is null)
                return (false, null, "Invalid macro format");

            // Save to scripts folder
            string scriptsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
            Directory.CreateDirectory(scriptsDir);

            string fileName = SanitizeFileName(script.Name) + ".json";
            string filePath = Path.Combine(scriptsDir, fileName);

            // Avoid overwriting
            int counter = 1;
            while (File.Exists(filePath))
            {
                fileName = $"{SanitizeFileName(script.Name)}_{counter++}.json";
                filePath = Path.Combine(scriptsDir, fileName);
            }

            await File.WriteAllTextAsync(filePath, json, token);
            return (true, filePath, null);
        }
        catch (HttpRequestException ex)
        {
            return (false, null, $"Connection failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Publishes a macro to the marketplace.
    /// </summary>
    public async Task<(bool Success, string? Error)> PublishAsync(MacroScript script, MarketplaceMetadata metadata, CancellationToken token = default)
    {
        try
        {
            // Validate script
            if (script.Actions.Count == 0)
                return (false, "Cannot publish an empty macro");

            var payload = new
            {
                metadata.Name,
                metadata.Description,
                metadata.Category,
                metadata.Tags,
                metadata.Author,
                Script = JsonSerializer.Serialize(script),
            };

            string url = $"{_apiBase}/macros/publish";
            var response = await _httpClient.PostAsJsonAsync(url, payload, token);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(token);
                return (false, $"Publish failed ({response.StatusCode}): {body}");
            }

            return (true, null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Submits a rating for a downloaded macro.
    /// </summary>
    public async Task<(bool Success, string? Error)> RateAsync(string macroId, int stars, CancellationToken token = default)
    {
        if (stars is < 1 or > 5)
            return (false, "Rating must be 1-5 stars");

        try
        {
            string url = $"{_apiBase}/macros/{macroId}/rate";
            var payload = new { Stars = stars };
            var response = await _httpClient.PostAsJsonAsync(url, payload, token);

            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, $"Rating failed: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string SanitizeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string safe = new(name.Where(c => !invalid.Contains(c)).ToArray());
        return safe.Length > 50 ? safe[..50] : (safe.Length == 0 ? "macro" : safe);
    }
}

/// <summary>Metadata for publishing a macro to the marketplace.</summary>
public class MarketplaceMetadata
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "general";
    public List<string> Tags { get; set; } = [];
    public string Author { get; set; } = "";
}

/// <summary>Search result from the marketplace API.</summary>
public class MarketplaceSearchResult
{
    public List<MarketplaceItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public string? Error { get; set; }
    public bool IsOffline { get; set; }
}

/// <summary>A single macro listing in the marketplace.</summary>
public class MarketplaceItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public int DownloadCount { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public DateTime LastUpdated { get; set; }
    public string StarDisplay => AverageRating > 0 ? $"⭐ {AverageRating:F1} ({RatingCount})" : "No ratings";
}
