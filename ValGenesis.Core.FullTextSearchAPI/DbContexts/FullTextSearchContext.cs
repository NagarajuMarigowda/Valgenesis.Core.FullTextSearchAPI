using Microsoft.EntityFrameworkCore;
using ValGenesis.Core.FullTextSearchAPI.Entities;

namespace ValGenesis.Core.FullTextSearchAPI.DbContexts
{
    public class FullTextSearchContext : DbContext
    {
        public DbSet<FileContent> FileContent { get; set; } = null!;
        public FullTextSearchContext(DbContextOptions<FullTextSearchContext> options)
            : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileContent>()
                .HasGeneratedTsVectorColumn(
                    p => p.SearchVector,
                    "english",  // Text search config
                    p => new { p.Name, p.Description })  // Included properties
                .HasIndex(p => p.SearchVector)
                .HasMethod("GIN");
        }
    }
}
