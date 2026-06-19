using HomeServer.Classes.Models;
using HomeServer.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace HomeServer.Classes.Services
{
    public class StockService
    {
        private readonly HttpClient _httpClient;
        private readonly DataContext _context; // Utiliza o teu DbContext configurado

        public StockService(HttpClient httpClient, DataContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        // Procura um único ativo no Yahoo
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

        // Pesquisa ativos no Yahoo por nome ou ticker
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
            catch { /* Tratar erro ou log se necessário */ }
            return new List<YahooSearchQuote>();
        }

        // Guarda/Adiciona uma nova posição no SQLite
        public async Task AddPositionAsync(string code, string name, decimal quantity, decimal price, DateTime date, int userId, int groupId)
        {
            var asset = await _context.InvestedAssets
                .FirstOrDefaultAsync(a => a.Code == code.Trim().ToUpper());

            if (asset == null)
            {
                asset = new InvestedAsset
                {
                    Code = code.Trim().ToUpper(),
                    Name = name,
                    Currency = code.Contains(".") ? "EUR" : "USD"
                };
                _context.InvestedAssets.Add(asset);
                await _context.SaveChangesAsync();
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

            _context.StockPositions.Add(position);
            await _context.SaveChangesAsync();
        }

        // Recupera todos os ativos com as suas respetivas posições abertas
        public async Task<List<InvestedAsset>> GetActivePortfolioAsync()
        {
            return await _context.InvestedAssets
                .Include(a => a.Positions)
                .Where(a => a.Positions.Any(p => p.IsOpen))
                .ToListAsync();
        }

        // Captura as tendências do Yahoo Finance
        public async Task<List<string>> GetTrendingTickersAsync(string region = "US", int count = 6)
        {
            try
            {
                var url = $"https://query1.finance.yahoo.com/v1/finance/trending/{region}?count={count}";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                var response = await client.GetFromJsonAsync<YahooTrendingResponse>(url);

                if (response?.Finance?.Result?.FirstOrDefault()?.Quotes != null)
                {
                    return response.Finance.Result[0].Quotes
                        .Select(q => q.Symbol)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao procurar ativos populares: {ex.Message}");
            }

            return new List<string> { "AAPL", "NVDA", "TSLA", "MSFT", "AMD" };
        }

        // ================= NOVO: REGISTAR VENDA (ABORDAGEM FIFO) =================
        public async Task RecordSalePositionAsync(string code, decimal quantity, decimal price, DateTime date)
        {
            var asset = await _context.InvestedAssets
                .Include(a => a.Positions)
                .FirstOrDefaultAsync(a => a.Code == code.Trim().ToUpper());

            if (asset == null) return;

            // Filtra apenas as posições abertas e ordena por data de compra (Mais antigas primeiro)
            var openPositions = asset.Positions
                .Where(p => p.IsOpen)
                .OrderBy(p => p.PurchaseDate)
                .ToList();

            decimal remainingToSell = quantity;

            foreach (var position in openPositions)
            {
                if (remainingToSell <= 0) break;

                // Se a posição atual tem menos ou igual quantidade do que a que falta vender, fecha-a por completo
                if (position.Quantity <= remainingToSell)
                {
                    remainingToSell -= position.Quantity;
                    position.Quantity = 0;
                    position.IsOpen = false;
                }
                // Se a posição atual cobre o resto da venda, abate e encerra o ciclo
                else
                {
                    position.Quantity -= remainingToSell;
                    remainingToSell = 0;
                }
            }

            await _context.SaveChangesAsync();
        }

        // ================= NOVO: APAGAR ATIVO/POSIÇÕES ADICIONADOS POR ENGANO =================
        public async Task DeleteAssetPositionsAsync(string code)
        {
            var asset = await _context.InvestedAssets
                .Include(a => a.Positions)
                .FirstOrDefaultAsync(a => a.Code == code.Trim().ToUpper());

            if (asset != null)
            {
                // 1. Remove todas as linhas de posições associadas no SQLite
                if (asset.Positions != null && asset.Positions.Any())
                {
                    _context.StockPositions.RemoveRange(asset.Positions);
                }

                // 2. Remove o cabeçalho do ativo para não deixar registos órfãos na tabela principal
                _context.InvestedAssets.Remove(asset);

                await _context.SaveChangesAsync();
            }
        }
    }
}