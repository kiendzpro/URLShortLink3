using Microsoft.AspNetCore.Mvc;
using System.Text;
using URl.Services; // Thêm service để xử lý lưu vào database

namespace URL.Controllers
{
    public class HomeController : Controller
    {
        private readonly UrlShortenerService _urlShortenerService;

        // Inject UrlShortenerService vào controller
        public HomeController(UrlShortenerService urlShortenerService)
        {
            _urlShortenerService = urlShortenerService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Shorten(string originalUrl)
        {
            if (string.IsNullOrEmpty(originalUrl))
            {
                ViewBag.Error = "Please enter a valid URL.";
                return View("Index");
            }

            // Validate and shorten URL
            var (shortCode, errorMessage, isPrivate) = await _urlShortenerService.ValidateAndShortenUrlAsync(originalUrl);
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                ViewBag.Error = errorMessage;
                return View("Index");
            }
            
            if (shortCode == null)
            {
                ViewBag.Error = "Failed to generate short URL. Please try again.";
                return View("Index");
            }

            ViewBag.OriginalUrl = originalUrl;
            ViewBag.ShortUrl = $"{Request.Scheme}://{Request.Host}/go/{shortCode}";
            
            if (isPrivate)
            {
                ViewBag.Message = "URL shortened successfully. Note: This is a private URL.";
            }
            else
            {
                ViewBag.Message = "URL shortened successfully!";
            }

            return View("Index");
        }

        [HttpGet("/go/{shortCode}")]
        public async Task<IActionResult> RedirectToOriginal(string shortCode)
        {
            var originalUrl = await _urlShortenerService.GetOriginalUrlAsync(shortCode);
            if (originalUrl == null)
            {
                return NotFound("Shortened URL not found.");
            }

            return Redirect(originalUrl);
        }
    }
}
