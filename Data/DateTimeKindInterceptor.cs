using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Data;

public class DateTimeKindInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        NormalizeDateTimes(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void NormalizeDateTimes(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            NormalizeEntity(entry);
        }
    }

    private static void NormalizeEntity(EntityEntry entry)
    {
        var properties = entry.Properties.Where(p => p.Metadata.ClrType == typeof(DateTime) || p.Metadata.ClrType == typeof(DateTime?));

        foreach (var prop in properties)
        {
            var value = prop.CurrentValue;
            if (value == null) continue;

            var dt = (DateTime)value;
            switch (dt.Kind)
            {
                case DateTimeKind.Utc:
                    break;
                case DateTimeKind.Local:
                    prop.CurrentValue = dt.ToUniversalTime();
                    break;
                case DateTimeKind.Unspecified:
                    prop.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    break;
            }
        }
    }
}
