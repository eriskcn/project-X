using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ProjectX.Data;

public static class ModelBuilderExtensions
{
    public static void ConfigureSoftDelete(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType)) continue;
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);

            foreach (var navigation in entityType.GetNavigations())
            {
                if (!typeof(ISoftDelete).IsAssignableFrom(navigation.TargetEntityType.ClrType)) continue;
                if (navigation.ForeignKey.DeleteBehavior == DeleteBehavior.Cascade)
                {
                    navigation.ForeignKey.DeleteBehavior = DeleteBehavior.ClientCascade;
                }
            }
        }
    }
}