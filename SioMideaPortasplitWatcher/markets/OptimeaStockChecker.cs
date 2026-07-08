using Microsoft.Playwright;
using SioMideaPortasplitWatcher;

public class OptimeaStockChecker : IStockChecker
{
    public event EventHandler? NewStockDetected;
    public event EventHandler? StockOutDetected;

    private bool? _previousState;

    private IPage? _page;

    public readonly string ProductName;
    public readonly string ProductUrl;

    public OptimeaStockChecker(string productName, int productId, string productUrl)
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

        var addToCartButton = _page.Locator("button.single_add_to_cart_button[name='add-to-cart']");

        bool available = await addToCartButton.IsVisibleAsync();

        if (_previousState.HasValue)
        {
            if (!_previousState.Value && available)
                NewStockDetected?.Invoke(this, EventArgs.Empty);

            if (_previousState.Value && !available)
                StockOutDetected?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            if (available)
                NewStockDetected?.Invoke(this, EventArgs.Empty);
        }

        _previousState = available;
    }
}