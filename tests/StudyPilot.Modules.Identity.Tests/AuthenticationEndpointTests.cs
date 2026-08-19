using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StudyPilot.Modules.Identity.Authentication;
using StudyPilot.Modules.Identity.Domain;

namespace StudyPilot.Modules.Identity.Tests;

/// <summary>End-to-end authentication against the real API and a real database.</summary>
[Collection(IdentityApiCollection.Name)]
public class AuthenticationEndpointTests(IdentityApiFixture fixture)
{
    private const string Password = "an-acceptable-test-password";

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private async Task<string> RegisterAsync(string email, string password = Password)
    {
        using var scope = fixture.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountService>();

        var result = await accounts.CreateAsync(email, password);
        Assert.True(result.IsSuccess);

        return email;
    }

    private async Task<string> LoginAsync(string email, string password = Password)
    {
        var response = await fixture.CreateClient()
            .PostAsJsonAsync("/api/identity/login", new { email, password });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginBody>();
        return body!.AccessToken;
    }

    private sealed record LoginBody(string AccessToken, DateTimeOffset ExpiresAtUtc);

    private sealed record MeBody(Guid UserId, string Email);

    [Fact]
    public async Task A_registered_account_can_log_in_and_read_its_own_details()
    {
        var email = await RegisterAsync(UniqueEmail());
        var token = await LoginAsync(email);

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/identity/me");
        response.EnsureSuccessStatusCode();

        var me = await response.Content.ReadFromJsonAsync<MeBody>();
        Assert.Equal(email, me!.Email);
        Assert.NotEqual(Guid.Empty, me.UserId);
    }

    [Fact]
    public async Task Logging_in_with_a_wrong_password_is_rejected()
    {
        var email = await RegisterAsync(UniqueEmail());

        var response = await fixture.CreateClient()
            .PostAsJsonAsync("/api/identity/login", new { email, password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logging_in_with_an_unknown_account_is_rejected_the_same_way()
    {
        var response = await fixture.CreateClient()
            .PostAsJsonAsync("/api/identity/login", new { email = UniqueEmail(), password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_protected_endpoint_rejects_an_anonymous_request()
    {
        var response = await fixture.CreateClient().GetAsync("/api/identity/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_protected_endpoint_rejects_a_garbage_token()
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.token");

        var response = await client.GetAsync("/api/identity/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_stops_working_once_the_password_changes()
    {
        // The security stamp is what makes a password change actually revoke tokens already issued.
        var email = await RegisterAsync(UniqueEmail());
        var token = await LoginAsync(email);

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/identity/me")).StatusCode);

        using (var scope = fixture.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var user = await users.FindByEmailAsync(EmailAddress.Create(email).Value);
            user!.ChangePassword(hasher.Hash("a-brand-new-password-entirely"));
            await users.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/identity/me")).StatusCode);
    }

    [Fact]
    public async Task A_deactivated_account_cannot_use_a_token_it_already_holds()
    {
        var email = await RegisterAsync(UniqueEmail());
        var token = await LoginAsync(email);

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/identity/me")).StatusCode);

        using (var scope = fixture.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = await users.FindByEmailAsync(EmailAddress.Create(email).Value);
            user!.Deactivate();
            await users.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/identity/me")).StatusCode);
    }

    [Fact]
    public async Task The_database_rejects_a_duplicate_address_even_when_the_check_passes()
    {
        // Proves the unique index is doing the work, not just the application-level check.
        var email = UniqueEmail();
        await RegisterAsync(email);

        using var scope = fixture.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountService>();

        var result = await accounts.CreateAsync(email.ToUpperInvariant(), Password);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.EmailAlreadyRegistered.Code, result.Error!.Code);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/modules")]
    [InlineData("/api/identity/ping")]
    [InlineData("/api/academic/ping")]
    public async Task Endpoints_marked_anonymous_stay_reachable_under_the_fallback_policy(string route)
    {
        var response = await fixture.CreateClient().GetAsync(route);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
