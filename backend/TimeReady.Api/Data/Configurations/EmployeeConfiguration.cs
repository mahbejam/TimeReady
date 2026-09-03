using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeReady.Api.Models;

namespace TimeReady.Api.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(120);

        // Hours are money-like: a fixed scale, never a float. PostgreSQL maps
        // this to numeric(6,2), which covers -9999.99 to 9999.99.
        builder.Property(e => e.TimeBalanceHours)
            .HasPrecision(6, 2);

        builder.Property(e => e.RemainingVacationDays)
            .IsRequired();

        builder.Property(e => e.ManagerInformed)
            .IsRequired();

        builder.Property(e => e.HandoverCompleted)
            .IsRequired();

        // The dashboard orders employees by their upcoming vacation start.
        builder.HasIndex(e => e.VacationStartDate);
    }
}
