using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Services;

public interface ITurnstileService
{
    Task<(bool IsValid, string[]? Errors)> VerifyTokenAsync(string token);
}

public class TurnstileService : ITurnstileService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;
    private readonly ILogger<TurnstileService> _logger;

    public TurnstileService(
        HttpClient httpClient,
        AppDbContext db,
        ILogger<TurnstileService> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _logger = logger;
    }

    public async Task<(bool IsValid, string[]? Errors)> VerifyTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, new[] { "missing-token" });
        }

        // Get secret key from database
        var secretKeySetting = await _db.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "Turnstile:SecretKey");

        if (secretKeySetting == null || string.IsNullOrWhiteSpace(secretKeySetting.ValueJson))
        {
            _logger.LogError("Turnstile secret key not configured in database");
            return (false, new[] { "server-misconfigured" });
        }

        // ValueJson stores the secret key as a plain string (may be quoted JSON)
        var secretKey = secretKeySetting.ValueJson.Trim('"');

        try
        {
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = secretKey,
                ["response"] = token
            });

            var verifyResp = await _httpClient.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify", 
                form
            );

            var verify = await verifyResp.Content.ReadFromJsonAsync<TurnstileVerifyResponse>();

            if (verify == null)
            {
                _logger.LogError("Failed to deserialize Turnstile response");
                return (false, new[] { "verification-failed" });
            }

            if (!verify.Success)
            {
                _logger.LogWarning("Turnstile verification failed: {Errors}", 
                    string.Join(", ", verify.ErrorCodes ?? Array.Empty<string>()));
                return (false, verify.ErrorCodes);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Turnstile token");
            return (false, new[] { "verification-error" });
        }
    }
}
