namespace GhanaHybridRentalApi.Extensions;

/// <summary>
/// Extension methods for country-aware endpoint routing
/// </summary>
public static class CountryRoutingExtensions
{
    /// <summary>
    /// Maps endpoints with both country-specific and default routes
    /// Example: /api/v1/bookings AND /api/v1/{country}/bookings
    /// </summary>
    public static RouteHandlerBuilder MapGetWithCountry(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
    {
        // Map both routes: with and without country prefix
        // The middleware will set the default country (GH) if not specified
        
        // Default route (backward compatible): /api/v1/...
        var defaultRoute = endpoints.MapGet(pattern, handler);
        
        // Country-specific route: /api/v1/{country:alpha:length(2)}/...
        var countryPattern = pattern.Replace("/api/v1/", "/api/v1/{country:alpha:length(2)}/");
        endpoints.MapGet(countryPattern, handler);
        
        return defaultRoute;
    }

    /// <summary>
    /// Maps POST endpoints with both country-specific and default routes
    /// </summary>
    public static RouteHandlerBuilder MapPostWithCountry(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
    {
        var defaultRoute = endpoints.MapPost(pattern, handler);
        var countryPattern = pattern.Replace("/api/v1/", "/api/v1/{country:alpha:length(2)}/");
        endpoints.MapPost(countryPattern, handler);
        return defaultRoute;
    }

    /// <summary>
    /// Maps PUT endpoints with both country-specific and default routes
    /// </summary>
    public static RouteHandlerBuilder MapPutWithCountry(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
    {
        var defaultRoute = endpoints.MapPut(pattern, handler);
        var countryPattern = pattern.Replace("/api/v1/", "/api/v1/{country:alpha:length(2)}/");
        endpoints.MapPut(countryPattern, handler);
        return defaultRoute;
    }

    /// <summary>
    /// Maps DELETE endpoints with both country-specific and default routes
    /// </summary>
    public static RouteHandlerBuilder MapDeleteWithCountry(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
    {
        var defaultRoute = endpoints.MapDelete(pattern, handler);
        var countryPattern = pattern.Replace("/api/v1/", "/api/v1/{country:alpha:length(2)}/");
        endpoints.MapDelete(countryPattern, handler);
        return defaultRoute;
    }

    /// <summary>
    /// Creates a route group with country awareness
    /// </summary>
    public static RouteGroupBuilder MapGroupWithCountry(
        this IEndpointRouteBuilder endpoints,
        string prefix)
    {
        // Return the default group - country handling is done by middleware
        return endpoints.MapGroup(prefix);
    }
}
