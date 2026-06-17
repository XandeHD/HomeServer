namespace HomeServer.Classes.Models
{
    public class GeminiModel
    {
        public class GeminiResponse
        {
            public Candidate[] candidates { get; set; }
        }

        public class Candidate
        {
            public Content content { get; set; }
        }

        public class Content
        {
            public Part[] parts { get; set; }
        }

        public class Part
        {
            public string text { get; set; }
        }

        public class ReceiptData
        {
            public string Market { get; set; }
            public string Date { get; set; }
            public float Total { get; set; }
            public List<BuyOrderLine> Items { get; set; }
        }
    }
}
