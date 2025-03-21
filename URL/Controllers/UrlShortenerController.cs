using Microsoft.AspNetCore.Mvc;
using URl.Services;
using System.Net;

namespace URl.Controllers
{
    [ApiController]
    [Route("api")]
    public class UrlShortenerController : ControllerBase
    {
        private readonly UrlShortenerService _urlShortenerService;

        // Inject trực tiếp UrlShortenerService từ DI container
        public UrlShortenerController(UrlShortenerService urlShortenerService)
        {
            _urlShortenerService = urlShortenerService;
        }

        [HttpPost("shorten")]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Shorten([FromBody] UrlRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.OriginalUrl))
                return BadRequest(new { error = "Invalid URL" });

            var (shortCode, errorMessage, isPrivate) = await _urlShortenerService.ValidateAndShortenUrlAsync(request.OriginalUrl);
            
            if (!string.IsNullOrEmpty(errorMessage))
                return BadRequest(new { error = errorMessage });
        
            if (shortCode == null)
                return BadRequest(new { error = "Failed to generate short URL. Please try again." });

            string shortUrl = $"{Request.Scheme}://{Request.Host}/api/{shortCode}";
            
            if (isPrivate)
            {
                return Ok(new { shortUrl, message = "URL shortened successfully. Note: This is a private URL." });
            }
            
            return Ok(new { shortUrl, message = "URL shortened successfully!" });
        }

        [HttpGet("{shortCode}")]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> RedirectUrl(string shortCode)
        {
            var originalUrl = await _urlShortenerService.GetOriginalUrlAsync(shortCode);
            if (originalUrl == null)
                return NotFound(new { error = "URL not found" });

            // Get stats for the shortened URL
            var urlInfo = await _urlShortenerService.GetUrlInfoAsync(shortCode);
            int clickCount = urlInfo?.ClickCount ?? 0;

            return Ok(new { 
                message = "Click on the link to redirect", 
                url = originalUrl,
                clicks = clickCount
            });
        }
    }

    public class UrlRequest
    {
        public string OriginalUrl { get; set; } = string.Empty;
    }
}
