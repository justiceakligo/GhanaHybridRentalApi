namespace GhanaHybridRentalApi.Dtos;

public record SendBulkMarketingRequest(List<Guid> userIds, string templateName, Dictionary<string, string>? customPlaceholders);
public record SendSingleMarketingRequest(Guid userId, string templateName, Dictionary<string, string>? customPlaceholders);
public record SendRoleMarketingRequest(string role, string templateName, Dictionary<string, string>? customPlaceholders);