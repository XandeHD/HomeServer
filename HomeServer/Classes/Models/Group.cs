using HomeServer.Classes.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeServer.Classes.Models
{
    [Table("groups")]
    public class Group
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Name { get; set; } = "New group";

        [Required]
        public int CreatedById { get; set; }

        [Required]
        public string CreatedBy { get; set; } = "Unknown";

        public string? InviteCode { get; set; } = string.Empty;
    }
}
