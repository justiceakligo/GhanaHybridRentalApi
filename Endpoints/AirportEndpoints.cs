using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class AirportEndpoints
{
    public static void MapAirportEndpoints(this IEndpointRouteBuilder app)
    {
        // Public endpoints
        app.MapGet("/api/v1/airports", GetActiveAirportsAsync)
            .WithName("GetActiveAirports")
            .WithDescription("Get all active airports");

        app.MapGet("/api/v1/airports/{airportId:guid}", GetAirportByIdAsync)
            .WithName("GetAirportById")
            .WithDescription("Get airport details by ID");

        app.MapGet("/api/v1/cities/{cityId:guid}/airports", GetAirportsByCityAsync)
            .WithName("GetAirportsByCity")
            .WithDescription("Get all active airports in a specific city");

        // Admin endpoints
        app.MapGet("/api/v1/admin/airports", GetAllAirportsAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("AdminGetAllAirports")
            .WithDescription("Admin: Get all airports including inactive");

        app.MapPost("/api/v1/admin/airports", CreateAirportAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("CreateAirport")
            .WithDescription("Admin: Create a new airport");

        app.MapPut("/api/v1/admin/airports/{airportId:guid}", UpdateAirportAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("UpdateAirport")
            .WithDescription("Admin: Update airport details");

        app.MapDelete("/api/v1/admin/airports/{airportId:guid}", DeleteAirportAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("DeleteAirport")
            .WithDescription("Admin: Soft delete an airport");

        app.MapPost("/api/v1/admin/airports/{airportId:guid}/activate", ActivateAirportAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("ActivateAirport")
            .WithDescription("Admin: Activate an airport");

        app.MapPost("/api/v1/admin/airports/{airportId:guid}/deactivate", DeactivateAirportAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("DeactivateAirport")
            .WithDescription("Admin: Deactivate an airport");
    }

    // Public endpoints

    private static async Task<IResult> GetActiveAirportsAsync(AppDbContext db)
    {
        var airports = await db.Airports
            .Include(a => a.City)
            .Where(a => a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Code,
                city = new
                {
                    a.City!.Id,
                    a.City.Name,
                    a.City.Region
                },
                a.Address,
                a.Latitude,
                a.Longitude,
                a.PickupFee,
                a.DropoffFee,
                a.Instructions
            })
            .ToListAsync();

        return Results.Ok(airports);
    }

    private static async Task<IResult> GetAirportByIdAsync(Guid airportId, AppDbContext db)
    {
        var airport = await db.Airports
            .Include(a => a.City)
            .Where(a => a.Id == airportId && a.IsActive)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Code,
                city = new
                {
                    a.City!.Id,
                    a.City.Name,
                    a.City.Region
                },
                a.Address,
                a.Latitude,
                a.Longitude,
                a.PickupFee,
                a.DropoffFee,
                a.Instructions
            })
            .FirstOrDefaultAsync();

        if (airport == null)
            return Results.NotFound(new { error = "Airport not found" });

        return Results.Ok(airport);
    }

    private static async Task<IResult> GetAirportsByCityAsync(Guid cityId, AppDbContext db)
    {
        var airports = await db.Airports
            .Include(a => a.City)
            .Where(a => a.CityId == cityId && a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Code,
                a.Address,
                a.Latitude,
                a.Longitude,
                a.PickupFee,
                a.DropoffFee,
                a.Instructions
            })
            .ToListAsync();

        return Results.Ok(airports);
    }

    // Admin endpoints

    private static async Task<IResult> GetAllAirportsAsync(
        AppDbContext db,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.Airports
            .Include(a => a.City);

        var total = await query.CountAsync();
        var airports = await query
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Code,
                a.CityId,
                city = new
                {
                    a.City!.Id,
                    a.City.Name,
                    a.City.Region
                },
                a.Address,
                a.Latitude,
                a.Longitude,
                a.IsActive,
                a.PickupFee,
                a.DropoffFee,
                a.DisplayOrder,
                a.Instructions,
                a.CreatedAt,
                a.UpdatedAt
            })
            .ToListAsync();

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            data = airports
        });
    }

    private static async Task<IResult> CreateAirportAsync(
        [FromBody] CreateAirportRequest request,
        AppDbContext db)
    {
        // Validate city exists and is active
        var cityExists = await db.Cities.AnyAsync(c => c.Id == request.CityId && c.IsActive);
        if (!cityExists)
            return Results.BadRequest(new { error = "Invalid or inactive city" });

        // Check for duplicate airport code
        var codeExists = await db.Airports.AnyAsync(a => a.Code.ToLower() == request.Code.ToLower());
        if (codeExists)
            return Results.BadRequest(new { error = "Airport code already exists" });

        var airport = new Airport
        {
            Name = request.Name,
            Code = request.Code.ToUpper(),
            CityId = request.CityId,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PickupFee = request.PickupFee,
            DropoffFee = request.DropoffFee,
            Instructions = request.Instructions,
            DisplayOrder = request.DisplayOrder ?? 0,
            IsActive = true
        };

        db.Airports.Add(airport);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/airports/{airport.Id}", new
        {
            airport.Id,
            airport.Name,
            airport.Code,
            airport.CityId,
            airport.IsActive
        });
    }

    private static async Task<IResult> UpdateAirportAsync(
        Guid airportId,
        [FromBody] UpdateAirportRequest request,
        AppDbContext db)
    {
        var airport = await db.Airports.FirstOrDefaultAsync(a => a.Id == airportId);
        if (airport == null)
            return Results.NotFound(new { error = "Airport not found" });

        // Validate city if changed
        if (request.CityId.HasValue && request.CityId.Value != airport.CityId)
        {
            var cityExists = await db.Cities.AnyAsync(c => c.Id == request.CityId.Value && c.IsActive);
            if (!cityExists)
                return Results.BadRequest(new { error = "Invalid or inactive city" });

            airport.CityId = request.CityId.Value;
        }

        // Check for duplicate code if changed
        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code.ToUpper() != airport.Code)
        {
            var codeExists = await db.Airports.AnyAsync(a => a.Code.ToLower() == request.Code.ToLower());
            if (codeExists)
                return Results.BadRequest(new { error = "Airport code already exists" });

            airport.Code = request.Code.ToUpper();
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            airport.Name = request.Name;

        if (request.Address != null)
            airport.Address = request.Address;

        if (request.Latitude.HasValue)
            airport.Latitude = request.Latitude.Value;

        if (request.Longitude.HasValue)
            airport.Longitude = request.Longitude.Value;

        if (request.PickupFee.HasValue)
            airport.PickupFee = request.PickupFee.Value;

        if (request.DropoffFee.HasValue)
            airport.DropoffFee = request.DropoffFee.Value;

        if (request.DisplayOrder.HasValue)
            airport.DisplayOrder = request.DisplayOrder.Value;

        if (request.Instructions != null)
            airport.Instructions = request.Instructions;

        airport.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Airport updated successfully", airportId = airport.Id });
    }

    private static async Task<IResult> DeleteAirportAsync(Guid airportId, AppDbContext db)
    {
        var airport = await db.Airports.FirstOrDefaultAsync(a => a.Id == airportId);
        if (airport == null)
            return Results.NotFound(new { error = "Airport not found" });

        // Soft delete
        airport.IsActive = false;
        airport.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Airport deactivated successfully" });
    }

    private static async Task<IResult> ActivateAirportAsync(Guid airportId, AppDbContext db)
    {
        var airport = await db.Airports.FirstOrDefaultAsync(a => a.Id == airportId);
        if (airport == null)
            return Results.NotFound(new { error = "Airport not found" });

        airport.IsActive = true;
        airport.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Airport activated successfully" });
    }

    private static async Task<IResult> DeactivateAirportAsync(Guid airportId, AppDbContext db)
    {
        var airport = await db.Airports.FirstOrDefaultAsync(a => a.Id == airportId);
        if (airport == null)
            return Results.NotFound(new { error = "Airport not found" });

        airport.IsActive = false;
        airport.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Airport deactivated successfully" });
    }

    // DTOs

    public record CreateAirportRequest(
        string Name,
        string Code,
        Guid CityId,
        string? Address,
        decimal? Latitude,
        decimal? Longitude,
        decimal? PickupFee,
        decimal? DropoffFee,
        string? Instructions,
        int? DisplayOrder
    );

    public record UpdateAirportRequest(
        string? Name,
        string? Code,
        Guid? CityId,
        string? Address,
        decimal? Latitude,
        decimal? Longitude,
        decimal? PickupFee,
        decimal? DropoffFee,
        string? Instructions,
        int? DisplayOrder
    );
}
