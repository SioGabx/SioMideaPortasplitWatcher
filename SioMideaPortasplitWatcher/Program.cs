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

        private static void PrintNewStockDetected(string StoreName,string ProductName, ConsoleColor ProductNameColor, int Stock, string Url)
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
        private static void ShowBallon(string StoreName, string ProductName, TimeSpan Duration, double DistanceKm, int NewQuantity, string Url)
        {
            var t = new BalloonNotifier("🚨 Midea disponible !", $"{ProductName}\n{StoreName}\n{Math.Floor(Duration.TotalHours)}h {Duration.Minutes}min pour {DistanceKm:F0} km \nStock: {NewQuantity}", Url, StoreName);
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
            var obiCheckerMP = new ObiDeStockChecker("Midea Portasplit 12000 BTU","8620890"); //https://www.obi.de/p/8620890/midea-mobile-split-klimaanlage-portasplit
            
            obiCheckerMP.NewStockDetected += async (sender, e) =>
            {
                var url = $"https://www.obi.de/api/disc/store/change?storeNumber={e.Store.StoreId}&redirectUrl={Uri.EscapeDataString($"https://www.obi.de/p/{obiCheckerMP.ProductId}")}";
                PrintNewStockDetected(e.Store.Name, obiCheckerMP.ProductName, ConsoleColor.Red, e.NewQuantity, url);
                var (Duration, DistanceKm) = await Drive.DisplayTravelTimeWithCacheAsync(e.Store.Address + ", Deutschland", e.Store.Name, "de");
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
                var (Duration, DistanceKm) = await Drive.DisplayTravelTimeWithCacheAsync(e.Store.Address + ", Deutschland", e.Store.Name, "de");
                ShowBallon(e.Store.Name, obiCheckerMPC.ProductName, Duration, DistanceKm, e.NewQuantity, url);
            };

            obiCheckerMPC.StockOutDetected += (sender, e) =>
            {
                PrintStockOutDetected(obiCheckerMPC.ProductName, e.Store.Name);
            };


            List<IStockChecker> stockCheckers = new List<IStockChecker>
            {
                obiCheckerMP, obiCheckerMPC
            };

            while (true)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Analyse (Actualisation)...");
                    Console.ResetColor();

                    foreach (var stockChecker in stockCheckers)
                    {
                        await stockChecker.CheckStockAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[Erreur Boucle] {ex.Message}");
                    Console.ResetColor();
                }

                // IMPORTANT : Attendre (ex: 2 minutes) pour éviter de spammer l'API d'OBI 
                // et se faire bannir l'IP (Rate limit)
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }
}