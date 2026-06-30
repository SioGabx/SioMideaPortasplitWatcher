using Microsoft.Playwright;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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
            public int AvailableQuantity { get; internal set; }
        }

        public class StockEventArgs : EventArgs
        {
            public LeroyMerlinStore Store { get; }
            public int Quantity { get; }

            public StockEventArgs(LeroyMerlinStore store, int quantity)
            {
                Store = store;
                Quantity = quantity;
            }
        }

        public event EventHandler<StockEventArgs>? NewStockDetected;
        public event EventHandler<StockEventArgs>? StockOutDetected;

        private IPage? _page;

        private readonly string _url;
        private readonly Dictionary<int, int> _previousStockState = new();
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
        private int ParseQuantity(string text)
        {
            // "31 en stock" -> 31
            var match = Regex.Match(text, @"(\d+)\s+en stock");

            int currentQty = 0;

            if (match.Success)
            {
                currentQty = int.Parse(match.Groups[1].Value);
                return currentQty;
            }

            if (text.Contains("Bientôt disponible", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (text.Contains("Actuellement indisponible", StringComparison.OrdinalIgnoreCase))
                return 0;

           
            return 0;
        }

        private async Task ParseStoresAsync()
        {
            var cards = await _page!.QuerySelectorAllAsync("article.m-store-search-card");


            foreach (var card in cards)
            {
                try
                {
                    var button = await card.QuerySelectorAsync("button[data-store-id]");
                    var idAttr = await button?.GetAttributeAsync("data-store-id");

                    if (!int.TryParse(idAttr, out int storeId))
                        continue;


                    var name = await card.QuerySelectorAsync(".m-store-info-header--title");
                    var city = await card.GetAttributeAsync("data-store-city") ?? "";

                    var distance = await card.QuerySelectorAsync(".m-store-info-header__store-distance");

                    var nameText = name != null ? await name.InnerTextAsync() : "";
                    var distanceText = distance != null ? await distance.InnerTextAsync() : "";

                    var statusText = await card.InnerTextAsync();
                    int currentQty = ParseQuantity(statusText);

                    var store = new LeroyMerlinStore
                    {
                        Id = storeId,
                        Name = nameText.Trim(),
                        City = city,
                        Distance = distanceText.Trim(),
                        AvailableQuantity = currentQty,
                        Status = currentQty > 0 ? "available" : "unavailable"
                    };

                    bool hasPrevious = _previousStockState.TryGetValue(storeId, out int previousQty);

                    // Mettre à jour l'objet complet
                    // fullStoreInfo.AvailableQuantity = currentQty;

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
                        else if (previousQty != currentQty)
                        {
                            Console.WriteLine($"[Info] Changement de quantité pour {store.Name} : {previousQty} -> {currentQty}");
                        }
                    }
                    else
                    {
                        if (currentQty > 0)
                        {
                            NewStockDetected?.Invoke(this, new StockEventArgs(store, currentQty));
                        }
                    }

                    // sauvegarde état
                    _previousStockState[storeId] = currentQty;
                }
                catch
                {
                    // ignore store parsing errors
                }
            }
        }
    }
}