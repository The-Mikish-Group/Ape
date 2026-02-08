# Ape Framework

A production-ready ASP.NET Core MVC framework with comprehensive authentication, role-based access control, document management, and administrative tools. This framework provides a solid foundation for building secure, enterprise-grade web applications.

## Quick Start Feature Matrix

| Feature | Status | Access Level | Key Files |
|---------|--------|--------------|-----------|
| User Authentication | ✅ | Public | `Areas/Identity/` |
| Role Management | ✅ | Admin | ASP.NET Identity |
| Two-Factor Auth | ✅ | All Users | `Manage/EnableAuthenticator` |
| Document Library | ✅ | Member/Admin | `DocumentController`, `IDocumentManagementService` |
| Photo Gallery | ✅ | Member/Admin | `GalleryController`, `IGalleryManagementService` |
| Links Directory | ✅ | Public/Admin | `LinksController`, `ILinksManagementService` |
| Credential Vault | ✅ | Admin | `SystemCredentialsController`, `CredentialEncryptionService` |
| Email (Dual Provider) | ✅ | System | `EnhancedEmailService` |
| Contact Form | ✅ | Public | `InfoController` |
| Health Checks | ✅ | Public | `GET /health`, `/health/live`, `/health/ready` |
| Activity Tracking | ✅ | Admin | `ActivityTrackingMiddleware` |
| Online Store | ✅ | Public/Member/Admin | `StoreController`, `IProductCatalogService` |
| Shopping Cart | ✅ | Member | `CartController`, `IShoppingCartService` |
| Checkout & Payments | ✅ | Member | `CheckoutController`, `IStorePaymentService` |
| Order Management | ✅ | Admin/Member | `StoreAdminController`, `IOrderService` |
| Digital Downloads | ✅ | Member | `DigitalDownloadController`, `IDigitalDeliveryService` |
| Subscriptions | ✅ | Member | `SubscriptionController`, `ISubscriptionService` |

## Live Demo

**[https://Illustrate.net](https://Illustrate.net)**

## Features

### Authentication & Authorization
- **ASP.NET Core Identity** with full user management
- **Role-Based Access Control** (Admin, Manager, Member roles pre-configured)
- **Two-Factor Authentication (2FA)** with authenticator app support
- **Email Verification** for new accounts
- **Password Reset** via email
- **Recovery Codes** for 2FA backup
- **User Profile Management** with extended fields (address, phone, dates)
- **Activity Tracking** - monitors user presence and last activity

### Email System
- **Dual-Provider Architecture**: Azure Communication Services (primary) with SMTP fallback
- **Automatic Failover**: Tracks consecutive failures and gracefully falls back to SMTP
- **Email Logging**: All sent emails logged with status, provider used, and error details
- **Attachment Support**: Send emails with file attachments via both providers
- **Admin Dashboard**: View, search, and filter email logs; clear old entries

### Contact Form
- **Configurable Recipients**: Admin-editable list of email recipients
- **Anti-Spam Protection**: Triple-layer protection (honeypot fields, JavaScript token, timing validation)
- **HTML Email Templates**: Professional formatting for contact submissions
- **Spam Attempt Logging**: Records blocked attempts with IP addresses

### Document Library
- **Hierarchical Organization**: Nested folders with unlimited depth
- **Access Control**: Member-level and Admin-level document categories
- **PDF Management**: Upload, view, download, rename, move, and delete PDF files
- **Folder Management**: Create, rename, move, delete, and reorder folders
- **Breadcrumb Navigation**: Easy navigation through folder hierarchy
- **Sortable Content**: Drag-and-drop reordering support

### Photo Gallery
- **Category System**: Nested categories with unlimited depth
- **Access Control**: Member-level and Admin-level gallery categories
- **Image Management**: Upload, rename, move, delete images
- **Automatic Thumbnails**: Generated on upload for optimized display
- **Batch Upload**: Upload multiple images at once
- **Image Optimization**: Using SixLabors.ImageSharp

### Links Directory
- **Category Organization**: Group links into categories
- **Access Control**: Public and Admin-only link categories
- **CRUD Operations**: Full management of categories and links
- **Sortable Display**: Customizable display order

### System Credentials Management
- **Secure Storage**: AES-256 encryption for all credentials
- **Categorized Organization**: Database, API, Email, Billing, Site categories
- **Full Audit Trail**: Every access logged (view, create, update, delete, test)
- **Built-in Testing**: Test database connections and API keys directly
- **Environment Variable Fallback**: Supports legacy configurations

### Administrative Tools
- **Active Users Dashboard**: Real-time view of user activity status
- **Email Test Console**: Test Azure and SMTP email configurations
- **Email Log Viewer**: Search, filter, and manage sent email records
- **System Settings**: Key-value configuration storage

### Online Store / E-Commerce
- **Product Catalog**: Physical, digital, and subscription product types
- **Category Management**: Hierarchical product categories with images
- **Product Images**: Multiple images per product with primary image selection
- **Digital Products**: Secure file storage and token-based download delivery
- **Inventory Tracking**: Stock management with low-stock alerts for physical products
- **Shopping Cart**: Persistent cart with stock validation and free shipping thresholds
- **Checkout Flow**: Smart checkout (skips shipping for digital-only orders)
- **Payment Processing**: Stripe (card payments) and PayPal integration
- **Subscription Billing**: Recurring payments via Stripe Subscriptions and PayPal
- **Subscription Payment History**: Admin view of all subscription payments (initial + renewals) with refund capability
- **Member Pricing**: Discounted prices for active subscribers
- **Order Management**: Full order lifecycle (pending → processing → shipped → delivered)
- **Shipping & Tracking**: Carrier and tracking number management with email notifications
- **Digital Delivery**: Secure download links with count limits and expiry dates
- **Refunds**: Admin-initiated refunds via Stripe and PayPal
- **Sales Reports**: Date-range reporting with revenue breakdowns by product type
- **Email Notifications**: Order confirmation, shipping updates sent automatically
- **Webhook Support**: Stripe and PayPal webhooks for payment event handling

### UI/UX
- **Responsive Layout**: Mobile-friendly design
- **Partial Header/Footer**: Modular layout with sticky footer
- **Menu System**: Role-aware navigation
- **Bootstrap Styling**: Clean, professional appearance

## Technology Stack

- **Framework**: ASP.NET Core 10.0 (MVC + Razor Pages)
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Email**: Azure Communication Services + SMTP
- **Image Processing**: SixLabors.ImageSharp
- **Payments**: Stripe.net (card + subscriptions), PayPal REST API
- **Frontend**: Bootstrap, jQuery

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- SQL Server (LocalDB, Docker, or cloud-hosted)
- Azure Communication Services account (optional, for Azure email)
- SMTP server access (for fallback/primary email)

## Cross-Platform Development

This project runs on **Windows**, **macOS**, and **Linux**. The .NET 10.0 SDK and ASP.NET Core are fully cross-platform.

### Platform Support Matrix

| Component | Windows | macOS | Linux |
|-----------|---------|-------|-------|
| .NET 10.0 SDK | ✅ | ✅ | ✅ |
| ASP.NET Core | ✅ | ✅ | ✅ |
| Entity Framework Core | ✅ | ✅ | ✅ |
| Visual Studio | ✅ | ✅ (for Mac) | ❌ |
| VS Code + C# Dev Kit | ✅ | ✅ | ✅ |
| SQL Server LocalDB | ✅ | ❌ | ❌ |
| SQL Server (Docker) | ✅ | ✅ | ✅ |

### Install .NET SDK

#### Windows
Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download) or use winget:
```bash
winget install Microsoft.DotNet.SDK.10
```

#### macOS
```bash
brew install dotnet
```

#### Linux (Ubuntu/Debian)
```bash
sudo apt update
sudo apt install dotnet-sdk-10.0
```

#### Linux (Fedora)
```bash
sudo dnf install dotnet-sdk-10.0
```

### Database Setup for Mac/Linux

Since SQL Server LocalDB is Windows-only, use Docker to run SQL Server locally:

```bash
# Pull and run SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Pass123" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest

# Verify it's running
docker ps
```

Then set your environment variables to connect:
```bash
export DB_SERVER_ILLUSTRATE=localhost
export DB_NAME_ILLUSTRATE=aspnet-Ape
export DB_USER_ILLUSTRATE=sa
export DB_PASSWORD_ILLUSTRATE=YourStrong!Pass123
export MASTER_CREDENTIAL_KEY_ILLUSTRATE=YourSecure32CharacterOrLongerKey!
```

#### Alternative: Azure SQL Database
For cloud-hosted development, create an Azure SQL Database and use those connection details in your environment variables.

### VS Code Setup (All Platforms)

1. Install [VS Code](https://code.visualstudio.com/)
2. Install the **C# Dev Kit** extension (includes C# extension)
3. Open the project folder
4. Press `F5` to debug or use terminal:
   ```bash
   cd Ape
   dotnet run
   ```

### Quick Start (Mac/Linux)

```bash
# Clone repository
git clone https://github.com/MikishVaughn/Ape.git
cd Ape

# Start SQL Server in Docker (if not using cloud DB)
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Pass123" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

# Set environment variables
export DB_SERVER_ILLUSTRATE=localhost
export DB_NAME_ILLUSTRATE=aspnet-Ape
export DB_USER_ILLUSTRATE=sa
export DB_PASSWORD_ILLUSTRATE=YourStrong!Pass123
export MASTER_CREDENTIAL_KEY_ILLUSTRATE=$(openssl rand -base64 32)

# Run the application
cd Ape
dotnet run
```

The application will automatically create the database and apply migrations on first run.

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/MikishVaughn/Ape.git
cd Ape
```

### 2. Set Environment Variables

#### Development (Windows)
Set these in your system environment variables or use `launchSettings.json`:

```
MASTER_CREDENTIAL_KEY_ILLUSTRATE=YourSecure32CharacterOrLongerKey!
```

#### Development (User Secrets - Recommended)
```bash
cd Ape
dotnet user-secrets set "MASTER_CREDENTIAL_KEY_ILLUSTRATE" "YourSecure32CharacterOrLongerKey!"
```

#### Production
Set these at the IIS Application Pool level or in your hosting environment:

```
DB_SERVER_ILLUSTRATE=your-database-server
DB_NAME_ILLUSTRATE=your-database-name
DB_USER_ILLUSTRATE=your-database-user
DB_PASSWORD_ILLUSTRATE=your-database-password
MASTER_CREDENTIAL_KEY_ILLUSTRATE=YourSecure32CharacterOrLongerKey!
```

> **Important**: The master credential key must be at least 32 characters long and should be kept secure. This key encrypts all stored credentials.

### 3. Configure the Database Connection

#### Development
The default connection string in `appsettings.json` uses LocalDB:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=aspnet-Ape;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

#### Production
Set the environment variables listed above. The application will construct the connection string automatically.

### 4. Run Database Migrations

```bash
cd Ape
dotnet ef database update
```

Or let the application auto-migrate on first run (configured by default).

### 5. Build and Run

```bash
dotnet build
dotnet run
```

Navigate to `https://localhost:5001` or the URL shown in the console.

### 6. Initial Admin Account

On first run, if no admin users exist, the system creates a default admin account:
- **Email**: admin@admin.com
- **Password**: Admin123!

> **Important**: Change this password immediately after first login!

You can also specify custom admin credentials in `appsettings.json`:
```json
{
  "AdminEmail": "your-admin@example.com",
  "AdminPassword": "YourSecurePassword123!"
}
```

## Configuration

### Email Configuration

After logging in as Admin, navigate to **System Credentials** to configure email settings:

#### Azure Communication Services (Primary)
| Credential Key | Description |
|----------------|-------------|
| `AZURE_COMMUNICATION_CONNECTION_STRING` | Your Azure Communication Services connection string |
| `AZURE_EMAIL_FROM` | The verified sender email address |

#### SMTP (Fallback)
| Credential Key | Description |
|----------------|-------------|
| `SMTP_SERVER` | SMTP server address (e.g., smtp.gmail.com) |
| `SMTP_PORT` | SMTP port (typically 587 for TLS) |
| `SMTP_USERNAME` | SMTP authentication username |
| `SMTP_PASSWORD` | SMTP authentication password |
| `SMTP_SSL` | Enable SSL/TLS (true/false) |

#### Site Settings
| Credential Key | Description |
|----------------|-------------|
| `SITE_NAME` | Your site name (used in email templates) |

#### Stripe Payment Processing
| Credential Key | Description |
|----------------|-------------|
| `Stripe__SecretKey` | Stripe secret API key |
| `Stripe__PublishableKey` | Stripe publishable key (client-side) |
| `Stripe__WebhookSecret` | Stripe webhook endpoint secret |

#### PayPal Payment Processing
| Credential Key | Description |
|----------------|-------------|
| `PayPal__ClientId` | PayPal REST API client ID |
| `PayPal__ClientSecret` | PayPal REST API client secret |
| `PayPal__Mode` | `sandbox` or `live` |
| `PayPal__WebhookId` | PayPal webhook ID for verification |

#### Store Settings (System Settings)
| Setting Key | Default | Description |
|-------------|---------|-------------|
| `Store__FlatRateShipping` | `5.99` | Flat shipping rate for physical items |
| `Store__FreeShippingThreshold` | *(empty)* | Order total for free shipping (empty = disabled) |
| `Store__StoreName` | `Shop` | Store display name |
| `Store__StoreEnabled` | `true` | Enable/disable the storefront |

### Contact Form Recipients

1. Navigate to **Admin > Contact Form Settings**
2. Enter email addresses separated by commas
3. Save changes

## Usage Guide

### For Administrators

#### Managing Users
- User management is handled through ASP.NET Core Identity
- Users can be assigned to Admin, Manager, or Member roles
- **Manager role** has the same access as Admin except for System Credentials
- View active users in the **Active Users** dashboard

#### Document Library
1. Navigate to **Documents** (Admin view)
2. Create categories using **New Folder**
3. Set **Access Level** (Member or Admin)
4. Upload PDFs to categories
5. Organize with drag-and-drop reordering

#### Photo Gallery
1. Navigate to **Gallery** (Admin view)
2. Create categories for organization
3. Set **Access Level** as needed
4. Upload images (thumbnails generated automatically)
5. Add descriptions and reorder as needed

#### Links Management
1. Navigate to **More Links** (Admin view)
2. Create categories for grouping
3. Add links with names and URLs
4. Set visibility (public or admin-only)

#### System Credentials
1. Navigate to **System Credentials**
2. Add new credentials with appropriate category
3. Values are encrypted before storage
4. Use **Test** buttons to verify configurations
5. View audit history for security tracking

### For Members

- **Documents**: Browse and download documents in Member-accessible categories
- **Gallery**: View images in Member-accessible categories
- **Links**: Access public link categories
- **Profile**: Update personal information, enable 2FA

## Project Structure

```
Ape/
├── Areas/
│   └── Identity/Pages/Account/    # Identity Razor Pages (login, register, 2FA)
├── Controllers/
│   ├── InfoController.cs          # Public pages, contact form
│   ├── DocumentController.cs      # Document library management
│   ├── GalleryController.cs       # Photo gallery management
│   ├── LinksController.cs         # Links directory
│   ├── SystemCredentialsController.cs  # Credential management
│   ├── EmailLogController.cs      # Email log viewer
│   ├── EmailTestController.cs     # Email testing tools
│   ├── ContactFormSettingsController.cs  # Contact recipients
│   ├── ActiveUsersController.cs   # User activity dashboard
│   ├── HealthController.cs        # Health check endpoints
│   ├── StoreController.cs         # Public storefront browsing
│   ├── CartController.cs          # Shopping cart (AJAX)
│   ├── CheckoutController.cs      # Checkout + payment flow
│   ├── OrderHistoryController.cs  # Customer orders + addresses
│   ├── SubscriptionController.cs  # Subscription management
│   ├── DigitalDownloadController.cs    # Secure file downloads
│   ├── StoreAdminController.cs    # Admin: products, orders, reports
│   ├── StoreStripeWebhookController.cs # Stripe webhook handler
│   └── StorePayPalWebhookController.cs # PayPal webhook handler
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core context
│   └── Migrations/                # Database migrations
├── Models/
│   ├── PDFCategory.cs             # Document categories
│   ├── CategoryFile.cs            # Document files
│   ├── GalleryCategory.cs         # Gallery categories
│   ├── GalleryImage.cs            # Gallery images
│   ├── LinkCategory.cs            # Link categories
│   ├── CategoryLink.cs            # Links
│   ├── SystemCredential.cs        # Encrypted credentials
│   ├── CredentialAuditLog.cs      # Credential access log
│   ├── EmailLog.cs                # Email sending log
│   ├── SystemSetting.cs           # System settings
│   ├── UserProfiles.cs            # Extended user data
│   ├── StoreCategory.cs           # Store product categories
│   ├── Product.cs                 # Products (physical/digital/subscription)
│   ├── ProductImage.cs            # Product images
│   ├── DigitalProductFile.cs      # Downloadable files
│   ├── ShoppingCart.cs            # Shopping carts
│   ├── ShoppingCartItem.cs        # Cart line items
│   ├── Order.cs                   # Customer orders
│   ├── OrderItem.cs               # Order line items
│   ├── CustomerDownload.cs        # Digital download tokens
│   ├── Subscription.cs            # Recurring subscriptions
│   ├── SubscriptionPayment.cs     # Subscription payment history
│   ├── CustomerPaymentMethod.cs   # Payment method storage
│   ├── ShippingAddress.cs         # Customer addresses
│   ├── PaymentResult.cs           # Payment result DTO
│   ├── PayPal/                    # PayPal API models
│   └── ViewModels/                # View-specific models
├── Services/
│   ├── CredentialEncryptionService.cs   # AES-256 encryption
│   ├── SecureConfigurationService.cs    # Credential retrieval
│   ├── EnhancedEmailService.cs          # Email sending (Azure + SMTP)
│   ├── DocumentManagementService.cs     # Document operations
│   ├── GalleryManagementService.cs      # Gallery operations
│   ├── LinksManagementService.cs        # Links directory operations
│   ├── ImageOptimizationService.cs      # Image processing
│   ├── SystemSettingsService.cs         # Settings management
│   ├── ProductCatalogService.cs         # Product/category CRUD
│   ├── ShoppingCartService.cs           # Cart operations
│   ├── OrderService.cs                  # Order lifecycle
│   ├── ShippingAddressService.cs        # Address management
│   ├── StorePaymentService.cs           # Stripe + PayPal payments
│   ├── DigitalDeliveryService.cs        # Download token management
│   ├── SubscriptionService.cs           # Subscription management
│   └── PayPalApiClient.cs              # PayPal REST API client
├── Middleware/
│   └── ActivityTrackingMiddleware.cs    # User activity tracking
├── Views/                         # Razor views
├── wwwroot/
│   ├── css/                       # Stylesheets (includes store.css)
│   ├── js/                        # JavaScript (includes store.js)
│   ├── Galleries/                 # Uploaded gallery images
│   ├── Images/                    # Site images
│   └── store/                     # Product and category images
├── ProtectedFiles/
│   ├── PDFs/                      # Uploaded PDF documents
│   └── store/                     # Digital product files (secure)
├── Program.cs                     # Application startup
└── appsettings.json              # Configuration
```

## Database Schema

### Identity Tables
- `AspNetUsers` - User accounts
- `AspNetRoles` - Roles (Admin, Manager, Member)
- `AspNetUserRoles` - User-role assignments
- `AspNetUserClaims`, `AspNetRoleClaims` - Claims
- `AspNetUserLogins`, `AspNetUserTokens` - External logins, tokens

### Application Tables
- `UserProfiles` - Extended user information
- `SystemCredentials` - Encrypted credentials
- `CredentialAuditLogs` - Credential access history
- `EmailLogs` - Email sending history
- `SystemSettings` - Key-value settings
- `PDFCategories` - Document folders
- `CategoryFiles` - Document files
- `GalleryCategories` - Image categories
- `GalleryImages` - Gallery images
- `LinkCategories` - Link groups
- `CategoryLinks` - Individual links

### Store Tables
- `StoreCategories` - Hierarchical product categories
- `Products` - Products (Physical, Digital, Subscription types)
- `ProductImages` - Multiple images per product
- `DigitalProductFiles` - Downloadable files (stored outside wwwroot)
- `ShippingAddresses` - Customer shipping addresses
- `ShoppingCarts` - Persistent shopping carts
- `ShoppingCartItems` - Cart line items
- `Orders` - Purchase records with shipping/payment/refund info
- `OrderItems` - Order line items (product snapshot)
- `CustomerDownloads` - Token-based digital download access
- `Subscriptions` - Recurring subscriptions (Stripe/PayPal)
- `SubscriptionPayments` - Payment history for subscription charges and refunds
- `CustomerPaymentMethods` - Stored payment gateway customer IDs

## Security Considerations

### Encryption
- All credentials stored with AES-256 encryption
- Master key stored in environment variables (not in code)
- IV randomly generated for each encryption

### Authentication
- Email confirmation required for new accounts
- Two-factor authentication available
- Account lockout after failed attempts
- Secure password requirements enforced

### Authorization
- Role-based access control throughout
- Admin-only areas protected
- Document/Gallery access levels enforced

### Audit Trail
- All credential access logged
- Email sending logged
- User activity tracked

### Data Protection
- CSRF protection on all forms
- Input validation and sanitization
- Parameterized database queries (EF Core)

## Deployment

### IIS Deployment

1. Publish the application:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. Create an IIS website pointing to the publish folder

3. Set Application Pool to "No Managed Code"

4. Configure environment variables at the Application Pool level:
   - Open IIS Manager
   - Select the Application Pool
   - Click "Advanced Settings"
   - Under "Environment Variables" add your keys

5. Ensure the application has write access to:
   - `/ProtectedFiles/` (documents)
   - `/wwwroot/Galleries/` (images)

### Azure App Service

1. Create an Azure App Service

2. Configure Application Settings:
   - `DB_SERVER_ILLUSTRATE`
   - `DB_NAME_ILLUSTRATE`
   - `DB_USER_ILLUSTRATE`
   - `DB_PASSWORD_ILLUSTRATE`
   - `MASTER_CREDENTIAL_KEY_ILLUSTRATE`

3. Deploy via Visual Studio, GitHub Actions, or Azure CLI

## Migration Commands

```bash
# Add a new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script
```

## Health Check Endpoints

The application includes health check endpoints for monitoring and container orchestration:

### `GET /health`
Returns comprehensive health status including database, encryption, and email configuration.

```json
{
  "status": "Healthy",
  "timestamp": "2026-01-28T12:00:00Z",
  "version": "1.0.0",
  "database": { "name": "Database", "status": "Healthy", "message": "Connected. 5 users in database." },
  "encryption": { "name": "Encryption", "status": "Healthy", "message": "Master key configured and encryption working." },
  "email": { "name": "Email", "status": "Healthy", "message": "Both SMTP and Azure Email configured (dual-provider)." }
}
```

### `GET /health/live`
Kubernetes liveness probe - returns 200 if the application is running.

### `GET /health/ready`
Kubernetes readiness probe - returns 200 if database is connected and ready to serve traffic.

## Environment Variables Checklist

Use this checklist when deploying to a new environment:

### Required
- [ ] `MASTER_CREDENTIAL_KEY_ILLUSTRATE` - 32+ character encryption master key

### Database (Production)
- [ ] `DB_SERVER_ILLUSTRATE` - SQL Server hostname
- [ ] `DB_NAME_ILLUSTRATE` - Database name
- [ ] `DB_USER_ILLUSTRATE` - Database user
- [ ] `DB_PASSWORD_ILLUSTRATE` - Database password

### Optional (Configured via System Credentials UI)
- [ ] Azure Email connection string
- [ ] SMTP server settings
- [ ] Site name for email templates

### Generate a New Master Key

You can generate a cryptographically secure master key using PowerShell:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Or in C#:
```csharp
var key = Ape.Services.CredentialEncryptionService.GenerateNewMasterKey();
Console.WriteLine(key);
```

> **Warning**: Changing the master key after credentials are stored will make them unreadable. Export credentials before key rotation.

## Troubleshooting

### Email Not Sending
1. Check **Email Test** page to verify configuration
2. Review **Email Logs** for error messages
3. Verify credentials in **System Credentials**
4. Ensure firewall allows SMTP port (typically 587)

### Database Connection Failed
1. Verify environment variables are set correctly
2. Test connection in **System Credentials** page
3. Check SQL Server is running and accessible
4. Verify firewall rules for SQL Server port (1433)

### 2FA Not Working
1. Ensure server time is synchronized (TOTP is time-based)
2. Have user regenerate authenticator setup
3. Use recovery codes if authenticator is lost

### Files Not Uploading
1. Check folder permissions on `/ProtectedFiles/` and `/wwwroot/Galleries/`
2. Verify file size limits in web.config/IIS settings
3. Ensure correct file types (PDF for documents, images for gallery)

## Building Your Own Features

This framework follows consistent patterns. Use this guide when adding new functionality.

### Pattern: Service Layer Architecture

All features follow the Controller → Service → DbContext pattern:

```
┌─────────────┐     ┌──────────────────────┐     ┌─────────────────────┐
│ Controller  │────▶│ IFeatureService      │────▶│ ApplicationDbContext│
│ (HTTP/Auth) │     │ (Business Logic)     │     │ (Data Access)       │
└─────────────┘     └──────────────────────┘     └─────────────────────┘
```

### Step 1: Create the Entity Model

```csharp
// Models/MyEntity.cs
public class MyEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public int SortOrder { get; set; }
}
```

### Step 2: Add to DbContext

```csharp
// Data/ApplicationDbContext.cs
public DbSet<MyEntity> MyEntities { get; set; }
```

### Step 3: Create ViewModels

```csharp
// Models/ViewModels/MyFeatureViewModels.cs
public class MyEntityViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class MyEntityOperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static MyEntityOperationResult Succeeded(string? message = null)
        => new() { Success = true, Message = message };

    public static MyEntityOperationResult Failed(string message)
        => new() { Success = false, Message = message };
}
```

### Step 4: Create Service Interface & Implementation

```csharp
// Services/IMyFeatureService.cs
public interface IMyFeatureService
{
    Task<List<MyEntityViewModel>> GetAllAsync();
    Task<MyEntityOperationResult> CreateAsync(string name);
}

// Services/MyFeatureService.cs
public class MyFeatureService(
    ApplicationDbContext context,
    ILogger<MyFeatureService> logger) : IMyFeatureService
{
    // Implementation using primary constructor pattern
}
```

### Step 5: Register in Program.cs

```csharp
builder.Services.AddScoped<IMyFeatureService, MyFeatureService>();
```

### Step 6: Create Controller

```csharp
// Controllers/MyFeatureController.cs
public class MyFeatureController(
    IMyFeatureService service,
    ILogger<MyFeatureController> logger) : Controller
{
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Index()
    {
        var items = await service.GetAllAsync();
        return View(items);
    }
}
```

### Step 7: Generate Migration

```bash
dotnet ef migrations add AddMyEntity
dotnet ef database update
```

### Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Entity | Singular noun | `GalleryImage` |
| DbSet | Plural | `GalleryImages` |
| Service Interface | `IFeatureManagementService` | `IGalleryManagementService` |
| Service Class | `FeatureManagementService` | `GalleryManagementService` |
| Controller | `FeatureController` | `GalleryController` |
| ViewModels | `FeatureViewModels.cs` | `GalleryViewModels.cs` |
| Operation Result | `FeatureOperationResult` | `GalleryImageOperationResult` |

### Authorization Patterns

```csharp
// Controller-level (all actions)
[Authorize(Roles = "Admin")]
public class AdminOnlyController : Controller { }

// Action-level
[Authorize(Roles = "Admin,Manager")]
public IActionResult ManageItems() { }

// Public access
[AllowAnonymous]
public IActionResult PublicPage() { }

// Check in views
@if (User.IsInRole("Admin") || User.IsInRole("Manager"))
{
    <a href="...">Manage</a>
}
```

## Documentation

Detailed guides are available in the [`Docs/`](Docs/) folder:

| Guide | Description |
|-------|-------------|
| [01 - Getting Started](Docs/01-Getting-Started.md) | Prerequisites, installation, first run |
| [02 - Administrator Guide](Docs/02-Administrator-Guide.md) | Admin tools, user management, store admin |
| [03 - Email Configuration](Docs/03-Email-Configuration-Guide.md) | Azure + SMTP email setup |
| [04 - Document Library](Docs/04-Document-Library-Guide.md) | PDF document management |
| [05 - Security Guide](Docs/05-Security-Guide.md) | Authentication, encryption, access control |
| [06 - Deployment Guide](Docs/06-Deployment-Guide.md) | IIS and Azure deployment |
| [07 - Contact Form](Docs/07-Contact-Form-Guide.md) | Contact form and spam protection |
| [08 - Directory Structure](Docs/08-Directory-Structure-Guide.md) | Project layout and file reference |
| [09 - Database Schema](Docs/09-Database-Schema.md) | Complete SQL schema with store tables |
| [10 - Photo Gallery](Docs/10-Photo-Gallery-Guide.md) | Image gallery management |
| [11 - Store Guide](Docs/11-Store-Guide.md) | E-commerce: products, orders, subscriptions |
| [12 - Payment Setup](Docs/12-Payment-Setup-Guide.md) | Stripe and PayPal configuration |

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Azure Communication Services](https://azure.microsoft.com/services/communication-services)
- [SixLabors.ImageSharp](https://sixlabors.com/products/imagesharp)
- [Bootstrap](https://getbootstrap.com)

---

**Version**: 2.0.0
**Last Updated**: February 2026
