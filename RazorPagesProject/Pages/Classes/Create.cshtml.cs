using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesProject.Data;
using RazorPagesProject.Models;
using System.Threading.Tasks;

namespace RazorPagesProject.Pages.Classes
{
    public class CreateModel : PageModel
    {
        private readonly SchoolDbContext _context;
        public CreateModel(SchoolDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Class Class { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            _context.Classes.Add(Class);
            await _context.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
} 