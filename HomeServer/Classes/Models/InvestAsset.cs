using System.ComponentModel.DataAnnotations.Schema;

namespace HomeServer.Classes.Models
{
    [Table("investedassets")]
    public class InvestedAsset
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // Ex: G.MI, UNH
        public string Name { get; set; } = string.Empty; // Ex: Assicurazioni Generali
        public string Currency { get; set; } = "USD";

        // Relação 1:N - Um ativo pode ter várias compras/posições abertas
        public List<StockPosition> Positions { get; set; } = new();
    }

    [Table("stockpositions")]
    // Representa cada compra individual (Posição)
    public class StockPosition
    {
        public int Id { get; set; }
        public int InvestedAssetId { get; set; }
        public InvestedAsset? Asset { get; set; }
        public decimal Quantity { get; set; }     // Quantidade comprada
        public decimal PurchasePrice { get; set; } // Preço de compra unitário
        public DateTime PurchaseDate { get; set; } = DateTime.Today;
        public bool IsOpen { get; set; } = true;   // Se a posição continua ativa ou já foi vendida
        public int UserId { get; set; }
        public int GroupId { get; set; } = 0;
    }

}
