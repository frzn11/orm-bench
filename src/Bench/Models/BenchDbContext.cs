using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;


// namespace Bench.Models
// {
//     public class BenchDbContext : DbContext
//     {
//         private readonly string _connectionString;

//         public BenchDbContext(string connectionString)
//         {
//             _connectionString = connectionString;
//         }

//         public DbSet<SmallTable> SmallTable { get; set; }



//         protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//         {
//             optionsBuilder.UseSqlServer(_connectionString);
//         }
//     }
// }








// 1) DbContext with a runtime table name
namespace Bench.Models
{
public class BenchDbContext : DbContext
{
    private readonly string _tableName;
    public string TableName => _tableName;

    public BenchDbContext(DbContextOptions<BenchDbContext> options, string tableName)
        : base(options) => _tableName = tableName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // map the entity to the runtime table name
        modelBuilder.Entity<SmallTable>().ToTable(_tableName);
    }

    // Optional DbSet if you like:
    public DbSet<SmallTable> SmallTable => Set<SmallTable>();
}


// public sealed class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
// {
//     public object Create(DbContext context, bool designTime)
//         => context is BenchDbContext b
//            ? (context.GetType(), b.TableName, designTime)
//            : (object)(context.GetType(), designTime);
// }
}