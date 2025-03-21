using MongoDB.Driver;
using URl.Models;
using URl.Data;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace URl.Services
{
    public class UrlShortenerService
    {
        private readonly MongoDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;

        public UrlShortenerService(MongoDbContext dbContext, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
            _httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "URL-Shortener-Validator/1.0");
        }

        public async Task<(string? shortCode, string? errorMessage, bool isPrivate)> ValidateAndShortenUrlAsync(string originalUrl, string userId = "")
        {
            // Check if URL is valid
            if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out Uri uri) || 
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                return (null, "Please enter a valid URL with http:// or https://", false);
            }

            try
            {
                // Check if URL exists
                var response = await _httpClient.GetAsync(originalUrl);
                
                // Handle status codes
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return (null, "URL does not exist.", false);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized || 
                        response.StatusCode == HttpStatusCode.Forbidden)
                {
                    // Still shorten private URLs but mark as private
                    string shortCode = await ShortenUrlAsync(originalUrl, userId);
                    return (shortCode, null, true);
                }
                else if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                {
                    return (null, $"The URL returned an error: {response.StatusCode}", false);
                }
                
                // URL exists normally
                string normalShortCode = await ShortenUrlAsync(originalUrl, userId);
                return (normalShortCode, null, false);
            }
            catch (TaskCanceledException)
            {
                // Timeout occurred
                return (null, "URL validation timed out. The site might be unavailable.", false);
            }
            catch (HttpRequestException ex)
            {
                // Network or DNS errors
                return (null, $"Could not connect to the URL: {ex.Message}", false);
            }
            catch (Exception)
            {
                // For any other errors, we'll still allow shortening
                string shortCode = await ShortenUrlAsync(originalUrl, userId);
                return (shortCode, null, false);
            }
        }

        public async Task<string> ShortenUrlAsync(string originalUrl, string userId = "")
        {
            // Check if URL already exists in database
            var existingUrl = await _dbContext.ShortUrls
                .Find(u => u.OriginalUrl == originalUrl)
                .FirstOrDefaultAsync();

            if (existingUrl != null)
            {
                // URL already exists, return existing short code
                return existingUrl.ShortCode;
            }

            // Generate a short code
            string shortCode = GenerateShortCode(originalUrl);

            // Create new ShortUrl entity
            var shortUrl = new ShortUrl
            {
                OriginalUrl = originalUrl,
                ShortCode = shortCode,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };

            // Save to MongoDB
            await _dbContext.ShortUrls.InsertOneAsync(shortUrl);
            
            // Add to cache for quick retrieval
            _cache.Set(shortCode, originalUrl, TimeSpan.FromDays(7));

            return shortCode;
        }

        public async Task<string> GetOriginalUrlAsync(string shortCode)
        {
            // Try to get from cache first
            if (_cache.TryGetValue(shortCode, out string cachedUrl))
            {
                // Increment click count asynchronously without waiting
                _ = IncrementClickCountAsync(shortCode);
                return cachedUrl;
            }

            // Get from database if not in cache
            var shortUrl = await _dbContext.ShortUrls
                .Find(u => u.ShortCode == shortCode)
                .FirstOrDefaultAsync();

            if (shortUrl == null)
            {
                return null;
            }

            // Add to cache for future requests
            _cache.Set(shortCode, shortUrl.OriginalUrl, TimeSpan.FromDays(7));
            
            // Increment click count asynchronously without waiting
            _ = IncrementClickCountAsync(shortCode);

            return shortUrl.OriginalUrl;
        }

        private async Task IncrementClickCountAsync(string shortCode)
        {
            try
            {
                // Update the click count in the database
                var update = Builders<ShortUrl>.Update.Inc(u => u.ClickCount, 1);
                await _dbContext.ShortUrls.UpdateOneAsync(u => u.ShortCode == shortCode, update);
            }
            catch
            {
                // Silently fail if updating click count fails
                // This is non-critical functionality
            }
        }

        private string GenerateShortCode(string url)
        {
            // Use MD5 hash to generate a consistent short code
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
                
                // Convert first 8 bytes to base64 and make URL-safe
                string base64 = Convert.ToBase64String(hashBytes, 0, 8)
                    .Replace("/", "_")
                    .Replace("+", "-")
                    .Replace("=", "");
                
                return base64.Substring(0, 6); // Take first 6 characters
            }
        }

        public async Task<ShortUrl> GetUrlInfoAsync(string shortCode)
        {
            return await _dbContext.ShortUrls
                .Find(u => u.ShortCode == shortCode)
                .FirstOrDefaultAsync();
        }
    }
}
