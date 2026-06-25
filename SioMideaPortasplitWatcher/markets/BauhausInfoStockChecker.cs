using Microsoft.Playwright;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SioMideaPortasplitWatcher.markets
{
    public class BauhausInfoStockChecker : IStockChecker
    {
        public class BauhausLocation
        {
            [JsonPropertyName("lat")]
            public double Latitude { get; set; }

            [JsonPropertyName("lng")]
            public double Longitude { get; set; }
        }

        public class BauhausAddress
        {
            [JsonPropertyName("city")]
            public string City { get; set; } = string.Empty;

            [JsonPropertyName("zipcode")]
            public string ZipCode { get; set; } = string.Empty;

            [JsonPropertyName("lines")]
            public List<string> Lines { get; set; } = [];
        }

        public class BauhausStore
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("display_name")]
            public string DisplayName { get; set; } = string.Empty;

            [JsonPropertyName("slug")]
            public string Slug { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("location")]
            public BauhausLocation Location { get; set; } = new();

            [JsonPropertyName("address")]
            public BauhausAddress Address { get; set; } = new();

            [JsonIgnore]
            public int AvailableQuantity { get; set; }

            [JsonIgnore]
            public double Latitude => Location.Latitude;

            [JsonIgnore]
            public double Longitude => Location.Longitude;

            [JsonIgnore]
            public string City => Address.City;

            [JsonIgnore]
            public string ZipCode => Address.ZipCode;

            [JsonIgnore]
            public string Street =>
                Address.Lines.FirstOrDefault() ?? string.Empty;
        }


        public class AvailabilityItem
        {
            [JsonPropertyName("location_id")]
            public string LocationId { get; set; } = string.Empty;

            [JsonPropertyName("available_quantity")]
            public int AvailableQuantity { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;
        }

        public class AvailabilityResponse
        {
            [JsonPropertyName("data")]
            public List<AvailabilityItem> Data { get; set; } = new();
        }

        public class StockEventArgs : EventArgs
        {
            public BauhausStore Store { get; }
            public int NewQuantity { get; }

            public StockEventArgs(BauhausStore store, int qty)
            {
                Store = store;
                NewQuantity = qty;
            }
        }

        public event EventHandler<StockEventArgs>? NewStockDetected;
        public event EventHandler<StockEventArgs>? StockOutDetected;

        private IPage? _page;

        private readonly List<BauhausStore> _cachedStores;
        private readonly Dictionary<int, int> _previousStockState = new();

        public string ProductId { get; }
        public string ProductName { get; }

        public BauhausInfoStockChecker(string productName, string productId)
        {
            ProductId = productId;
            ProductName = productName;

            _cachedStores = LoadStoresFromResources();
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
            }

            // Charge la page produit pour obtenir le contexte Bauhaus
            string Landingurl = "https://www.bauhaus.info/";
            if (_page?.Url != Landingurl)
            {
                await _page!.GotoAsync(Landingurl, Browser.GotoOptions);
            }

            const int chunkSize = 300;

            for (int i = 0; i < _cachedStores.Count; i += chunkSize)
            {
                var chunk = _cachedStores.Skip(i).Take(chunkSize).ToList();

                string storeIds = string.Join(",", chunk.Select(s => s.Id));

                string json = await _page.EvaluateAsync<string>(
                    @"async ({ productId, storeIds }) => {

                        const response = await fetch(
                            `https://www.bauhaus.info/api/product-availability/locations?storeIds=${storeIds}&productId=${productId}`,
                            {
                                credentials: 'include',
                                method: 'GET',
                                mode: 'cors'
                            }
                        );

                        return await response.text();
                    }",
                    new
                    {
                        productId = ProductId,
                        storeIds
                    });

                ProcessStockJson(json);

                await Task.Delay(500);
            }
        }

        private void ProcessStockJson(string json)
        {
            try
            {
                var response = JsonSerializer.Deserialize<AvailabilityResponse>(json);

                if (response == null)
                    return;

                foreach (var stock in response.Data)
                {
                    if (!int.TryParse(stock.LocationId, out int storeId))
                        continue;

                    var store =
                        _cachedStores.FirstOrDefault(s => s.Id == storeId);

                    if (store == null)
                        continue;

                    int currentQty = stock.AvailableQuantity;

                    store.AvailableQuantity = currentQty;

                    bool hasPrevious = _previousStockState.TryGetValue(storeId, out int previousQty);

                    if (hasPrevious)
                    {
                        if (previousQty == 0 && currentQty > 0)
                        {
                            NewStockDetected?.Invoke(this, new StockEventArgs(store, currentQty));
                        }
                        else if (previousQty > 0 && currentQty == 0)
                        {
                            StockOutDetected?.Invoke(this, new StockEventArgs(store, currentQty));
                        }
                    }
                    else
                    {
                        if (currentQty > 0)
                        {
                            NewStockDetected?.Invoke(this, new StockEventArgs(store, currentQty));
                        }
                    }

                    _previousStockState[storeId] = currentQty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erreur analyse stock Bauhaus : {ex.Message}");
            }
        }

        private List<BauhausStore> LoadStoresFromResources()
        {
            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = "SioMideaPortasplitWatcher.res.bauhausinfo_germany_stores.json";

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null) return [];

            using StreamReader reader = new(stream);

            string json = reader.ReadToEnd();

            return JsonSerializer.Deserialize<List<BauhausStore>>(json) ?? new List<BauhausStore>();
        }
    }
}