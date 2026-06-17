using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HomeServer.Classes.Enums;

namespace HomeServer.Data.Models
{
    // 1. Força o EF Core a apontar para a tabela correta em minúsculas como está no teu SQLite
    [Table("salaries")]
    public class Salary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive")]
        public float Amount { get; set; }

        public string? Description { get; set; }

        // 2. Tornar 'string?' permite que o leitor de dados do SQLite leia registos vazios sem crashar
        [Required(ErrorMessage = "The Person field is required.")]
        public string? Owner { get; set; } = "Me";

        // 3. O SQLite armazena booleanos como inteiros que podem vir NULL se a tabela foi gerada manualmente
        public bool? IsActive { get; set; } = false;

        public SalaryType Type { get; set; } = SalaryType.Trabalho;

        [Required]
        public int UserId { get; set; } = 0;
        public int GroupId { get; set; } = 0;

    }
}