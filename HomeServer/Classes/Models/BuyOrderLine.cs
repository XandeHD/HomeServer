using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeServer.Classes.Models
{
    [Table("buyorderlines")]
    public class BuyOrderLine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LineId { get; set; }

        [Required]
        public int BuyOrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string? Description { get; set; }

        [Required]
        public float Quantity { get; set; }

        public string? Unit { get; set; }

        [Required]
        public float Price { get; set; }
        public DateTime ExpireDate { get; set; }
        public string? Batch { get; set; }

        [Required]
        public int UserId { get; set; } = 0;
        public int GroupId { get; set; } = 0;
    }
}
