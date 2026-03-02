using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Payments.Gateways;
using LicenseManagement.Domain.Enums;

namespace LicenseManagement.Infrastructure.Services.Payment;

public class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly IEnumerable<IPaymentGateway> _gateways;

    public PaymentGatewayResolver(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways;
    }

    public IPaymentGateway Resolve(PaymentMethod method)
    {
        var providerName = method switch
        {
            PaymentMethod.MoMo => "MoMo",
            PaymentMethod.VnPay => "VnPay",
            PaymentMethod.ZaloPay => "ZaloPay",
            _ => throw new ArgumentException($"Phương thức thanh toán không hỗ trợ: {method}")
        };

        return _gateways.FirstOrDefault(g => g.ProviderName == providerName)
               ?? throw new InvalidOperationException($"Payment gateway '{providerName}' chưa được đăng ký");
    }
}
