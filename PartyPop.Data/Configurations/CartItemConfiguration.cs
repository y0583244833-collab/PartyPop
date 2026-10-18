using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PartyPop.Core.Entities;

namespace PartyPop.Data.Configurations;

/// <summary>טבלת קישור עם נתונים: Cart ⇄ Product (many-to-many).</summary>
public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems", t =>
            t.HasCheckConstraint("CK_CartItems_Quantity", "\"Quantity\" > 0"));
        builder.HasKey(i => i.Id);

        builder.HasOne(i => i.Cart)
               .WithMany(c => c.Items)
               .HasForeignKey(i => i.CartId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
               .WithMany(p => p.CartItems)
               .HasForeignKey(i => i.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        // מוצר מופיע פעם אחת בסל. הוספה חוזרת מגדילה כמות
        builder.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
    }
}
