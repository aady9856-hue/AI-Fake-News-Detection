using FakeNewsDetection.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FakeNewsDetection.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<NewsArticle> NewsArticles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NewsArticle>(entity =>
            {
                entity.ToTable("NewsArticles");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Headline)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(e => e.ArticleContent)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.Url)
                    .HasMaxLength(2048);

                entity.Property(e => e.Prediction)
                    .HasMaxLength(50);

                entity.Property(e => e.Confidence)
                    .HasColumnType("decimal(5,2)");

                entity.Property(e => e.Explanation)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}