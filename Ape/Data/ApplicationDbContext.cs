using Ape.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ape.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<UserProfiles> UserProfiles { get; set; }

        // System Credentials Management DbSets
        public DbSet<SystemCredential> SystemCredentials { get; set; }
        public DbSet<CredentialAuditLog> CredentialAuditLogs { get; set; }

        // Email Logging
        public DbSet<EmailLog> EmailLogs { get; set; }

        // System Settings
        public DbSet<SystemSetting> SystemSettings { get; set; }

        // Document Library
        public DbSet<PDFCategory> PDFCategories { get; set; }
        public DbSet<CategoryFile> CategoryFiles { get; set; }

        // Image Gallery
        public DbSet<GalleryCategory> GalleryCategories { get; set; }
        public DbSet<GalleryImage> GalleryImages { get; set; }

        // More Links
        public DbSet<LinkCategory> LinkCategories { get; set; }
        public DbSet<CategoryLink> CategoryLinks { get; set; }

        // Store: Product Catalog
        public DbSet<StoreCategory> StoreCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<DigitalProductFile> DigitalProductFiles { get; set; }

        // Store: Shopping
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
        public DbSet<ShippingAddress> ShippingAddresses { get; set; }

        // Store: Orders
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CustomerDownload> CustomerDownloads { get; set; }

        // Store: Subscriptions & Payments
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }
        public DbSet<CustomerPaymentMethod> CustomerPaymentMethods { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // PDFCategory configuration
            modelBuilder.Entity<PDFCategory>(entity =>
            {
                entity.HasKey(e => e.CategoryID);
                entity.Property(e => e.CategoryID).ValueGeneratedOnAdd();

                // Self-referential FK for hierarchical folders
                entity.HasOne(e => e.ParentCategory)
                    .WithMany(e => e.ChildCategories)
                    .HasForeignKey(e => e.ParentCategoryID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ParentCategoryID);
            });

            // GalleryCategory configuration
            modelBuilder.Entity<GalleryCategory>(entity =>
            {
                entity.HasKey(e => e.CategoryID);
                entity.Property(e => e.CategoryID).ValueGeneratedOnAdd();

                // Self-referential FK for hierarchical categories
                entity.HasOne(e => e.ParentCategory)
                    .WithMany(e => e.ChildCategories)
                    .HasForeignKey(e => e.ParentCategoryID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ParentCategoryID);
            });

            // GalleryImage configuration
            modelBuilder.Entity<GalleryImage>(entity =>
            {
                entity.HasKey(e => e.ImageID);
                entity.Property(e => e.ImageID).ValueGeneratedOnAdd();
            });

            // ============================================================
            // Store: StoreCategory
            // ============================================================
            modelBuilder.Entity<StoreCategory>(entity =>
            {
                entity.HasKey(e => e.CategoryID);
                entity.Property(e => e.CategoryID).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.Slug).IsUnique().HasFilter("[IsActive] = 1");
                entity.HasIndex(e => e.ParentCategoryID);

                entity.HasOne(e => e.ParentCategory)
                    .WithMany(e => e.ChildCategories)
                    .HasForeignKey(e => e.ParentCategoryID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // Store: Product
            // ============================================================
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductID);
                entity.Property(e => e.ProductID).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.Slug).IsUnique().HasFilter("[IsActive] = 1");
                entity.HasIndex(e => e.SKU).IsUnique().HasFilter("[IsActive] = 1");
                entity.HasIndex(e => e.CategoryID);
                entity.HasIndex(e => e.ProductType);

                entity.HasOne(e => e.Category)
                    .WithMany(e => e.Products)
                    .HasForeignKey(e => e.CategoryID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ============================================================
            // Store: ProductImage
            // ============================================================
            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.HasKey(e => e.ImageID);
                entity.Property(e => e.ImageID).ValueGeneratedOnAdd();

                entity.HasOne(e => e.Product)
                    .WithMany(e => e.Images)
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // Store: DigitalProductFile
            // ============================================================
            modelBuilder.Entity<DigitalProductFile>(entity =>
            {
                entity.HasKey(e => e.FileID);
                entity.Property(e => e.FileID).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.ProductID);

                entity.HasOne(e => e.Product)
                    .WithMany(e => e.DigitalFiles)
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // Store: ShoppingCart
            // ============================================================
            modelBuilder.Entity<ShoppingCart>(entity =>
            {
                entity.HasKey(e => e.CartID);
                entity.Property(e => e.CartID).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.UserId);
            });

            // ============================================================
            // Store: ShoppingCartItem
            // ============================================================
            modelBuilder.Entity<ShoppingCartItem>(entity =>
            {
                entity.HasKey(e => e.CartItemID);
                entity.Property(e => e.CartItemID).ValueGeneratedOnAdd();

                entity.HasOne(e => e.Cart)
                    .WithMany(e => e.Items)
                    .HasForeignKey(e => e.CartID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // Store: ShippingAddress
            // ============================================================
            modelBuilder.Entity<ShippingAddress>(entity =>
            {
                entity.HasKey(e => e.AddressID);
                entity.Property(e => e.AddressID).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.UserId);
            });

            // ============================================================
            // Store: Order
            // ============================================================
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderID);
                entity.Property(e => e.OrderID).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
            });

            // ============================================================
            // Store: OrderItem
            // ============================================================
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemID);
                entity.Property(e => e.OrderItemID).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.ProductID);

                entity.HasOne(e => e.Order)
                    .WithMany(e => e.Items)
                    .HasForeignKey(e => e.OrderID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // Store: CustomerDownload
            // ============================================================
            modelBuilder.Entity<CustomerDownload>(entity =>
            {
                entity.HasKey(e => e.DownloadID);
                entity.Property(e => e.DownloadID).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.DownloadToken).IsUnique();
                entity.HasIndex(e => e.UserId);

                entity.HasOne(e => e.OrderItem)
                    .WithMany()
                    .HasForeignKey(e => e.OrderItemID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.DigitalFile)
                    .WithMany()
                    .HasForeignKey(e => e.DigitalFileID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // Store: Subscription
            // ============================================================
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasKey(e => e.SubscriptionID);
                entity.Property(e => e.SubscriptionID).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.StripeSubscriptionId);
                entity.HasIndex(e => e.PayPalSubscriptionId);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // Store: SubscriptionPayment
            // ============================================================
            modelBuilder.Entity<SubscriptionPayment>(entity =>
            {
                entity.HasKey(e => e.PaymentID);
                entity.Property(e => e.PaymentID).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.SubscriptionID);
                entity.HasIndex(e => e.TransactionId);

                entity.HasOne(e => e.Subscription)
                    .WithMany()
                    .HasForeignKey(e => e.SubscriptionID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // Store: CustomerPaymentMethod
            // ============================================================
            modelBuilder.Entity<CustomerPaymentMethod>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.UserId).IsUnique();
            });
        }
    }
}
