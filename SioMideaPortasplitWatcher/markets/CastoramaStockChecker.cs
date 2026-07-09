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
        /*
        {
            "type": "store",
            "id": "1431",
            "attributes": {
                "store": {
                    "brand": "CAFR",
                    "clickAndCollect": {
                        "sameDay": true,
                        "nextDay": true,
                        "mktpAvailable": false,
                        "trainingReadyStore": false
                    },
                    "collectionMessage": "Les commandes spéciales clients ne sont ni reprises, ni échangées",
                    "contactPoint": {
                        "email": "",
                        "faxNumber": "03 83 53 24 05",
                        "telephone": "03 83 51 03 86",
                        "additionalContacts": [],
                        "departmentContacts": []
                    },
                    "distance": "27.95 KM",
                    "externalId": "1431",
                    "facilities": ["La carte cadeau Castorama", "La carte Castorama", "La conception 3D cuisine et rangement", "La découpe du bois et du verre", "La livraison à domicile", "La location de véhicule", "L'installation", "Le financement", "Le service après-vente", "Location de matériel", "Forfaits d’entretien des outils
de jardin motorisés", "Service après-vente (hors dimanche)"],
                    "geoCoordinates": {
                        "country": "FR",
                        "countryCode": "FR",
                        "postalCode": "54504",
                        "coordinates": {
                            "latitude": 48.65580498232446,
                            "longitude": 6.181587342605553
                        },
                        "address": {
                            "lines": ["Espace vandoeuvre sud", "Rue Bernard Palissy BP124", "", "VANDOEUVRE LES NANCY", ""]
                        },
                        "latitude": "48.65580498232446",
                        "longitude": "6.181587342605553"
                    },
                    "geoSearchResults": {
                        "distance": 27.95,
                        "unitOfMeasure": "KM"
                    },
                    "emergencyMessageTitle": "",
                    "emergencyMessageText": "",
                    "storeOpeningHoursTitle": "Horaires du magasin",
                    "locale": "fr_FR",
                    "name": "Castorama Vandoeuvre",
                    "openingHoursSpecifications": [{
                            "closes": "20:00:00 Europe/Paris",
                            "dayOfWeek": "Monday",
                            "opens": "08:00:00 Europe/Paris"
                        }, {
                            "closes": "20:00:00 Europe/Paris",
                            "dayOfWeek": "Tuesday",
                            "opens": "08:00:00 Europe/Paris"
                        }, {
                            "closes": "20:00:00 Europe/Paris",
                            "dayOfWeek": "Wednesday",
                            "opens": "08:00:00 Europe/Paris"
                        }, {
                            "closes": "20:00:00 Europe/Paris",
                            "dayOfWeek": "Thursday",
                            "opens": "08:00:00 Europe/Paris"
                        }, {
                            "closes": "20:00:00 Europe/Paris",
                            "dayOfWeek": "Friday",
                            "opens": "08:00:00 Europe/Paris"
                        }, {
                            "closes": "20:00:00 Europe/Paris",
                            "dayOfWeek": "Saturday",
                            "opens": "08:00:00 Europe/Paris"
                        }, {
                            "closes": "13:00:00 Europe/Paris",
                            "dayOfWeek": "Sunday",
                            "opens": "09:00:00 Europe/Paris"
                        }
                    ],
                    "additionalOpeningHoursSpecifications": [{
                            "identifier": "TRADE_YARD",
                            "openingHoursSpecifications": [{
                                    "closes": "20:00:00 Europe/Paris",
                                    "dayOfWeek": "Monday",
                                    "opens": "08:00:00 Europe/Paris"
                                }, {
                                    "closes": "20:00:00 Europe/Paris",
                                    "dayOfWeek": "Tuesday",
                                    "opens": "08:00:00 Europe/Paris"
                                }, {
                                    "closes": "20:00:00 Europe/Paris",
                                    "dayOfWeek": "Wednesday",
                                    "opens": "08:00:00 Europe/Paris"
                                }, {
                                    "closes": "20:00:00 Europe/Paris",
                                    "dayOfWeek": "Thursday",
                                    "opens": "08:00:00 Europe/Paris"
                                }, {
                                    "closes": "20:00:00 Europe/Paris",
                                    "dayOfWeek": "Friday",
                                    "opens": "08:00:00 Europe/Paris"
                                }, {
                                    "closes": "20:00:00 Europe/Paris",
                                    "dayOfWeek": "Saturday",
                                    "opens": "08:00:00 Europe/Paris"
                                }, {
                                    "closes": "13:00:00 Europe/Paris",
                                    "dayOfWeek": "Sunday",
                                    "opens": "09:00:00 Europe/Paris"
                                }
                            ]
                        }
                    ],
                    "additionalOpeningHoursTitle": "Horaires de la cour des matériaux",
                    "publicHolidaysTitle": "Horaires exceptionnels",
                    "paymentMethod": "NONPED",
                    "publicHolidays": [{
                            "closes": "19:00:00 Europe/Paris",
                            "date": "2026-07-14",
                            "opens": "09:00:00 Europe/Paris"
                        }
                    ],
                    "salesOrganization": "CA-FR",
                    "seoId": "CAFR_VAN1431",
                    "storeType": "Installation-Centre",
                    "storeMerchandising": [{
                            "url": "/store/en-ce-moment-dans-votre-magasin-castorama"
                        }, {
                            "url": "/store/seo-vandoeuvre"
                        }, {
                            "url": "/store"
                        }
                    ]
                },
                "openingTimes": {
                    "openToday": true,
                    "openingTimeToday": "08:00",
                    "closingTimeToday": "20:00",
                    "storeClosesInMs": "28200000",
                    "openTomorrow": true,
                    "openingTimeTomorrow": "08:00",
                    "closingTimeTomorrow": "20:00",
                    "upcomingDays": [{
                            "date": "2026-06-30",
                            "openingTimes": {
                                "openingTime": "08:00:00 Europe/Paris",
                                "closingTime": "20:00:00 Europe/Paris"
                            }
                        }, {
                            "date": "2026-07-01",
                            "openingTimes": {
                                "openingTime": "08:00:00 Europe/Paris",
                                "closingTime": "20:00:00 Europe/Paris"
                            }
                        }, {
                            "date": "2026-07-02",
                            "openingTimes": {
                                "openingTime": "08:00:00 Europe/Paris",
                                "closingTime": "20:00:00 Europe/Paris"
                            }
                        }, {
                            "date": "2026-07-03",
                            "openingTimes": {
                                "openingTime": "08:00:00 Europe/Paris",
                                "closingTime": "20:00:00 Europe/Paris"
                            }
                        }, {
                            "date": "2026-07-04",
                            "openingTimes": {
                                "openingTime": "08:00:00 Europe/Paris",
                                "closingTime": "20:00:00 Europe/Paris"
                            }
                        }, {
                            "date": "2026-07-05",
                            "openingTimes": {
                                "openingTime": "09:00:00 Europe/Paris",
                                "closingTime": "13:00:00 Europe/Paris"
                            }
                        }, {
                            "date": "2026-07-06",
                            "openingTimes": {
                                "openingTime": "08:00:00 Europe/Paris",
                                "closingTime": "20:00:00 Europe/Paris"
                            }
                        }
                    ]
                },
                "clickAndCollect": {
                    "products": [{
                            "availability": "NotAvailable",
                            "ean": "8431312260509",
                            "name": "Climatiseur portasplit Midea réversible 3500W",
                            "clickAndCollectType": "NextDay",
                            "shippingMethodClickAndCollectFulfilmentCentre": {
                                "availability": "NotAvailable"
                            }
                        }
                    ],
                    "summary": {
                        "primaryMessage": "Currently out of stock",
                        "primaryMessageLanguageKey": "fulfil_out_of_stock",
                        "secondaryMessage": "for Click & Collect",
                        "secondaryMessageLanguageKey": "fulfil_for_cc",
                        "showOpeningTimesFor": "Today",
                        "availability": "AllNotAvailable",
                        "fulfilmentCenterAvailability": "AllNotAvailable",
                        "clickAndCollectType": "NextDay",
                        "aggregateAvailability": "AllNotAvailable"
                    }
                },
                "stock": {
                    "products": [{
                            "ean": "8431312260509",
                            "productType": "Stockable",
                            "description": "Item out of stock",
                            "stockLevel": "OutOfStock",
                            "quantity": 0
                        }
                    ]
                },
                "seoPath": "/store/castorama-vandoeuvre/CAFR_VAN1431"
            }
        }
        */
        public class Store
        {
            public string StoreId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public double Latitude { get; set; } = 0.0;
            public double Longitude { get; set; } = 0.0;

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
        public readonly string ProductUrl;
        public readonly double Latitude;
        public readonly double Longitude;

        private readonly Dictionary<string, int> _previousStockState = new();

        public CastoramaStockChecker(string productName, double latitude, double longitude, string productId, string productUrl)
        {
            ProductName = productName;
            ProductUrl = productUrl;
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
                await _page.GotoAsync(ProductUrl, Browser.GotoOptions);
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
                   "&page[size]=500" +
                   "&include=clickAndCollect,stock" +
                   $"&filter[ean]={ean}";
        }

        private void ProcessCastoramaJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("data", out var stores))
                {
                    return;
                }

                foreach (var storeItem in stores.EnumerateArray())
                {
                    var attr = storeItem.GetProperty("attributes").GetProperty("store");

                    string storeId = attr.GetProperty("externalId").GetString()
                                     ?? storeItem.GetProperty("id").GetString();

                    string name = attr.GetProperty("name").GetString() ?? "Unknown";
                    var geoCoordinates = attr.GetProperty("geoCoordinates");
                    var latitude = geoCoordinates.GetProperty("coordinates").GetProperty("latitude").GetDouble();
                    var longitude = geoCoordinates.GetProperty("coordinates").GetProperty("longitude").GetDouble();
                    int qty = ExtractAvailability(storeItem);

                    bool hasPrev = _previousStockState.TryGetValue(storeId, out int prev);

                    var store = new Store
                    {
                        StoreId = storeId,
                        Name = name,
                        AvailableQuantity = qty,
                        Latitude = latitude,
                        Longitude = longitude
                    };

                    if (hasPrev)
                    {
                        if (prev == 0 && qty > 0)
                        {
                            NewStockDetected?.Invoke(this, new StockEventArgs(store, qty));
                        }
                        else if (prev > 0 && qty == 0)
                        {
                            StockOutDetected?.Invoke(this, new StockEventArgs(store, qty));
                        }
                    }
                    else
                    {
                        if (qty > 0)
                        {
                            NewStockDetected?.Invoke(this, new StockEventArgs(store, qty));
                        }
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

    }
}