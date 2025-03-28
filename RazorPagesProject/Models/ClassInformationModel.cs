using System.ComponentModel.DataAnnotations;

namespace RazorPagesProject.Models
{
    public class ClassInformationModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Class Name is required")]
        [Display(Name = "Class Name")]
        public string ClassName { get; set; }

        [Required(ErrorMessage = "Student Count is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Student Count must be greater than 0")]
        [Display(Name = "Student Count")]
        public int StudentCount { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
    }
} 