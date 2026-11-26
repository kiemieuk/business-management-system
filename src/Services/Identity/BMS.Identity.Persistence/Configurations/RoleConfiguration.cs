using BMS.Identity.Domain.Roles;
using BMS.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BMS.Identity.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).HasMaxLength(Role.NameMaxLength).IsRequired();
        builder.Property(r => r.NormalizedName).HasMaxLength(Role.NameMaxLength).IsRequired();
        builder.HasIndex(r => r.NormalizedName).IsUnique();
        builder.Property(r => r.Description).HasMaxLength(Role.DescriptionMaxLength);
        builder.Property(r => r.CreatedBy).HasMaxLength(User.UsernameMaxLength);
        builder.Property(r => r.UpdatedBy).HasMaxLength(User.UsernameMaxLength);

        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}
