using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class InsuranceEndpoints
{
    public static void MapInsuranceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/insurance/plans", GetPlansAsync);

        app.MapPost("/api/v1/admin/insurance/plans", UpsertPlanAsync)
            .RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> GetPlansAsync(AppDbContext db)
    {
        var plans = await db.InsurancePlans
            .Where(p => p.Active)
            .ToListAsync();

        return Results.Ok(plans);
    }

    private static async Task<IResult> UpsertPlanAsync(
        [FromBody] InsurancePlan plan,
        AppDbContext db)
    {
        if (plan.Id == Guid.Empty)
        {
            plan.Id = Guid.NewGuid();
            db.InsurancePlans.Add(plan);
        }
        else
        {
            var existing = await db.InsurancePlans.FirstOrDefaultAsync(p => p.Id == plan.Id);
            if (existing is null)
            {
                db.InsurancePlans.Add(plan);
            }
            else
            {
                existing.Name = plan.Name;
                existing.Description = plan.Description;
                existing.DailyPrice = plan.DailyPrice;
                existing.CoverageSummary = plan.CoverageSummary;
                existing.IsMandatory = plan.IsMandatory;
                existing.IsDefault = plan.IsDefault;
                existing.Active = plan.Active;
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(plan);
    }
}
