using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeServer.Classes.Models
{
    [Table("expenses")]
    public class Expense
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        public DateTime? EndDate { get; set; }

        [Required]
        public string Place { get; set; } = string.Empty;

        [Required]
        public float Total { get; set; }

        public string? Description { get; set; }

        [Required]
        public string Category { get; set; } = "Other"; // Restaurant, Shopping, Transport, etc.

        public List<ExpenseLine> Lines { get; set; } = new();

        // Add this property to your Expense class
        public bool IsRecurring { get; set; } = false;

        [Required]
        public int UserId { get; set; } = 0;
        public int GroupId { get; set; } = 0;
    }
}
