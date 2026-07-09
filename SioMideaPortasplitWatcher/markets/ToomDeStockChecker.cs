//market list

//    await fetch("https://api.toom.de/public/v1/buyboxcases", {
//        "credentials": "omit",
//    "headers": {
//            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:152.0) Gecko/20100101 Firefox/152.0",
//        "Accept": "application/json, text/plain, */*",
//        "Accept-Language": "fr,fr-FR;q=0.9,en-US;q=0.8,en;q=0.7",
//        "Content-Type": "application/json",
//        "X-Requested-With": "XMLHttpRequest",
//        "Sec-Fetch-Dest": "empty",
//        "Sec-Fetch-Mode": "cors",
//        "Sec-Fetch-Site": "same-site",
//        "Pragma": "no-cache",
//        "Cache-Control": "no-cache"
//    },
//    "referrer": "https://toom.de/",
//    "body": "[{\"market_id\":3209,\"sap_id\":\"10272593\"},{\"market_id\":3379,\"sap_id\":\"10272593\"},{\"market_id\":3453,\"sap_id\":\"10272593\"},{\"market_id\":3658,\"sap_id\":\"10272593\"},{\"market_id\":3633,\"sap_id\":\"10272593\"},{\"market_id\":3875,\"sap_id\":\"10272593\"},{\"market_id\":3381,\"sap_id\":\"10272593\"}]",
//    "method": "POST",
//    "mode": "cors"
//});

//POST arguments = //[{"market_id":3209,"sap_id":"10272593"},{ "market_id":3379,"sap_id":"10272593"},{ "market_id":3453,"sap_id":"10272593"},{ "market_id":3658,"sap_id":"10272593"},{ "market_id":3633,"sap_id":"10272593"},{ "market_id":3875,"sap_id":"10272593"},{ "market_id":3381,"sap_id":"10272593"}]

using Microsoft.Playwright;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SioMideaPortasplitWatcher.markets
{
    public class ToomDeStockChecker : IStockChecker
    {
        public class ToomStore
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("phone")]
            public string Phone { get; set; } = "";

            [JsonPropertyName("email")]
            public string Email { get; set; } = "";

            [JsonPropertyName("link")]
            public string Link { get; set; } = "";

            [JsonPropertyName("address")]
            public Address Address { get; set; } = new();

            public bool Available { get; set; }
        }

        public class Address
        {
            [JsonPropertyName("street")]
            public string Street { get; set; } = "";

            [JsonPropertyName("zip")]
            public string Zip { get; set; } = "";

            [JsonPropertyName("city")]
            public string City { get; set; } = "";

            [JsonPropertyName("country")]
            public string Country { get; set; } = "";

            [JsonPropertyName("longitude")]
            public double Longitude { get; set; } = 0;

            [JsonPropertyName("latitude")]
            public double Latitude { get; set; } = 0;
        }

        public class StoresRoot
        {
            [JsonPropertyName("markets")]
            public List<ToomStore> Markets { get; set; } = new();
        }

        public class StockResponse
        {
            [JsonPropertyName("sap_id")]
            public string SapId { get; set; } = "";

            [JsonPropertyName("market_id")]
            public int MarketId { get; set; }

            [JsonPropertyName("state")]
            public string State { get; set; } = "";
        }

        public class RequestItem
        {
            [JsonPropertyName("market_id")]
            public int MarketId { get; set; }

            [JsonPropertyName("sap_id")]
            public string SapId { get; set; } = "";
        }

        public class StockEventArgs : EventArgs
        {
            public ToomStore Store { get; }
            public string Status { get; }

            public StockEventArgs(ToomStore store, string status)
            {
                Store = store;
                Status = status;
            }
        }

        public event EventHandler<StockEventArgs>? NewStockDetected;
        public event EventHandler<StockEventArgs>? StockOutDetected;

        private IPage? _page;

        public readonly string ProductId;
        public readonly string ProductName;

        private readonly List<ToomStore> _stores;

        private readonly Dictionary<int, bool> _previousState = new();

        public ToomDeStockChecker(string productName, string sapId)
        {
            ProductName = productName;
            ProductId = sapId;

            _stores = LoadStoresFromResources();
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
                await _page.GotoAsync("https://toom.de/", Browser.GotoOptions);
            }

            const int chunkSize = 300;

            for (int i = 0; i < _stores.Count; i += chunkSize)
            {
                var chunk = _stores
                    .Skip(i)
                    .Take(chunkSize)
                    .ToList();

                await CheckChunkAsync(chunk);

                await Task.Delay(500);
            }
        }

        private async Task CheckChunkAsync(List<ToomStore> stores)
        {
            var payload = stores
                .Select(s => new RequestItem
                {
                    MarketId = s.Id,
                    SapId = ProductId
                })
                .ToList();

            var jsonPayload = JsonSerializer.Serialize(payload);

            var response = await _page!.APIRequest.PostAsync(
                "https://api.toom.de/public/v1/buyboxcases",
                new()
                {
                    Headers = new Dictionary<string, string>
                    {
                        ["Content-Type"] = "application/json",
                        ["X-Requested-With"] = "XMLHttpRequest"
                    },
                    DataObject = payload
                });

            if (!response.Ok)
            {
                Console.WriteLine($"Toom API Error : {response.Status}");
                return;
            }

            string json = await response.TextAsync();

            ProcessStockJson(json);
        }

        private void ProcessStockJson(string json)
        {
            try
            {
                var apiResponse =
                    JsonSerializer.Deserialize<List<StockResponse>>(json);

                if (apiResponse == null)
                {
                    return;
                }

                foreach (var stock in apiResponse)
                {
                    bool available = !stock.State.Equals("unavailable", StringComparison.OrdinalIgnoreCase);

                    var store = _stores.FirstOrDefault(s => s.Id == stock.MarketId);

                    if (store == null)
                    {
                        continue;
                    }

                    store.Available = available;

                    bool hasPrevious = _previousState.TryGetValue(stock.MarketId, out bool previousAvailable);

                    if (hasPrevious)
                    {
                        if (!previousAvailable && available)
                        {
                            NewStockDetected?.Invoke(this, new StockEventArgs(store, stock.State));
                        }
                        else if (previousAvailable && !available)
                        {
                            StockOutDetected?.Invoke(this, new StockEventArgs(store, stock.State));
                        }
                    }
                    else
                    {
                        if (available)
                        {
                            NewStockDetected?.Invoke(this, new StockEventArgs(store, stock.State));
                        }
                    }

                    _previousState[stock.MarketId] = available;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erreur analyse stock Toom : {ex.Message}");
            }
        }

        private List<ToomStore> LoadStoresFromResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                string resourceName =
                    "SioMideaPortasplitWatcher.res.toomde_stores.json";

                using Stream? stream =
                    assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    return new();
                }

                using StreamReader reader =
                    new(stream);

                string json = reader.ReadToEnd();

                var root =
                    JsonSerializer.Deserialize<StoresRoot>(json);

                return root?.Markets ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}