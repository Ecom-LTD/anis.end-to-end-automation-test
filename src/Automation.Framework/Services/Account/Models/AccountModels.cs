namespace Automation.Framework.Services.Account.Models
{
    public class AccountByPhoneNumberResponse { public AccountData Data { get; set; } = new(); }
    public class AccountData { public List<AccountItem> Results { get; set; } = new(); }
    public class AccountItem { public Guid Id { get; set; } }
}
