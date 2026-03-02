using FluentAssertions;
using LicenseManagement.Infrastructure.Services.Payment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LicenseManagement.UnitTests.Services;

public class VnPayGatewayTests
{
    private readonly VnPayGateway _gateway;

    public VnPayGatewayTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payment:VnPay:TmnCode"] = "TESTCODE",
                ["Payment:VnPay:HashSecret"] = "TESTSECRETKEY12345678901234567890",
                ["Payment:VnPay:PaymentUrl"] = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
            })
            .Build();

        _gateway = new VnPayGateway(config, NullLogger<VnPayGateway>.Instance);
    }

    [Fact]
    public void ProviderName_ShouldBeVnPay()
    {
        _gateway.ProviderName.Should().Be("VnPay");
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldReturnPaymentUrl()
    {
        var result = await _gateway.CreateOrderAsync(new Application.Payments.Gateways.CreateOrderParams
        {
            OrderId = Guid.NewGuid().ToString(),
            Amount = 100_000,
            Description = "Test top-up",
            ReturnUrl = "https://example.com/return",
            NotifyUrl = "https://example.com/notify",
            IpAddress = "127.0.0.1",
        });

        result.Success.Should().BeTrue();
        result.PaymentUrl.Should().NotBeNullOrEmpty();
        result.PaymentUrl.Should().StartWith("https://sandbox.vnpayment.vn");
        result.PaymentUrl.Should().Contain("vnp_TmnCode=TESTCODE");
        result.PaymentUrl.Should().Contain("vnp_SecureHash=");
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldMultiplyAmountBy100()
    {
        var result = await _gateway.CreateOrderAsync(new Application.Payments.Gateways.CreateOrderParams
        {
            OrderId = "test-order",
            Amount = 50_000,
            Description = "Test",
            ReturnUrl = "https://example.com/return",
            NotifyUrl = "https://example.com/notify",
            IpAddress = "127.0.0.1",
        });

        result.PaymentUrl.Should().Contain("vnp_Amount=5000000"); // 50000 * 100
    }

    [Fact]
    public void VerifyCallback_WithInvalidSignature_ShouldReturnFalse()
    {
        var queryString = "vnp_Amount=10000000&vnp_TxnRef=test-123&vnp_ResponseCode=00&vnp_SecureHash=invalid_hash";

        var result = _gateway.VerifyCallback(queryString, "invalid_hash");
        result.Should().BeFalse();
    }

    [Fact]
    public void ParseCallback_ShouldExtractFields()
    {
        var queryString = "vnp_Amount=10000000&vnp_TxnRef=test-order-id&vnp_ResponseCode=00&vnp_TransactionNo=123456";

        var result = _gateway.ParseCallback(queryString);

        result.Success.Should().BeTrue();
        result.OrderId.Should().Be("test-order-id");
        result.Amount.Should().Be(100_000); // 10000000 / 100
        result.TransactionRef.Should().Be("123456");
    }

    [Fact]
    public void ParseCallback_WithFailedResponse_ShouldReturnFalse()
    {
        var queryString = "vnp_Amount=10000000&vnp_TxnRef=test-order&vnp_ResponseCode=24&vnp_TransactionNo=0";

        var result = _gateway.ParseCallback(queryString);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("24");
    }
}
