using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.OperatorCashback.Endpoint
{
    public class OperatorCashbackEndpoint
    {
        // ========== ✅ تقارير الكاش باك للمشغل ==========
        public const string GetWeeklyOperatorCashbackReport = "/api/management/v3/operator-cashback-report/filter-weekly";
        public const string GetMonthlyOperatorCashbackReport = "/api/management/v3/operator-cashback-report/filter-monthly";
    }
}
