using Microsoft.Playwright;
using System.Threading.Tasks;

namespace SioMideaPortasplitWatcher
{
    internal interface IStockChecker
    {
        public Task<IPage> CreatePage();
        public Task CheckStockAsync();
    }
}