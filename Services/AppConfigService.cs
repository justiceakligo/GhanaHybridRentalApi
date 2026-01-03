using GhanaHybridRentalApi.Data;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Services;

/// <summary>
/// Service to read configuration from AppConfigs table with fallback to appsettings.json
/// </summary>
public interface IAppConfigService
{
    Task<string?> GetConfigValueAsync(string key);
    Task<T?> GetConfigValueAsync<T>(string key, T? defaultValue = default);
    Task<Dictionary<string, string>> GetConfigGroupAsync(string prefix);
}

public class AppConfigService : IAppConfigService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppConfigService> _logger;

    public AppConfigService(AppDbContext db, IConfiguration configuration, ILogger<AppConfigService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetConfigValueAsync(string key)
    {
        try
        {
            // First try database
            var config = await _db.AppConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConfigKey == key);

            if (config != null && !string.IsNullOrWhiteSpace(config.ConfigValue))
            {
                return config.ConfigValue;
            }

            // Fallback to appsettings.json
            var fallbackValue = _configuration[key];
            if (!string.IsNullOrWhiteSpace(fallbackValue))
            {
                _logger.LogDebug("Config key '{Key}' not found in database, using appsettings.json", key);
                return fallbackValue;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting config value for key: {Key}", key);
            // Fallback to appsettings if database fails
            return _configuration[key];
        }
    }

    public async Task<T?> GetConfigValueAsync<T>(string key, T? defaultValue = default)
    {
        var value = await GetConfigValueAsync(key);
        
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            _logger.LogWarning("Failed to convert config value '{Value}' to type {Type} for key {Key}", 
                value, typeof(T).Name, key);
            return defaultValue;
        }
    }

    public async Task<Dictionary<string, string>> GetConfigGroupAsync(string prefix)
    {
        try
        {
            var configs = await _db.AppConfigs
                .AsNoTracking()
                .Where(c => c.ConfigKey.StartsWith(prefix))
                .ToDictionaryAsync(c => c.ConfigKey, c => c.ConfigValue);

            // Add fallback values from appsettings.json
            var section = _configuration.GetSection(prefix.Replace(":", "__"));
            foreach (var pair in section.AsEnumerable(true))
            {
                var key = pair.Key.Replace("__", ":");
                if (!configs.ContainsKey(key) && !string.IsNullOrWhiteSpace(pair.Value))
                {
                    configs[key] = pair.Value;
                }
            }

            return configs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting config group for prefix: {Prefix}", prefix);
            return new Dictionary<string, string>();
        }
    }
}
