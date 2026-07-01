using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SioMideaPortasplitWatcher.markets
{
    public class ObiDeStockChecker : IStockChecker
    {
        public class ObiStore
        {
            [JsonPropertyName("storeId")]
            public string StoreId { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("street")]
            public string Street { get; set; } = string.Empty;

            [JsonPropertyName("postal_code")]
            public string PostalCode { get; set; } = string.Empty;

            [JsonPropertyName("city")]
            public string City { get; set; } = string.Empty;

            [JsonPropertyName("state")]
            public string State { get; set; } = string.Empty;

            [JsonPropertyName("maps_url")]
            public string MapsUrl { get; set; } = string.Empty;

            [JsonPropertyName("email")]
            public string Email { get; set; } = string.Empty;

            [JsonPropertyName("phone")]
            public string Phone { get; set; } = string.Empty;

            [JsonPropertyName("url")]
            public string Url { get; set; } = string.Empty;

            // Alimentée dynamiquement par l'API de stock
            [JsonPropertyName("availableQuantity")]

            public int AvailableQuantity { get; set; }
        }

        public class StockEventArgs : EventArgs
        {
            public ObiStore Store { get; }
            public int NewQuantity { get; }

            public StockEventArgs(ObiStore store, int newQuantity)
            {
                Store = store;
                NewQuantity = newQuantity;
            }
        }

        // Événements demandés
        public event EventHandler<StockEventArgs>? NewStockDetected;
        public event EventHandler<StockEventArgs>? StockOutDetected;

        private IPage? _page;
        public readonly string ProductId;
        public readonly string ProductName;

        // Cache pour éviter de relire le fichier JSON local à chaque cycle
        private readonly List<ObiStore> _cachedStores;

        // Permet de suivre le dernier état du stock connu par magasin (Key: storeId, Value: quantité)
        private readonly Dictionary<string, int> _previousStockState = new();

        public ObiDeStockChecker(string productName, string productId)
        {
            ProductName = productName;
            ProductId = productId;
            _cachedStores = LoadStoresFromResources();
        }

        // Correction de l'interface : Task<IPage> au lieu de async void
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

            // OBI limite à 10 magasins par requête API. On découpe notre liste par paquets de 10.
            int chunkSize = 10;
            for (int i = 0; i < _cachedStores.Count; i += chunkSize)
            {
                var chunk = _cachedStores.Skip(i).Take(chunkSize).ToList();
                string url = GetUrl(ProductId, chunk);

                string? jsonResponse = await WaitForJsonAfter502Async(url);

                if (string.IsNullOrEmpty(jsonResponse))
                {
                    Console.WriteLine($"[Erreur] Impossible de récupérer le JSON pour {url}");
                    continue;
                }

                // Traitement et comparaison des stocks pour ce groupe
                ProcessStockJson(jsonResponse);

                // Optionnel : Un léger délai pour ne pas enchaîner les requêtes trop brutalement sur la même instance Playwright
                await Task.Delay(850);
            }
        }

        // On adapte la méthode pour générer l'URL uniquement pour le groupe de 10 en cours
        private string GetUrl(string productId, List<ObiStore> storesChunk)
        {
            var storeIds = storesChunk.Select(t => t.StoreId);
            return $"https://www.obi.de/api/pdp/v1/stock/{productId}?storeIds={string.Join(",", storeIds)}";
        }

        private void ProcessStockJson(string jsonResponse)
        {
            try
            {
                // Désérialisation de la réponse API (ex: [{"storeId":"511","availableQuantity":0}, ...])
                var apiStocks = JsonSerializer.Deserialize<List<ObiStore>>(jsonResponse);
                if (apiStocks == null) return;

                foreach (var apiData in apiStocks)
                {
                    // 1. Réassociation complète grâce à notre cache local
                    var fullStoreInfo = _cachedStores.FirstOrDefault(s => s.StoreId == apiData.StoreId);
                    if (fullStoreInfo == null) continue; // Magasin non référencé localement, on ignore

                    int currentQty = apiData.AvailableQuantity;
                    bool hasPreviousState = _previousStockState.TryGetValue(apiData.StoreId, out int previousQty);

                    // Mettre à jour l'objet complet avec la nouvelle quantité
                    fullStoreInfo.AvailableQuantity = currentQty;

                    if (hasPreviousState)
                    {
                        // CAS 1 : Le stock était à 0 (ou inconnu) et devient > 0
                        if (previousQty == 0 && currentQty > 0)
                        {
                            NewStockDetected?.Invoke(this, new StockEventArgs(fullStoreInfo, currentQty));
                        }
                        // CAS 2 : Le stock était > 0 et tombe à 0
                        else if (previousQty > 0 && currentQty == 0)
                        {
                            StockOutDetected?.Invoke(this, new StockEventArgs(fullStoreInfo, currentQty));
                        }
                        // CAS 3 : Simple changement de quantité (optionnel, mais utile si tu veux suivre les variations)
                        else if (previousQty != currentQty)
                        {
                            Console.WriteLine($"[Info] Changement de quantité pour {fullStoreInfo.Name} : {previousQty} -> {currentQty}");
                        }
                    }
                    else
                    {
                        // Premier passage du script : On lève l'alerte direct si du stock est déjà là
                        if (currentQty > 0)
                        {
                            NewStockDetected?.Invoke(this, new StockEventArgs(fullStoreInfo, currentQty));
                        }
                    }

                    // Sauvegarde du nouvel état pour le prochain cycle de vérification
                    _previousStockState[apiData.StoreId] = currentQty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'analyse du JSON de stock : {ex.Message}");
            }
        }

        private async Task<string?> WaitForJsonAfter502Async(string url)
        {
            var response = await _page!.GotoAsync(url, Browser.GotoOptions);

            if (response == null)
                return null;

            if (response.Ok)
                return await response.TextAsync();

            if (response.Status != 502 && response.Status != 503)
            {
                Console.WriteLine($"[HTTP {response.Status}] {url}");
                return null;
            }

            Console.WriteLine($"[502] Reçu pour {url}, attente de 5 secondes...");

            for (int attempt = 0; attempt < 60; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    string content = await _page.ContentAsync();

                    // Vérification simple que le contenu ressemble à du JSON
                    content = content.Trim();

                    if ((content.StartsWith("{") && content.EndsWith("}")) ||
                        (content.StartsWith("[") && content.EndsWith("]")))
                    {
                        Console.WriteLine("[OK] JSON récupéré après attente.");
                        return content;
                    }

                    Console.WriteLine($"[Tentative {attempt + 1}] Toujours pas de JSON.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Erreur lecture contenu] {ex.Message}");
                }
            }

            return null;
        }

        private List<ObiStore> LoadStoresFromResources()
        {
            try
            {
                // 1. Récupérer l'assembly actuel
                var assembly = Assembly.GetExecutingAssembly();

                // 2. Déterminer le nom complet de la ressource.
                // ATTENTION : Le namespace doit correspondre à la structure de ton projet.
                // Format général : "NomDuProjet.Dossier.NomDuFichier.json"
                string resourceName = "SioMideaPortasplitWatcher.res.obide_stores.json";

                // Optionnel/Sécurité : Si tu n'es pas sûr du nom, décommente la ligne suivante pour lister les ressources dans la console :
                // foreach (var name in assembly.GetManifestResourceNames()) Console.WriteLine($"Ressource trouvée : {name}");

                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Console.WriteLine($"[Erreur] La ressource intégrée '{resourceName}' est introuvable.");
                        return new List<ObiStore>();
                    }

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonString = reader.ReadToEnd();

                        var stores = JsonSerializer.Deserialize<List<ObiStore>>(jsonString);
                        return stores?.Where(s => !string.IsNullOrWhiteSpace(s.StoreId)).ToList() ?? new List<ObiStore>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des magasins depuis les ressources : {ex.Message}");
                return new List<ObiStore>();
            }
        }
    }
}
