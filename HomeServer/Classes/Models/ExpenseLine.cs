// ExpenseLine.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeServer.Classes.Models
{
    [Table("expenselines")]
    public class ExpenseLine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ExpenseId { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public float Quantity { get; set; } = 1;

        [Required]
        public float Price { get; set; } // Unit price

        [Required]
        public int UserId { get; set; } = 0;
        public int GroupId { get; set; } = 0;
    }
}