using Stripe;
using Stripe.Terminal;

namespace StripeClientApp.Services;

public interface IStripeService
{
    // Subscription Methods
    Task<StripeList<Subscription>> ListAllSubscriptionsAsync(int limit = 10);
    Task<Subscription> GetSubscriptionAsync(string subscriptionId);
    Task<Subscription> UpdateSubscriptionAsync(string subscriptionId, SubscriptionUpdateOptions options);
    Task<Subscription> CancelSubscriptionAsync(string subscriptionId);
    Task<Subscription> CancelUpcomingSubscriptionAsync(string subscriptionId);
    Task<Subscription> CreateSubscriptionAsync(SubscriptionCreateOptions options);
    Task<SubscriptionItem> CreateSubscriptionItemAsync(SubscriptionItemCreateOptions options);
    Task DeleteSubscriptionItemAsync(string itemId);

    // Invoice Methods
    Task<Invoice> GetInvoiceAsync(string invoiceId);
    Task<Invoice> PayInvoiceAsync(string invoiceId);

    // Payment Intent Methods
    Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId);
    Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options);
    Task<PaymentIntent> UpdatePaymentIntentAsync(string paymentIntentId, PaymentIntentUpdateOptions options);
    Task<PaymentIntent> ConfirmPaymentIntentAsync(string paymentIntentId);

    // Refund Methods
    Task<Refund> CreateRefundAsync(RefundCreateOptions options);

    // Payment Method Methods
    Task<PaymentMethod> GetPaymentMethodAsync(string paymentMethodId);
    Task<PaymentMethodCard> GetCardBrandAsync(string paymentMethodId);

    // Charge Methods
    Task<Charge> GetChargeAsync(string chargeId);
    Task<StripeList<Charge>> GetCustomerChargesAsync(string customerId, int limit = 1);

    // Terminal Methods
    Task<ConnectionToken> CreateConnectionTokenAsync();

    // Customer Methods
    Task<Customer> GetCustomerAsync(string customerId);
    Task<Customer> GetCustomerPaymentSourcesAsync(string customerId);
}