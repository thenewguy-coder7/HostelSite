using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HostelSite.Models.Data;

public partial class HostelDbContext : DbContext
{
    public HostelDbContext()
    {
    }

    public HostelDbContext(DbContextOptions<HostelDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<AestheticRequest> AestheticRequests { get; set; }

    public virtual DbSet<LogisticsItem> LogisticsItems { get; set; }

    public virtual DbSet<LogisticsOrder> LogisticsOrders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<RoomAesthetic> RoomAesthetics { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId);

            entity.ToTable("admins");

            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
        });

        modelBuilder.Entity<AestheticRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.ToTable("aesthetic_requests");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.AestheticId).HasColumnName("aesthetic_id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Notes)
                .HasMaxLength(1000)
                .HasColumnName("notes");
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("requested_at");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending")
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Aesthetic).WithMany(p => p.AestheticRequests)
                .HasForeignKey(d => d.AestheticId)
                .HasConstraintName("aesthetic_requests_aesthetic_id_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.AestheticRequests)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("aesthetic_requests_student_id_fkey");
        });

        modelBuilder.Entity<LogisticsItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.ToTable("logistics_items");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ItemName)
                .HasMaxLength(100)
                .HasColumnName("item_name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("price");
            entity.Property(e => e.StockQuantity).HasColumnName("stock_quantity");
        });

        modelBuilder.Entity<LogisticsOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId);

            entity.ToTable("logistics_orders");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeliveryNotes)
                .HasMaxLength(500)
                .HasColumnName("delivery_notes");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(30)
                .HasDefaultValue("Confirmed")
                .HasColumnName("order_status");
            entity.Property(e => e.OrderedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("ordered_at");
            entity.Property(e => e.PickupDate).HasColumnName("pickup_date");
            entity.Property(e => e.ReturnDate).HasColumnName("return_date");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Student).WithMany(p => p.LogisticsOrders)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("logistics_orders_student_id_fkey");
            entity.Property(e => e.PickupTime).HasColumnName("pickup_time");
            entity.Property(e => e.PreviousHostel).HasMaxLength(150).HasColumnName("previous_hostel");
            entity.Property(e => e.NewHostel).HasMaxLength(150).HasColumnName("new_hostel");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId);

            entity.ToTable("order_items");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Item).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ItemId)
                .HasConstraintName("order_items_item_id_fkey");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("order_items_order_id_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);

            entity.ToTable("payments");

            entity.HasIndex(e => e.PaystackReference).IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("GHS")
                .IsFixedLength()
                .HasColumnName("currency");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentChannel)
                .HasMaxLength(30)
                .HasColumnName("payment_channel");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending")
                .HasColumnName("payment_status");
            entity.Property(e => e.PaystackReference)
                .HasMaxLength(100)
                .HasColumnName("paystack_reference");
        });

        modelBuilder.Entity<RoomAesthetic>(entity =>
        {
            entity.HasKey(e => e.AestheticId);

            entity.ToTable("room_aesthetics");

            entity.Property(e => e.AestheticId).HasColumnName("aesthetic_id");
            entity.Property(e => e.AdditionalCost)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("additional_cost");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.StyleCategory)
                .HasMaxLength(50)
                .HasColumnName("style_category");
            entity.Property(e => e.ThemeName)
                .HasMaxLength(100)
                .HasColumnName("theme_name");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId);

            entity.ToTable("students");

            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.EnrollmentDate).HasColumnName("enrollment_date");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasColumnName("last_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active")
                .HasColumnName("status");
            entity.Property(e => e.StudentNumber)
                .HasMaxLength(50)
                .HasColumnName("student_number");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}