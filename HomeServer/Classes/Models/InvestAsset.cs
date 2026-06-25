using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeServer.Classes.Models
{
    [Table("investedassets")]
    public class InvestedAsset
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";

        public List<StockPosition> Positions { get; set; } = new();
    }

    [Table("stockpositions")]
    public class StockPosition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int InvestedAssetId { get; set; }
        public InvestedAsset? Asset { get; set; }
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Today;
        public bool IsOpen { get; set; } = true;
        public int UserId { get; set; }
        public int GroupId { get; set; } = 0;
    }
}
