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

    public virtual DbSet<AestheticRequest> AestheticRequests { get; set; }

    public virtual DbSet<Amenity> Amenities { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<LogisticsItem> LogisticsItems { get; set; }

    public virtual DbSet<LogisticsOrder> LogisticsOrders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomAesthetic> RoomAesthetics { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-SOHL0DV\\SQLEXPRESS;Database=HostelDB;Integrated Security=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AestheticRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.ToTable("Aesthetic_Requests");

            entity.HasIndex(e => e.AestheticId, "IX_AestheticReq_Aesthetic");

            entity.HasIndex(e => e.RoomId, "IX_AestheticReq_Room");

            entity.HasIndex(e => e.StudentId, "IX_AestheticReq_Student");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.AestheticId).HasColumnName("aesthetic_id");
            entity.Property(e => e.Notes)
                .HasMaxLength(500)
                .HasColumnName("notes");
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("requested_at");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Aesthetic).WithMany(p => p.AestheticRequests)
                .HasForeignKey(d => d.AestheticId)
                .HasConstraintName("FK_AestheticReq_Aesthetic");

            entity.HasOne(d => d.Room).WithMany(p => p.AestheticRequests)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK_AestheticReq_Room");

            entity.HasOne(d => d.Student).WithMany(p => p.AestheticRequests)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AestheticReq_Student");
        });

        modelBuilder.Entity<Amenity>(entity =>
        {
            entity.HasIndex(e => e.RoomId, "IX_Amenities_Room");

            entity.Property(e => e.AmenityId).HasColumnName("amenity_id");
            entity.Property(e => e.AmenityName)
                .HasMaxLength(100)
                .HasColumnName("amenity_name");
            entity.Property(e => e.AmenityType)
                .HasMaxLength(50)
                .HasColumnName("amenity_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("available")
                .HasColumnName("status");

            entity.HasOne(d => d.Room).WithMany(p => p.Amenities)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Amenities_Room");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(e => e.RoomId, "IX_Bookings_Room");

            entity.HasIndex(e => e.StudentId, "IX_Bookings_Student");

            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.BookingStatus)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("booking_status");
            entity.Property(e => e.CheckInDate).HasColumnName("check_in_date");
            entity.Property(e => e.CheckOutDate).HasColumnName("check_out_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Room).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Room");

            entity.HasOne(d => d.Student).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Student");
        });

        modelBuilder.Entity<LogisticsItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.ToTable("Logistics_Items");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ItemName)
                .HasMaxLength(150)
                .HasColumnName("item_name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.StockQuantity).HasColumnName("stock_quantity");
        });

        modelBuilder.Entity<LogisticsOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId);

            entity.ToTable("Logistics_Orders");

            entity.HasIndex(e => e.StudentId, "IX_Logistics_Orders_Student");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.DeliveryNotes)
                .HasMaxLength(500)
                .HasColumnName("delivery_notes");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("order_status");
            entity.Property(e => e.OrderedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("ordered_at");
            entity.Property(e => e.PickupDate).HasColumnName("pickup_date");
            entity.Property(e => e.ReturnDate).HasColumnName("return_date");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Student).WithMany(p => p.LogisticsOrders)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Logistics_Orders_Student");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("Order_Items");

            entity.HasIndex(e => e.ItemId, "IX_Order_Items_Item");

            entity.HasIndex(e => e.OrderId, "IX_Order_Items_Order");

            entity.HasIndex(e => new { e.OrderId, e.ItemId }, "UQ_Order_Items_Pair").IsUnique();

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Item).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_Items_Item");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_Items_Order");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.BookingId, "IX_Payments_Booking");

            entity.HasIndex(e => e.PaystackReference, "UQ_Payments_PaystackRef").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("GHS")
                .IsFixedLength()
                .HasColumnName("currency");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentChannel)
                .HasMaxLength(50)
                .HasColumnName("payment_channel");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("payment_status");
            entity.Property(e => e.PaystackReference)
                .HasMaxLength(100)
                .HasColumnName("paystack_reference");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_Payments_Booking");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasIndex(e => e.RoomNumber, "UQ_Rooms_Number").IsUnique();

            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.Capacity)
                .HasDefaultValue(1)
                .HasColumnName("capacity");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.FloorNumber).HasColumnName("floor_number");
            entity.Property(e => e.PricePerSemester)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price_per_semester");
            entity.Property(e => e.RoomNumber)
                .HasMaxLength(20)
                .HasColumnName("room_number");
            entity.Property(e => e.RoomType)
                .HasMaxLength(50)
                .HasColumnName("room_type");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("available")
                .HasColumnName("status");
        });

        modelBuilder.Entity<RoomAesthetic>(entity =>
        {
            entity.HasKey(e => e.AestheticId);

            entity.ToTable("Room_Aesthetics");

            entity.HasIndex(e => e.ThemeName, "UQ_Room_Aesthetics_Name").IsUnique();

            entity.Property(e => e.AestheticId).HasColumnName("aesthetic_id");
            entity.Property(e => e.AdditionalCost)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("additional_cost");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
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
            entity.HasIndex(e => e.Email, "UQ_Students_Email").IsUnique();

            entity.HasIndex(e => e.StudentNumber, "UQ_Students_Number").IsUnique();

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EnrollmentDate).HasColumnName("enrollment_date");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.StudentNumber)
                .HasMaxLength(50)
                .HasColumnName("student_number");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
