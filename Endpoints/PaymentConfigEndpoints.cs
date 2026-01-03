using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Services;
using System.Security.Claims;

namespace GhanaHybridRentalApi.Endpoints;

public static class PaymentConfigEndpoints
{
    public static void MapPaymentConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/payment-config")
            .WithTags("Payment Configuration")
            .RequireAuthorization();

        // Get all payment configurations
        group.MapGet("/", GetPaymentConfigs)
            .WithName("GetPaymentConfigs")
            .WithDescription("Get all payment gateway configurations");

        // Diagnostic status for payment providers (admin-only)
        group.MapGet("/status", GetPaymentConfigStatus)
            .WithName("GetPaymentConfigStatus")
            .WithDescription("Get payment provider configuration status (non-sensitive)");

        // Get exchange rate (admin-only)
        group.MapGet("/exchange-rate", GetExchangeRateConfig)
            .WithName("GetExchangeRateConfig")
            .WithDescription("Get current GHS to USD exchange rate");

        // Public (unauthenticated) diagnostic status for guests/frontends
        app.MapGet("/api/v1/payment-config/status", GetPublicPaymentConfigStatus)
            .WithName("GetPublicPaymentConfigStatus")
            .WithDescription("Public payment provider configuration status (non-sensitive)");

        // Public (unauthenticated) exchange rate for guests/frontends
        app.MapGet("/api/v1/payment-config/exchange-rate", GetPublicExchangeRateConfig)
            .WithName("GetPublicExchangeRateConfig")
            .WithDescription("Public exchange rate for currency conversion");

        // Update Stripe configuration
        group.MapPut("/stripe", UpdateStripeConfig)
            .WithName("UpdateStripeConfig")
            .WithDescription("Update Stripe payment gateway configuration");

        // Update Paystack configuration
        group.MapPut("/paystack", UpdatePaystackConfig)
            .WithName("UpdatePaystackConfig")
            .WithDescription("Update Paystack payment gateway configuration");

        // Update WhatsApp configuration
        group.MapPut("/whatsapp", UpdateWhatsAppConfig)
            .WithName("UpdateWhatsAppConfig")
            .WithDescription("Update WhatsApp Cloud API configuration");

        // Update SMTP Email configuration
        group.MapPut("/smtp", UpdateSmtpConfig)
            .WithName("UpdateSmtpConfig")
            .WithDescription("Update SMTP email configuration");

        // Update Currency configuration
        group.MapPut("/currency", UpdateCurrencyConfig)
            .WithName("UpdateCurrencyConfig")
            .WithDescription("Update default currency configuration");

        // Update Exchange Rate configuration
        group.MapPut("/exchange-rate", UpdateExchangeRateConfig)
            .WithName("UpdateExchangeRateConfig")
            .WithDescription("Update GHS to USD exchange rate for Stripe payments");
    }

    private static async Task<IResult> GetPaymentConfigs(
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var configs = await db.AppConfigs
            .Where(c => c.ConfigKey.StartsWith("Payment:") || c.ConfigKey.StartsWith("WhatsApp:"))
            .ToListAsync();

        // Return masked sensitive data
        var result = configs.Select(c => new
        {
            c.ConfigKey,
            ConfigValue = c.IsSensitive ? "********" : c.ConfigValue,
            c.IsSensitive,
            c.UpdatedAt
        });

        return Results.Ok(result);
    }

    private static async Task<IResult> GetPaymentConfigStatus(
        IAppConfigService configService,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var stripeEnabled = bool.TryParse(await configService.GetConfigValueAsync("Payment:Stripe:Enabled"), out var se) && se;
        var paystackEnabled = bool.TryParse(await configService.GetConfigValueAsync("Payment:Paystack:Enabled"), out var pe) && pe;

        var stripeKey = await configService.GetConfigValueAsync("Payment:Stripe:SecretKey");
        var paystackKey = await configService.GetConfigValueAsync("Payment:Paystack:SecretKey");

        return Results.Ok(new
        {
            stripeEnabled,
            stripeConfigured = !string.IsNullOrWhiteSpace(stripeKey),
            paystackEnabled,
            paystackConfigured = !string.IsNullOrWhiteSpace(paystackKey)
        });
    }

    private static async Task<IResult> GetExchangeRateConfig(
        IAppConfigService configService,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var exchangeRateStr = await configService.GetConfigValueAsync("Payment:ExchangeRate:GHS_To_USD");
        var exchangeRate = 11.0m; // Default: 1 USD = 11 GHS
        
        if (!string.IsNullOrWhiteSpace(exchangeRateStr) && decimal.TryParse(exchangeRateStr, out var rate) && rate > 0)
        {
            exchangeRate = rate;
        }

        return Results.Ok(new
        {
            ghsToUsd = exchangeRate,
            description = $"1 USD = {exchangeRate:F2} GHS"
        });
    }

    // Public endpoint - non-sensitive booleans only, accessible without auth
    private static async Task<IResult> GetPublicPaymentConfigStatus(
        IAppConfigService configService)
    {
        var stripeEnabled = bool.TryParse(await configService.GetConfigValueAsync("Payment:Stripe:Enabled"), out var se) && se;
        var paystackEnabled = bool.TryParse(await configService.GetConfigValueAsync("Payment:Paystack:Enabled"), out var pe) && pe;

        var stripeKey = await configService.GetConfigValueAsync("Payment:Stripe:SecretKey");
        var paystackKey = await configService.GetConfigValueAsync("Payment:Paystack:SecretKey");

        return Results.Ok(new
        {
            stripeEnabled,
            stripeConfigured = !string.IsNullOrWhiteSpace(stripeKey),
            paystackEnabled,
            paystackConfigured = !string.IsNullOrWhiteSpace(paystackKey)
        });
    }

    private static async Task<IResult> GetPublicExchangeRateConfig(
        IAppConfigService configService)
    {
        var exchangeRateStr = await configService.GetConfigValueAsync("Payment:ExchangeRate:GHS_To_USD");
        var exchangeRate = 11.0m; // Default: 1 USD = 11 GHS
        
        if (!string.IsNullOrWhiteSpace(exchangeRateStr) && decimal.TryParse(exchangeRateStr, out var rate) && rate > 0)
        {
            exchangeRate = rate;
        }

        var currency = await configService.GetConfigValueAsync("Payment:Currency") ?? "GHS";
        var currencySymbol = await configService.GetConfigValueAsync("Payment:CurrencySymbol") ?? "GHS";

        return Results.Ok(new
        {
            ghsToUsd = exchangeRate,
            usdToGhs = Math.Round(exchangeRate, 2),
            currency,
            currencySymbol,
            description = $"1 USD = {exchangeRate:F2} GHS"
        });
    }

    private static async Task<IResult> UpdateStripeConfig(
        StripeConfigDto dto,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        // Update or create Stripe configurations
        await UpsertConfigAsync(db, "Payment:Stripe:SecretKey", dto.SecretKey, true);
        await UpsertConfigAsync(db, "Payment:Stripe:PublishableKey", dto.PublishableKey, false);
        await UpsertConfigAsync(db, "Payment:Stripe:WebhookSecret", dto.WebhookSecret, true);
        await UpsertConfigAsync(db, "Payment:Stripe:Enabled", dto.Enabled.ToString(), false);

        await db.SaveChangesAsync();

        return Results.Ok(new { Message = "Stripe configuration updated successfully" });
    }

    private static async Task<IResult> UpdatePaystackConfig(
        PaystackConfigDto dto,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        // Basic validation: SecretKey should not be a publishable key (pk_...)
        if (!string.IsNullOrWhiteSpace(dto.SecretKey) && dto.SecretKey.StartsWith("pk_", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "Provided SecretKey appears to be a publishable key (starts with 'pk_'). Please provide the Paystack secret key (sk_...)" });
        }

        // Update or create Paystack configurations
        await UpsertConfigAsync(db, "Payment:Paystack:SecretKey", dto.SecretKey, true);
        await UpsertConfigAsync(db, "Payment:Paystack:PublicKey", dto.PublicKey, false);
        await UpsertConfigAsync(db, "Payment:Paystack:WebhookSecret", dto.WebhookSecret, true);
        await UpsertConfigAsync(db, "Payment:Paystack:Enabled", dto.Enabled.ToString(), false);

        await db.SaveChangesAsync();

        return Results.Ok(new { Message = "Paystack configuration updated successfully" });
    }

    private static async Task<IResult> UpdateWhatsAppConfig(
        WhatsAppConfigDto dto,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        // Update or create WhatsApp configurations
        await UpsertConfigAsync(db, "WhatsApp:CloudApi:AccessToken", dto.AccessToken, true);
        await UpsertConfigAsync(db, "WhatsApp:CloudApi:PhoneNumberId", dto.PhoneNumberId, false);
        await UpsertConfigAsync(db, "WhatsApp:UseCloudApi", dto.Enabled.ToString(), false);

        await db.SaveChangesAsync();

        return Results.Ok(new { Message = "WhatsApp configuration updated successfully" });
    }

    private static async Task<IResult> UpdateSmtpConfig(
        SmtpConfigDto dto,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        // Update or create SMTP configurations
        await UpsertConfigAsync(db, "Email:Smtp:Host", dto.Host, false);
        await UpsertConfigAsync(db, "Email:Smtp:Port", dto.Port.ToString(), false);
        await UpsertConfigAsync(db, "Email:Smtp:Username", dto.Username, false);
        await UpsertConfigAsync(db, "Email:Smtp:Password", dto.Password, true);
        await UpsertConfigAsync(db, "Email:Smtp:UseSsl", dto.UseSsl.ToString(), false);
        await UpsertConfigAsync(db, "Email:Smtp:FromEmail", dto.FromEmail, false);
        await UpsertConfigAsync(db, "Email:Smtp:FromName", dto.FromName, false);

        await db.SaveChangesAsync();

        return Results.Ok(new { Message = "SMTP configuration updated successfully" });
    }

    private static async Task<IResult> UpdateCurrencyConfig(
        CurrencyConfigDto dto,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        // Update currency configuration
        await UpsertConfigAsync(db, "Payment:Currency", dto.Currency, false);
        await UpsertConfigAsync(db, "Payment:CurrencySymbol", dto.Symbol ?? "GHS", false);

        await db.SaveChangesAsync();

        return Results.Ok(new { Message = $"Currency updated to {dto.Currency}" });
    }

    private static async Task<IResult> UpdateExchangeRateConfig(
        ExchangeRateConfigDto dto,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        if (dto.GhsToUsd <= 0)
            return Results.BadRequest(new { error = "Exchange rate must be greater than zero" });

        // Update exchange rate configuration (1 USD = X GHS)
        await UpsertConfigAsync(db, "Payment:ExchangeRate:GHS_To_USD", dto.GhsToUsd.ToString("F2"), false);

        await db.SaveChangesAsync();

        return Results.Ok(new { Message = $"Exchange rate updated: 1 USD = {dto.GhsToUsd:F2} GHS" });
    }

    private static async Task UpsertConfigAsync(AppDbContext db, string key, string value, bool isSensitive)
    {
        var config = await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == key);
        
        if (config == null)
        {
            config = new Models.AppConfig
            {
                ConfigKey = key,
                ConfigValue = value,
                IsSensitive = isSensitive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.AppConfigs.Add(config);
        }
        else
        {
            config.ConfigValue = value;
            config.IsSensitive = isSensitive;
            config.UpdatedAt = DateTime.UtcNow;
        }
    }
}

public record StripeConfigDto(
    string SecretKey,
    string PublishableKey,
    string WebhookSecret,
    bool Enabled
);

public record PaystackConfigDto(
    string SecretKey,
    string PublicKey,
    string WebhookSecret,
    bool Enabled
);

public record WhatsAppConfigDto(
    string AccessToken,
    string PhoneNumberId,
    bool Enabled
);

public record SmtpConfigDto(
    string Host,
    int Port,
    string Username,
    string Password,
    bool UseSsl,
    string FromEmail,
    string FromName
);

public record CurrencyConfigDto(
    string Currency,
    string? Symbol
);

public record ExchangeRateConfigDto(
    decimal GhsToUsd
);
