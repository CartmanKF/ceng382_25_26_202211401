using System.ComponentModel.DataAnnotations;

namespace RazorPagesProject.Models
{
    public class ClassInformationModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Class name is required")]
        [Display(Name = "Class Name")]
        public string ClassName { get; set; }

        [Required(ErrorMessage = "Student count is required")]
        [Range(1, 100, ErrorMessage = "Student count must be between 1 and 100")]
        [Display(Name = "Student Count")]
        public int StudentCount { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        public string Instructor { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public int Capacity { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
} 