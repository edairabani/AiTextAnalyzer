using AiTextAnalyzer.Infrastruction.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Infrastruction.Data
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
