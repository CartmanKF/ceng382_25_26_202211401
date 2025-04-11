using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesProject.Models;

namespace RazorPagesProject.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private static List<ClassInformationModel> _classes = new List<ClassInformationModel>();
        private static int _nextId = 1;

        [BindProperty]
        public ClassInformationModel ClassInfo { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EditId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public List<ClassInformationTable> FilteredClasses { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            // Generate sample data if empty
            if (_classes.Count == 0)
            {
                GenerateSampleData();
            }
        }

        private void GenerateSampleData()
        {
            var random = new Random();
            for (int i = 0; i < 100; i++)
            {
                _classes.Add(new ClassInformationModel
                {
                    Id = _nextId++,
                    ClassName = $"Class {i + 1}",
                    StudentCount = random.Next(20, 50),
                    Description = $"Description for Class {i + 1}"
                });
            }
        }

        public void OnGet()
        {
            if (EditId.HasValue)
            {
                var classToEdit = _classes.FirstOrDefault(c => c.Id == EditId.Value);
                if (classToEdit != null)
                {
                    ClassInfo = new ClassInformationModel
                    {
                        Id = classToEdit.Id,
                        ClassName = classToEdit.ClassName,
                        StudentCount = classToEdit.StudentCount,
                        Description = classToEdit.Description
                    };
                }
            }

            // Apply filtering
            var query = _classes.AsQueryable();
            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(c => 
                    c.ClassName.Contains(SearchString, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(SearchString, StringComparison.OrdinalIgnoreCase));
            }

            // Calculate pagination
            TotalPages = (int)Math.Ceiling(query.Count() / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages));

            // Apply pagination
            var paginatedQuery = query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize);

            // Convert to ClassInformationTable
            FilteredClasses = paginatedQuery.Select(c => new ClassInformationTable
            {
                Id = c.Id,
                ClassName = c.ClassName,
                StudentCount = c.StudentCount,
                Description = c.Description
            }).ToList();
        }

        public IActionResult OnPostAdd()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (EditId.HasValue)
            {
                var existingClass = _classes.FirstOrDefault(c => c.Id == EditId.Value);
                if (existingClass != null)
                {
                    existingClass.ClassName = ClassInfo.ClassName;
                    existingClass.StudentCount = ClassInfo.StudentCount;
                    existingClass.Description = ClassInfo.Description;
                }
            }
            else
            {
                ClassInfo.Id = _nextId++;
                _classes.Add(ClassInfo);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int id)
        {
            var classToDelete = _classes.FirstOrDefault(c => c.Id == id);
            if (classToDelete != null)
            {
                _classes.Remove(classToDelete);
            }

            return RedirectToPage();
        }
    }
}
