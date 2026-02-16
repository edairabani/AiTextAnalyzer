using Microsoft.EntityFrameworkCore;

namespace AiTextAnalyzer.Data
{
    public class VectorDbContext : DbContext
    {
        public VectorDbContext(DbContextOptions<VectorDbContext> options) : base(options) { }

        public DbSet<DocumentChunk> Chunks => Set<DocumentChunk>();
        public DbSet<RagQueryLog> RagLogs => Set<RagQueryLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<DocumentChunk>()
                .Property(x => x.Embedding)
                .HasColumnType("vector(1536)");
        }
    }
}
