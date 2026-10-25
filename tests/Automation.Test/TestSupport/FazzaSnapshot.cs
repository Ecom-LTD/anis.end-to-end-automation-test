using Automation.Framework.Services.FazzaTopup.Models;

namespace Automation.Test.TestSupport
{
    public class FazzaSnapshot
    {
        public decimal WalletBalance { get; set; }
        public SulfaAccount Account { get; set; } = null!;
    }
}