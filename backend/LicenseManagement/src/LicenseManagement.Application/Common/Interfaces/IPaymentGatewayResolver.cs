using LicenseManagement.Application.Payments.Gateways;
using LicenseManagement.Domain.Enums;

namespace LicenseManagement.Application.Common.Interfaces;

public interface IPaymentGatewayResolver
{
    IPaymentGateway Resolve(PaymentMethod method);
}
