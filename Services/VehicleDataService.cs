using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhanaHybridRentalApi.Services;

public interface IVehicleDataService
{
    Task<VehicleLookupResult?> LookupVehicleDataAsync(int year, string make, string model, string? trim = null);
}

public class VehicleLookupResult
{
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? Trim { get; set; }
    
    // Specifications
    public string? EngineSize { get; set; }
    public string? FuelType { get; set; }
    public string? TransmissionType { get; set; }
    public string? Drivetrain { get; set; }
    public string? BodyStyle { get; set; }
    public int? SeatingCapacity { get; set; }
    public string? FuelEfficiency { get; set; }
    
    // Auto-detected features based on year/trim
    public List<string> Features { get; set; } = new();
    
    // Additional specifications as JSON
    public Dictionary<string, string> AdditionalSpecs { get; set; } = new();
}

public class VehicleDataService : IVehicleDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VehicleDataService> _logger;
    private const string NhtsaApiBaseUrl = "https://vpic.nhtsa.dot.gov/api/vehicles";

    public VehicleDataService(HttpClient httpClient, ILogger<VehicleDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<VehicleLookupResult?> LookupVehicleDataAsync(int year, string make, string model, string? trim = null)
    {
        try
        {
            // NHTSA vPIC API: Decode VIN by Make/Model/Year
            // We'll use GetModelsForMakeYear and GetVehicleVariableValuesList
            
            var result = new VehicleLookupResult
            {
                Year = year,
                Make = make,
                Model = model,
                Trim = trim
            };

            // Build query string for NHTSA API
            var queryParams = $"modelyear={year}&make={Uri.EscapeDataString(make)}&model={Uri.EscapeDataString(model)}";
            if (!string.IsNullOrWhiteSpace(trim))
            {
                queryParams += $"&trim={Uri.EscapeDataString(trim)}";
            }

            var url = $"{NhtsaApiBaseUrl}/GetModelsForMakeYear/make/{Uri.EscapeDataString(make)}/modelyear/{year}?format=json";
            
            _logger.LogInformation("Fetching vehicle data from NHTSA: {Url}", url);
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NHTSA API returned {StatusCode} for {Make} {Model} {Year}", 
                    response.StatusCode, make, model, year);
                return CreateFallbackResult(year, make, model, trim);
            }

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<NhtsaModelsResponse>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (apiResponse?.Results == null || apiResponse.Results.Count == 0)
            {
                _logger.LogInformation("No results from NHTSA for {Make} {Model} {Year}", make, model, year);
                return CreateFallbackResult(year, make, model, trim);
            }

            // Find matching model
            var matchingModel = apiResponse.Results
                .FirstOrDefault(r => r.ModelName?.Contains(model, StringComparison.OrdinalIgnoreCase) == true);

            if (matchingModel != null)
            {
                result.Make = matchingModel.MakeName ?? make;
                result.Model = matchingModel.ModelName ?? model;
            }

            // Fetch detailed specs using GetVehicleVariableValuesList
            await EnrichWithDetailedSpecs(result);

            // Infer features based on year
            result.Features = InferFeaturesFromYear(year, trim);

            // Estimate fuel efficiency based on body style and engine
            result.FuelEfficiency = EstimateFuelEfficiency(result.BodyStyle, result.EngineSize);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vehicle data for {Year} {Make} {Model}", year, make, model);
            return CreateFallbackResult(year, make, model, trim);
        }
    }

    private async Task EnrichWithDetailedSpecs(VehicleLookupResult result)
    {
        try
        {
            // Use GetVehicleVariableValuesList to get detailed specs
            var url = $"{NhtsaApiBaseUrl}/GetVehicleVariableValuesList/make?format=json";
            
            // For now, use common defaults based on year and make
            // NHTSA API has limitations for non-VIN lookups
            result.EngineSize = InferEngineSize(result.Make, result.Model);
            result.FuelType = InferFuelType(result.Make, result.Model, result.Year);
            result.TransmissionType = InferTransmissionType(result.Year);
            result.Drivetrain = "FWD"; // Default, can be enhanced
            result.BodyStyle = InferBodyStyle(result.Model);
            result.SeatingCapacity = InferSeatingCapacity(result.BodyStyle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching vehicle specs");
        }
    }

    private VehicleLookupResult CreateFallbackResult(int year, string make, string model, string? trim)
    {
        // Create sensible defaults when API fails
        var result = new VehicleLookupResult
        {
            Year = year,
            Make = make,
            Model = model,
            Trim = trim,
            FuelType = "Petrol",
            TransmissionType = year >= 2015 ? "Automatic" : "Manual",
            SeatingCapacity = 5,
            Features = InferFeaturesFromYear(year, trim)
        };

        result.EngineSize = InferEngineSize(make, model);
        result.BodyStyle = InferBodyStyle(model);
        result.FuelEfficiency = EstimateFuelEfficiency(result.BodyStyle, result.EngineSize);

        return result;
    }

    private List<string> InferFeaturesFromYear(int year, string? trim)
    {
        var features = new List<string>();

        // Standard features based on year
        if (year >= 2000)
        {
            features.Add("Power Steering");
            features.Add("Power Windows");
        }

        if (year >= 2005)
        {
            features.Add("Air Conditioning");
            features.Add("CD Player/Radio");
        }

        if (year >= 2010)
        {
            features.Add("Power Locks");
            features.Add("ABS Brakes");
        }

        if (year >= 2015)
        {
            features.Add("Bluetooth Audio");
            features.Add("USB Charging Port");
            features.Add("Aux Input");
        }

        if (year >= 2018)
        {
            features.Add("Backup Camera");
            features.Add("Keyless Entry");
        }

        if (year >= 2020)
        {
            features.Add("Apple CarPlay/Android Auto");
            features.Add("Lane Departure Warning");
        }

        // Premium trim features
        if (!string.IsNullOrWhiteSpace(trim))
        {
            var trimLower = trim.ToLower();
            if (trimLower.Contains("sport") || trimLower.Contains("premium") || trimLower.Contains("limited"))
            {
                features.Add("Leather Seats");
                features.Add("Sunroof/Moonroof");
                features.Add("Cruise Control");
            }
        }

        return features;
    }

    private string InferEngineSize(string make, string model)
    {
        var modelLower = model.ToLower();
        
        // Compact/Economy cars
        if (modelLower.Contains("yaris") || modelLower.Contains("corolla") || 
            modelLower.Contains("civic") || modelLower.Contains("sentra"))
        {
            return "1.5L - 1.8L";
        }

        // Small SUVs
        if (modelLower.Contains("rav4") || modelLower.Contains("crv") || 
            modelLower.Contains("tucson") || modelLower.Contains("escape"))
        {
            return "2.0L - 2.5L";
        }

        // Midsize sedans
        if (modelLower.Contains("camry") || modelLower.Contains("accord") || 
            modelLower.Contains("altima") || modelLower.Contains("malibu"))
        {
            return "2.0L - 2.5L";
        }

        // Large SUVs/Trucks
        if (modelLower.Contains("4runner") || modelLower.Contains("pilot") || 
            modelLower.Contains("tahoe") || modelLower.Contains("f-150"))
        {
            return "3.5L - 5.0L";
        }

        // Default for unknown
        return "1.5L - 2.0L";
    }

    private string InferFuelType(string make, string model, int year)
    {
        var modelLower = model.ToLower();

        if (modelLower.Contains("prius") || modelLower.Contains("hybrid"))
            return "Hybrid";

        if (modelLower.Contains("tesla") || modelLower.Contains("leaf") || 
            modelLower.Contains("bolt") || (year >= 2020 && modelLower.Contains("ev")))
            return "Electric";

        if (modelLower.Contains("diesel") || modelLower.Contains("tdi"))
            return "Diesel";

        return "Petrol";
    }

    private string InferTransmissionType(int year)
    {
        // Most modern cars (2015+) have automatic transmissions
        if (year >= 2018)
            return "Automatic (CVT)";
        
        if (year >= 2010)
            return "Automatic";

        return "Manual";
    }

    private string InferBodyStyle(string model)
    {
        var modelLower = model.ToLower();

        if (modelLower.Contains("suv") || modelLower.Contains("rav4") || 
            modelLower.Contains("crv") || modelLower.Contains("tucson") ||
            modelLower.Contains("escape") || modelLower.Contains("explorer"))
            return "SUV";

        if (modelLower.Contains("truck") || modelLower.Contains("f-150") || 
            modelLower.Contains("silverado") || modelLower.Contains("tacoma"))
            return "Pickup Truck";

        if (modelLower.Contains("van") || modelLower.Contains("odyssey") || 
            modelLower.Contains("caravan"))
            return "Minivan";

        if (modelLower.Contains("coupe") || modelLower.Contains("mustang") ||
            modelLower.Contains("camaro"))
            return "Coupe";

        // Default to sedan for most cars
        return "Sedan";
    }

    private int InferSeatingCapacity(string? bodyStyle)
    {
        return bodyStyle?.ToLower() switch
        {
            "minivan" => 7,
            "suv" when bodyStyle.Contains("large", StringComparison.OrdinalIgnoreCase) => 7,
            "suv" => 5,
            "pickup truck" => 5,
            "coupe" => 4,
            _ => 5 // Default sedan
        };
    }

    private string EstimateFuelEfficiency(string? bodyStyle, string? engineSize)
    {
        // Rough estimates based on body style and engine size
        if (bodyStyle?.ToLower().Contains("suv") == true || bodyStyle?.ToLower().Contains("truck") == true)
        {
            return "10-14 km/L";
        }

        if (engineSize?.Contains("1.") == true) // 1.0L - 1.9L
        {
            return "15-20 km/L";
        }

        if (engineSize?.Contains("2.") == true) // 2.0L - 2.9L
        {
            return "12-17 km/L";
        }

        if (engineSize?.Contains("3.") == true || engineSize?.Contains("4.") == true)
        {
            return "10-14 km/L";
        }

        return "13-18 km/L"; // Default estimate
    }
}

// NHTSA API Response Models
public class NhtsaModelsResponse
{
    [JsonPropertyName("Count")]
    public int Count { get; set; }

    [JsonPropertyName("Message")]
    public string? Message { get; set; }

    [JsonPropertyName("Results")]
    public List<NhtsaModelResult> Results { get; set; } = new();
}

public class NhtsaModelResult
{
    [JsonPropertyName("Make_ID")]
    public int MakeId { get; set; }

    [JsonPropertyName("Make_Name")]
    public string? MakeName { get; set; }

    [JsonPropertyName("Model_ID")]
    public int ModelId { get; set; }

    [JsonPropertyName("Model_Name")]
    public string? ModelName { get; set; }
}
