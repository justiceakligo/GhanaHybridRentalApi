namespace GhanaHybridRentalApi.Middleware;

/// <summary>
/// Middleware to extract country code from route and set it in HttpContext
/// Supports both /api/v1/{country}/... and /api/v1/... (defaults to GH for backward compatibility)
/// </summary>
public class CountryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CountryMiddleware> _logger;
    private const string DefaultCountry = "GH";

    public CountryMiddleware(RequestDelegate next, ILogger<CountryMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        
        // Extract country code from route pattern: /api/v1/{country}/...
        // Example: /api/v1/ng/bookings -> country = "ng"
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        string countryCode = DefaultCountry;
        
        // Check if path starts with /api/v1/
        if (segments.Length >= 3 && 
            segments[0].Equals("api", StringComparison.OrdinalIgnoreCase) &&
            segments[1].Equals("v1", StringComparison.OrdinalIgnoreCase))
        {
            // Check if third segment is a 2-letter country code
            var potentialCountry = segments[2];
            if (potentialCountry.Length == 2 && IsAlpha(potentialCountry))
            {
                countryCode = potentialCountry.ToUpper();
                _logger.LogDebug("Country code extracted from route: {CountryCode}", countryCode);
            }
            else
            {
                // No country code in route, use default (Ghana)
                _logger.LogDebug("No country code in route, using default: {DefaultCountry}", DefaultCountry);
            }
        }
        
        // Store country code in HttpContext for access by services
        context.Items["CountryCode"] = countryCode;
        
        await _next(context);
    }

    private static bool IsAlpha(string str)
    {
        return str.All(char.IsLetter);
    }
}

/// <summary>
/// Extension method to add CountryMiddleware to the pipeline
/// </summary>
public static class CountryMiddlewareExtensions
{
    public static IApplicationBuilder UseCountryContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CountryMiddleware>();
    }
}
