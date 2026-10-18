using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PartyPop.Core.Entities;

namespace PartyPop.Data.Configurations;

/// <summary>טבלת קישור עם נתונים: Order ⇄ Product (many-to-many).</summary>
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", t =>
            t.HasCheckConstraint("CK_OrderItems_Quantity", "\"Quantity\" > 0"));
        builder.HasKey(i => i.Id);

        builder.Property(i => i.UnitPrice).HasPrecision(10, 2);

        builder.HasOne(i => i.Order)
               .WithMany(o => o.Items)
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        // מוצר עם היסטוריית הזמנות לא נמחק (יש Soft delete)
        builder.HasOne(i => i.Product)
               .WithMany(p => p.OrderItems)
               .HasForeignKey(i => i.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
