using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesProject.Models;
using System.Text.Json;

namespace RazorPagesProject.Pages.ClassInformation
{
    public class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<IndexModel> _logger;
        private readonly string _jsonPath;

        public IndexModel(IWebHostEnvironment environment, ILogger<IndexModel> logger)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonPath = Path.Combine(_environment.ContentRootPath, "class_data.json");
            ClassInformation = new List<ClassInformationModel>();
            EditClass = new ClassInformationModel();
        }

        public List<ClassInformationModel> ClassInformation { get; set; }
        
        [BindProperty]
        public ClassInformationModel EditClass { get; set; }

        private async Task LoadClassesFromJson()
        {
            if (System.IO.File.Exists(_jsonPath))
            {
                var json = await System.IO.File.ReadAllTextAsync(_jsonPath);
                var loadedClasses = JsonSerializer.Deserialize<List<ClassInformationModel>>(json);
                ClassInformation = loadedClasses ?? new List<ClassInformationModel>();
            }
            else
            {
                ClassInformation = new List<ClassInformationModel>();
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Session & Cookie check
            var sUser = HttpContext.Session.GetString("username");
            var sToken = HttpContext.Session.GetString("token");
            var sId = HttpContext.Session.GetString("session_id");
            var cUser = Request.Cookies["username"];
            var cToken = Request.Cookies["token"];
            var cId = Request.Cookies["session_id"];
            if (string.IsNullOrEmpty(sUser) || string.IsNullOrEmpty(sToken) || string.IsNullOrEmpty(sId) ||
                string.IsNullOrEmpty(cUser) || string.IsNullOrEmpty(cToken) || string.IsNullOrEmpty(cId) ||
                sUser != cUser || sToken != cToken || sId != cId)
            {
                TempData["ErrorMessage"] = "You must login first!";
                return RedirectToPage("/Login");
            }

            try
            {
                await LoadClassesFromJson();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading class information");
                ClassInformation = new List<ClassInformationModel>();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadClassesFromJson();
                return Page();
            }

            try
            {
                await LoadClassesFromJson();

                if (EditClass.Id > 0)
                {
                    // Update existing class
                    var existingClass = ClassInformation.FirstOrDefault(c => c.Id == EditClass.Id);
                    if (existingClass != null)
                    {
                        var index = ClassInformation.IndexOf(existingClass);
                        if (index != -1)
                        {
                            ClassInformation[index] = EditClass;
                        }
                    }
                }
                else
                {
                    // Add new class
                    EditClass.Id = ClassInformation.Any() ? ClassInformation.Max(c => c.Id) + 1 : 1;
                    ClassInformation.Add(EditClass);
                }

                var updatedJson = JsonSerializer.Serialize(ClassInformation, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(_jsonPath, updatedJson);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving class information");
                ModelState.AddModelError("", "Error saving class information. Please try again.");
                return Page();
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnGetEditAsync(int id)
        {
            try
            {
                await LoadClassesFromJson();
                var classToEdit = ClassInformation.FirstOrDefault(c => c.Id == id);
                if (classToEdit == null)
                {
                    return NotFound();
                }
                return new JsonResult(classToEdit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching class information for editing");
                return StatusCode(500, "An error occurred while fetching class information");
            }
        }
    }
} 