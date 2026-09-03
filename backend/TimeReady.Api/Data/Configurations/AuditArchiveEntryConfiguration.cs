using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeReady.Api.Models.Auditing;

namespace TimeReady.Api.Data.Configurations;

public class AuditArchiveEntryConfiguration : IEntityTypeConfiguration<AuditArchiveEntry>
{
    public void Configure(EntityTypeBuilder<AuditArchiveEntry> builder)
    {
        builder.ToTable("AuditArchiveEntries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(entry => entry.EntityId)
            .IsRequired()
            .HasMaxLength(64);

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

        builder.HasIndex(entry => new { entry.EntityName, entry.EntityId });
        builder.HasIndex(entry => entry.TimestampUtc);
        builder.HasIndex(entry => entry.ArchivedAtUtc);
    }
}
