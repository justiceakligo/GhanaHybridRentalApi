using System.Security.Claims;
using System.Text.Json;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class CountryEndpoints
{
    public static void MapCountryEndpoints(this IEndpointRouteBuilder app)
    {
        // Public endpoint to get active countries
        app.MapGet("/api/v1/countries", GetActiveCountriesAsync)
            .AllowAnonymous();

        // Get current country context
        app.MapGet("/api/v1/country/current", GetCurrentCountryAsync)
            .AllowAnonymous();

        // Admin endpoints
        var adminGroup = app.MapGroup("/api/v1/admin/countries")
            .RequireAuthorization("AdminOnly");

        adminGroup.MapGet("/", GetAllCountriesAsync);
        adminGroup.MapGet("/{code}", GetCountryByCodeAsync);
        adminGroup.MapPost("/", CreateCountryAsync);
        adminGroup.MapPut("/{code}", UpdateCountryAsync);
        adminGroup.MapDelete("/{code}", DeactivateCountryAsync);
        adminGroup.MapPost("/{code}/activate", ActivateCountryAsync);
        
        // Country configuration endpoints
        adminGroup.MapPut("/{code}/payment-providers", UpdatePaymentProvidersAsync);
        adminGroup.MapPut("/{code}/config", UpdateCountryConfigAsync);
        adminGroup.MapPost("/{code}/app-config", SetCountryAppConfigAsync);
        adminGroup.MapGet("/{code}/app-config", GetCountryAppConfigAsync);
        
        // Bulk city management for country onboarding
        adminGroup.MapPost("/{code}/cities/bulk", BulkCreateCitiesAsync);
        adminGroup.MapGet("/{code}/cities", GetCountryCitiesAsync);
        
        // Country onboarding workflow
        adminGroup.MapGet("/{code}/onboarding-status", GetOnboardingStatusAsync);
        adminGroup.MapPost("/{code}/onboarding/complete", CompleteOnboardingAsync);
        
        // Testing and validation
        adminGroup.MapPost("/{code}/test", TestCountryConfigurationAsync);
        adminGroup.MapGet("/{code}/stats", GetCountryStatsAsync);
    }

    private static async Task<IResult> GetActiveCountriesAsync(AppDbContext db)
    {
        var countries = await db.Countries
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Code,
                c.Name,
                c.CurrencyCode,
                c.CurrencySymbol,
                c.PhoneCode,
                c.IsDefault
            })
            .ToListAsync();

        return Results.Ok(countries);
    }

    private static async Task<IResult> GetCurrentCountryAsync(
        ICountryContext countryContext,
        AppDbContext db)
    {
        var countryCode = countryContext.CountryCode;
        
        var country = await db.Countries
            .Where(c => c.Code == countryCode)
            .Select(c => new
            {
                c.Code,
                c.Name,
                c.CurrencyCode,
                c.CurrencySymbol,
                c.PhoneCode,
                c.Timezone,
                c.DefaultLanguage,
                c.IsActive,
                PaymentProviders = countryContext.GetEnabledPaymentProviders()
            })
            .FirstOrDefaultAsync();

        if (country == null)
        {
            return Results.Ok(new
            {
                Code = countryCode,
                Name = "Unknown",
                CurrencyCode = "GHS",
                CurrencySymbol = "₵",
                PhoneCode = "+233",
                Timezone = "Africa/Accra",
                DefaultLanguage = "en-GH",
                IsActive = false,
                PaymentProviders = new[] { "paystack" }
            });
        }

        return Results.Ok(country);
    }

    private static async Task<IResult> GetAllCountriesAsync(
        AppDbContext db,
        [FromQuery] bool? activeOnly = null)
    {
        var query = db.Countries.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(c => c.IsActive);
        }

        var countries = await query
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.CurrencyCode,
                c.CurrencySymbol,
                c.PhoneCode,
                c.Timezone,
                c.DefaultLanguage,
                c.IsActive,
                c.IsDefault,
                c.PaymentProvidersJson,
                c.CreatedAt,
                c.UpdatedAt
            })
            .ToListAsync();

        return Results.Ok(countries);
    }

    private static async Task<IResult> GetCountryByCodeAsync(
        AppDbContext db,
        string code)
    {
        var country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        return Results.Ok(country);
    }

    private static async Task<IResult> CreateCountryAsync(
        AppDbContext db,
        [FromBody] CountryCreateDto dto)
    {
        // Validate code
        if (string.IsNullOrWhiteSpace(dto.Code) || dto.Code.Length != 2)
        {
            return Results.BadRequest(new { error = "Country code must be 2 characters" });
        }

        // Check if country already exists
        var exists = await db.Countries.AnyAsync(c => c.Code == dto.Code.ToUpper());
        if (exists)
        {
            return Results.BadRequest(new { error = "Country already exists" });
        }

        var country = new Country
        {
            Code = dto.Code.ToUpper(),
            Name = dto.Name,
            CurrencyCode = dto.CurrencyCode,
            CurrencySymbol = dto.CurrencySymbol ?? "",
            PhoneCode = dto.PhoneCode ?? "",
            Timezone = dto.Timezone ?? "UTC",
            DefaultLanguage = dto.DefaultLanguage ?? "en",
            IsActive = dto.IsActive ?? false,
            IsDefault = false, // Only one default allowed
            PaymentProvidersJson = dto.PaymentProviders != null 
                ? JsonSerializer.Serialize(dto.PaymentProviders)
                : null,
            ConfigJson = dto.Config != null
                ? JsonSerializer.Serialize(dto.Config)
                : null
        };

        db.Countries.Add(country);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/admin/countries/{country.Code}", country);
    }

    private static async Task<IResult> UpdateCountryAsync(
        AppDbContext db,
        string code,
        [FromBody] CountryUpdateDto dto)
    {
        var country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        // Update fields if provided
        if (dto.Name != null) country.Name = dto.Name;
        if (dto.CurrencyCode != null) country.CurrencyCode = dto.CurrencyCode;
        if (dto.CurrencySymbol != null) country.CurrencySymbol = dto.CurrencySymbol;
        if (dto.PhoneCode != null) country.PhoneCode = dto.PhoneCode;
        if (dto.Timezone != null) country.Timezone = dto.Timezone;
        if (dto.DefaultLanguage != null) country.DefaultLanguage = dto.DefaultLanguage;
        if (dto.IsActive.HasValue) country.IsActive = dto.IsActive.Value;
        
        if (dto.PaymentProviders != null)
        {
            country.PaymentProvidersJson = JsonSerializer.Serialize(dto.PaymentProviders);
        }

        if (dto.Config != null)
        {
            country.ConfigJson = JsonSerializer.Serialize(dto.Config);
        }

        country.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(country);
    }

    private static async Task<IResult> DeactivateCountryAsync(
        AppDbContext db,
        string code)
    {
        var country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        if (country.IsDefault)
        {
            return Results.BadRequest(new { error = "Cannot deactivate the default country" });
        }

        country.IsActive = false;
        country.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Country deactivated successfully" });
    }

    private static async Task<IResult> ActivateCountryAsync(
        AppDbContext db,
        string code)
    {
        var country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        country.IsActive = true;
        country.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Country activated successfully" });
    }

    private static async Task<IResult> UpdatePaymentProvidersAsync(
        AppDbContext db,
        string code,
        [FromBody] PaymentProvidersDto dto)
    {
        var country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        country.PaymentProvidersJson = JsonSerializer.Serialize(dto.Providers);
        country.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new 
        { 
            message = "Payment providers updated successfully",
            providers = dto.Providers 
        });
    }

    private static async Task<IResult> UpdateCountryConfigAsync(
  

public record PaymentProvidersDto(
    List<string> Providers
);

public record CountryConfigDto(
    Dictionary<string, object> Config
);

public record CountryAppConfigDto(
    Dictionary<string, string> Settings
);

public record BulkCitiesDto(
    List<BulkCityDto> Cities
);

public record BulkCityDto(
    string Name,
    string? Region,
    bool? IsActive,
    int? DisplayOrder,
    decimal? DefaultDeliveryFee,
    string? Description
);      AppDbContext db,
        string code,
        [FromBody] CountryConfigDto dto)
    {
        var country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        // Merge with existing config
        var existingConfig = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(country.ConfigJson))
        {
            try
            {
                existingConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(country.ConfigJson) 
                    ?? new Dictionary<string, object>();
            }
            catch
            {
                // Start fresh if parsing fails
            }
        }

        // Update with new values
        foreach (var kvp in dto.Config)
        {
            existingConfig[kvp.Key] = kvp.Value;
        }

        country.ConfigJson = JsonSerializer.Serialize(existingConfig);
        country.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new 
        { 
            message = "Country configuration updated successfully",
            config = existingConfig 
        });
    }

    private static async Task<IResult> SetCountryAppConfigAsync(
        AppDbContext db,
        string code,
        [FromBody] CountryAppConfigDto dto)
    {
        var country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        // Create or update AppConfig entries with country prefix
        foreach (var kvp in dto.Settings)
        {
            var configKey = $"{code.ToUpper()}:{kvp.Key}";
            var existing = await db.AppConfigs
                .FirstOrDefaultAsync(a => a.ConfigKey == configKey);

            if (existing != null)
            {
                existing.ConfigValue = kvp.Value;
            }
            else
            {
                db.AppConfigs.Add(new AppConfig
                {
                    ConfigKey = configKey,
                    ConfigValue = kvp.Value
                });
            }
        }

        await db.SaveChangesAsync();

        return Results.Ok(new 
        { 
            message = "Country app configuration updated successfully",
            settings = dto.Settings 
        });
    }

    private static async Task<IResult> GetCountryAppConfigAsync(
        AppDbContext db,
        string code)
    {
        var country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        // Get all AppConfig entries for this country
        var prefix = $"{code.ToUpper()}:";
        var configs = await db.AppConfigs
            .Where(a => a.ConfigKey.StartsWith(prefix))
            .ToListAsync();

        var settings = configs.ToDictionary(
            c => c.ConfigKey.Substring(prefix.Length),
            c => c.ConfigValue
        );

        return Results.Ok(new 
        { 
            country = code.ToUpper(),
            settings = settings 
        });
    }

    private static async Task<IResult> BulkCreateCitiesAsync(
        AppDbContext db,
        string code,
        [FromBody] BulkCitiesDto dto)
    {
        var country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        var createdCities = new List<City>();
        var errors = new List<string>();

        foreach (var cityDto in dto.Cities)
        {
            // Check if city already exists
            var existingCity = await db.Cities
                .FirstOrDefaultAsync(c => 
                    c.Name.ToLower() == cityDto.Name.ToLower() && 
                    c.CountryId == country.Id);

            if (existingCity != null)
            {
                errors.Add($"City '{cityDto.Name}' already exists");
                continue;
            }

            var city = new City
            {
                Name = cityDto.Name,
                Region = cityDto.Region,
                CountryCode = country.Code,
                CountryId = country.Id,
                IsActive = cityDto.IsActive ?? true,
                DisplayOrder = cityDto.DisplayOrder ?? 0,
                DefaultDeliveryFee = cityDto.DefaultDeliveryFee,
                Description = cityDto.Description
            };

            db.Cities.Add(city);
            createdCities.Add(city);
        }

        await db.SaveChangesAsync();

        return Results.Ok(new 
        { 
            message = $"Created {createdCities.Count} cities for {country.Name}",
            created = createdCities.Count,
            errors = errors.Any() ? errors : null,
            cities = createdCities.Select(c => new { c.Id, c.Name, c.Region })
        });
    }

    private static async Task<IResult> GetCountryCitiesAsync(
        AppDbContext db,
        string code)
    {
        var country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        var cities = await db.Cities
            .Where(c => c.CountryId == country.Id)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Region,
                c.IsActive,
                c.DisplayOrder,
                c.DefaultDeliveryFee,
                c.Description
            })
            .ToListAsync();

        return Results.Ok(new 
        { 
            country = country.Name,
            countryCode = country.Code,
            totalCities = cities.Count,
            activeCities = cities.Count(c => c.IsActive),
            cities = cities
        });
    }

    private static async Task<IResult> GetOnboardingStatusAsync(
        AppDbContext db,
        string code)
    {
        var country = await db.Countries
            .Include(c => c.Cities)
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        // Check onboarding criteria
        var hasCities = country.Cities?.Any() == true;
        var hasPaymentProviders = !string.IsNullOrEmpty(country.PaymentProvidersJson);
        var hasCurrencyConfig = !string.IsNullOrEmpty(country.CurrencyCode);
        
        // Check for payment configuration in AppConfig
        var prefix = $"{code.ToUpper()}:Payment:";
        var hasPaymentConfig = await db.AppConfigs
            .AnyAsync(a => a.ConfigKey.StartsWith(prefix));

        // Count vehicles (if any)
        var vehicleCount = await db.Vehicles
            .Include(v => v.City)
            .CountAsync(v => v.City != null && v.City.CountryId == country.Id);

        var checklist = new[]
        {
            new { step = "Country Created", completed = true, required = true },
            new { step = "Currency Configured", completed = hasCurrencyConfig, required = true },
            new { step = "Payment Providers Set", completed = hasPaymentProviders, required = true },
            new { step = "Payment Configuration", completed = hasPaymentConfig, required = true },
            new { step = "Cities Added", completed = hasCities, required = true },
            new { step = "Vehicles Listed", completed = vehicleCount > 0, required = false },
            new { step = "Country Activated", completed = country.IsActive, required = true }
        };

        var requiredCompleted = checklist.Where(c => c.required && c.completed).Count();
        var requiredTotal = checklist.Count(c => c.required);
        var isReady = requiredCompleted == requiredTotal;

        return Results.Ok(new 
        { 
            country = country.Name,
            countryCode = country.Code,
            isActive = country.IsActive,
            isReady = isReady,
            progress = new
            {
                completed = requiredCompleted,
                total = requiredTotal,
                percentage = (int)((double)requiredCompleted / requiredTotal * 100)
            },
            checklist = checklist,
            stats = new
            {
                citiesCount = country.Cities?.Count ?? 0,
                vehiclesCount = vehicleCount,
                hasPaymentConfig = hasPaymentConfig
            },
            recommendations = !isReady ? new[]
            {
                !hasPaymentProviders ? "Set payment providers" : null,
                !hasPaymentConfig ? "Configure payment gateway credentials" : null,
                !hasCities ? "Add at least one city" : null,
                !country.IsActive ? "Activate country when ready" : null
            }.Where(r => r != null).ToArray() : null
        });
    }

    private static async Task<IResult> CompleteOnboardingAsync(
        AppDbContext db,
        string code)
    {
        var country = await db.Countries
            .Include(c => c.Cities)
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        // Verify onboarding requirements
        var hasCities = country.Cities?.Any() == true;
        var hasPaymentProviders = !string.IsNullOrEmpty(country.PaymentProvidersJson);
        
        if (!hasCities)
        {
            return Results.BadRequest(new { error = "Cannot complete onboarding: No cities added" });
        }

        if (!hasPaymentProviders)
        {
            return Results.BadRequest(new { error = "Cannot complete onboarding: No payment providers configured" });
        }

        // Activate country
        country.IsActive = true;
        country.UpdatedAt = DateTime.UtcNow;

        // Add onboarding completion timestamp to config
        var config = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(country.ConfigJson))
        {
            try
            {
                config = JsonSerializer.Deserialize<Dictionary<string, object>>(country.ConfigJson) 
                    ?? new Dictionary<string, object>();
            }
            catch { }
        }

        config["onboardingCompletedAt"] = DateTime.UtcNow.ToString("O");
        country.ConfigJson = JsonSerializer.Serialize(config);

        await db.SaveChangesAsync();

        return Results.Ok(new 
        { 
            message = $"{country.Name} onboarding completed successfully",
            country = country.Name,
            countryCode = country.Code,
            isActive = country.IsActive,
            completedAt = DateTime.UtcNow
        });
    }

    private static async Task<IResult> TestCountryConfigurationAsync(
        AppDbContext db,
        string code)
    {
        var country = await db.Countries
            .Include(c => c.Cities)
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        var tests = new List<object>();

        // Test 1: Basic info
        tests.Add(new 
        { 
            test = "Basic Information",
            passed = !string.IsNullOrEmpty(country.Name) && 
                     !string.IsNullOrEmpty(country.Code),
            details = new { country.Name, country.Code, country.CurrencyCode, country.CurrencySymbol }
        });

        // Test 2: Payment providers
        var providers = new List<string>();
        if (!string.IsNullOrEmpty(country.PaymentProvidersJson))
        {
            try
            {
                providers = JsonSerializer.Deserialize<List<string>>(country.PaymentProvidersJson) 
                    ?? new List<string>();
            }
            catch { }
        }
        tests.Add(new 
        { 
            test = "Payment Providers",
            passed = providers.Any(),
            details = new { providers = providers }
        });

        // Test 3: Cities
        tests.Add(new 
        { 
            test = "Cities",
            passed = country.Cities?.Any() == true,
            details = new 
            { 
                count = country.Cities?.Count ?? 0,
                activeCities = country.Cities?.Count(c => c.IsActive) ?? 0
            }
        });

        // Test 4: Payment configuration
        var prefix = $"{code.ToUpper()}:Payment:";
        var paymentConfigs = await db.AppConfigs
            .Where(a => a.ConfigKey.StartsWith(prefix))
            .Select(a => a.ConfigKey.Substring(prefix.Length))
            .ToListAsync();
        
        tests.Add(new 
        { 
            test = "Payment Configuration",
            passed = paymentConfigs.Any(),
            details = new { configKeys = paymentConfigs }
        });

        // Test 5: Vehicles
        var vehicleCount = await db.Vehicles
            .Include(v => v.City)
            .CountAsync(v => v.City != null && v.City.CountryId == country.Id);
        
        tests.Add(new 
        { 
            test = "Vehicles",
            passed = vehicleCount > 0,
            details = new { count = vehicleCount },
            optional = true
        });

        var passedTests = tests.Count(t => 
        {
            var passed = t.GetType().GetProperty("passed")?.GetValue(t);
            var optional = t.GetType().GetProperty("optional")?.GetValue(t);
            return passed is bool p && p || (optional is bool o && o);
        });

        return Results.Ok(new 
        { 
            country = country.Name,
            countryCode = country.Code,
            overallStatus = passedTests == tests.Count ? "PASSED" : "NEEDS ATTENTION",
            testsRun = tests.Count,
            testsPassed = passedTests,
            tests = tests,
            isReady = tests.Where(t => 
            {
                var optional = t.GetType().GetProperty("optional")?.GetValue(t);
                return optional is not bool || !(bool)optional;
            }).All(t => 
            {
                var passed = t.GetType().GetProperty("passed")?.GetValue(t);
                return passed is bool p && p;
            })
        });
    }

    private static async Task<IResult> GetCountryStatsAsync(
        AppDbContext db,
        string code)
    {
        var country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (country == null)
        {
            return Results.NotFound(new { error = "Country not found" });
        }

        // Get cities count
        var citiesCount = await db.Cities
            .CountAsync(c => c.CountryId == country.Id);

        var activeCitiesCount = await db.Cities
            .CountAsync(c => c.CountryId == country.Id && c.IsActive);

        // Get vehicles count
        var vehiclesCount = await db.Vehicles
            .Include(v => v.City)
            .CountAsync(v => v.City != null && v.City.CountryId == country.Id);

        var activeVehiclesCount = await db.Vehicles
            .Include(v => v.City)
            .CountAsync(v => v.City != null && 
                           v.City.CountryId == country.Id && 
                           v.Status == "active");

        // Get bookings count
        var bookingsCount = await db.Bookings
            .Include(b => b.Vehicle)
            .ThenInclude(v => v.City)
            .CountAsync(b => b.Vehicle != null &&
                           b.Vehicle.City != null &&
                           b.Vehicle.City.CountryId == country.Id);

        // Get owners count (owners with vehicles in this country)
        var ownersCount = await db.Vehicles
            .Include(v => v.City)
            .Where(v => v.City != null && v.City.CountryId == country.Id)
            .Select(v => v.OwnerId)
            .Distinct()
            .CountAsync();

        return Results.Ok(new 
        { 
            country = country.Name,
            countryCode = country.Code,
            isActive = country.IsActive,
            stats = new
            {
                cities = new
                {
                    total = citiesCount,
                    active = activeCitiesCount
                },
                vehicles = new
                {
                    total = vehiclesCount,
                    active = activeVehiclesCount
                },
                bookings = new
                {
                    total = bookingsCount
                },
                owners = new
                {
                    total = ownersCount
                }
            },
            createdAt = country.CreatedAt,
            updatedAt = country.UpdatedAt
        });
    }
}

// DTOs
public record CountryCreateDto(
    string Code,
    string Name,
    string CurrencyCode,
    string? CurrencySymbol,
    string? PhoneCode,
    string? Timezone,
    string? DefaultLanguage,
    bool? IsActive,
    List<string>? PaymentProviders,
    Dictionary<string, object>? Config
);

public record CountryUpdateDto(
    string? Name,
    string? CurrencyCode,
    string? CurrencySymbol,
    string? PhoneCode,
    string? Timezone,
    string? DefaultLanguage,
    bool? IsActive,
    List<string>? PaymentProviders,
    Dictionary<string, object>? Config
);
