using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SioMideaPortasplitWatcher
{
    internal class Drive
    {
        private static readonly HttpClient client = new HttpClient();

        // Simule ton cache et tes coordonnées de départ (Nancy)
        private static readonly Dictionary<string, RouteCacheItem> _routeCache = new();
        private static readonly double NancyLat = 48.692054;
        private static readonly double NancyLon = 6.184417;

        public class RouteCacheItem
        {
            public TimeSpan Duration { get; set; }
            public double DistanceKm { get; set; }
        }

        public static async Task<(TimeSpan Duration, double DistanceKm)> DisplayTravelTimeWithCacheAsync(string destinationAddress, string storeName, string acceptlanguage = "en")
        {
            if (string.IsNullOrWhiteSpace(destinationAddress))
            {
                Console.WriteLine($"\t[Erreur] Adresse manquante pour le magasin {storeName}");
                return (TimeSpan.FromMinutes(0), 0);
            }

            // 1. Vérification du cache de route
            if (_routeCache.TryGetValue(storeName, out var cachedRoute))
            {
                PrintRouteInfo(storeName, cachedRoute.Duration, cachedRoute.DistanceKm, isFromCache: true);
                return (cachedRoute.Duration, cachedRoute.DistanceKm);
            }

            try
            {
                // 2. GÉOCODAGE : Convertir l'adresse textuelle en coordonnées GPS (Lat/Lon)
                var (destLat, destLon) = await GeocodeAddressAsync(destinationAddress);
                if (destLat == 0 && destLon == 0)
                {
                    Console.WriteLine($"\t[Erreur] Impossible de géocoder l'adresse : {destinationAddress}");
                    return (TimeSpan.FromMinutes(0), 0);
                }

                // 3. Calcul de l'itinéraire classique avec OSRM
                string lonNancy = NancyLon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string latNancy = NancyLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonDest = destLon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string latDest = destLat.ToString(System.Globalization.CultureInfo.InvariantCulture);

                string osrmUrl = $"https://router.project-osrm.org/route/v1/driving/{lonNancy},{latNancy};{lonDest},{latDest}?overview=false";

                using (var request = new HttpRequestMessage(HttpMethod.Get, osrmUrl))
                {
                    request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) MideaPortasplitWatcher/2.0");

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();

                        using (JsonDocument doc = JsonDocument.Parse(responseBody))
                        {
                            var root = doc.RootElement;
                            if (root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
                            {
                                var firstRoute = routes[0];
                                double durationSeconds = firstRoute.GetProperty("duration").GetDouble();
                                double distanceMeters = firstRoute.GetProperty("distance").GetDouble();

                                TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);
                                double distanceKm = distanceMeters / 1000.0;

                                _routeCache[storeName] = new RouteCacheItem { Duration = duration, DistanceKm = distanceKm };
                                PrintRouteInfo(storeName, duration, distanceKm, isFromCache: false);
                                return (duration, distanceKm);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Impossible de calculer l'itinéraire depuis Nancy : {ex.Message}");
                Console.ResetColor();
            }
            return (TimeSpan.FromMinutes(0), 0);
        }

        // --- NOUVELLE MÉTHODE : GÉOCODAGE (Appel à Nominatim OpenStreetMap) ---
        private static async Task<(double lat, double lon)> GeocodeAddressAsync(string address, string acceptlanguage = "en")
        {
            try
            {
                // Échapper l'adresse pour l'URL
                string encodedAddress = Uri.EscapeDataString(address);
                string url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1&accept-language={acceptlanguage}";

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    // Un User-Agent valide et unique est STRICTEMENT OBLIGATOIRE pour Nominatim sous peine de BAN instantané
                    request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) MideaPortasplitWatcher/2.0 (contact@siogabx.fr)");

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (!response.IsSuccessStatusCode) return (0, 0);

                        string responseBody = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(responseBody))
                        {
                            var root = doc.RootElement;
                            if (root.GetArrayLength() > 0)
                            {
                                var firstResult = root[0];

                                // Nominatim renvoie des chaînes de caractères pour lat/lon, on les parse en double
                                double lat = double.Parse(firstResult.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                                double lon = double.Parse(firstResult.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);

                                return (lat, lon);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erreur Géocodage] Impossible de trouver les coordonnées de l'adresse : {ex.ToString()}");
            }

            return (0, 0);
        }

        private static void PrintRouteInfo(string storeName, TimeSpan duration, double distanceKm, bool isFromCache)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\tTemps de route estimé : {Math.Floor(duration.TotalHours)}h {duration.Minutes}min pour {distanceKm:F0} km");
            Console.ResetColor();
        }
    }
}
