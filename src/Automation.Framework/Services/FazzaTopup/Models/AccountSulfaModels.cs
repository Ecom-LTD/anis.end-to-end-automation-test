using Newtonsoft.Json;

namespace Automation.Framework.Services.FazzaTopup.Models
{
    public class SetAccountFazzaDeptMaxLimitRequest
    {
        public Guid AccountId { get; set; }
        public decimal MaxFazaaLimit { get; set; } = 0;
    }

    public class SetFazzaDeptMaxLimitResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChangeSulfaExtraRequestCountRequest
    {
        public Guid AccountId { get; set; }
        public int Number { get; set; }
        public bool IsReducingExtraCount { get; set; }
    }

    public class SetSulfaExtraGracePeriodRequest
    {
        public Guid AccountId { get; set; }
        public int Hours { get; set; }
        public bool IsReducingExtraGracePeriod { get; set; }
    }
    public class AddSulfaProvisionalExtraGracePeriodRequest
    {
        public Guid AccountId { get; set; }
        public int Hours { get; set; }
    }

    public class SulfaLimitOperationResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class SulfaAccountResponse
    {
        [JsonProperty("results")]
        public List<SulfaAccount> Results { get; set; } = new();

        [JsonProperty("currentPage")]
        public int CurrentPage { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }
    }

    public class SulfaAccount
    {
        public string Id { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;

        [JsonProperty("currentFazaaDebt")]
        public decimal CurrentFazaaDebt { get; set; }

        [JsonProperty("maxFazaaDebtLimit")]
        public decimal MaxFazaaDebtLimit { get; set; }

        [JsonProperty("currentSulfaDebt")]
        public decimal CurrentSulfaDebt { get; set; }

        [JsonProperty("extraSulfaRequestCount")]
        public int ExtraSulfaRequestCount { get; set; }

        [JsonProperty("extraSulfaGracePeriodHours")]
        public int ExtraSulfaGracePeriodHours { get; set; }

        [JsonProperty("debtOverdueAt")]
        public DateTime? DebtOverdueAt { get; set; }

        [JsonProperty("expiredAt")]
        public DateTime? ExpiredAt { get; set; }

        [JsonProperty("confirmedDebt")]
        public decimal ConfirmedDebt { get; set; }
    }
}
