using Microsoft.Playwright;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace SioMideaPortasplitWatcher.markets
{
    public class LeroyMerlinStockChecker : IStockChecker
    {
        public class LeroyMerlinStore
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string City { get; set; } = "";
            public string Distance { get; set; } = "";
            public bool Available { get; set; }
            public string Status { get; set; } = "";
        }

        public class StockEventArgs : EventArgs
        {
            public LeroyMerlinStore Store { get; }
            public string Status { get; }

            public StockEventArgs(LeroyMerlinStore store, string status)
            {
                Store = store;
                Status = status;
            }
        }

        public event EventHandler<StockEventArgs>? NewStockDetected;
        public event EventHandler<StockEventArgs>? StockOutDetected;

        private IPage? _page;

        private readonly string _url;
        private readonly Dictionary<int, bool> _previousState = new();
        public readonly string ProductName;

        public LeroyMerlinStockChecker(string productName, double latitude, double longitude, string productRef)
        {
            ProductName = productName;
            _url =
                $"https://www.leroymerlin.fr/store-header-module/services/contextlayer/store-search-result" +
                $"?latitude={latitude}&longitude={longitude}&productRef={productRef}&storeSearchType=STOCK";
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
                await _page.GotoAsync("https://www.leroymerlin.fr", Browser.GotoOptions);
            }

            await _page!.GotoAsync(_url, Browser.GotoOptions);

            // ✅ attendre le message de refresh (clé de ton besoin)
            await _page.WaitForSelectorAsync(
                "p[aria-live='polite']",
                new PageWaitForSelectorOptions
                {
                    Timeout = 15000
                });

            await _page.WaitForFunctionAsync(
                @"() => document.body.innerText.includes('La liste des magasins a été mise à jour')"
            );

            await ParseStoresAsync();
        }

        private async Task ParseStoresAsync()
        {
            var cards = await _page!.QuerySelectorAllAsync("article.m-store-search-card");

            foreach (var card in cards)
            {
                try
                {
                    var idAttr = await card.GetAttributeAsync("data-store-id");
                    if (!int.TryParse(idAttr, out int storeId))
                        continue;

                    var name = await card.QuerySelectorAsync(".m-store-info-header--title");
                    var city = await card.GetAttributeAsync("data-store-city") ?? "";

                    var distance = await card.QuerySelectorAsync(".m-store-info-header__store-distance");

                    var nameText = name != null ? await name.InnerTextAsync() : "";
                    var distanceText = distance != null ? await distance.InnerTextAsync() : "";

                    var statusText = await card.InnerTextAsync();

                    bool available =
                        !statusText.Contains("Actuellement indisponible");

                    var store = new LeroyMerlinStore
                    {
                        Id = storeId,
                        Name = nameText.Trim(),
                        City = city,
                        Distance = distanceText.Trim(),
                        Available = available,
                        Status = available ? "available" : "unavailable"
                    };

                    bool hasPrevious = _previousState.TryGetValue(storeId, out bool previous);

                    if (hasPrevious)
                    {
                        if (!previous && available)
                            NewStockDetected?.Invoke(this, new StockEventArgs(store, store.Status));

                        if (previous && !available)
                            StockOutDetected?.Invoke(this, new StockEventArgs(store, store.Status));
                    }
                    else
                    {
                        if (available)
                            NewStockDetected?.Invoke(this, new StockEventArgs(store, store.Status));
                    }

                    _previousState[storeId] = available;
                }
                catch
                {
                    // ignore store parsing errors
                }
            }
        }
    }
}