using System.ComponentModel.DataAnnotations;

namespace Backend_ASP.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(160)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 999999)]
        public decimal Price { get; set; }

        public bool IsAvailable { get; set; } = true;

        public int DisplayOrder { get; set; }
    }
}
