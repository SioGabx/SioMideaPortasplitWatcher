using Microsoft.Playwright;
using System.Text.RegularExpressions;
using SioMideaPortasplitWatcher;

public class ManoManoStockChecker : IStockChecker
{
    public event EventHandler? NewStockDetected;
    public event EventHandler? StockOutDetected;

    private bool? _previousState;
    private IPage? _page;

    public readonly string ProductName;
    public readonly string ProductUrl;

    public ManoManoStockChecker(string productName, string productUrl)
    {
        ProductName = productName;
        ProductUrl = productUrl;
    }

    public async Task<IPage> CreatePage()
    {
        _page = await Browser.CreatePage();
        return _page;
    }

    public async Task CheckStockAsync()
    {
        if (_page?.IsClosed != false)
            await CreatePage();

        await _page!.GotoAsync(ProductUrl, Browser.GotoOptions);

        // Récupère tous les scripts JSON-LD
        var jsonLdScripts = await _page
            .Locator("script[type='application/ld+json']")
            .AllTextContentsAsync();

        bool available = false;

        foreach (var json in jsonLdScripts)
        {
            if (json.Contains("\"availability\""))
            {
                available = json.Contains("InStock");
                break;
            }
        }

        if (_previousState.HasValue)
        {
            if (!_previousState.Value && available)
                NewStockDetected?.Invoke(this, EventArgs.Empty);

            if (_previousState.Value && !available)
                StockOutDetected?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            // Premier lancement : si déjà en stock on notifie
            if (available)
                NewStockDetected?.Invoke(this, EventArgs.Empty);
        }

        _previousState = available;
    }
}