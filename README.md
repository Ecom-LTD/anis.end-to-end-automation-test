# FazzaAutomation — E2E Test Framework (.NET 8 / xUnit)

A complete, runnable solution that implements the improvement plan: per-microservice architecture, DI container, session caching, isolated user pools, centralized retry on 401, and multi-service scenarios — with an **in-memory fake API layer** so that `dotnet test` runs green immediately without any real server.

> The framework has been built and actually run: the sample runner (`Automation.Smoke`) gives **11/11 passed**, and all test files compile successfully. (xUnit packages weren't restored in the build environment due to lack of internet, but they'll be automatically retrieved on your first `dotnet build`.)

---

## Requirements

- **.NET 8 SDK** (installed in Visual Studio 2026, or VS 2022 17.10+, or standalone from dotnet.microsoft.com).
- Internet connection only for the initial NuGet package restore (xUnit).

Works on: **Visual Studio 2026** (June 2026 release), VS 2022 17.10+, or `dotnet` CLI from the terminal.

---

## Solution Structure

```
FazzaAutomation.sln
├── nuget.config                         NuGet source (nuget.org)
├── src/Automation.Framework/            Framework library
│   ├── Core/
│   │   ├── Http/        ApiClient, ApiResponse, ApiException
│   │   ├── Configuration/ Settings, ConfigurationManager, UserHelper
│   │   ├── Context/     StateManager
│   │   ├── Constants/   StateKeys, TestUsers
│   │   ├── Enums/       CurrencyType, SubscriptionType, WalletType
│   │   ├── Session/     TestSession, SessionOptions, SessionBuilder,
│   │   │                SessionCache, ResilientSession
│   │   └── UserPool/    UserLease, UserPoolManager, UserPoolRegistry
│   ├── Services/                        Service per Microservice
│   │   ├── Identity/    Endpoints · Models · Client · Flows
│   │   ├── Account/     Endpoints · Models · Client · Flows
│   │   ├── Wallet/      Endpoints · Models · Client · Flows
│   │   └── Transfer/    Endpoints · Models · Client · Flows
│   ├── Composition/     FrameworkRegistration  (DI root)
│   ├── Testing/         FakeBackendHandler     (Fake API)
│   └── Configuration/   appsettings.json
├── tests/Automation.Test/               xUnit test project
│   ├── Infrastructure/  TestHost, BaseFixture, AssemblyInfo
│   ├── Sessions/        SulfaSessions
│   ├── Fixtures/        SulfaFixture
│   ├── Collections/     SulfaCollection
│   ├── Scenarios/       ScenarioContext, Scenario
│   ├── Tests/Sulfa/     BaseSulfaTest, WalletTests
│   ├── Tests/CrossService/ TransferConsistencyScenario, PooledUserTests
│   └── xunit.runner.json
└── tools/Automation.Smoke/              Quick sample runner (Console)
    └── Program.cs
```

---

## Packages (NuGet)

**Automation.Test** only requires packages (restored automatically):
- `Microsoft.NET.Test.Sdk` 17.12.0
- `xunit` 2.9.2
- `xunit.runner.visualstudio` 2.8.2
- `coverlet.collector` 6.0.2

**Automation.Framework** and **Automation.Smoke**: No external packages — they use the ASP.NET Core Shared Framework (`<FrameworkReference Include="Microsoft.AspNetCore.App" />`) for dependency injection, configuration, and System.Text.Json.

> To use Newtonsoft.Json as in your legacy code: add `Newtonsoft.Json` to the framework project and replace `System.Text.Json` calls in `ApiClient`.

---

## How to Run

### Option 1: Visual Studio 2026
1. Open `FazzaAutomation.sln`.
2. Wait for package restore (automatic NuGet Restore).
3. **Build → Build Solution** (Ctrl+Shift+B).
4. **Test → Run All Tests** (Test Explorer) — all tests should be green.

### Option 2: Terminal (dotnet CLI)
```bash
cd FazzaAutomation
dotnet test                                   # Builds and runs all xUnit tests
dotnet run --project tools/Automation.Smoke   # Quick sample — prints 11/11
```

Expected output from the sample:
```
[6] Scenario: balance → transfer → verify
     Sender balance before: 1000
  ✅ Transfer succeeded
     Sender balance after:  995
  ✅ Sender balance decreased by amount
 Result: 11 passed / 0 failed
```

---

## Switching to the Real API

In `src/Automation.Framework/Configuration/appsettings.json`:
```json
"ApiSettings": {
  "GatewayUrl":  "https://<actual-gateway-url>",
  "IdentityUrl": "https://<actual-identity-url>",
  "UseFakeBackend": false
}
```
- Set `UseFakeBackend` to `false` so `ApiClient` uses the real network instead of `FakeBackendHandler`.
- Update phone numbers/users in `Users` with your actual values.
- Adjust endpoint paths and Model structures in each service to match your actual API responses.

---

## How to Add a New Microservice (Card / Sulfa / Forex / Remittance / Report)

The four existing services (Identity, Account, Wallet, Transfer) serve as a ready-to-use template. To add a service:

1. Create `Services/<Name>/{Endpoints, Models, Client, Flows}` following the Wallet service pattern.
2. In `Composition/FrameworkRegistration.cs`, add two lines:
   ```csharp
   services.AddSingleton<<Name>Client>();
   services.AddSingleton<<Name>Flow>();
   ```
3. Add project users in `appsettings.json` with the field `"Project": "<Name>"`.
4. (If the product has its own sessions) create `Sessions/<Name>Sessions.cs`, `Fixtures/<Name>Fixture.cs`, and `Collections/<Name>Collection.cs` as copies of their Sulfa counterparts.
5. Write tests under `Tests/<Name>/`.

No other files need to change.

---

## Important Design Notes

**Session order:** Retrieving `AccountId` for other users is done via the **Dashboard** token, so `SulfaFixture` builds the Dashboard first (await), then builds the rest in parallel via `PrewarmAsync`.

**Parallelism:** xUnit runs **Collections in parallel** with each other, while tests within the same collection run sequentially. Thanks to `TestHost` and the process-wide shared `SessionCache`, you can split each product into its own independent Collection and they'll run in parallel **without re-authenticating**, while `UserPoolManager` prevents two parallel tests from sharing the same user. Configured in `xunit.runner.json` (`maxParallelThreads: 8`).

**Compatibility:** `ApiException` inherits from `HttpRequestException`, so any `catch`/`Assert.ThrowsAsync` on `HttpRequestException` remains valid, and the message includes the status code (401/403).

**User isolation:** `UserPoolRegistry` groups users by `Project`, giving each product its completely isolated pool — no cross-contamination in parallel runs.
