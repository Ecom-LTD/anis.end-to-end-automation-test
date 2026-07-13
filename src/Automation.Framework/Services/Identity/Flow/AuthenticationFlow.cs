using Automation.Framework.Configuration;
using Automation.Framework.Context;
using Automation.Framework.Services.Identity.Client;

namespace Automation.Framework.Services.Identity.Flow;

public class AuthenticationFlow
{
    private readonly AuthClient _authClient;
    private readonly StateManager _state;

    public AuthenticationFlow(AuthClient authClient, StateManager state)
    {
        _authClient = authClient;
        _state = state;
    }

    public async Task LoginAsAsync(string userKey)
    {
        var user = UserHelper.GetUser(userKey);
        var resp = await _authClient.LoginAsync(user.PhoneNumber);
        _state.SetToken(userKey, resp.AccessToken);
    }
}
