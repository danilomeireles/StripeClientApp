﻿using Stripe;
using Stripe.Terminal;

namespace StripeClientApp.Services;

public class StripeService : IStripeService
{
	private const string StripeApiKey = "stripe-api-key";
	
	private readonly SubscriptionService _subscriptionService;
	private readonly InvoiceService _invoiceService;
	private readonly ChargeService _chargeService;
	private readonly PaymentIntentService _paymentIntentService;
	private readonly RefundService _refundService;
	private readonly PaymentMethodService _paymentMethodService;
	private readonly ConnectionTokenService _connectionTokenService;
	private readonly CustomerService _customerService;
	
	public StripeService(
		SubscriptionService subscriptionService,
		InvoiceService invoiceService,
		ChargeService chargeService,
		PaymentIntentService paymentIntentService,
		RefundService refundService,
		PaymentMethodService paymentMethodService,
		ConnectionTokenService connectionTokenService,
		CustomerService customerService)
	{
		StripeConfiguration.ApiKey = StripeApiKey;
		
		_subscriptionService = subscriptionService;
		_invoiceService = invoiceService;
		_chargeService = chargeService;
		_paymentIntentService = paymentIntentService;
		_refundService = refundService;
		_paymentMethodService = paymentMethodService;
		_connectionTokenService = connectionTokenService;
		_customerService = customerService;
	}
	
	public async Task<StripeList<Subscription>> ListAllSubscriptionsAsync(int limit = 10)
	{
		var options = new SubscriptionListOptions
		{
			Limit = limit
		};

		return await _subscriptionService.ListAsync(options);
	}
	
	public async Task<Subscription> GetSubscriptionAsync(string subscriptionId)
	{
		return await _subscriptionService.GetAsync(subscriptionId);
	}

	public async Task<Subscription> UpdateSubscriptionAsync(string subscriptionId, SubscriptionUpdateOptions options)
	{
		options.ProrationBehavior = "none";

		var updatedSubscription = await _subscriptionService.UpdateAsync(subscriptionId, options);

		var subscription = await GetSubscriptionAsync(subscriptionId);
		var latestItem = subscription.Items.OrderByDescending(x => x.Created).FirstOrDefault();

		var subscriptionItems = subscription.Items.Where(item => item.Id != latestItem?.Id);
		foreach (var item in subscriptionItems)
		{
			await DeleteSubscriptionItemAsync(item.Id);
		}

		await ClearInvoiceLineItemsAsync(subscription.CustomerId, subscription.Id);
		return updatedSubscription;
	}

	public async Task<Subscription> CancelSubscriptionAsync(string subscriptionId)
	{
		var options = new SubscriptionCancelOptions
		{
			Prorate = false,
			InvoiceNow = false			
		};
		return await _subscriptionService.CancelAsync(subscriptionId, options);
	}

	public async Task<Subscription> CancelUpcomingSubscriptionAsync(string subscriptionId)
	{
		var options = new SubscriptionUpdateOptions
		{
			CancelAtPeriodEnd = true,
		};

		return await _subscriptionService.UpdateAsync(subscriptionId, options);
	}


	public async Task<Subscription> CreateSubscriptionAsync(SubscriptionCreateOptions options)
	{
		return await _subscriptionService.CreateAsync(options);
	}

	public async Task<SubscriptionItem> CreateSubscriptionItemAsync(SubscriptionItemCreateOptions options)
	{
		var service = new SubscriptionItemService();
		return await service.CreateAsync(options);
	}

	public async Task DeleteSubscriptionItemAsync(string itemId)
	{
		var service = new SubscriptionItemService();
		await service.DeleteAsync(itemId);
	}
	
	public async Task<Invoice> GetInvoiceAsync(string invoiceId)
	{
		var options = new InvoiceGetOptions
		{
			Expand = ["charge", "payment_intent", "subscription", "subscription.default_payment_method"]
		};
		return await _invoiceService.GetAsync(invoiceId, options);
	}

	public async Task<Invoice> PayInvoiceAsync(string invoiceId)
	{
		return await _invoiceService.PayAsync(invoiceId);
	}

	private async Task ClearInvoiceLineItemsAsync(string customerId, string subscriptionId)
	{
		var invoiceOptions = new UpcomingInvoiceOptions
		{
			Customer = customerId,
			Subscription = subscriptionId
		};
		var upcomingInvoice = await _invoiceService.UpcomingAsync(invoiceOptions);
		var lastInvoiceItemId = upcomingInvoice.Lines.Last().InvoiceItem;

		foreach (var lineItem in upcomingInvoice.Lines.Where(lineItem => lineItem.InvoiceItem != lastInvoiceItemId))
		{
			await DeleteInvoiceLineItemAsync(lineItem.Id);
		}
	}

	private async Task DeleteInvoiceLineItemAsync(string itemId)
	{
		var service = new InvoiceItemService();
		await service.DeleteAsync(itemId);
	}
	
	public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
	{
		var options = new PaymentIntentGetOptions
		{
			Expand = ["payment_method"]
		};
		return await _paymentIntentService.GetAsync(paymentIntentId, options);
	}

	public async Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options)
	{
		return await _paymentIntentService.CreateAsync(options);
	}

	public async Task<PaymentIntent> UpdatePaymentIntentAsync(string paymentIntentId, PaymentIntentUpdateOptions options)
	{
		return await _paymentIntentService.UpdateAsync(paymentIntentId, options);
	}

	public async Task<PaymentIntent> ConfirmPaymentIntentAsync(string paymentIntentId)
	{
		return await _paymentIntentService.ConfirmAsync(paymentIntentId);
	}
	
	public async Task<Refund> CreateRefundAsync(RefundCreateOptions options)
	{
		return await _refundService.CreateAsync(options);
	}
	
	public async Task<PaymentMethod> GetPaymentMethodAsync(string paymentMethodId)
	{
		return await _paymentMethodService.GetAsync(paymentMethodId);
	}

	public async Task<PaymentMethodCard> GetCardBrandAsync(string paymentMethodId)
	{
		var paymentMethod = await GetPaymentMethodAsync(paymentMethodId);
		return paymentMethod.Card;
	}
	
	public async Task<Charge> GetChargeAsync(string chargeId)
	{
		return await _chargeService.GetAsync(chargeId);
	}

	public async Task<StripeList<Charge>> GetCustomerChargesAsync(string customerId, int limit = 1)
	{
		var options = new ChargeListOptions { Limit = limit, Customer = customerId };
		return await _chargeService.ListAsync(options);
	}
	
	public async Task<ConnectionToken> CreateConnectionTokenAsync()
	{
		var options = new ConnectionTokenCreateOptions();
		return await _connectionTokenService.CreateAsync(options);
	}
	
	public async Task<Customer> GetCustomerAsync(string customerId)
	{
		return await _customerService.GetAsync(customerId);
	}
	
	public async Task<Customer> GetCustomerPaymentSourcesAsync(string customerId)
	{
		var options = new CustomerGetOptions
		{
			Expand = ["sources"]
		};
		return await _customerService.GetAsync(customerId, options);
	}
}