using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PartyPop.Core.Entities;

namespace PartyPop.Data.Configurations;

/// <summary>המשאב המוגבל: שורה אחת לכל מוצר + תאריך (סעיף 6.1 בתוכנית).</summary>
public class ProductDayUsageConfiguration : IEntityTypeConfiguration<ProductDayUsage>
{
    public void Configure(EntityTypeBuilder<ProductDayUsage> builder)
    {
        builder.ToTable("ProductDayUsages", t =>
        {
            t.HasCheckConstraint("CK_ProductDayUsages_Morning", "\"MorningReserved\" >= 0");
            t.HasCheckConstraint("CK_ProductDayUsages_Afternoon", "\"AfternoonReserved\" >= 0");
        });
        builder.HasKey(u => u.Id);

        // Npgsql ממפה uint + IsRowVersion לעמודת המערכת xmin.
        // EF מוסיף אותה ל-WHERE של כל UPDATE → מי שמאחר מקבל DbUpdateConcurrencyException
        builder.Property(u => u.Version).IsRowVersion();

        builder.HasOne(u => u.Product)
               .WithMany(p => p.DayUsages)
               .HasForeignKey(u => u.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        // שם קבוע: ה-catch ב-UnitOfWork מזהה לפיו "שני לקוחות יצרו את השורה הראשונה במקביל"
        builder.HasIndex(u => new { u.ProductId, u.EventDate }).IsUnique()
               .HasDatabaseName(DbConstraints.ProductDayUsageUnique);
    }
}
