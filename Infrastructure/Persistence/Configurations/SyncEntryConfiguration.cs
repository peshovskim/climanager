using CliManager.Domain.Drive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliManager.Infrastructure.Persistence.Configurations;

public sealed class SyncEntryConfiguration : IEntityTypeConfiguration<SyncEntry>
{
    public void Configure(EntityTypeBuilder<SyncEntry> builder)
    {
        builder.ToTable("SyncEntry");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.DriveFileId)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(entry => entry.DriveFileId)
            .IsUnique();

        builder.Property(entry => entry.FileName)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(entry => entry.LocalPath)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(entry => entry.DownloadedAt)
            .IsRequired();
    }
}
