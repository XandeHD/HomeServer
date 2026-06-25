using HomeServer.Classes.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeServer.Data.Models
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GroupUser>()
                .HasKey(gu => new { gu.GroupId, gu.UserId });

            // Índices para melhorar performance em queries frequentes
            modelBuilder.Entity<Expense>()
                .HasIndex(e => e.UserId);

            modelBuilder.Entity<Expense>()
                .HasIndex(e => new { e.UserId, e.Date });

            modelBuilder.Entity<BuyOrder>()
                .HasIndex(b => b.UserId);

            modelBuilder.Entity<Salary>()
                .HasIndex(s => s.UserId);

            modelBuilder.Entity<GroupUser>()
                .HasIndex(gu => gu.UserId);

            modelBuilder.Entity<StockPosition>()
                .HasIndex(sp => sp.UserId);
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<BuyOrder> BuyOrders { get; set; }
        public DbSet<BuyOrderLine> BuyOrderLines { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseLine> ExpenseLines { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupUser> GroupUsers { get; set; }
        public DbSet<InvestedAsset> InvestedAssets { get; set; }
        public DbSet<StockPosition> StockPositions { get; set; }
    }
}
