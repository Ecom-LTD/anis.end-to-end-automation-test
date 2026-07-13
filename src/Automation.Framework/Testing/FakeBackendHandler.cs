using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Automation.Framework.Testing
{
    /// <summary>
    /// طبقة API وهمية داخل الذاكرة. تتيح تشغيل الاختبارات كاملةً دون خادم حقيقي.
    /// تحاكي: تسجيل الدخول، الملف الشخصي (اشتراكات/محافظ)، الحساب بالهاتف، الرصيد، والتحويل
    /// مع رصيد متّسق فعليًا (التحويل يخصم من المرسل ويضيف للمستقبل).
    /// استبدلها بخادمك الحقيقي بضبط ApiSettings.UseFakeBackend = false.
    /// </summary>
    public sealed class FakeBackendHandler : HttpMessageHandler
    {
        // walletId -> balance
        private readonly ConcurrentDictionary<Guid, decimal> _balances = new();
        // subscriptionId -> walletId (يُملأ أثناء توليد الملف الشخصي)
        private readonly ConcurrentDictionary<Guid, Guid> _subToWallet = new();
        private readonly object _transferLock = new();

        private const decimal StartingBalance = 1000m;

        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = null // PascalCase
        };

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var query = request.RequestUri!.Query;

            // 1) تسجيل الدخول: التوكن يحمل رقم الهاتف لتمييز المستخدم لاحقًا
            if (path.EndsWith("/connect/token"))
            {
                var form = ReadForm(request);
                var phone = form.GetValueOrDefault("number", "0000000000");
                return Ok(new { access_token = $"FAKE.{phone}", expires_in = 3600, token_type = "Bearer" });
            }

            // 2) الحساب بالهاتف
            if (path.Contains("/accounts"))
            {
                var phone = GetQuery(query, "phone");
                var accountId = GuidFrom($"account:{phone}");
                return Ok(new { Data = new { Results = new[] { new { Id = accountId } } } });
            }

            // 3) الملف الشخصي (يُشتق من رقم الهاتف داخل التوكن)
            if (path.Contains("/profile"))
            {
                var phone = PhoneFromToken(request);
                var profile = BuildProfile(phone);
                return Ok(profile);
            }

            // 4) الرصيد
            if (path.Contains("/wallets/") && path.EndsWith("/balance"))
            {
                var walletId = Guid.Parse(path.Split('/')[^2]);
                var balance = _balances.GetOrAdd(walletId, StartingBalance);
                return Ok(new { Data = balance });
            }

            // 5) التحويل
            if (path.EndsWith("/transactions/transfer"))
            {
                var body = ReadJson(request);
                var fromWallet = Guid.Parse(body.GetProperty("FromWalletId").GetString()!);
                var toSubscription = Guid.Parse(body.GetProperty("ToSubscriptionId").GetString()!);
                var amount = body.GetProperty("Amount").GetDecimal();

                lock (_transferLock)
                {
                    var senderBalance = _balances.GetOrAdd(fromWallet, StartingBalance);
                    if (senderBalance < amount)
                        return Ok(new { Success = false, Message = "Insufficient balance" });

                    _balances[fromWallet] = senderBalance - amount;

                    if (_subToWallet.TryGetValue(toSubscription, out var toWallet))
                    {
                        var receiverBalance = _balances.GetOrAdd(toWallet, StartingBalance);
                        _balances[toWallet] = receiverBalance + amount;
                    }
                }

                return Ok(new { Success = true, Message = "Operation successfully completed" });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent($"No fake route for {path}")
            });
        }

        private object BuildProfile(string phone)
        {
            // اشتراكان لكل مستخدم (Operator=0, Business=1) كلٌّ بمحفظته (LYD/Tripoli/Cash)
            var subscriptions = new List<object>();
            foreach (var type in new[] { 0, 1 })
            {
                var subId = GuidFrom($"sub:{phone}:{type}");
                var walletId = GuidFrom($"wallet:{phone}:{type}");
                _subToWallet[subId] = walletId;
                _balances.GetOrAdd(walletId, StartingBalance);

                subscriptions.Add(new
                {
                    SubscriptionId = subId,
                    SubscriptionName = "اختبارات ",
                    SubscriptionType = type,
                    Wallets = new[]
                    {
                        new
                        {
                            WalletId = walletId,
                            RegionId = GuidFrom("region:Tripoli"),
                            RegionName = "Tripoli",
                            HolderName = "Cash",
                            CurrencyType = 1 // LYD
                        }
                    }
                });
            }
            return new { Data = new { Subscriptions = subscriptions } };
        }

        // ---------- helpers ----------
        private static Task<HttpResponseMessage> Ok(object payload)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, Json), Encoding.UTF8, "application/json")
            });

        private static Dictionary<string, string> ReadForm(HttpRequestMessage req)
        {
            var raw = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return raw.Split('&')
                .Select(p => p.Split('=', 2))
                .Where(kv => kv.Length == 2)
                .ToDictionary(kv => Uri.UnescapeDataString(kv[0]), kv => Uri.UnescapeDataString(kv[1]));
        }

        private static JsonElement ReadJson(HttpRequestMessage req)
        {
            var raw = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonDocument.Parse(raw).RootElement;
        }

        private static string GetQuery(string query, string key)
        {
            var q = query.TrimStart('?').Split('&')
                .Select(p => p.Split('=', 2))
                .FirstOrDefault(kv => kv.Length == 2 && kv[0] == key);
            return q is null ? "" : Uri.UnescapeDataString(q[1]);
        }

        private static string PhoneFromToken(HttpRequestMessage req)
        {
            var token = req.Headers.Authorization?.Parameter ?? "";
            return token.StartsWith("FAKE.") ? token[5..] : "0000000000";
        }

        private static Guid GuidFrom(string seed)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(seed));
            return new Guid(bytes);
        }
    }
}
