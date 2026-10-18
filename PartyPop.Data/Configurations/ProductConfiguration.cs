using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;

namespace PartyPop.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", t =>
        {
            t.HasCheckConstraint("CK_Products_TotalQuantity", "\"TotalQuantity\" >= 0");
            t.HasCheckConstraint("CK_Products_OutOfService",
                "\"OutOfServiceQuantity\" >= 0 AND \"OutOfServiceQuantity\" <= \"TotalQuantity\"");
            t.HasCheckConstraint("CK_Products_Price", "\"PricePerShift\" > 0");
        });
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(1000);
        builder.Property(p => p.PricePerShift).HasPrecision(10, 2);
        builder.Property(p => p.ImageUrl).HasMaxLength(300);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        // one-to-many: אי אפשר למחוק קטגוריה שיש בה מוצרים
        builder.HasOne(p => p.Category)
               .WithMany(c => c.Products)
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Status);

        builder.HasData(
            P(1, "טרמפולינה 3 מטר", "טרמפולינה עגולה עם רשת בטיחות", 1, 250, 5, 4),
            P(2, "טרמפולינה 4 מטר", "טרמפולינה גדולה לקבוצות", 1, 350, 3, 6),
            P(3, "מתנפח טירה", "טירה מתנפחת צבעונית", 2, 400, 4, 3),
            P(4, "מגלשה מתנפחת", "מגלשה מתנפחת בגובה 5 מטר", 2, 550, 2, 5),
            P(5, "מתנפח מסלול מכשולים", "מסלול מכשולים באורך 10 מטר", 2, 700, 2, 6),
            P(6, "מכונת פופקורן", "כולל חומרי גלם ל-100 מנות", 3, 180, 6, null),
            P(7, "מכונת שערות סבתא", "כולל סוכר צבעוני ומקלות", 3, 200, 5, null),
            P(8, "מכונת ברד", "מכונה כפולה, שני טעמים", 3, 220, 4, null),
            P(9, "כדורגל שולחן ענק", "משחק לשמונה שחקנים", 4, 300, 2, 7),
            P(10, "קיר טיפוס נייד", "קיר טיפוס בגובה 6 מטר עם ציוד בטיחות", 4, 900, 1, 8));
    }

    private static Product P(int id, string name, string description, int categoryId,
        decimal price, int quantity, int? minAge) => new()
    {
        Id = id,
        Name = name,
        Description = description,
        CategoryId = categoryId,
        PricePerShift = price,
        TotalQuantity = quantity,
        OutOfServiceQuantity = 0,
        MinAge = minAge,
        Status = ProductStatus.Active,
        CreatedAt = SeedConstants.CreatedAt,
        UpdatedAt = SeedConstants.CreatedAt
    };
}
