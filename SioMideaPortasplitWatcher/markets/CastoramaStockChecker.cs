using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SioMideaPortasplitWatcher.markets
{
    public class CastoramaStockChecker : IStockChecker
    {
        public class Store
        {
            public string StoreId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;

            public int AvailableQuantity { get; set; }
        }

        public class StockEventArgs : EventArgs
        {
            public Store Store { get; }
            public int NewQuantity { get; }

            public StockEventArgs(Store store, int newQuantity)
            {
                Store = store;
                NewQuantity = newQuantity;
            }
        }

        public event EventHandler<StockEventArgs>? NewStockDetected;
        public event EventHandler<StockEventArgs>? StockOutDetected;

        private IPage? _page;

        public readonly string ProductId;
        public readonly string ProductName;
        public readonly double Latitude;
        public readonly double Longitude;

        private readonly Dictionary<string, int> _previousStockState = new();

        public CastoramaStockChecker(string productName, double latitude, double longitude, string productId)
        {
            ProductName = productName;
            ProductId = productId;
            Latitude = latitude;
            Longitude = longitude;
        }

        public async Task<IPage> CreatePage()
        {
            _page = await Browser.CreatePage();
            return _page;
        }

         public async Task CheckStockAsync()
        {
            if (_page?.IsClosed != false)
            {
                await CreatePage();
                await _page.GotoAsync("https://www.castorama.fr", Browser.GotoOptions);
            }
            //await Test();
            string url = GetUrl(ProductId);

            var response = await _page!.APIRequest.GetAsync(url, new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["Accept"] = "application/json",
                    ["Authorization"] = "Atmosphere atmosphere_app_id=kingfisher-o4ITR0sWAyCVQBraQf4Es61jHV3dN4oO9UwJQMrS",
                    ["Referrer"] = "https://www.castorama.fr"
                }
            });

            if (!response.Ok)
            {
                Console.WriteLine($"[Castorama API Error] {response.Status}");
                return;
            }

            string json = await response.TextAsync();
            ProcessCastoramaJson(json);

            await Task.Delay(800);
        }

        private string GetUrl(string ean)
        {
            return "https://api.kingfisher.com/v1/mobile/stores/CAFR" +
                   $"?nearLatLong={Math.Round(Latitude, 4)},{Math.Round(Longitude, 4)}" +
                   "&page[size]=10" +
                   "&include=clickAndCollect,stock" +
                   $"&filter[ean]={ean}";
        }

        private void ProcessCastoramaJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("data", out var stores))
                    return;

                foreach (var storeItem in stores.EnumerateArray())
                {
                    var attr = storeItem.GetProperty("attributes").GetProperty("store");

                    string storeId = attr.GetProperty("externalId").GetString()
                                     ?? storeItem.GetProperty("id").GetString();

                    string name = attr.GetProperty("name").GetString() ?? "Unknown";

                    int qty = ExtractAvailability(storeItem);

                    bool hasPrev = _previousStockState.TryGetValue(storeId, out int prev);

                    var store = new Store
                    {
                        StoreId = storeId,
                        Name = name,
                        AvailableQuantity = qty
                    };

                    if (hasPrev)
                    {
                        if (prev == 0 && qty > 0)
                            NewStockDetected?.Invoke(this, new StockEventArgs(store, qty));

                        else if (prev > 0 && qty == 0)
                            StockOutDetected?.Invoke(this, new StockEventArgs(store, qty));
                    }
                    else
                    {
                        if (qty > 0)
                            NewStockDetected?.Invoke(this, new StockEventArgs(store, qty));
                    }

                    _previousStockState[storeId] = qty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Castorama JSON Error] {ex.Message}");
            }
        }

        private int ExtractAvailability(JsonElement storeItem)
        {
            try
            {
                if (storeItem.TryGetProperty("attributes", out var attr) &&
                    attr.TryGetProperty("clickAndCollect", out var cc) &&
                    cc.TryGetProperty("products", out var products) &&
                    products.GetArrayLength() > 0)
                {
                    string availability =
                        products[0].GetProperty("availability").GetString() ?? "";

                    return availability switch
                    {
                        "Available" => 1,
                        "LowStock" => 1,
                        "NotAvailable" => 0,
                        _ => 0
                    };
                }

                if (storeItem.TryGetProperty("attributes", out var attr2) &&
                    attr2.TryGetProperty("stock", out var stock) &&
                    stock.TryGetProperty("products", out var stockProducts) &&
                    stockProducts.GetArrayLength() > 0)
                {
                    string level =
                        stockProducts[0].GetProperty("stockLevel").GetString() ?? "";

                    return level switch
                    {
                        "InStock" => 1,
                        "LowStock" => 1,
                        "OutOfStock" => 0,
                        _ => 0
                    };
                }
            }
            catch
            {
                // ignore parsing errors
            }

            return 0;
        }

        private List<Store> LoadStoresFromResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "SioMideaPortasplitWatcher.res.castorama_stores.json";

                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    Console.WriteLine($"[Error] Missing resource {resourceName}");
                    return new List<Store>();
                }

                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                var stores = JsonSerializer.Deserialize<List<Store>>(json);

                return stores?.Where(s => !string.IsNullOrWhiteSpace(s.StoreId)).ToList()
                       ?? new List<Store>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoadStores Error] {ex.Message}");
                return new List<Store>();
            }
        }
    }
}