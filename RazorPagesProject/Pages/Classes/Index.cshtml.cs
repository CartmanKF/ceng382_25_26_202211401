using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RazorPagesProject.Data;
using RazorPagesProject.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesProject.Pages.Classes
{
    public class IndexModel : PageModel
    {
        private readonly SchoolDbContext _context;
        public IndexModel(SchoolDbContext context)
        {
            _context = context;
        }

        public IList<Class> ClassList { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public string SearchString { get; set; }

        public async Task OnGetAsync(string searchString, int? pageNumber)
        {
            SearchString = searchString;
            CurrentPage = pageNumber ?? 1;

            var classes = _context.Classes.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                classes = classes.Where(c => 
                    c.Name.Contains(searchString) || 
                    c.Description.Contains(searchString));
            }

            var totalItems = await classes.CountAsync();
            TotalPages = (int)System.Math.Ceiling(totalItems / (double)PageSize);

            ClassList = await classes
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
} 