using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;

namespace GhanaHybridRentalApi.Endpoints;

public static class PricingEndpoints
{
    public static void MapPricingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/pricing")
            .WithTags("Regional Pricing")
            .RequireAuthorization();

        // List all pricing rules
        group.MapGet("/", GetPricingRules)
            .WithName("GetPricingRules")
            .WithDescription("Get all regional pricing rules");

        // Get pricing rule by ID
        group.MapGet("/{id:guid}", GetPricingRule)
            .WithName("GetPricingRule")
            .WithDescription("Get a specific pricing rule");

        // Create pricing rule
        group.MapPost("/", CreatePricingRule)
            .WithName("CreatePricingRule")
            .WithDescription("Create a new regional pricing rule");

        // Update pricing rule
        group.MapPut("/{id:guid}", UpdatePricingRule)
            .WithName("UpdatePricingRule")
            .WithDescription("Update an existing pricing rule");

        // Delete pricing rule
        group.MapDelete("/{id:guid}", DeletePricingRule)
            .WithName("DeletePricingRule")
            .WithDescription("Delete a pricing rule");

        // Get applicable pricing for a region/category
        group.MapGet("/calculate", CalculatePricing)
            .WithName("CalculatePricing")
            .WithDescription("Calculate pricing for a specific region and category");
    }

    private static async Task<IResult> GetPricingRules(
        AppDbContext db,
        HttpContext context,
        string? region = null,
        Guid? categoryId = null,
        bool? isActive = null)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var query = db.RegionalPricings
            .Include(r => r.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(r => r.Region.ToLower() == region.ToLower());

        if (categoryId.HasValue)
            query = query.Where(r => r.CategoryId == categoryId.Value);

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        var rules = await query.OrderBy(r => r.Region).ThenBy(r => r.City).ToListAsync();

        return Results.Ok(rules);
    }

    private static async Task<IResult> GetPricingRule(
        Guid id,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var rule = await db.RegionalPricings
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
            return Results.NotFound(new { error = "Pricing rule not found" });

        return Results.Ok(rule);
    }

    private static async Task<IResult> CreatePricingRule(
        CreatePricingRuleDto dto,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        // Validate category if provided
        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await db.CarCategories.AnyAsync(c => c.Id == dto.CategoryId.Value);
            if (!categoryExists)
                return Results.BadRequest(new { error = "Invalid category ID" });
        }

        var rule = new RegionalPricing
        {
            Region = dto.Region,
            City = dto.City,
            CategoryId = dto.CategoryId,
            PriceMultiplier = dto.PriceMultiplier,
            ExtraHoldAmount = dto.ExtraHoldAmount,
            MinDailyRate = dto.MinDailyRate,
            MaxDailyRate = dto.MaxDailyRate,
            IsActive = dto.IsActive
        };

        db.RegionalPricings.Add(rule);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/admin/pricing/{rule.Id}", rule);
    }

    private static async Task<IResult> UpdatePricingRule(
        Guid id,
        UpdatePricingRuleDto dto,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var rule = await db.RegionalPricings.FirstOrDefaultAsync(r => r.Id == id);
        if (rule == null)
            return Results.NotFound(new { error = "Pricing rule not found" });

        // Validate category if provided
        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await db.CarCategories.AnyAsync(c => c.Id == dto.CategoryId.Value);
            if (!categoryExists)
                return Results.BadRequest(new { error = "Invalid category ID" });
        }

        rule.Region = dto.Region;
        rule.City = dto.City;
        rule.CategoryId = dto.CategoryId;
        rule.PriceMultiplier = dto.PriceMultiplier;
        rule.ExtraHoldAmount = dto.ExtraHoldAmount;
        rule.MinDailyRate = dto.MinDailyRate;
        rule.MaxDailyRate = dto.MaxDailyRate;
        rule.IsActive = dto.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(rule);
    }

    private static async Task<IResult> DeletePricingRule(
        Guid id,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var rule = await db.RegionalPricings.FirstOrDefaultAsync(r => r.Id == id);
        if (rule == null)
            return Results.NotFound(new { error = "Pricing rule not found" });

        db.RegionalPricings.Remove(rule);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Pricing rule deleted" });
    }

    private static async Task<IResult> CalculatePricing(
        AppDbContext db,
        string region,
        Guid categoryId,
        decimal baseRate)
    {
        // Find most specific pricing rule (with city if available, otherwise region-only)
        var rule = await db.RegionalPricings
            .Where(r => r.IsActive && r.Region.ToLower() == region.ToLower() && r.CategoryId == categoryId)
            .OrderByDescending(r => r.City != null ? 1 : 0) // Prefer city-specific rules
            .FirstOrDefaultAsync();

        // If no specific rule, try region-only without category
        if (rule == null)
        {
            rule = await db.RegionalPricings
                .Where(r => r.IsActive && r.Region.ToLower() == region.ToLower() && r.CategoryId == null)
                .FirstOrDefaultAsync();
        }

        var calculatedRate = baseRate;
        var extraHold = 0m;

        if (rule != null)
        {
            calculatedRate = baseRate * rule.PriceMultiplier;
            extraHold = rule.ExtraHoldAmount;

            // Apply min/max overrides if set
            if (rule.MinDailyRate.HasValue && calculatedRate < rule.MinDailyRate.Value)
                calculatedRate = rule.MinDailyRate.Value;

            if (rule.MaxDailyRate.HasValue && calculatedRate > rule.MaxDailyRate.Value)
                calculatedRate = rule.MaxDailyRate.Value;
        }

        return Results.Ok(new
        {
            baseRate,
            calculatedRate,
            extraHold,
            multiplier = rule?.PriceMultiplier ?? 1.0m,
            ruleApplied = rule != null,
            ruleId = rule?.Id
        });
    }
}

public record CreatePricingRuleDto(
    string Region,
    string? City,
    Guid? CategoryId,
    decimal PriceMultiplier,
    decimal ExtraHoldAmount,
    decimal? MinDailyRate,
    decimal? MaxDailyRate,
    bool IsActive
);

public record UpdatePricingRuleDto(
    string Region,
    string? City,
    Guid? CategoryId,
    decimal PriceMultiplier,
    decimal ExtraHoldAmount,
    decimal? MinDailyRate,
    decimal? MaxDailyRate,
    bool IsActive
);
