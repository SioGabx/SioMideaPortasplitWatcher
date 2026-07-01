using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;

namespace SioMideaPortasplitWatcher
{
    public static class Browser
    {
        private static IPlaywright Playwright;
        private static IBrowserContext BrowserContext;

        public static async Task Initialize()
        {
            string binPath = AppDomain.CurrentDomain.BaseDirectory; 
            string profilePath = System.IO.Path.Combine(binPath, @"Browser User Data\Default");

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            BrowserContext = await Playwright.Chromium.LaunchPersistentContextAsync(
                profilePath,
               new BrowserTypeLaunchPersistentContextOptions
               {
                   //ExecutablePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                   Headless = false,
                   //BypassCSP = true,
                   Channel = "msedge", //chrome
                   Args = new[] {
                        "--disable-blink-features=AutomationControlled", // Masque navigator.webdriver
                        "--no-sandbox",
                        //"--disable-web-security",//cloudflare loop if activated
                        "--disable-infobars" // Retire la barre "Chrome est contrôlé par un logiciel de test..."
                }
               });
        }

        public static readonly PageGotoOptions GotoOptions = new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 1500000
        };

        public static readonly PageReloadOptions ReloadOptions = new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 1500000
        };

        public static async Task<IPage> CreatePage()
        {
            if (BrowserContext == null)
            {
                throw new NullReferenceException("BrowserContext is not initialized. Call Initialize() first.");
            }
            return await BrowserContext.NewPageAsync();
        }

    }
}
