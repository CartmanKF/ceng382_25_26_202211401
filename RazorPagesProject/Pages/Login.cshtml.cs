using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesProject.Models;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace RazorPagesProject.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IWebHostEnvironment environment, ILogger<LoginModel> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string ErrorMessage { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            // If user is already logged in, redirect to ClassInformation
            if (HttpContext.Session.GetString("username") != null)
            {
                return RedirectToPage("/ClassInformation/Index");
            }

            // Create users.json if it doesn't exist
            var jsonPath = Path.Combine(_environment.WebRootPath, "data", "users.json");
            if (!System.IO.File.Exists(jsonPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);
                var defaultUsers = new List<User>
                {
                    new User 
                    { 
                        Username = "admin", 
                        Password = "admin123", 
                        Role = "Admin",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };
                var json = JsonSerializer.Serialize(defaultUsers, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(jsonPath, json);
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Please enter both username and password.";
                return Page();
            }

            try
            {
                var jsonPath = Path.Combine(_environment.WebRootPath, "data", "users.json");
                if (System.IO.File.Exists(jsonPath))
                {
                    var json = System.IO.File.ReadAllText(jsonPath);
                    var users = JsonSerializer.Deserialize<List<User>>(json);

                    var user = users?.FirstOrDefault(u => 
                        u.Username == Username && 
                        u.Password == Password && 
                        u.IsActive);

                    if (user != null)
                    {
                        // Generate token
                        var token = GenerateToken();

                        // Store in session
                        HttpContext.Session.SetString("username", user.Username);
                        HttpContext.Session.SetString("token", token);
                        HttpContext.Session.SetString("session_id", HttpContext.Session.Id);

                        // Store in cookies
                        var cookieOptions = new CookieOptions
                        {
                            Expires = DateTime.Now.AddMinutes(30),
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict
                        };

                        Response.Cookies.Append("username", user.Username, cookieOptions);
                        Response.Cookies.Append("token", token, cookieOptions);
                        Response.Cookies.Append("session_id", HttpContext.Session.Id, cookieOptions);

                        TempData.Remove("ErrorMessage");
                        return RedirectToPage("/Index");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ErrorMessage = "An error occurred during login. Please try again.";
                return Page();
            }

            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        private string GenerateToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenData = new byte[32];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData);
        }
    }
} 