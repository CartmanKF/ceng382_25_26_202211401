using System.ComponentModel.DataAnnotations;

namespace RazorPagesProject.Models
{
    public class ClassInformationTable
    {
        public int Id { get; set; }

        [Display(Name = "Class Name")]
        public string ClassName { get; set; }

        [Display(Name = "Student Count")]
        public int StudentCount { get; set; }

        public string Description { get; set; }
    }
} 