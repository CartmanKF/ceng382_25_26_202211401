using System.ComponentModel.DataAnnotations;

namespace RazorPagesProject.Models
{
    public class Class
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public int PersonCount { get; set; }
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public bool IsActive { get; set; }
    }
} 