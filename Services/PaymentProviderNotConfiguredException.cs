using System;

namespace GhanaHybridRentalApi.Services;

public class PaymentProviderNotConfiguredException : Exception
{
    public string Provider { get; }

    public PaymentProviderNotConfiguredException(string provider, string message) : base(message)
    {
        Provider = provider;
    }
}
