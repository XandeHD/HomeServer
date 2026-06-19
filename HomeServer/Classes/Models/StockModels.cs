using System.Text.Json.Serialization;

namespace HomeServer.Data.Models
{
    // 1. Classe Plana que a UI do Blazor vai usar
    public class StockQuote
    {
        public string Code { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string LongName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
    }

    // 2. Classes de Mapeamento do JSON do Yahoo Finance
    public class YahooFinanceResponse
    {
        [JsonPropertyName("chart")]
        public YahooChart Chart { get; set; } = new();
    }

    public class YahooChart
    {
        [JsonPropertyName("result")]
        public List<YahooResult>? Result { get; set; }
    }

    public class YahooResult
    {
        [JsonPropertyName("meta")]
        public YahooMeta Meta { get; set; } = new();
    }

    public class YahooMeta
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("regularMarketPrice")]
        public decimal RegularMarketPrice { get; set; }

        [JsonPropertyName("exchangeTimezoneName")]
        public string ExchangeTimezoneName { get; set; } = string.Empty;

        [JsonPropertyName("shortName")]
        public string? ShortName { get; set; }

        [JsonPropertyName("longName")]
        public string? LongName { get; set; }
    }


    // 3. Classes de Mapeamento do JSON do Yahoo Search (para pesquisa de ativos)
    public class YahooSearchResponse
    {
        [JsonPropertyName("quotes")]
        public List<YahooSearchQuote> Quotes { get; set; } = new();
    }

    public class YahooSearchQuote
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("shortname")]
        public string? ShortName { get; set; }

        [JsonPropertyName("longname")]
        public string? LongName { get; set; }

        [JsonPropertyName("exchange")]
        public string? Exchange { get; set; }
    }

    // 4. Classes Auxiliares para o Parser do JSON do Yahoo , Trending ---
    public class YahooTrendingResponse
    {
        public TrendingFinance Finance { get; set; }
    }

    public class TrendingFinance
    {
        public List<TrendingResult> Result { get; set; }
    }

    public class TrendingResult
    {
        public List<TrendingQuote> Quotes { get; set; }
    }

    public class TrendingQuote
    {
        public string Symbol { get; set; }
    }


}