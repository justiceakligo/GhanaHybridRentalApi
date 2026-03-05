using System.Security.Claims;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class CityEndpoints
{
    public static void MapCityEndpoints(this IEndpointRouteBuilder app)
    {
        // Public endpoint to get cities (filtered by country)
        app.MapGet("/api/v1/cities", GetCitiesAsync)
            .AllowAnonymous();

        // Admin endpoints for city management
        var adminGroup = app.MapGroup("/api/v1/admin/cities")
            .RequireAuthorization("AdminOnly");

        adminGroup.MapGet("/", GetAllCitiesAsync);
        adminGroup.MapGet("/{id:guid}", GetCityByIdAsync);
        adminGroup.MapPost("/", CreateCityAsync);
        adminGroup.MapPut("/{id:guid}", UpdateCityAsync);
        adminGroup.MapDelete("/{id:guid}", DeleteCityAsync);
    }

    private static async Task<IResult> GetCitiesAsync(
        AppDbContext db,
        ICountryContext countryContext,
        [FromQuery] bool? activeOnly = true)
    {
        var countryCode = countryContext.CountryCode;

        var query = db.Cities
            .Include(c => c.Country)
            .Where(c => c.Country != null && c.Country.Code == countryCode)
            .AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(c => c.IsActive);
        }

        var cities = await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Region,
                c.IsActive,
                c.DefaultDeliveryFee,
                CountryCode = c.Country!.Code,
                CountryName = c.Country.Name
            })
            .ToListAsync();

        return Results.Ok(cities);
    }

    private static async Task<IResult> GetAllCitiesAsync(
        AppDbContext db,
        [FromQuery] string? countryCode = null,
        [FromQuery] bool? activeOnly = null)
    {
        var query = db.Cities
            .Include(c => c.Country)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            query = query.Where(c => c.Country != null && c.Country.Code == countryCode.ToUpper());
        }

        if (activeOnly == true)
        {
            query = query.Where(c => c.IsActive);
        }

        var cities = await query
            .OrderBy(c => c.Country!.Name)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Region,
                c.CountryCode,
                c.CountryId,
                CountryName = c.Country != null ? c.Country.Name : "Unknown",
                c.IsActive,
                c.DisplayOrder,
                c.DefaultDeliveryFee,
                c.CreatedAt,
                c.UpdatedAt
            })
            .ToListAsync();

        return Results.Ok(cities);
    }

    private static async Task<IResult> GetCityByIdAsync(
        AppDbContext db,
        Guid id)
    {
        var city = await db.Cities
            .Include(c => c.Country)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (city == null)
        {
            return Results.NotFound(new { error = "City not found" });
        }

        return Results.Ok(city);
    }

    private static async Task<IResult> CreateCityAsync(
        AppDbContext db,
        [FromBody] CityCreateDto dto)
    {
        // Validate country exists
        var country = await db.Countries.FirstOrDefaultAsync(c => c.Code == dto.CountryCode.ToUpper());
        if (country == null)
        {
            return Results.BadRequest(new { error = "Country not found" });
        }

        var city = new City
        {
            Name = dto.Name,
            Region = dto.Region,
            CountryCode = country.Code,
            CountryId = country.Id,
            IsActive = dto.IsActive ?? true,
            DisplayOrder = dto.DisplayOrder ?? 0,
            DefaultDeliveryFee = dto.DefaultDeliveryFee,
            Description = dto.Description
        };

        db.Cities.Add(city);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/admin/cities/{city.Id}", city);
    }

    private static async Task<IResult> UpdateCityAsync(
        AppDbContext db,
        Guid id,
        [FromBody] CityUpdateDto dto)
    {
        var city = await db.Cities.FirstOrDefaultAsync(c => c.Id == id);

        if (city == null)
        {
            return Results.NotFound(new { error = "City not found" });
        }

        // Update fields if provided
        if (dto.Name != null) city.Name = dto.Name;
        if (dto.Region != null) city.Region = dto.Region;
        if (dto.IsActive.HasValue) city.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) city.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.DefaultDeliveryFee.HasValue) city.DefaultDeliveryFee = dto.DefaultDeliveryFee;
        if (dto.Description != null) city.Description = dto.Description;

        // Update country if provided
        if (dto.CountryCode != null)
        {
            var country = await db.Countries.FirstOrDefaultAsync(c => c.Code == dto.CountryCode.ToUpper());
            if (country == null)
            {
                return Results.BadRequest(new { error = "Country not found" });
            }
            city.CountryCode = country.Code;
            city.CountryId = country.Id;
        }

        city.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(city);
    }

    private static async Task<IResult> DeleteCityAsync(
        AppDbContext db,
        Guid id)
    {
        var city = await db.Cities.FirstOrDefaultAsync(c => c.Id == id);

        if (city == null)
        {
            return Results.NotFound(new { error = "City not found" });
        }

        // Check if city has vehicles
        var hasVehicles = await db.Vehicles.AnyAsync(v => v.CityId == id);
        if (hasVehicles)
        {
            return Results.BadRequest(new { error = "Cannot delete city with existing vehicles. Set IsActive to false instead." });
        }

        db.Cities.Remove(city);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "City deleted successfully" });
    }
}

// DTOs
public record CityCreateDto(
    string Name,
    string? Region,
    string CountryCode,
    bool? IsActive,
    int? DisplayOrder,
    decimal? DefaultDeliveryFee,
    string? Description
);

public record CityUpdateDto(
    string? Name,
    string? Region,
    string? CountryCode,
    bool? IsActive,
    int? DisplayOrder,
    decimal? DefaultDeliveryFee,
    string? Description
);
