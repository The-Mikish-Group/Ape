# Ape Framework - Online Store Guide

## Introduction

The Online Store module provides a full-featured e-commerce system supporting physical products, digital downloads, and recurring subscriptions. It includes dual payment gateway integration (Stripe and PayPal), inventory management, order lifecycle tracking, digital delivery with secure download links, and a comprehensive admin dashboard. This guide covers all aspects of configuring, using, and managing the store.

---

## Features Overview

- **Three Product Types** - Physical goods, digital downloads, and subscription plans
- **Dual Payment Gateways** - Stripe (cards + subscriptions) and PayPal (orders + subscriptions)
- **Product Catalog** - Categories, images, search, sorting, and pagination
- **Member Pricing** - Optional discounted pricing for active subscribers
- **Shopping Cart** - Persistent cart with real-time stock validation
- **Smart Checkout** - Automatically skips shipping for digital-only orders
- **Order Management** - Full lifecycle from Pending through Delivered, with tracking and refunds
- **Digital Delivery** - Secure download links with configurable count limits and expiry dates
- **Subscription Billing** - Recurring payments via Stripe Subscriptions or PayPal Subscriptions
- **Inventory Tracking** - Stock levels, low-stock alerts, and manual adjustments
- **Sales Reports** - Revenue and order analytics with date-range filtering
- **Webhook Integration** - Automated payment confirmations, subscription renewals, and cancellations
- **Admin Dashboard** - Centralized management for products, orders, subscriptions, and reporting

---

## Product Types

### Physical Products

Physical products require shipping to a customer address. They support:
- Inventory tracking with stock quantities and low-stock thresholds
- Weight for shipping calculations
- Shipping address collection at checkout

### Digital Products

Digital products are delivered electronically after purchase. They support:
- Uploaded digital files stored securely outside the web root
- Configurable maximum download count per purchase
- Configurable download link expiry (in days)
- Immediate download access after payment

### Subscription Products

Subscription products provide recurring billing for membership or service access. They support:
- Configurable billing interval (e.g., monthly, yearly)
- Stripe Price ID and PayPal Plan ID for gateway integration
- Active subscribers receive member pricing on other products
- Payment history tracking for all renewals

---

## Storefront

### Browsing Products

The public storefront is accessible without authentication. Visitors can:

1. **Home Page** (`/Store`) - View featured products and categories
2. **Browse** (`/Store/Browse`) - Filter by category, product type, or search term
3. **Product Detail** (`/Store/Product/{slug}`) - View full product details, images, and pricing
4. **Search** (`/Store/Search?q=term`) - Search products by name or description

### Sorting and Filtering

The browse page supports:
- **Category filtering** - Select a category to narrow results
- **Product type filtering** - Filter by Physical, Digital, or Subscription
- **Search** - Full-text search across product names and descriptions
- **Sorting** - Sort by name, price, or newest
- **Pagination** - 24 products per page

### Member Pricing

Products can optionally have a `MemberPrice` that is displayed to users with an active subscription. When a subscriber browses the store, they see both the regular price and their discounted member price. The member price is automatically applied at checkout.

---

## Shopping Cart

*Requires authentication*

### Adding Items

- Click **Add to Cart** on any product page
- AJAX-powered — no page reload required
- Cart badge in the navigation updates automatically

### Cart Page

Navigate to **Cart** (`/Cart`) to view your cart. The cart page shows:
- Product name, quantity, and unit price
- Line totals and cart subtotal
- Shipping cost (flat rate for physical items, free for digital-only)
- Free shipping threshold notification (if configured)
- Out-of-stock warnings for items that are no longer available

### Cart Actions

| Action | Description |
|--------|-------------|
| Update quantity | Change item quantity with +/- controls |
| Remove item | Remove a single item from the cart |
| Continue shopping | Return to the store to add more items |
| Proceed to checkout | Begin the checkout process |

### Stock Validation

The cart validates stock levels in real time:
- Items that exceed available stock are flagged
- Out-of-stock items must be removed before checkout
- Stock is reserved at order creation, not when added to cart

---

## Checkout

*Requires authentication*

### Checkout Flow

1. **Review Cart** - The checkout page displays cart contents, pricing, and shipping
2. **Shipping Address** - Select a saved address or add a new one (physical items only)
3. **Place Order** - Creates the order and redirects to payment
4. **Payment** - Choose Stripe (card) or PayPal and complete payment
5. **Confirmation** - Order confirmation with receipt and download links (if applicable)

### Smart Checkout

The checkout process adapts based on cart contents:
- **Digital-only orders** - Shipping address step is skipped entirely
- **Physical orders** - Shipping address is required
- **Mixed orders** - Shipping address is required for the physical items

### Shipping

| Setting | Description |
|---------|-------------|
| `Store__FlatRateShipping` | Flat shipping rate applied to orders with physical items (default: 5.99) |
| `Store__FreeShippingThreshold` | Order subtotal for free shipping; leave empty to disable |

### Payment Options

**Stripe (Card Payments)**
- Powered by Stripe PaymentIntents API
- Card input via Stripe Elements (PCI-compliant)
- Payment confirmation is handled client-side, verified by webhook

**PayPal**
- Powered by PayPal Orders API
- Redirects to PayPal for approval, then captures on return
- Fallback redirect handling for browser navigation

### Order Confirmation

After successful payment, the confirmation page displays:
- Order number
- Items purchased with prices
- Total amount and payment method
- Download links for digital items (if applicable)
- Print receipt button

---

## Order History

*Requires authentication*

### My Orders

Navigate to **My Orders** (`/OrderHistory`) to view your order history. The page shows:
- Order number, date, status, and total
- Number of items per order
- Pagination for long histories

### Order Detail

Click any order to view details (`/OrderHistory/Detail/{id}`):
- Order items with quantities and prices
- Order status and payment information
- Shipping address and tracking details (physical orders)
- Download links for digital items
- Print receipt button

### My Downloads

Navigate to **My Downloads** (`/OrderHistory/Downloads`) to see all your digital purchases:
- Product name and file name
- Download count remaining
- Expiry date
- Direct download link

### Shipping Addresses

Navigate to **My Addresses** (`/OrderHistory/Addresses`) to manage saved shipping addresses:
- View all saved addresses
- Add a new address
- Edit existing addresses
- Set a default address for checkout
- Delete addresses no longer needed

---

## Digital Downloads

### How It Works

1. Customer purchases a digital product
2. After payment confirmation, download records are created automatically
3. Download links appear on the order confirmation page and in order history
4. Each link is a unique, secure token tied to the user and order

### Download Limits

Digital downloads are controlled by two configurable limits per product:
- **Max Downloads** - Maximum number of times the file can be downloaded (e.g., 5)
- **Download Expiry Days** - Number of days the download link remains active (e.g., 30)

Once either limit is reached, the download link becomes inactive.

### Secure File Access

Download URLs use the format `/download/{token}` where the token is a unique, non-guessable string. The system verifies:
- The token is valid and belongs to the authenticated user
- The download count has not been exceeded
- The link has not expired
- The physical file exists on disk

Digital files are stored in `ProtectedFiles/store/` outside the web root, so they cannot be accessed directly via URL.

---

## Subscriptions

*Requires authentication*

### Subscribing

1. Find a subscription product in the store
2. Click **Subscribe** to go to the subscription page (`/Subscription/Subscribe/{productId}`)
3. Choose a payment method:
   - **Stripe** - Enter card details via Stripe Elements
   - **PayPal** - Redirect to PayPal for approval
4. After payment, your subscription is activated immediately

**Note:** Only one active subscription per user is allowed. If you already have an active subscription, you will be redirected to the manage page.

### Managing Your Subscription

Navigate to **Manage Subscription** (`/Subscription/Manage`) to view:
- Subscription plan name and price
- Billing interval and next billing date
- Current status (Active, Past Due, Cancelled)
- Option to cancel

### Cancelling

1. Go to **Manage Subscription**
2. Click **Cancel Subscription**
3. Optionally provide a cancellation reason
4. The subscription is cancelled at the payment gateway (Stripe or PayPal)
5. Access continues until the end of the current billing period

### Subscription Statuses

| Status | Description |
|--------|-------------|
| Active | Subscription is current and billing normally |
| Past Due | Payment failed; subscription at risk of cancellation |
| Cancelled | User or admin cancelled; access may continue until period end |
| Expired | Subscription period ended without renewal |

---

## Payment Gateways

### Stripe Configuration

Stripe credentials are stored as encrypted credentials via `SecureConfigurationService`:

| Credential Key | Description |
|----------------|-------------|
| `Stripe__SecretKey` | Stripe secret API key |
| `Stripe__PublishableKey` | Stripe publishable key (used client-side) |
| `Stripe__WebhookSecret` | Stripe webhook signing secret |

**Stripe handles:**
- One-time card payments via PaymentIntents API
- Recurring subscriptions via Stripe Subscriptions API
- Refunds via the Stripe Refunds API

### PayPal Configuration

PayPal credentials are stored as encrypted credentials via `SecureConfigurationService`:

| Credential Key | Description |
|----------------|-------------|
| `PayPal__ClientId` | PayPal REST API client ID |
| `PayPal__ClientSecret` | PayPal REST API client secret |
| `PayPal__Mode` | `sandbox` or `live` |

**PayPal handles:**
- One-time payments via PayPal Orders API (create + capture)
- Recurring subscriptions via PayPal Subscriptions API
- Refunds via the PayPal Payments API

### Webhook Endpoints

Webhooks provide server-to-server confirmation of payment events. Configure these URLs in your Stripe and PayPal dashboards:

| Gateway | Endpoint | Events Handled |
|---------|----------|----------------|
| Stripe | `/api/store/stripe-webhook` | `payment_intent.succeeded`, `payment_intent.payment_failed`, `invoice.paid`, `customer.subscription.updated`, `customer.subscription.deleted` |
| PayPal | `/api/store/paypal-webhook` | `PAYMENT.CAPTURE.COMPLETED`, `BILLING.SUBSCRIPTION.ACTIVATED`, `BILLING.SUBSCRIPTION.CANCELLED`, `BILLING.SUBSCRIPTION.EXPIRED`, `BILLING.SUBSCRIPTION.PAYMENT.FAILED` |

---

## Store Administration

*Requires Admin or Manager role*

### Admin Dashboard

Navigate to **Store Admin** (`/StoreAdmin`) to see the dashboard overview:
- Total revenue and today's revenue
- Total orders and pending orders count
- Active subscription count
- Low-stock product alerts
- Recent orders list

### Managing Products

**Product List** (`/StoreAdmin/Products`)
- View all products with filtering by type, status, and search
- Pagination for large catalogs
- Click a product row to edit

**Creating a Product** (`/StoreAdmin/CreateProduct`)
1. Click **Create Product**
2. Fill in the form:
   - **Name** - Product name (required)
   - **SKU** - Stock keeping unit (required, unique)
   - **Product Type** - Physical, Digital, or Subscription
   - **Description** - Full product description (HTML supported)
   - **Short Description** - Brief summary for listings
   - **Price** - Regular selling price
   - **Compare At Price** - Optional original price for showing discounts
   - **Cost Price** - Optional cost for profit tracking
   - **Member Price** - Optional discounted price for active subscribers
   - **Category** - Assign to a product category
   - **Active** - Whether the product is visible in the store
   - **Featured** - Whether the product appears on the store home page
3. Type-specific fields:
   - **Physical** - Stock quantity, low-stock threshold, weight, track inventory toggle
   - **Digital** - Max downloads, download expiry days
   - **Subscription** - Billing interval, billing interval count, Stripe Price ID, PayPal Plan ID
4. Click **Create**

**Editing a Product** (`/StoreAdmin/EditProduct/{id}`)
- Update any product field
- Upload or remove product images
- Set the primary product image
- Upload or remove digital files (digital products)

**Deleting a Product**
- Click **Delete** from the product list
- Confirm the deletion

### Managing Product Images

From the **Edit Product** page:
- **Upload images** - Select one or more image files to upload
- **Set primary image** - Choose which image appears as the main product photo
- **Delete images** - Remove individual images

Product images are stored in `wwwroot/store/products/`.

### Managing Digital Files

From the **Edit Product** page (digital products only):
- **Upload digital file** - Upload the downloadable file
- **Delete digital file** - Remove the file

Digital files are stored in `ProtectedFiles/store/` (outside the web root for security).

### Managing Categories

**Category List** (`/StoreAdmin/Categories`)
- View all categories with sort ordering
- Create, edit, and delete categories
- Upload category images
- Drag to reorder categories

**Category Fields:**
- **Name** - Category display name
- **Slug** - URL-friendly identifier (auto-generated)
- **Description** - Optional description
- **Image** - Optional category image
- **Active** - Whether the category is visible
- **Sort Order** - Display order position

Category images are stored in `wwwroot/store/categories/`.

### Managing Orders

**Order List** (`/StoreAdmin/Orders`)
- View all orders with filtering by status and search
- Click a table row to view order details
- Pagination for large order histories

**Order Detail** (`/StoreAdmin/OrderDetail/{id}`)
- Full order information: customer, items, totals, payment
- Shipping address (physical orders)
- Order status management
- Tracking number entry with carrier
- Admin notes
- Refund button (Admin role only)

### Order Status Lifecycle

| Status | Description |
|--------|-------------|
| Pending | Order created, awaiting payment |
| Processing | Payment received, being prepared |
| Shipped | Physical items shipped with tracking |
| Delivered | Order delivery confirmed |
| Refunded | Payment refunded to customer |
| Cancelled | Order cancelled |

**Typical flow:** Pending → Processing → Shipped → Delivered

### Shipping and Tracking

1. Navigate to the order detail page
2. Enter the **Carrier** (e.g., USPS, UPS, FedEx)
3. Enter the **Tracking Number**
4. Click **Add Tracking**
5. The order status can then be updated to Shipped
6. An email notification is sent to the customer with tracking information

### Refunding Orders

*Requires Admin role*

1. Navigate to the order detail page
2. Click **Refund Order**
3. Optionally provide a reason
4. The refund is processed through the original payment gateway (Stripe or PayPal)
5. The order status is updated to Refunded

### Inventory Management

**Inventory Page** (`/StoreAdmin/Inventory`)
- View all physical products with current stock levels
- Low-stock alerts highlighted at the top
- Adjust stock quantities with reason tracking

**Adjusting Stock:**
1. Find the product in the inventory list
2. Enter a positive number to add stock or negative to reduce
3. Provide a reason (e.g., "Received shipment", "Damaged goods")
4. Click **Adjust**

### Sales Reports

**Sales Report** (`/StoreAdmin/SalesReport`)
- Date-range selector (defaults to last 30 days)
- Total revenue, order count, and average order value
- Revenue breakdown by payment gateway
- Order volume trends

### Managing Subscriptions

**Subscription List** (`/StoreAdmin/Subscriptions`)
- View all subscriptions with filtering by status
- Click a table row to view subscription details
- Pagination for large lists

**Subscription Detail** (`/StoreAdmin/SubscriptionDetail/{id}`)
- Subscriber information and plan details
- Current status and billing period dates
- Gateway subscription ID (Stripe or PayPal)
- Complete payment history table showing all payments (initial + renewals)
- Refund button on individual payments (Admin role only)

### Refunding Subscription Payments

*Requires Admin role*

1. Navigate to the subscription detail page
2. Find the payment in the payment history table
3. Click **Refund** on the specific payment
4. Optionally provide a reason
5. The refund is processed through the original payment gateway
6. The payment record is updated to Refunded status

---

## Store Settings

Store settings are managed via `SystemSettingsService` (key-value pairs) in the admin settings area:

| Setting Key | Default | Description |
|-------------|---------|-------------|
| `Store__FlatRateShipping` | 5.99 | Flat shipping rate for orders with physical items |
| `Store__FreeShippingThreshold` | *(empty)* | Order subtotal for free shipping; leave empty to disable |
| `Store__StoreName` | Shop | Display name for the store |
| `Store__StoreEnabled` | true | Enable or disable the store entirely |

---

## File Storage

### Storage Locations

```
/wwwroot/store/
├── products/          # Product images (publicly accessible)
│   ├── product-1.jpg
│   ├── product-2.png
│   └── ...
└── categories/        # Category images (publicly accessible)
    ├── electronics.jpg
    └── ...

/ProtectedFiles/store/ # Digital download files (NOT publicly accessible)
├── ebook-v2.pdf
├── software-installer.zip
└── ...
```

### Security

- **Product and category images** are stored in `wwwroot/store/` and served as static files
- **Digital files** are stored in `ProtectedFiles/store/` outside the web root
- Digital files are served exclusively through the `DigitalDownloadController`, which enforces authentication, ownership verification, and download limits on every request

### Backup Recommendations

To fully backup the store:
1. **Database** - Contains all product, order, subscription, and download metadata
2. **Product images** - `wwwroot/store/products/` directory
3. **Category images** - `wwwroot/store/categories/` directory
4. **Digital files** - `ProtectedFiles/store/` directory

All four components are required for a complete restore.

---

## Architecture

### Controllers

| Controller | Auth | Description |
|------------|------|-------------|
| `StoreController` | Public | Storefront browsing, product detail, search |
| `CartController` | Authorized | AJAX shopping cart operations |
| `CheckoutController` | Authorized | Checkout flow, payment processing, order confirmation |
| `OrderHistoryController` | Authorized | Customer order history, addresses, downloads |
| `SubscriptionController` | Authorized | Subscribe, manage, and cancel subscriptions |
| `DigitalDownloadController` | Authorized | Secure file downloads via token |
| `StoreAdminController` | Admin/Manager | Product, category, order, inventory, subscription, and report management |
| `StoreStripeWebhookController` | API (no auth) | Stripe webhook event handler |
| `StorePayPalWebhookController` | API (no auth) | PayPal webhook event handler |

### Services

| Interface | Implementation | Description |
|-----------|----------------|-------------|
| `IProductCatalogService` | `ProductCatalogService` | Product and category CRUD, images, digital files, inventory |
| `IShoppingCartService` | `ShoppingCartService` | Persistent cart management, stock validation |
| `IOrderService` | `OrderService` | Order lifecycle, tracking, refunds, sales reports |
| `IStorePaymentService` | `StorePaymentService` | Stripe + PayPal payment creation, capture, and refunds |
| `ISubscriptionService` | `SubscriptionService` | Subscription CRUD, payment history, refund tracking |
| `IDigitalDeliveryService` | `DigitalDeliveryService` | Download token generation with count limits and expiry |
| `IShippingAddressService` | `ShippingAddressService` | Customer shipping address CRUD |

---

## API Endpoints

### Storefront (Public)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Store` | GET | Store home page with featured products |
| `/Store/Browse` | GET | Browse products with filters and pagination |
| `/Store/Product/{slug}` | GET | Product detail page |
| `/Store/Search` | GET | Search products |

### Cart (Authenticated)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Cart` | GET | View shopping cart |
| `/Cart/Add` | POST | Add item to cart (JSON) |
| `/Cart/UpdateQuantity` | POST | Update item quantity (JSON) |
| `/Cart/Remove` | POST | Remove item from cart (JSON) |
| `/Cart/GetCartCount` | GET | Get current cart item count (JSON) |

### Checkout (Authenticated)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Checkout` | GET | Checkout page |
| `/Checkout/PlaceOrder` | POST | Create order from cart |
| `/Checkout/Payment/{orderId}` | GET | Payment page |
| `/Checkout/ConfirmStripePayment` | POST | Confirm Stripe payment (JSON) |
| `/Checkout/CreatePayPalOrder` | POST | Create PayPal order |
| `/Checkout/CapturePayPalOrder` | POST | Capture PayPal payment (JSON) |
| `/Checkout/Confirmation/{orderNumber}` | GET | Order confirmation page |

### Order History (Authenticated)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/OrderHistory` | GET | Order list |
| `/OrderHistory/Detail/{id}` | GET | Order detail |
| `/OrderHistory/Downloads` | GET | All digital downloads |
| `/OrderHistory/Addresses` | GET | Saved addresses |
| `/OrderHistory/AddAddress` | GET/POST | Add new address |
| `/OrderHistory/EditAddress/{id}` | GET/POST | Edit address |
| `/OrderHistory/DeleteAddress` | POST | Delete address |
| `/OrderHistory/SetDefaultAddress` | POST | Set default address |

### Subscriptions (Authenticated)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Subscription/Subscribe/{productId}` | GET | Subscribe to a plan |
| `/Subscription/ConfirmStripeSubscription` | POST | Confirm Stripe subscription (JSON) |
| `/Subscription/CreatePayPalSubscription` | POST | Create PayPal subscription |
| `/Subscription/Manage` | GET | Manage active subscription |
| `/Subscription/Cancel` | POST | Cancel subscription |

### Digital Downloads (Authenticated)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/download/{token}` | GET | Download a file via secure token |

### Admin (Admin/Manager)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/StoreAdmin` | GET | Admin dashboard |
| `/StoreAdmin/Products` | GET | Product list |
| `/StoreAdmin/CreateProduct` | GET/POST | Create product |
| `/StoreAdmin/EditProduct/{id}` | GET/POST | Edit product |
| `/StoreAdmin/DeleteProduct` | POST | Delete product |
| `/StoreAdmin/UploadProductImages` | POST | Upload product images |
| `/StoreAdmin/SetPrimaryImage` | POST | Set primary product image |
| `/StoreAdmin/DeleteProductImage` | POST | Delete product image |
| `/StoreAdmin/UploadDigitalFile` | POST | Upload digital file |
| `/StoreAdmin/DeleteDigitalFile` | POST | Delete digital file |
| `/StoreAdmin/Categories` | GET | Category list |
| `/StoreAdmin/CreateCategory` | POST | Create category |
| `/StoreAdmin/UpdateCategory` | POST | Update category |
| `/StoreAdmin/DeleteCategory` | POST | Delete category |
| `/StoreAdmin/UploadCategoryImage` | POST | Upload category image |
| `/StoreAdmin/UpdateCategorySortOrder` | POST | Reorder categories (JSON) |
| `/StoreAdmin/Orders` | GET | Order list |
| `/StoreAdmin/OrderDetail/{id}` | GET | Order detail |
| `/StoreAdmin/UpdateOrderStatus` | POST | Update order status |
| `/StoreAdmin/AddTracking` | POST | Add shipping tracking |
| `/StoreAdmin/UpdateAdminNotes` | POST | Update admin notes on order |
| `/StoreAdmin/RefundOrder` | POST | Refund an order (Admin only) |
| `/StoreAdmin/Inventory` | GET | Inventory management |
| `/StoreAdmin/AdjustStock` | POST | Adjust product stock |
| `/StoreAdmin/SalesReport` | GET | Sales report |
| `/StoreAdmin/Subscriptions` | GET | Subscription list |
| `/StoreAdmin/SubscriptionDetail/{id}` | GET | Subscription detail with payment history |
| `/StoreAdmin/RefundSubscriptionPayment` | POST | Refund a subscription payment (Admin only) |

### Webhooks (API, No Auth)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/store/stripe-webhook` | POST | Stripe webhook handler |
| `/api/store/paypal-webhook` | POST | PayPal webhook handler |

All POST endpoints (except webhooks and JSON APIs) require an anti-forgery token.

---

## Troubleshooting

### Products Not Appearing in Store

- Verify the product's **IsActive** flag is set to true
- Check that the product's category is active
- Verify `Store__StoreEnabled` is set to true in system settings

### Payment Not Processing

**Stripe:**
- Verify `Stripe__SecretKey` and `Stripe__PublishableKey` are configured in encrypted credentials
- Check Stripe dashboard for error logs
- Ensure the webhook endpoint is registered and `Stripe__WebhookSecret` is set

**PayPal:**
- Verify `PayPal__ClientId` and `PayPal__ClientSecret` are configured
- Check `PayPal__Mode` matches your environment (sandbox vs. live)
- Review PayPal developer dashboard for webhook delivery status

### Downloads Not Working

- Verify the digital file exists in `ProtectedFiles/store/`
- Check that the download record was created (payment confirmation triggers this)
- Confirm the download count has not been exceeded
- Confirm the download link has not expired
- Ensure the user is authenticated and owns the download

### Subscription Not Activating

- Check the webhook endpoint is properly configured at the payment gateway
- Review application logs for webhook processing errors
- Verify the `StripePriceId` or `PayPalPlanId` on the product matches the gateway configuration
- For Stripe, confirm the webhook secret matches the one in your Stripe dashboard

### Inventory Warnings

- Low-stock alerts appear on the admin dashboard when stock falls below the product's `LowStockThreshold`
- Use the **Inventory** page to adjust stock levels with tracked reasons
- Out-of-stock items are flagged in the cart and cannot proceed to checkout

### Webhook Events Not Received

- Verify the webhook URL is publicly accessible (not blocked by firewall or authentication)
- Check that the correct events are selected in the gateway dashboard
- Review application logs for signature verification failures
- For Stripe, ensure the webhook signing secret is current

---

## Related Guides

- **Administrator Guide** - Overview of all admin features
- **Security Guide** - Security features and best practices
- **Database Schema** - Complete database table documentation
- **Getting Started** - Initial setup and configuration

---

**Version:** 2.0.0
**Framework:** Ape Framework
**Site:** https://Illustrate.net
