using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class ProtectionEndpoints
{
    public static void MapProtectionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/protection/plans", GetPlansAsync);

        app.MapPost("/api/v1/admin/protection/plans", UpsertPlanAsync)
            .RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> GetPlansAsync(AppDbContext db)
    {
        var plans = await db.ProtectionPlans.Where(p => p.IsActive).ToListAsync();
        return Results.Ok(plans);
    }

    private static async Task<IResult> UpsertPlanAsync(
        [FromBody] ProtectionPlan plan,
        AppDbContext db)
    {
        if (plan.Id == Guid.Empty)
        {
            plan.Id = Guid.NewGuid();
            db.ProtectionPlans.Add(plan);
        }
        else
        {
            var existing = await db.ProtectionPlans.FirstOrDefaultAsync(p => p.Id == plan.Id);
            if (existing is null)
            {
                db.ProtectionPlans.Add(plan);
            }
            else
            {
                existing.Code = plan.Code;
                existing.Name = plan.Name;
                existing.Description = plan.Description;
                existing.PricingMode = plan.PricingMode;
                existing.DailyPrice = plan.DailyPrice;
                existing.FixedPrice = plan.FixedPrice;
                existing.MinFee = plan.MinFee;
                existing.MaxFee = plan.MaxFee;
                existing.Currency = plan.Currency;
                existing.IncludesMinorDamageWaiver = plan.IncludesMinorDamageWaiver;
                existing.MinorWaiverCap = plan.MinorWaiverCap;
                existing.Deductible = plan.Deductible;
                existing.ExcludesJson = plan.ExcludesJson;
                existing.IsMandatory = plan.IsMandatory;
                existing.IsDefault = plan.IsDefault;
                existing.IsActive = plan.IsActive;
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(plan);
    }
}
