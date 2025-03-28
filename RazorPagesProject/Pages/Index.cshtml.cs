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

        public List<ClassInformationModel> Classes => _classes;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
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
