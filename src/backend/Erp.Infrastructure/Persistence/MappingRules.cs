using Erp.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence;

internal static class MappingRules
{
    public static void ConfigureCompanyScoped<TEntity>(
        EntityTypeBuilder<TEntity> entity,
        string tableName)
        where TEntity : class
    {
        entity.HasKey("Id");
        entity.Property<Guid>("CompanyId").IsRequired();
        entity.HasAlternateKey("CompanyId", "Id");
        entity.HasOne<Company>()
            .WithMany()
            .HasForeignKey("CompanyId")
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(tableName);
    }

    public static void ConfigureCodeAndName<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : class
    {
        entity.Property<string>("Code").HasMaxLength(40).IsRequired();
        entity.Property<string>("Name").HasMaxLength(160).IsRequired();
        entity.ToTable(table =>
            table.HasCheckConstraint(
                $"CK_{entity.Metadata.GetTableName()}_Code_NotBlank",
                """length(trim("Code")) > 0"""));
    }
}
