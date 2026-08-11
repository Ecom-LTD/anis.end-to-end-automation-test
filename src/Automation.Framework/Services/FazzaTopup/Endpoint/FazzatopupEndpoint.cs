using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.FazzaTopup.Endpoint
{
    public class FazzatopupEndpoint
    {
        public static string SetFazzaDeptMaxLimit() =>
             $"/api/v1/fazaa-top-up/set-fazaa-debt-max-limit";

        public static string ChangeSulfaExtraRequestCount() =>
            "/api/v1/fazaa-top-up/change-sulfa-extra-request-count";

        public static string SetSulfaExtraGracePeriod() =>
            "/api/v1/fazaa-top-up/set-sulfa-extra-grace-period";

        public static string AddSulfaProvisionalExtraGracePeriod() =>
            "/api/v1/fazaa-top-up/add-sulfa-provisional-extra-grace-period";
     
        public static string GetAccountDetails(Guid accountId) =>
             $"/api/management/v1.0/accounts/{accountId}";
        public static string SetRegionMaxFazaaLimit() =>
             "/api/v1/fazaa-top-up/set-region-max-fazaa-limit";

        public static string FilterSulfaAccounts(string phone) =>
              $"/api/v1/fazaa-top-up/filter-accounts?page=1&size=25&phone={phone}&isoverdue=False&isexpired=False";

        public static string RequestSulfa() => $"/api/v1/fazaa-top-up-service-manager/request-fazaa-sulfa-top-up";
        public static string GetRegionSulfaData(string regionId) =>
              $"/api/v1/fazaa-top-up/get-main-regions?regionId={regionId}";
    }
}
