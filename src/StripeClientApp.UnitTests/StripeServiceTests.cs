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
            new Mock<SubscriptionItemService>().Object,
            new Mock<InvoiceService>().Object,
            new Mock<InvoiceItemService>().Object,
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
    
    [Fact]
    public async Task UpdateSubscriptionAsync_ShouldUpdateSubscription_WhenCalledWithValidOptions()
    {
        // Arrange
        var subscriptionId = "sub_123";
        var subscription = new Subscription
        {
            Id = subscriptionId,
            CustomerId = "cus_123",
            Items = new StripeList<SubscriptionItem> { Data = new List<SubscriptionItem>()}
        };
        
        subscription.Items.Data.Add(new SubscriptionItem { Id = "item_1", Created = DateTime.UtcNow.AddDays(-1) });
        subscription.Items.Data.Add(new SubscriptionItem { Id = "item_2", Created = DateTime.UtcNow });

        var updateOptions = new SubscriptionUpdateOptions
        {
            ProrationBehavior = "none"
        };

        _subscriptionServiceMock
            .Setup(x => x.UpdateAsync(subscriptionId, updateOptions, null, default))
            .ReturnsAsync(subscription);

        _subscriptionServiceMock
            .Setup(x => x.GetAsync(subscriptionId, It.IsAny<SubscriptionGetOptions>(), It.IsAny<RequestOptions>(), default))
            .ReturnsAsync(subscription);

        var subscriptionItemServiceMock = new Mock<SubscriptionItemService>();
        subscriptionItemServiceMock
            .Setup(x => x.DeleteAsync("item_1", It.IsAny<SubscriptionItemDeleteOptions>(), It.IsAny<RequestOptions>(), default))
            .ReturnsAsync(new SubscriptionItem { Id = "item_1" });

        var invoiceServiceMock = new Mock<InvoiceService>();
        invoiceServiceMock
            .Setup(x => x.UpcomingAsync(It.IsAny<UpcomingInvoiceOptions>(), null, default))
            .ReturnsAsync(new Invoice
            {
                Lines = new StripeList<InvoiceLineItem>
                {
                    Data = new List<InvoiceLineItem>
                    {
                        new InvoiceLineItem { Id = "line_1", InvoiceItem = new InvoiceItem { Id = "item_1" } },
                        new InvoiceLineItem { Id = "line_2", InvoiceItem = new InvoiceItem { Id = "item_2"} }
                    }
                }
            });

        var invoiceItemServiceMock = new Mock<InvoiceItemService>();
        invoiceItemServiceMock
            .Setup(x => x.DeleteAsync("item_1", It.IsAny<InvoiceItemDeleteOptions>(), It.IsAny<RequestOptions>(), default))
            .ReturnsAsync(new InvoiceItem { Id = "item_1" });

        var stripeService = new StripeService(
            _subscriptionServiceMock.Object,
            subscriptionItemServiceMock.Object,
            invoiceServiceMock.Object,
            invoiceItemServiceMock.Object,
            new Mock<ChargeService>().Object,
            new Mock<PaymentIntentService>().Object,
            new Mock<RefundService>().Object,
            new Mock<PaymentMethodService>().Object,
            new Mock<ConnectionTokenService>().Object,
            new Mock<CustomerService>().Object
        );

        // Act
        var result = await stripeService.UpdateSubscriptionAsync(subscriptionId, updateOptions);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(subscriptionId);
        
        subscriptionItemServiceMock.Verify(x => x.DeleteAsync("item_1", It.IsAny<SubscriptionItemDeleteOptions>(), It.IsAny<RequestOptions>(), default), Times.Once);
        invoiceItemServiceMock.Verify(x => x.DeleteAsync("line_1", It.IsAny<InvoiceItemDeleteOptions>(), It.IsAny<RequestOptions>(), default), Times.Once);
    }
    
    [Fact]
    public async Task CancelSubscriptionAsync_ShouldCancelSubscription_WhenCalledWithValidId()
    {
        // Arrange
        var subscriptionId = "sub_123";
        var canceledSubscription = new Subscription
        {
            Id = subscriptionId,
            Status = "canceled"
        };

        var cancelOptions = new SubscriptionCancelOptions
        {
            Prorate = false,
            InvoiceNow = false
        };
        
        _subscriptionServiceMock
            .Setup(x => x.CancelAsync(subscriptionId, It.IsAny<SubscriptionCancelOptions>(), null, default))
            .ReturnsAsync(canceledSubscription);

        // Act
        var result = await _stripeService.CancelSubscriptionAsync(subscriptionId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(subscriptionId);
        result.Status.Should().Be("canceled");
        
        _subscriptionServiceMock.Verify(x => x.CancelAsync(subscriptionId, It.IsAny<SubscriptionCancelOptions>(), null, default), Times.Once);
    }
}