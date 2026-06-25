using HomeServer.Classes.Models;
using HomeServer.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace HomeServer.Classes.Services
{
    public class StockService
    {
        private readonly HttpClient _httpClient;
        private readonly IDbContextFactory<DataContext> _dbFactory;

        public StockService(HttpClient httpClient, IDbContextFactory<DataContext> dbFactory)
        {
            _httpClient = httpClient;
            _dbFactory = dbFactory;
        }

        public async Task<StockQuote?> GetQuoteAsync(string symbol)
        {
            try
            {
                var cleanSymbol = symbol.Trim().ToUpper();
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{cleanSymbol}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResult = await response.Content.ReadFromJsonAsync<YahooFinanceResponse>();
                    var meta = jsonResult?.Chart?.Result?.FirstOrDefault()?.Meta;

                    if (meta != null)
                    {
                        return new StockQuote
                        {
                            Code = meta.Symbol,
                            Price = meta.RegularMarketPrice,
                            Currency = meta.Currency,
                            Location = meta.ExchangeTimezoneName,
                            ShortName = meta.ShortName ?? meta.Symbol,
                            LongName = meta.LongName ?? meta.ShortName ?? meta.Symbol
                        };
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<StockQuote>> GetMultipleQuotesAsync(List<string> symbols)
        {
            if (symbols == null || !symbols.Any()) return new List<StockQuote>();

            var tasks = symbols.Select(symbol => GetQuoteAsync(symbol));
            var results = await Task.WhenAll(tasks);

            return results.Where(q => q != null).Cast<StockQuote>().ToList();
        }

        public async Task<List<YahooSearchQuote>> SearchStocksAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<YahooSearchQuote>();

            try
            {
                var url = $"https://query1.finance.yahoo.com/v1/finance/search?q={Uri.EscapeDataString(query)}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows) AppleWebKit/537.36");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var searchResult = await response.Content.ReadFromJsonAsync<YahooSearchResponse>();
                    return searchResult?.Quotes ?? new List<YahooSearchQuote>();
                }
            }
            catch { }
            return new List<YahooSearchQuote>();
        }

        public async Task AddPositionAsync(string code, string name, decimal quantity, decimal price, DateTime date, int userId, int groupId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var asset = await db.InvestedAssets
                .FirstOrDefaultAsync(a => a.Code == code.Trim().ToUpper());

            if (asset == null)
            {
                asset = new InvestedAsset
                {
                    Code = code.Trim().ToUpper(),
                    Name = name,
                    Currency = code.Contains(".") ? "EUR" : "USD"
                };
                db.InvestedAssets.Add(asset);
                await db.SaveChangesAsync();
            }

            var position = new StockPosition
            {
                InvestedAssetId = asset.Id,
                Quantity = quantity,
                PurchasePrice = price,
                PurchaseDate = date,
                IsOpen = true,
                UserId = userId,
                GroupId = groupId
            };

            db.StockPositions.Add(position);
            await db.SaveChangesAsync();
        }

        public async Task<List<InvestedAsset>> GetActivePortfolioAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.InvestedAssets
                .Include(a => a.Positions)
                .Where(a => a.Positions.Any(p => p.IsOpen))
                .ToListAsync();
        }

        public async Task<List<string>> GetTrendingTickersAsync(string region = "US", int count = 6)
        {
            try
            {
                var url = $"https://query1.finance.yahoo.com/v1/finance/trending/{region}?count={count}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<YahooTrendingResponse>();
                    if (data?.Finance?.Result?.FirstOrDefault()?.Quotes != null)
                    {
                        return data.Finance.Result[0].Quotes
                            .Select(q => q.Symbol)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao procurar ativos populares: {ex.Message}");
            }

            return new List<string> { "AAPL", "NVDA", "TSLA", "MSFT", "AMD" };
        }

        public async Task RecordSalePositionAsync(string code, decimal quantity, decimal price, DateTime date)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var asset = await db.InvestedAssets
                .Include(a => a.Positions)
                .FirstOrDefaultAsync(a => a.Code == code.Trim().ToUpper());

            if (asset == null) return;

            var openPositions = asset.Positions
                .Where(p => p.IsOpen)
                .OrderBy(p => p.PurchaseDate)
                .ToList();

            decimal remainingToSell = quantity;

            foreach (var position in openPositions)
            {
                if (remainingToSell <= 0) break;

                if (position.Quantity <= remainingToSell)
                {
                    remainingToSell -= position.Quantity;
                    position.Quantity = 0;
                    position.IsOpen = false;
                }
                else
                {
                    position.Quantity -= remainingToSell;
                    remainingToSell = 0;
                }
            }

            await db.SaveChangesAsync();
        }

        public async Task DeleteAssetPositionsAsync(string code)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var asset = await db.InvestedAssets
                .Include(a => a.Positions)
                .FirstOrDefaultAsync(a => a.Code == code.Trim().ToUpper());

            if (asset != null)
            {
                if (asset.Positions != null && asset.Positions.Any())
                {
                    db.StockPositions.RemoveRange(asset.Positions);
                }

                db.InvestedAssets.Remove(asset);
                await db.SaveChangesAsync();
            }
        }
    }
}
