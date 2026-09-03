using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeReady.Api.Models.Auditing;

namespace TimeReady.Api.Data.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(entry => entry.EntityId)
            .IsRequired()
            .HasMaxLength(64);

        // Stored as text so the table stays readable in a database client.
        builder.Property(entry => entry.Action)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(entry => entry.UserId)
            .HasMaxLength(450);

        builder.Property(entry => entry.UserName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(entry => entry.TraceId)
            .HasMaxLength(128);

        // The two questions the endpoints ask: "what happened to this record"
        // and "what happened recently".
        builder.HasIndex(entry => new { entry.EntityName, entry.EntityId });
        builder.HasIndex(entry => entry.TimestampUtc);
        builder.HasIndex(entry => entry.UserId);
    }
}
