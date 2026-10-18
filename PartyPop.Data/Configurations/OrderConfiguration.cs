using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PartyPop.Core.Entities;

namespace PartyPop.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        // מספרי הזמנה בני 5 ספרות: המספור הרץ מתחיל מ-10000
        builder.Property(o => o.Id).HasIdentityOptions(startValue: 10000);

        builder.Property(o => o.EventAddress).IsRequired().HasMaxLength(200);
        builder.Property(o => o.ContactPhone).IsRequired().HasMaxLength(20);
        builder.Property(o => o.Notes).HasMaxLength(500);
        builder.Property(o => o.TotalPrice).HasPrecision(10, 2);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.Shift).HasConversion<string>().HasMaxLength(20);

        // Concurrency token: ב-PostgreSQL ממופה לעמודת המערכת xmin (בלי עמודה חדשה)
        builder.Property(o => o.Version).IsRowVersion();

        builder.HasOne(o => o.User)
               .WithMany(u => u.Orders)
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.EventDate);
    }
}
