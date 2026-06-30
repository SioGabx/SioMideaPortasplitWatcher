using SioMideaPortasplitWatcher.markets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SioMideaPortasplitWatcher
{
    internal class Program
    {
        static async Task Main()
        {
            BalloonNotifier.Initialize();

            await Browser.Initialize();
            // Lance la boucle de surveillance principale et attend son exécution
            await WatchLoopAsync();
        }

        private static void PrintNewStockDetected(string StoreName, string ProductName, ConsoleColor ProductNameColor, object Stock, string Url)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] [STOCK] ");
            Console.ForegroundColor = ProductNameColor;
            Console.Write(ProductName);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($" chez {StoreName} | Stock: {Stock}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\tLien : {Url}");
            Console.ResetColor();
        }
        private static void PrintStockOutDetected(string StoreName, string ProductName)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[RUPTURE DE STOCK] Article {ProductName} à {StoreName}.");
            Console.ResetColor();
        }
        private static void ShowBallon(string StoreName, string ProductName, TimeSpan Duration, double DistanceKm, object Stock, string Url)
        {
            var t = new BalloonNotifier("🚨 Midea disponible !", $"{ProductName}\n{StoreName}\n{Math.Floor(Duration.TotalHours)}h {Duration.Minutes}min pour {DistanceKm:F0} km \nStock: {Stock}", Url, StoreName);
            t.Show();
        }


        private static async Task WatchLoopAsync()
        {
            Console.Title = "Midea Portasplit Watcher";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=================================================");
            Console.WriteLine("    WATCHER : Log uniquement sur changement");
            Console.WriteLine("=================================================");
            Console.ResetColor();

            // Instance spécifique pour s'abonner aux événements
            var obiCheckerMP = new ObiDeStockChecker("Midea Portasplit 12000 BTU", "8620890"); //https://www.obi.de/p/8620890/midea-mobile-split-klimaanlage-portasplit

            obiCheckerMP.NewStockDetected += async (sender, e) =>
            {
                var url = $"https://www.obi.de/api/disc/store/change?storeNumber={e.Store.StoreId}&redirectUrl={Uri.EscapeDataString($"https://www.obi.de/p/{obiCheckerMP.ProductId}")}";
                PrintNewStockDetected(e.Store.Name, obiCheckerMP.ProductName, ConsoleColor.Red, e.NewQuantity, url);
                var (Duration, DistanceKm) = await Drive.DisplayTravelTimeWithCacheAsync($"{e.Store.City} {e.Store.PostalCode}, Deutschland", e.Store.Name, "de");
                ShowBallon(e.Store.Name, obiCheckerMP.ProductName, Duration, DistanceKm, e.NewQuantity, url);
            };

            obiCheckerMP.StockOutDetected += (sender, e) =>
            {
                PrintStockOutDetected(obiCheckerMP.ProductName, e.Store.Name);
            };

            var obiCheckerMPC = new ObiDeStockChecker("Midea Portasplit Cool 8000 BTU", "2191158911022"); //https://www.obi.de/p/2191158911022/midea-split-klimaanlage-portasplit-cool-mobil-weissgrau
            obiCheckerMPC.NewStockDetected += async (sender, e) =>
            {
                var url = $"https://www.obi.de/api/disc/store/change?storeNumber={e.Store.StoreId}&redirectUrl={Uri.EscapeDataString($"https://www.obi.de/p/{obiCheckerMPC.ProductId}")}";
                PrintNewStockDetected(e.Store.Name, obiCheckerMPC.ProductName, ConsoleColor.Blue, e.NewQuantity, url);
                var (Duration, DistanceKm) = await Drive.DisplayTravelTimeWithCacheAsync($"{e.Store.City} {e.Store.PostalCode}, Deutschland", e.Store.Name, "de");
                ShowBallon(e.Store.Name, obiCheckerMPC.ProductName, Duration, DistanceKm, e.NewQuantity, url);
            };

            obiCheckerMPC.StockOutDetected += (sender, e) =>
            {
                PrintStockOutDetected(obiCheckerMPC.ProductName, e.Store.Name);
            };

            var BauhausInfoMP = new BauhausInfoStockChecker("Midea Portasplit 12000 BTU", "31934233");

            BauhausInfoMP.NewStockDetected += async (sender, e) =>
            {
                string url = $"https://www.bauhaus.info/p/31934233";
                PrintNewStockDetected(e.Store.Name, BauhausInfoMP.ProductName, ConsoleColor.Red, e.NewQuantity, url);
                var (Duration, DistanceKm) = await Drive.DisplayTravelTimeWithCacheAsync(e.Store.Name, e.Store.Latitude, e.Store.Longitude);
                ShowBallon(e.Store.Name, obiCheckerMP.ProductName, Duration, DistanceKm, e.NewQuantity, url);
            };

            BauhausInfoMP.StockOutDetected += (sender, e) =>
            {
                PrintStockOutDetected(BauhausInfoMP.ProductName, e.Store.Name);
            };

            var BauhausInfoMPC = new BauhausInfoStockChecker("Midea Portasplit Cool 8000 BTU", "33946696");
            BauhausInfoMPC.NewStockDetected += async (sender, e) =>
            {
                string url = $"https://www.bauhaus.info/p/33946696";
                PrintNewStockDetected(e.Store.Name, BauhausInfoMPC.ProductName, ConsoleColor.Blue, e.NewQuantity, url);
                var (Duration, DistanceKm) = await Drive.DisplayTravelTimeWithCacheAsync(e.Store.Name, e.Store.Latitude, e.Store.Longitude);
                ShowBallon(e.Store.Name, obiCheckerMP.ProductName, Duration, DistanceKm, e.NewQuantity, url);
            };

            BauhausInfoMPC.StockOutDetected += (sender, e) =>
            {
                PrintStockOutDetected(BauhausInfoMPC.ProductName, e.Store.Name);
            };
            //10272593
            //10515238
            var ToomDeCheckerMP = new ToomDeStockChecker("Midea Portasplit 12000 BTU", "10272593");

            ToomDeCheckerMP.NewStockDetected += async (sender, e) =>
            {
                var url = $"https://toom.de/p/mobiles-klimageraet-portasplit-12000-btuh/9350668";
                PrintNewStockDetected(e.Store.Name, ToomDeCheckerMP.ProductName, ConsoleColor.Red, e.Status, url);
                var (Duration, DistanceKm) = await Drive.DisplayTravelTimeWithCacheAsync(e.Store.Name, e.Store.Address.Latitude, e.Store.Address.Longitude);
                ShowBallon(e.Store.Name, ToomDeCheckerMP.ProductName, Duration, DistanceKm, e.Status, url);
            };

            ToomDeCheckerMP.StockOutDetected += (sender, e) =>
            {
                PrintStockOutDetected(ToomDeCheckerMP.ProductName, e.Store.Name);
            };
         

            var ToomDeCheckerMPC = new ToomDeStockChecker("Midea Portasplit Cool 8000 BTU", "10515238");
            ToomDeCheckerMPC.NewStockDetected += async (sender, e) =>
            {
                var url = $"https://toom.de/p/split-klimaanlage-portasplit-cool-8000btuh/10515238";
                PrintNewStockDetected(e.Store.Name, ToomDeCheckerMPC.ProductName, ConsoleColor.Blue, e.Status, url);
                var (Duration, DistanceKm) = await Drive.DisplayTravelTimeWithCacheAsync(e.Store.Name, e.Store.Address.Latitude, e.Store.Address.Longitude);
                ShowBallon(e.Store.Name, ToomDeCheckerMPC.ProductName, Duration, DistanceKm, e.Status, url);
            };

            ToomDeCheckerMPC.StockOutDetected += (sender, e) =>
            {
                PrintStockOutDetected(ToomDeCheckerMPC.ProductName, e.Store.Name);
            };   
            
            var LeroyMerlinCheckerMP = new LeroyMerlinStockChecker("Midea Portasplit 12000 BTU", 48.693100359086536, 6.173689718165843, "93857579");

            LeroyMerlinCheckerMP.NewStockDetected += async (sender, e) =>
            {
                var url = $"https://www.leroymerlin.fr/produits/climatiseur-split-mobile-reversible-portasplit-midea-par-optimea-93857579.html";
                PrintNewStockDetected(e.Store.Name, LeroyMerlinCheckerMP.ProductName, ConsoleColor.Red, e.Quantity, url);
                var (Duration, DistanceKm) = await Drive.DisplayTravelTimeWithCacheAsync(e.Store.City,e.Store.Name);
                ShowBallon(e.Store.Name, LeroyMerlinCheckerMP.ProductName, Duration, DistanceKm, e.Quantity, url);
            };

            LeroyMerlinCheckerMP.StockOutDetected += (sender, e) =>
            {
                PrintStockOutDetected(LeroyMerlinCheckerMP.ProductName, e.Store.Name);
            };

            var CastoramaCheckerMP = new CastoramaStockChecker("Midea Portasplit 12000 BTU", 48.693100359086536, 6.173689718165843, "8431312260509");

            CastoramaCheckerMP.NewStockDetected += async (sender, e) =>
            {
                var url = $"https://www.castorama.fr/climatiseur-portasplit-midea-reversible-3500w/8431312260509_CAFR.prd";
                PrintNewStockDetected(e.Store.Name, CastoramaCheckerMP.ProductName, ConsoleColor.Red, e.NewQuantity, url);
                var (Duration, DistanceKm) = await Drive.DisplayTravelTimeWithCacheAsync(e.Store.Name, e.Store.Latitude, e.Store.Longitude);
                ShowBallon(e.Store.Name, CastoramaCheckerMP.ProductName, Duration, DistanceKm, e.NewQuantity, url);
            };

            CastoramaCheckerMP.StockOutDetected += (sender, e) =>
            {
                PrintStockOutDetected(CastoramaCheckerMP.ProductName, e.Store.Name);
            };


            List<IStockChecker> stockCheckersMP =
            [
                obiCheckerMP,
                BauhausInfoMP,
                ToomDeCheckerMP,
                LeroyMerlinCheckerMP,
                CastoramaCheckerMP
            ];

            List<IStockChecker> stockCheckersMPC =
            [
                 obiCheckerMPC,
                BauhausInfoMPC,
                ToomDeCheckerMPC
            ];
          
            while (true)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Analyse (Actualisation)...");
                    Console.ResetColor();

                    await MarketCheckerTask(stockCheckersMP);
                   // await MarketCheckerTask(stockCheckersMPC);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[Erreur Boucle] {ex.Message}");
                    Console.ResetColor();
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Fin de l'analyse...");
                Console.ResetColor();
                // IMPORTANT : Attendre (ex: 2 minutes) pour éviter de spammer l'API d'OBI 
                // et se faire bannir l'IP (Rate limit)
                await Task.Delay(TimeSpan.FromSeconds(20));
            }
        }


        private static async Task MarketCheckerTask(List<IStockChecker> stockCheckers)
        {
            var tasks = stockCheckers.Select(async checker =>
            {
                try
                {
                    await checker.CheckStockAsync(); // idéalement CheckStockAsync(token)
                }
                catch (OperationCanceledException)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[TIMEOUT] {checker}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[ERROR {checker}] {ex.Message}");
                    Console.ResetColor();
                }
            });

            var cycleTask = Task.WhenAll(tasks);

            if (await Task.WhenAny(cycleTask, Task.Delay(TimeSpan.FromMinutes(5))) != cycleTask)
            {
                Console.WriteLine("[TIMEOUT GLOBAL] cycle annulé, restart boucle");
            }
        }
    }
}