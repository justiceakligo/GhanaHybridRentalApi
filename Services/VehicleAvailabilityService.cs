using GhanaHybridRentalApi.Data;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Services;

public interface IVehicleAvailabilityService
{
    Task<bool> IsVehicleAvailableAsync(Guid vehicleId, DateTime startDate, DateTime endDate, Guid? excludeBookingId = null);
    Task<List<Guid>> GetAvailableVehicleIdsAsync(DateTime startDate, DateTime endDate, List<Guid>? vehicleIds = null);
}

public class VehicleAvailabilityService : IVehicleAvailabilityService
{
    private readonly AppDbContext _db;

    public VehicleAvailabilityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsVehicleAvailableAsync(Guid vehicleId, DateTime startDate, DateTime endDate, Guid? excludeBookingId = null)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle is null || vehicle.Status != "active")
            return false;

        // Respect vehicle-specific availability window when present
        if (vehicle.AvailableFrom.HasValue && startDate < vehicle.AvailableFrom.Value)
            return false;
        if (vehicle.AvailableUntil.HasValue && endDate > vehicle.AvailableUntil.Value)
            return false;

        // Add 4-hour buffer before pickup and after return for cleaning/inspection
        var startWithBuffer = startDate.AddHours(-4);
        var endWithBuffer = endDate.AddHours(4);

        var query = _db.Bookings
            .Where(b => b.VehicleId == vehicleId &&
                       b.Status != "cancelled" &&
                       b.Status != "completed" &&
                       b.PickupDateTime < endWithBuffer &&
                       b.ReturnDateTime > startWithBuffer);

        if (excludeBookingId.HasValue)
            query = query.Where(b => b.Id != excludeBookingId.Value);

        var hasConflict = await query.AnyAsync();
        return !hasConflict;
    }

    public async Task<List<Guid>> GetAvailableVehicleIdsAsync(DateTime startDate, DateTime endDate, List<Guid>? vehicleIds = null)
    {
        var query = _db.Vehicles
            .Where(v => v.Status == "active");

        if (vehicleIds is not null && vehicleIds.Any())
            query = query.Where(v => vehicleIds.Contains(v.Id));

        var allVehicles = await query.Select(v => v.Id).ToListAsync();

        // Add 4-hour buffer for cleaning/inspection between bookings
        var startWithBuffer = startDate.AddHours(-4);
        var endWithBuffer = endDate.AddHours(4);

        var conflictingBookings = await _db.Bookings
            .Where(b => b.Status != "cancelled" &&
                       b.Status != "completed" &&
                       b.PickupDateTime < endWithBuffer &&
                       b.ReturnDateTime > startWithBuffer)
            .Select(b => b.VehicleId)
            .Distinct()
            .ToListAsync();

        // Filter out vehicles whose availability windows do not contain the requested dates
        var availableByWindow = await _db.Vehicles
            .Where(v => v.Status == "active" &&
                   (v.AvailableFrom == null || v.AvailableFrom <= startDate) &&
                   (v.AvailableUntil == null || v.AvailableUntil >= endDate))
            .Select(v => v.Id)
            .ToListAsync();

        return allVehicles.Intersect(availableByWindow).Except(conflictingBookings).ToList();
    }
}
