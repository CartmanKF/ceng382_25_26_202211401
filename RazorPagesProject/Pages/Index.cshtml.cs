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

namespace RazorPagesProject.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private static List<ClassInformationModel> _classes = new List<ClassInformationModel>();
        private const int PageSize = 10;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            if (_classes.Count == 0)
            {
                GenerateSampleData();
            }
        }

        [BindProperty]
        public ClassInformationModel ClassInfo { get; set; } = new ClassInformationModel();
        public List<ClassInformationModel> Classes { get; set; } = new List<ClassInformationModel>();
        public int? EditId { get; set; }
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public List<string> SelectedColumns { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync(int? pageNumber, string searchTerm, int? editId)
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

            CurrentPage = pageNumber ?? 1;
            SearchTerm = searchTerm ?? string.Empty;
            EditId = editId;

            var filteredData = string.IsNullOrWhiteSpace(SearchTerm)
                ? _classes
                : _classes.Where(c => c.ClassName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                    c.Description.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

            TotalItems = filteredData.Count();
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            Classes = filteredData
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        public async Task<JsonResult> OnGetGetClassAsync(int id)
        {
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
                if (classInfo == null)
                {
                    return BadRequest(new { error = "No data received" });
                }

                ModelState.Clear();

                if (string.IsNullOrWhiteSpace(classInfo.ClassName))
                {
                    ModelState.AddModelError("ClassName", "Class name is required");
                }
                if (string.IsNullOrWhiteSpace(classInfo.Description))
                {
                    ModelState.AddModelError("Description", "Description is required");
                }
                if (classInfo.StudentCount < 1)
                {
                    ModelState.AddModelError("StudentCount", "Student count must be greater than 0");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { errors });
                }

                if (classInfo.Id > 0)
                {
                    // Update existing class
                    var existingClass = _classes.FirstOrDefault(c => c.Id == classInfo.Id);
                    if (existingClass != null)
                    {
                        var index = _classes.IndexOf(existingClass);
                        _classes[index] = classInfo;
                        return new JsonResult(new { success = true });
                    }
                    return NotFound(new { error = "Class not found" });
                }
                else
                {
                    // Add new class
                    classInfo.Id = _classes.Any() ? _classes.Max(c => c.Id) + 1 : 1;
                    _classes.Add(classInfo);
                    return new JsonResult(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while saving the class" });
            }
        }

        public IActionResult OnPostDelete(int id)
        {
            var classToDelete = _classes.FirstOrDefault(c => c.Id == id);
            if (classToDelete != null)
            {
                _classes.Remove(classToDelete);
                TempData["SuccessMessage"] = "Class deleted successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Class not found";
            }
            return RedirectToPage();
        }

        public IActionResult OnPostExportToJson(bool exportFiltered, List<string> selectedColumns)
        {
            try
            {
                // Adım 1: Veriyi filtrele (isteğe göre)
                var dataToExport = exportFiltered ?
                    FilterData(_classes, SearchTerm) :
                    _classes;

                // ✅ Adım 2: Sadece o anki sayfanın verisini al
                dataToExport = dataToExport
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Adım 3: Kolon filtreleme varsa uygulansın
                if (selectedColumns != null && selectedColumns.Any())
                {
                    dataToExport = FilterColumns(dataToExport, selectedColumns);
                }

                // Adım 4: JSON oluştur
                var jsonString = JsonSerializer.Serialize(dataToExport, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var fileName = $"class_data_page{CurrentPage}_{DateTime.Now:yyyyMMddHHmmss}.json";
                return File(
                    System.Text.Encoding.UTF8.GetBytes(jsonString),
                    "application/json",
                    fileName
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error exporting data: {ex.Message}";
                return RedirectToPage("./Index", new { searchTerm = SearchTerm, page = CurrentPage });
            }
        }

        public async Task<IActionResult> OnPostImportFromJson(IFormFile jsonFile)
        {
            try
            {
                if (jsonFile == null || jsonFile.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a JSON file to import.";
                    return RedirectToPage("./Index", new { searchTerm = SearchTerm, page = CurrentPage });
                }

                using (var stream = new MemoryStream())
                {
                    await jsonFile.CopyToAsync(stream);
                    stream.Position = 0;
                    using (var reader = new StreamReader(stream))
                    {
                        var jsonString = await reader.ReadToEndAsync();
                        var importedData = JsonSerializer.Deserialize<List<ClassInformationModel>>(jsonString);
                        
                        if (importedData != null && importedData.Any())
                        {
                            _classes = importedData;
                            TempData["SuccessMessage"] = $"Successfully imported {importedData.Count} classes.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "No valid data found in the JSON file.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error importing data: {ex.Message}";
            }

            return RedirectToPage("./Index", new { searchTerm = SearchTerm, page = CurrentPage });
        }

        private List<ClassInformationModel> FilterData(List<ClassInformationModel> data, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return data;

            return data.Where(c =>
                c.ClassName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Id)
                .ToList();
        }

        private List<ClassInformationModel> FilterColumns(List<ClassInformationModel> data, List<string> selectedColumns)
        {
            return data.Select(item => new ClassInformationModel
            {
                Id = selectedColumns.Contains("Id") ? item.Id : 0,
                ClassName = selectedColumns.Contains("ClassName") ? item.ClassName : null,
                StudentCount = selectedColumns.Contains("StudentCount") ? item.StudentCount : 0,
                Description = selectedColumns.Contains("Description") ? item.Description : null
            }).ToList();
        }

        private void GenerateSampleData()
        {
            var classNames = new[] { "CENG 382", "CENG 384", "CENG 386", "CENG 388", "CENG 390" };
            var descriptions = new[] { 
                "Introduction to Web Development",
                "Advanced Programming Concepts",
                "Database Management Systems",
                "Software Engineering Principles",
                "Computer Networks"
            };

            var random = new Random();
            for (int i = 1; i <= 100; i++)
            {
                _classes.Add(new ClassInformationModel
                {
                    Id = i,
                    ClassName = $"{classNames[random.Next(classNames.Length)]} - Section {random.Next(1, 4)}",
                    StudentCount = random.Next(20, 51),
                    Description = descriptions[random.Next(descriptions.Length)]
                });
            }
        }
    }
}
