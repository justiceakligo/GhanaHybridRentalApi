using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;

namespace GhanaHybridRentalApi.Endpoints;

public static class RefundPolicyEndpoints
{
    public static void MapRefundPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/refund-policies")
            .WithTags("Refund Policies")
            .RequireAuthorization();

        // List all refund policies
        group.MapGet("/", GetRefundPolicies)
            .WithName("GetRefundPolicies")
            .WithDescription("Get all refund policies");

        // Get policy by ID
        group.MapGet("/{id:guid}", GetRefundPolicy)
            .WithName("GetRefundPolicy")
            .WithDescription("Get a specific refund policy");

        // Create policy
        group.MapPost("/", CreateRefundPolicy)
            .WithName("CreateRefundPolicy")
            .WithDescription("Create a new refund policy");

        // Update policy
        group.MapPut("/{id:guid}", UpdateRefundPolicy)
            .WithName("UpdateRefundPolicy")
            .WithDescription("Update an existing refund policy");

        // Delete policy
        group.MapDelete("/{id:guid}", DeleteRefundPolicy)
            .WithName("DeleteRefundPolicy")
            .WithDescription("Delete a refund policy");

        // Calculate refund for a booking
        group.MapGet("/calculate", CalculateRefund)
            .WithName("CalculateRefund")
            .WithDescription("Calculate refund amount for a booking cancellation");
    }

    private static async Task<IResult> GetRefundPolicies(
        AppDbContext db,
        HttpContext context,
        Guid? categoryId = null,
        bool? isActive = null)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var query = db.RefundPolicies
            .Include(p => p.Category)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value || p.CategoryId == null);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var policies = await query
            .OrderBy(p => p.Priority)
            .ThenByDescending(p => p.HoursBeforePickup)
            .ToListAsync();

        return Results.Ok(policies);
    }

    private static async Task<IResult> GetRefundPolicy(
        Guid id,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var policy = await db.RefundPolicies
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (policy == null)
            return Results.NotFound(new { error = "Refund policy not found" });

        return Results.Ok(policy);
    }

    private static async Task<IResult> CreateRefundPolicy(
        CreateRefundPolicyDto dto,
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

        var policy = new RefundPolicy
        {
            PolicyName = dto.PolicyName,
            Description = dto.Description,
            HoursBeforePickup = dto.HoursBeforePickup,
            RefundPercentage = dto.RefundPercentage,
            RefundDeposit = dto.RefundDeposit,
            CategoryId = dto.CategoryId,
            Priority = dto.Priority,
            IsActive = dto.IsActive
        };

        db.RefundPolicies.Add(policy);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/admin/refund-policies/{policy.Id}", policy);
    }

    private static async Task<IResult> UpdateRefundPolicy(
        Guid id,
        UpdateRefundPolicyDto dto,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var policy = await db.RefundPolicies.FirstOrDefaultAsync(p => p.Id == id);
        if (policy == null)
            return Results.NotFound(new { error = "Refund policy not found" });

        // Validate category if provided
        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await db.CarCategories.AnyAsync(c => c.Id == dto.CategoryId.Value);
            if (!categoryExists)
                return Results.BadRequest(new { error = "Invalid category ID" });
        }

        policy.PolicyName = dto.PolicyName;
        policy.Description = dto.Description;
        policy.HoursBeforePickup = dto.HoursBeforePickup;
        policy.RefundPercentage = dto.RefundPercentage;
        policy.RefundDeposit = dto.RefundDeposit;
        policy.CategoryId = dto.CategoryId;
        policy.Priority = dto.Priority;
        policy.IsActive = dto.IsActive;
        policy.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(policy);
    }

    private static async Task<IResult> DeleteRefundPolicy(
        Guid id,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var policy = await db.RefundPolicies.FirstOrDefaultAsync(p => p.Id == id);
        if (policy == null)
            return Results.NotFound(new { error = "Refund policy not found" });

        db.RefundPolicies.Remove(policy);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Refund policy deleted" });
    }

    private static async Task<IResult> CalculateRefund(
        AppDbContext db,
        DateTime pickupTime,
        Guid? categoryId,
        decimal totalAmount,
        decimal depositAmount)
    {
        var hoursUntilPickup = (pickupTime - DateTime.UtcNow).TotalHours;

        // Find applicable policy
        var query = db.RefundPolicies
            .Where(p => p.IsActive && p.HoursBeforePickup <= hoursUntilPickup);

        if (categoryId.HasValue)
        {
            // Prefer category-specific policy, fallback to general
            query = query.Where(p => p.CategoryId == categoryId.Value || p.CategoryId == null);
        }
        else
        {
            query = query.Where(p => p.CategoryId == null);
        }

        var policy = await query
            .OrderBy(p => p.Priority)
            .ThenByDescending(p => p.HoursBeforePickup)
            .FirstOrDefaultAsync();

        if (policy == null)
        {
            return Results.Ok(new
            {
                refundAmount = 0m,
                depositRefund = 0m,
                totalRefund = 0m,
                refundPercentage = 0m,
                policyApplied = false,
                reason = "No refund policy applies to this cancellation time"
            });
        }

        var refundAmount = totalAmount * (policy.RefundPercentage / 100m);
        var depositRefund = policy.RefundDeposit ? depositAmount : 0m;
        var totalRefund = refundAmount + depositRefund;

        return Results.Ok(new
        {
            refundAmount,
            depositRefund,
            totalRefund,
            refundPercentage = policy.RefundPercentage,
            policyApplied = true,
            policyId = policy.Id,
            policyName = policy.PolicyName,
            hoursUntilPickup = Math.Round(hoursUntilPickup, 2)
        });
    }
}

public record CreateRefundPolicyDto(
    string PolicyName,
    string? Description,
    int HoursBeforePickup,
    decimal RefundPercentage,
    bool RefundDeposit,
    Guid? CategoryId,
    int Priority,
    bool IsActive
);

public record UpdateRefundPolicyDto(
    string PolicyName,
    string? Description,
    int HoursBeforePickup,
    decimal RefundPercentage,
    bool RefundDeposit,
    Guid? CategoryId,
    int Priority,
    bool IsActive
);
