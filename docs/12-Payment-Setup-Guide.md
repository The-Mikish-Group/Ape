# Ape Framework - Payment Setup Guide

## Introduction

The Ape Framework supports two payment providers: **Stripe** and **PayPal**. Both can be used simultaneously, giving customers a choice at checkout. Stripe handles card payments and subscriptions via PaymentIntents and Stripe Billing. PayPal handles payments and subscriptions via the Orders API and PayPal Subscriptions. This guide covers setting up both providers from scratch.

---

## Stripe Setup

### Creating a Stripe Account

1. Go to [stripe.com](https://stripe.com) and click **Start now**
2. Enter your email, full name, and create a password
3. Verify your email address
4. Once logged in, you will land on the **Stripe Dashboard**
   - The dashboard provides an overview of payments, revenue, and recent activity
   - New accounts start in **Test mode** by default — no real charges are made

### Getting API Keys

1. In the Stripe Dashboard, click **Developers** in the left sidebar
2. Click **API Keys**
3. You will see two keys:

| Key | Prefix | Purpose |
|-----|--------|---------|
| Publishable key | `pk_test_` or `pk_live_` | Used in client-side code (safe to expose) |
| Secret key | `sk_test_` or `sk_live_` | Used in server-side code (keep private) |

4. Click **Reveal test key** to copy the Secret Key
5. Copy both keys — you will enter them into the Ape admin panel

**Test mode vs Live mode:**
- **Test mode** uses `pk_test_` / `sk_test_` keys — no real money is charged
- **Live mode** uses `pk_live_` / `sk_live_` keys — real transactions are processed
- Toggle between modes using the **Test mode** switch in the dashboard header
- Always develop and test with test keys first

### Configuring Stripe in Ape Framework

1. Log in as Admin
2. Navigate to **System Credentials**
3. Add these credentials:

| Key | Category | Value |
|-----|----------|-------|
| `Stripe__SecretKey` | Payment | Stripe secret API key (starts with `sk_test_` or `sk_live_`) |
| `Stripe__PublishableKey` | Payment | Stripe publishable key (starts with `pk_test_` or `pk_live_`) |
| `Stripe__WebhookSecret` | Payment | Stripe webhook signing secret (starts with `whsec_`) |

**Note:** The `Stripe__WebhookSecret` is configured after setting up webhooks (see next section).

### Setting Up Stripe Webhooks

Webhooks allow Stripe to notify the Ape Framework when payment events occur (successful payments, failed charges, subscription changes, etc.).

1. In the Stripe Dashboard, go to **Developers** → **Webhooks**
2. Click **Add endpoint**
3. Enter the endpoint URL:
   ```
   https://yourdomain.com/api/store/stripe-webhook
   ```
4. Click **Select events** and subscribe to the following:

| Event | Purpose |
|-------|---------|
| `payment_intent.succeeded` | One-time payment completed successfully |
| `payment_intent.payment_failed` | One-time payment failed |
| `invoice.paid` | Subscription invoice paid successfully |
| `customer.subscription.updated` | Subscription plan or status changed |
| `customer.subscription.deleted` | Subscription cancelled or expired |

5. Click **Add endpoint**
6. On the endpoint detail page, click **Reveal** under **Signing secret**
7. Copy the signing secret (starts with `whsec_`)
8. Save it as the `Stripe__WebhookSecret` credential in the Ape admin panel

### Creating Subscription Products in Stripe

To sell subscription products through the Ape store, you must first create them in Stripe:

1. In the Stripe Dashboard, go to **Products** → **Add Product**
2. Enter a product name and description
3. Under **Pricing**, click **Add price**:
   - Select **Recurring**
   - Enter the price amount
   - Choose the billing interval (Monthly, Yearly, etc.)
   - Click **Save**
4. After saving, click into the price to find the **Price ID** (starts with `price_`)
5. Copy the Price ID
6. In the Ape admin panel, navigate to **Store** → **Products**
7. Edit the corresponding subscription product
8. Paste the Price ID into the **Stripe Price ID** field
9. Save the product

---

## PayPal Setup

### Creating a PayPal Developer Account

1. Go to [developer.paypal.com](https://developer.paypal.com) and log in with your PayPal account
   - If you do not have a PayPal account, create one first at [paypal.com](https://www.paypal.com)
2. Navigate to **Apps & Credentials** in the developer dashboard
3. Click **Create App**
4. Enter an app name (e.g., "Ape Framework Store")
5. Select **Merchant** as the app type
6. Click **Create App**

### Getting API Credentials

1. In the PayPal Developer Dashboard, go to **Apps & Credentials**
2. Select your app from the list
3. You will see two credentials:

| Credential | Purpose |
|-----------|---------|
| Client ID | Identifies your app (used in client-side and server-side code) |
| Client Secret | Authenticates API requests (keep private) |

4. Copy both the **Client ID** and **Client Secret**

**Sandbox vs Live mode:**
- **Sandbox** uses test credentials — no real money is charged
- **Live** uses production credentials — real transactions are processed
- Toggle between modes using the **Sandbox** / **Live** toggle at the top of the credentials page
- Always develop and test with sandbox credentials first

### Configuring PayPal in Ape Framework

1. Log in as Admin
2. Navigate to **System Credentials**
3. Add these credentials:

| Key | Category | Value |
|-----|----------|-------|
| `PayPal__ClientId` | Payment | PayPal REST API client ID |
| `PayPal__ClientSecret` | Payment | PayPal REST API client secret |
| `PayPal__Mode` | Payment | `sandbox` for testing, `live` for production |
| `PayPal__WebhookId` | Payment | PayPal webhook ID (configured after setting up webhooks) |

### Setting Up PayPal Webhooks

Webhooks allow PayPal to notify the Ape Framework when payment events occur.

1. In the PayPal Developer Dashboard, go to **Apps & Credentials**
2. Select your app
3. Scroll down to **Webhooks** and click **Add Webhook**
4. Enter the webhook URL:
   ```
   https://yourdomain.com/api/store/paypal-webhook
   ```
5. Select the following events:

| Event | Purpose |
|-------|---------|
| `PAYMENT.CAPTURE.COMPLETED` | One-time payment captured successfully |
| `BILLING.SUBSCRIPTION.ACTIVATED` | Subscription activated |
| `BILLING.SUBSCRIPTION.CANCELLED` | Subscription cancelled by user or admin |
| `BILLING.SUBSCRIPTION.EXPIRED` | Subscription reached end of term |
| `BILLING.SUBSCRIPTION.PAYMENT.FAILED` | Subscription payment failed |

6. Click **Save**
7. After saving, the webhook list will show your new webhook with a **Webhook ID**
8. Copy the Webhook ID
9. Save it as the `PayPal__WebhookId` credential in the Ape admin panel

### Creating Subscription Plans in PayPal

To sell subscription products through PayPal, you must create products and billing plans:

1. In the PayPal Developer Dashboard or PayPal Business account, go to **Products**
2. Click **Create Product**
   - Enter a product name and description
   - Select product type (Service or Digital)
   - Save the product
3. Under the product, click **Create Plan**
   - Enter a plan name
   - Set the billing cycle:
     - Frequency (Monthly, Yearly, etc.)
     - Price amount
     - Currency
   - Configure trial periods if needed
   - Click **Create Plan**
4. Copy the **Plan ID** (starts with `P-`)
5. In the Ape admin panel, navigate to **Store** → **Products**
6. Edit the corresponding subscription product
7. Paste the Plan ID into the **PayPal Plan ID** field
8. Save the product

---

## Testing Payments

### Stripe Test Cards

Use these card numbers in test mode to simulate different payment scenarios:

| Card Number | Result |
|------------|--------|
| `4242 4242 4242 4242` | Successful payment |
| `4000 0000 0000 3220` | 3D Secure authentication required |
| `4000 0000 0000 0002` | Card declined |

For all test cards:
- Use any **future expiry date** (e.g., 12/34)
- Use any **3-digit CVC** (e.g., 123)
- Use any **postal code** (e.g., 12345)

Additional useful test cards:

| Card Number | Result |
|------------|--------|
| `4000 0000 0000 9995` | Insufficient funds decline |
| `4000 0000 0000 0069` | Expired card decline |
| `4000 0000 0000 0127` | Incorrect CVC decline |

### PayPal Sandbox

1. Go to [developer.paypal.com](https://developer.paypal.com) → **Sandbox** → **Accounts**
2. PayPal automatically creates two sandbox accounts:
   - **Business account** — represents the merchant (your store)
   - **Personal account** — represents a buyer (for testing purchases)
3. To test a purchase:
   - Select PayPal as the payment method at checkout
   - Log in with the **sandbox personal account** credentials
   - Sandbox buyer accounts come with pre-loaded test funds
   - Complete the payment flow
4. To view sandbox account credentials, click the account and then **View/Edit account**

### Testing Webhooks Locally

For local development, use the Stripe CLI to forward webhooks:

```
stripe listen --forward-to https://localhost:5001/api/store/stripe-webhook
```

The CLI will display a webhook signing secret to use as your local `Stripe__WebhookSecret`.

For PayPal, use a tunnel service (e.g., ngrok) to expose your local server:

```
ngrok http 5001
```

Then configure the PayPal webhook URL to use the ngrok tunnel URL.

---

## Going Live

Checklist for switching from test to production:

1. **Replace Stripe API keys** — Update `Stripe__SecretKey` and `Stripe__PublishableKey` with live keys (`sk_live_` / `pk_live_`) in System Credentials
2. **Update PayPal mode** — Change `PayPal__Mode` from `sandbox` to `live` in System Credentials
3. **Replace PayPal credentials** — Update `PayPal__ClientId` and `PayPal__ClientSecret` with live credentials
4. **Create production webhooks** — Set up new webhook endpoints in both Stripe and PayPal dashboards using your production domain URL
5. **Update webhook secrets** — Save the new `Stripe__WebhookSecret` and `PayPal__WebhookId` from your production webhooks
6. **Create live products** — Create subscription products and pricing plans in both Stripe and PayPal live environments
7. **Update product IDs** — Enter the live Stripe Price IDs and PayPal Plan IDs in the Ape admin for each subscription product
8. **Test with a real transaction** — Make a small real purchase to verify the entire flow works end-to-end
9. **Verify webhooks** — Confirm webhook events are being received and processed correctly in production

---

## Credential Summary

All payment credentials configured through **System Credentials** in the Ape admin panel:

### Stripe Credentials

| Key | Category | Description |
|-----|----------|-------------|
| `Stripe__SecretKey` | Payment | Secret API key (`sk_test_` or `sk_live_`) |
| `Stripe__PublishableKey` | Payment | Publishable key (`pk_test_` or `pk_live_`) |
| `Stripe__WebhookSecret` | Payment | Webhook signing secret (`whsec_`) |

### PayPal Credentials

| Key | Category | Description |
|-----|----------|-------------|
| `PayPal__ClientId` | Payment | REST API client ID |
| `PayPal__ClientSecret` | Payment | REST API client secret |
| `PayPal__Mode` | Payment | `sandbox` or `live` |
| `PayPal__WebhookId` | Payment | Webhook ID from PayPal dashboard |

---

## Troubleshooting

### Stripe Issues

**"No such payment intent" or "Invalid API key"**
- Verify you are using the correct key pair (test keys with test mode, live keys with live mode)
- Ensure the `Stripe__SecretKey` is entered correctly without extra spaces
- Check that the key has not been rolled or revoked in the Stripe dashboard

**Webhook events not received**
- Verify the webhook endpoint URL is correct and publicly accessible
- Check that HTTPS is configured correctly on your server
- In Stripe Dashboard → Webhooks, check for failed delivery attempts and error messages
- Verify the `Stripe__WebhookSecret` matches the signing secret shown on the webhook endpoint page

**"Webhook signature verification failed"**
- The `Stripe__WebhookSecret` does not match the endpoint's signing secret
- If you recreated the webhook endpoint, copy the new signing secret
- Ensure the raw request body is not being modified by middleware before signature verification

**Subscription not activating**
- Verify the Stripe Price ID is entered correctly in the product's settings
- Check that the price exists and is active in the Stripe dashboard
- Review Stripe Dashboard → Events for detailed error information

### PayPal Issues

**"Authentication failed" or "Invalid client credentials"**
- Verify `PayPal__ClientId` and `PayPal__ClientSecret` are correct
- Ensure you are using sandbox credentials with `sandbox` mode and live credentials with `live` mode
- Check that the REST API app is active in the PayPal developer dashboard

**PayPal button not appearing at checkout**
- Verify `PayPal__ClientId` is configured in System Credentials
- Check the browser console for JavaScript errors
- Ensure the PayPal script is loading correctly

**Webhook events not received**
- Verify the webhook URL is correct and publicly accessible
- Check that the `PayPal__WebhookId` matches the webhook ID in the PayPal dashboard
- In the PayPal Developer Dashboard, check the webhook event log for delivery status

**Subscription not activating**
- Verify the PayPal Plan ID is entered correctly in the product's settings
- Check that the billing plan is active in PayPal
- Review the PayPal dashboard for subscription status and error details

### General Payment Issues

**Payments succeed in test mode but fail in live mode**
1. Verify all credentials have been switched to live/production values
2. Ensure `PayPal__Mode` is set to `live`
3. Check that live webhook endpoints are configured for your production domain
4. Verify your Stripe account has completed activation (identity verification, bank account)
5. Verify your PayPal business account is verified and in good standing

**Orders stuck in "Pending" status**
- Check webhook configuration — order status updates rely on webhook events
- Verify webhook endpoints are receiving events (check provider dashboards)
- Review application logs for webhook processing errors

**Customer charged but order not confirmed**
- This typically indicates a webhook delivery failure
- Check the payment in the Stripe or PayPal dashboard to confirm the charge
- Manually update the order status in the Ape admin if needed
- Investigate and fix the webhook issue to prevent recurrence

---

## Best Practices

### Security

1. **Never expose secret keys** — Keep `Stripe__SecretKey` and `PayPal__ClientSecret` server-side only
2. **Use webhook signature verification** — Always validate webhook signatures to prevent spoofing
3. **Rotate keys periodically** — Roll your API keys on a regular schedule
4. **Use restricted keys in Stripe** — Create restricted API keys with only the permissions you need

### Reliability

1. **Configure both providers** — Offering both Stripe and PayPal gives customers a choice and provides a fallback
2. **Monitor webhook delivery** — Regularly check both dashboards for failed webhook deliveries
3. **Test after changes** — Always test the payment flow after updating credentials or configuration

### Going Live

1. **Complete provider verification** — Ensure your Stripe and PayPal accounts are fully verified before going live
2. **Test the full flow** — Test purchase, webhook delivery, order confirmation, and email notifications
3. **Start with small amounts** — Make a small real purchase to verify everything works before launching

---

**Version:** 2.0.0
**Framework:** Ape Framework
**Site:** https://Illustrate.net
