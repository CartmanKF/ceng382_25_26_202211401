using System.ComponentModel.DataAnnotations;

namespace RazorPagesProject.Models
{
    public class ClassInformationTable
    {
        public int Id { get; set; }

        [Required]
        public string ClassName { get; set; } = string.Empty;

        public int StudentCount { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;
    }
} 