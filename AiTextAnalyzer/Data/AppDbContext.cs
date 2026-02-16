using Polly;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AiTextAnalyzer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<AnalyzeRecord> Analyses => Set<AnalyzeRecord>();
    }
}
