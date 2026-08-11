using Automation.Framework.Builders;
using Automation.Framework.Configuration;
using Automation.Framework.Context;
using Automation.Framework.Core.Http;
using Automation.Framework.Core.Session;
using Automation.Framework.Core.UserPool;
using Automation.Framework.Services.Identity.Flow;
using Automation.Framework.Services.Transfer.Flow;
using Automation.Framework.Services.Wallet.Flow;
using Automation.Framework.Services.Account.Flow;
using Automation.Framework.Services.Account.Client;
using Automation.Framework.Services.Cart.Client;
using Automation.Framework.Services.Cart.Flow;
using Automation.Framework.Services.Catalog.Client;
using Automation.Framework.Services.Catalog.Flow;
using Automation.Framework.Services.Identity.Client;
using Automation.Framework.Services.Transfer.Client;
using Automation.Framework.Services.Wallet.Client;
using Automation.Framework.Services.CashFlowReport.Client;
using Automation.Framework.Services.CashFlowReport.Flow;
using Automation.Framework.Services.OperatorCashback.Client;
using Automation.Framework.Services.OperatorCashback.Flow;
using Automation.Framework.Services.FazzaTopup.Client;
using Automation.Framework.Services.FazzaTopup.Flow;
using Automation.Framework.Services.Region.Flow;
using Automation.Framework.Services.Region.Client;
using Automation.Framework.Services.AccountReadiness.Flow;
using Automation.Framework.Services.Almusher.Client;
using Automation.Framework.Services.Almusher.Flow;
using Automation.Framework.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Automation.Framework.Composition
{
    /// <summary>
    /// جذر التهيئة (Composition Root). إضافة خدمة جديدة = إضافة سطرين هنا (Client + Flow).
    /// </summary>
    public static class FrameworkRegistration
    {
        public static IServiceCollection AddAutomationFramework(this IServiceCollection services)
        {
            // ---------- البنية التحتية ----------
            services.AddSingleton(_ => ConfigurationManager.Settings);
            services.AddSingleton<StateManager>();
            services.AddSingleton(_ => new UserPoolRegistry(ConfigurationManager.Settings.Users));

            // ApiClient: يختار طبقة وهمية أو شبكة حقيقية حسب الإعداد
            services.AddSingleton(_ =>
                ConfigurationManager.Settings.ApiSettings.UseFakeBackend
                    ? new ApiClient(new FakeBackendHandler())
                    : new ApiClient());

            // ---------- الـ Clients ----------
            services.AddSingleton<AuthClient>();
            services.AddSingleton<AccountClient>();
            services.AddSingleton<WalletClient>();
            services.AddSingleton<TransferClient>();

            // ✅ Catalog Service (جديد)
            services.AddSingleton<CatalogClient>();
            services.AddSingleton<CartClient>();
            services.AddSingleton<CatalogFlow>();

            // ✅ Cart Service (جديد)
          
            services.AddSingleton<CartFlow>();
            services.AddSingleton<CatalogFlow>();
            // ---------- الـ Flows ----------
            services.AddSingleton<AuthenticationFlow>();
            services.AddSingleton<AccountFlow>();
            services.AddSingleton<WalletFlow>();
            services.AddSingleton<TransferFlow>();
            services.AddSingleton<CartFlow>();
            services.AddSingleton<CatalogFlow>();

            // Fazza Service
            services.AddSingleton<FazzaTopUpClient>();   // الـ Client
            services.AddSingleton<FazzaTopUpFlow>();     // الـ Flow
            // region
            services.AddSingleton<RegionClient>();
            services.AddSingleton<RegionFlow>();
            //CashFlow Report 
            services.AddSingleton<CashflowReportClient>();
            services.AddSingleton<CashflowReportFlow>();
            //Operator Cashback Report
            services.AddSingleton<OperatorCashbackReportClient>();
            services.AddSingleton<OperatorCashbackFlow>();

            // Almusher Service
            services.AddSingleton<AlMusheerClient>();
            services.AddSingleton<AlMusheerFlow>();
            // ---------- آلية الجلسات ----------
            services.AddSingleton<SessionBuilder>();
            services.AddSingleton<SessionCache>();
            services.AddSingleton<ResilientSession>();

            services.AddSingleton<AccountReadinessFlow>();
            services.AddSingleton<FazzaLimitReadinessFlow>();

            return services;
        }
    }
}
