using Stripe;
using GhanaHybridRentalApi.Dtos;

namespace GhanaHybridRentalApi.Services;

public record StripePaymentIntentResult(string Id, string ClientSecret);

public interface IStripePaymentService
{
    Task<StripePaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, string customerId, Dictionary<string, string> metadata);
    Task<PaymentVerificationResult> ConfirmPaymentAsync(string paymentIntentId);
        Task<StripeCaptureResult> CapturePaymentAsync(string paymentIntentId, decimal amount, string currency);
        Task<string> CreateCustomerAsync(string email, string name, string phone);
        Task<bool> RefundPaymentAsync(string paymentIntentId, decimal? amount = null);
    }

    public record StripeCaptureResult(bool Success, string? Message);

    public class StripePaymentService : IStripePaymentService
    {
        private readonly IAppConfigService _configService;
        private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentService> _logger;
    private bool _initialized = false;

    public StripePaymentService(IAppConfigService configService, IConfiguration configuration, ILogger<StripePaymentService> logger)
    {
        _configService = configService;
        _configuration = configuration;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        // Get API key from AppConfig or fallback to appsettings
        var apiKey = await _configService.GetConfigValueAsync("Payment:Stripe:SecretKey");
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = _configuration["Stripe:SecretKey"];
        }

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            StripeConfiguration.ApiKey = apiKey;
            _initialized = true;
            _logger.LogInformation("Stripe API initialized");
        }
        else
        {
            _logger.LogWarning("Stripe API key not configured");
            throw new PaymentProviderNotConfiguredException("stripe", "Stripe API key not configured");
        }
    }

    public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, string customerId, Dictionary<string, string> metadata)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var stripeCurrency = currency.ToLower();
            var stripeAmount = amount;
            
            // Handle GHS to USD conversion for Stripe (which doesn't support GHS widely)
            if (stripeCurrency == "ghs")
            {
                // Get exchange rate from config (default: 1 USD = 11 GHS)
                var exchangeRateStr = await _configService.GetConfigValueAsync("Payment:ExchangeRate:GHS_To_USD");
                var exchangeRate = 11.0m; // Default: 1 USD = 11 GHS
                
                if (!string.IsNullOrWhiteSpace(exchangeRateStr) && decimal.TryParse(exchangeRateStr, out var rate) && rate > 0)
                {
                    exchangeRate = rate;
                }
                
                // Convert GHS to USD
                stripeAmount = amount / exchangeRate;
                stripeCurrency = "usd";
                
                _logger.LogInformation("Converting {AmountGHS} GHS to {AmountUSD} USD using rate 1:{Rate}", 
                    amount, stripeAmount, exchangeRate);
                
                // Store original currency in metadata for reference
                metadata["original_currency"] = currency.ToUpper();
                metadata["original_amount"] = amount.ToString("F2");
                metadata["exchange_rate"] = exchangeRate.ToString("F2");
            }
            
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(stripeAmount * 100), // Convert to cents
                Currency = stripeCurrency,
                Customer = customerId,
                Metadata = metadata,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe PaymentIntent: {PaymentIntentId} with currency {Currency} amount {Amount}", 
                paymentIntent.Id, stripeCurrency, stripeAmount);
            return new StripePaymentIntentResult(paymentIntent.Id, paymentIntent.ClientSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe payment intent");
            throw;
        }
    }

    public async Task<PaymentVerificationResult> ConfirmPaymentAsync(string paymentIntentId)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            if (paymentIntent.Status == "succeeded" || paymentIntent.Status == "requires_capture")
            {
                return new PaymentVerificationResult { Success = true };
            }

            // Categorize failure reasons based on Stripe status and last payment error
            var errorType = "unknown";
            var errorMessage = $"Payment status: {paymentIntent.Status}";

            if (paymentIntent.LastPaymentError != null)
            {
                var lastError = paymentIntent.LastPaymentError;
                errorMessage = lastError.Message ?? errorMessage;

                // Categorize based on Stripe error codes
                errorType = lastError.Code switch
                {
                    "card_declined" => "declined",
                    "insufficient_funds" => "insufficient_funds",
                    "invalid_card_number" or "invalid_expiry_month" or "invalid_expiry_year" or "invalid_cvc" => "invalid_details",
                    "processing_error" or "rate_limit" => "provider_error",
                    _ when lastError.Type == "api_connection_error" || lastError.Type == "api_error" => "network",
                    _ => "declined"
                };
            }
            else if (paymentIntent.Status == "canceled")
            {
                errorType = "declined";
                errorMessage = "Payment was canceled";
            }

            return new PaymentVerificationResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ErrorType = errorType,
                RawResponse = $"Status: {paymentIntent.Status}"
            };
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError(stripeEx, "Stripe API error confirming payment");
            
            var errorType = stripeEx.StripeError?.Type switch
            {
                "card_error" => "declined",
                "invalid_request_error" => "invalid_details",
                "api_connection_error" => "network",
                "rate_limit_error" => "provider_error",
                _ => "provider_error"
            };

            return new PaymentVerificationResult
            {
                Success = false,
                ErrorMessage = stripeEx.Message,
                ErrorType = errorType,
                RawResponse = stripeEx.StripeError?.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming Stripe payment");
            return new PaymentVerificationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorType = "network",
                RawResponse = ex.ToString()
            };
        }
    }

    public async Task<StripeCaptureResult> CapturePaymentAsync(string paymentIntentId, decimal amount, string currency)
    {
        await EnsureInitializedAsync();
        try
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentCaptureOptions();

            // If partial capture is requested, set amount_to_capture
            if (amount > 0)
            {
                options.AmountToCapture = (long)(amount * 100);
            }

            var captured = await service.CaptureAsync(paymentIntentId, options);

            if (captured.Status == "succeeded")
            {
                _logger.LogInformation("Captured Stripe PaymentIntent {PaymentIntentId}", paymentIntentId);
                return new StripeCaptureResult(true, null);
            }

            _logger.LogError("Stripe capture did not succeed for {PaymentIntentId}, status: {Status}", paymentIntentId, captured.Status);
            return new StripeCaptureResult(false, $"Status: {captured.Status}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing Stripe payment");
            return new StripeCaptureResult(false, ex.Message);
        }
    }

    public async Task<string> CreateCustomerAsync(string email, string name, string phone)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Phone = phone
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe customer: {CustomerId}", customer.Id);
            return customer.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe customer");
            throw;
        }
    }

    public async Task<bool> RefundPaymentAsync(string paymentIntentId, decimal? amount = null)
    {
        await EnsureInitializedAsync();
        
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId
            };

            if (amount.HasValue)
            {
                options.Amount = (long)(amount.Value * 100); // Partial refund
            }

            var service = new Stripe.RefundService();
            var refund = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe refund: {RefundId}", refund.Id);
            return refund.Status == "succeeded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe refund");
            return false;
        }
    }
}
