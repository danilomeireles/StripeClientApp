using FluentAssertions;
using Moq;
using Stripe;
using Stripe.Terminal;
using StripeClientApp.Services;

namespace StripeClientApp.UnitTests;

public class StripeServiceTests
{
    private readonly Mock<SubscriptionService> _subscriptionServiceMock;
    private readonly StripeService _stripeService;

    public StripeServiceTests()
    {
        _subscriptionServiceMock = new Mock<SubscriptionService>();
            
        _stripeService = new StripeService(
            _subscriptionServiceMock.Object,
            new Mock<InvoiceService>().Object,
            new Mock<ChargeService>().Object,
            new Mock<PaymentIntentService>().Object,
            new Mock<RefundService>().Object,
            new Mock<PaymentMethodService>().Object,
            new Mock<ConnectionTokenService>().Object,
            new Mock<CustomerService>().Object
        );
    }

    [Fact]
    public async Task ListAllSubscriptionsAsync_ShouldReturnSubscriptions_WhenCalled()
    {
        // Arrange
        var subscriptions = new StripeList<Subscription>
        {
            Data = new List<Subscription> { new Subscription { Id = "sub_123" } }
        };

        _subscriptionServiceMock
            .Setup(x => x.ListAsync(It.IsAny<SubscriptionListOptions>(), null, default))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _stripeService.ListAllSubscriptionsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data[0].Id.Should().Be("sub_123");
    }
    
    [Fact]
    public async Task GetSubscriptionAsync_ShouldReturnSubscription_WhenCalledWithValidId()
    {
        // Arrange
        var subscriptionId = "sub_123";
        var subscription = new Subscription { Id = subscriptionId };

        _subscriptionServiceMock
            .Setup(x => x.GetAsync(subscriptionId, It.IsAny<SubscriptionGetOptions>(), It.IsAny<RequestOptions>(), default))
            .ReturnsAsync(subscription);

        // Act
        var result = await _stripeService.GetSubscriptionAsync(subscriptionId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(subscriptionId);
    }
}