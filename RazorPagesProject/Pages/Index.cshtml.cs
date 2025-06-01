using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using RazorPagesProject.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace RazorPagesProject.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly string _jsonPath;
        private List<ClassInformationModel> _classes;
        private const int PageSize = 10;

        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _jsonPath = Path.Combine(environment.ContentRootPath, "class_data.json");
            _classes = new List<ClassInformationModel>();
            Classes = new List<ClassInformationModel>();
            ClassInfo = new ClassInformationModel();
            SearchTerm = string.Empty;
            SelectedColumns = new List<string>();
        }

        [BindProperty]
        public ClassInformationModel ClassInfo { get; set; }
        public List<ClassInformationModel> Classes { get; set; }
        public int CurrentPage { get; set; } = 1;
        public string SearchTerm { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public List<string> SelectedColumns { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? EditId { get; set; }

        private async Task LoadClassesFromFileAsync()
        {
            try
            {
                if (System.IO.File.Exists(_jsonPath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(_jsonPath);
                    _classes = JsonSerializer.Deserialize<List<ClassInformationModel>>(json) ?? new List<ClassInformationModel>();
                }
                else
                {
                    _classes = new List<ClassInformationModel>();
                    GenerateSampleData();
                    await SaveClassesToFileAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoadClassesFromFile exception: {ex}");
                _classes = new List<ClassInformationModel>();
            }
        }

        private async Task SaveClassesToFileAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_classes, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(_jsonPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SaveClassesToFile exception: {ex}");
            }
        }

        public async Task<IActionResult> OnGetAsync(int? pageNumber, string searchTerm)
        {
            await LoadClassesFromFileAsync();

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

            _logger.LogInformation($"OnGet called with pageNumber={pageNumber}, searchTerm={searchTerm}");

            // Set current page and search term
            CurrentPage = pageNumber ?? 1;
            SearchTerm = searchTerm ?? string.Empty;

            // Filter data based on search term and active status
            var filteredData = _classes.Where(c => c.IsActive).ToList();
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                filteredData = filteredData.Where(c => 
                    c.ClassName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            _logger.LogInformation($"Filtered data count: {filteredData.Count()}");

            // Calculate pagination
            TotalItems = filteredData.Count();
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

            // Ensure current page is within valid range
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            _logger.LogInformation($"Page: {CurrentPage}, TotalPages: {TotalPages}, Skip: {(CurrentPage - 1) * PageSize}, Take: {PageSize}");

            // Get the current page of data
            Classes = filteredData
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            _logger.LogInformation($"Returned items count: {Classes.Count}");
            if (Classes.Any())
            {
                _logger.LogInformation($"First item ID: {Classes.First().Id}");
                _logger.LogInformation($"Last item ID: {Classes.Last().Id}");
            }

            return Page();
        }

        public async Task<JsonResult> OnGetGetClassAsync(int id)
        {
            await LoadClassesFromFileAsync();
            var classInfo = _classes.FirstOrDefault(c => c.Id == id);
            if (classInfo == null)
            {
                return new JsonResult(new { error = "Class not found" }) { StatusCode = 404 };
            }
            return new JsonResult(classInfo);
        }

        public async Task<IActionResult> OnPostAsync([FromBody] ClassInformationModel classInfo)
        {
            try
            {
                _logger.LogInformation($"Received data: {JsonSerializer.Serialize(classInfo)}");

                if (classInfo == null)
                {
                    return BadRequest(new { error = "No data received" });
                }

                // Clear ModelState to prevent old validation errors
                ModelState.Clear();

                // Manual validation
                if (string.IsNullOrWhiteSpace(classInfo.ClassName))
                {
                    ModelState.AddModelError("ClassName", "Class name is required");
                }
                if (string.IsNullOrWhiteSpace(classInfo.Description))
                {
                    ModelState.AddModelError("Description", "Description is required");
                }
                if (classInfo.StudentCount < 1 || classInfo.StudentCount > 100)
                {
                    ModelState.AddModelError("StudentCount", "Student count must be between 1 and 100");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    _logger.LogWarning($"Validation errors: {string.Join(", ", errors)}");
                    return BadRequest(new { errors });
                }

                await LoadClassesFromFileAsync();

                if (classInfo.Id > 0)
                {
                    // Update existing class
                    var existingClass = _classes.FirstOrDefault(c => c.Id == classInfo.Id);
                    if (existingClass != null)
                    {
                        var index = _classes.IndexOf(existingClass);
                        _classes[index] = classInfo;
                        await SaveClassesToFileAsync();
                        return new JsonResult(new { success = true });
                    }
                    return NotFound(new { error = "Class not found" });
                }
                else
                {
                    // Add new class
                    classInfo.Id = _classes.Any() ? _classes.Max(c => c.Id) + 1 : 1;
                    classInfo.IsActive = true;  // Set new records as active by default
                    _classes.Add(classInfo);
                    await SaveClassesToFileAsync();
                    return new JsonResult(new { success = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in OnPostAsync: {ex}");
                return StatusCode(500, new { error = "An error occurred while saving the class" });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await LoadClassesFromFileAsync();
            var classToDelete = _classes.FirstOrDefault(c => c.Id == id);
            if (classToDelete != null)
            {
                classToDelete.IsActive = false;  // Soft delete by setting IsActive to false
                await SaveClassesToFileAsync();
                TempData["SuccessMessage"] = "Class deleted successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Class not found";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostExportToJsonAsync(bool exportFiltered)
        {
            await LoadClassesFromFileAsync();

            var dataToExport = exportFiltered && !string.IsNullOrWhiteSpace(SearchTerm)
                ? _classes.Where(c => c.ClassName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                    c.Description.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                : _classes;

            var jsonString = JsonSerializer.Serialize(dataToExport, new JsonSerializerOptions { WriteIndented = true });
            var fileName = $"class_data_export_{DateTime.Now:yyyyMMddHHmmss}.json";

            return File(System.Text.Encoding.UTF8.GetBytes(jsonString), "application/json", fileName);
        }

        private void GenerateSampleData()
        {
            if (_classes.Any()) return;

            var subjects = new[] { "Math", "Science", "History", "English", "Art" };
            var levels = new[] { "Beginner", "Intermediate", "Advanced" };
            var descriptions = new[] {
                "Introduction to basic concepts",
                "Comprehensive study of core principles",
                "Advanced topics and applications",
                "Practical exercises and projects",
                "Theory and practice combined"
            };

            var random = new Random();
            for (int i = 1; i <= 105; i++)
            {
                _classes.Add(new ClassInformationModel
                {
                    Id = i,
                    ClassName = $"{subjects[random.Next(subjects.Length)]} - {levels[random.Next(levels.Length)]}",
                    StudentCount = random.Next(10, 31),
                    Description = descriptions[random.Next(descriptions.Length)],
                    IsActive = true  // Set sample data as active by default
                });
            }
        }
    }

    public class ClassUpdateModel
    {
        public ClassInformationModel ClassInfo { get; set; }
    }
}
