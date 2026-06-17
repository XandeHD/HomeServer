using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeServer.Classes.Models
{
    [Table("buyorders")]
    public class BuyOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string? Description { get; set; }

        [Required]
        public float Total { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string? Market { get; set; }

        public int ListId { get; set; }

        public List<BuyOrderLine> Lines { get; set; } = new();

        [Required]
        public int UserId { get; set; } = 0;
        public int GroupId { get; set; } = 0;
    }
}
