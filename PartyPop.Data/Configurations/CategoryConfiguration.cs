using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PartyPop.Core.Entities;

namespace PartyPop.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName(DbConstraints.CategoryNameUnique);

        builder.HasData(
            new Category { Id = 1, Name = "טרמפולינות", Description = "טרמפולינות בגדלים שונים" },
            new Category { Id = 2, Name = "מתנפחים", Description = "מתנפחים ומגלשות מתנפחות" },
            new Category { Id = 3, Name = "מכונות מזון", Description = "פופקורן, סוכר ועוד" },
            new Category { Id = 4, Name = "משחקי שטח", Description = "אטרקציות ומשחקים לחצר" });
    }
}
