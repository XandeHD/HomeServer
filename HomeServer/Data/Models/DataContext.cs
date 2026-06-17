using HomeServer.Classes.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeServer.Data.Models
{
    public class DataContext : DbContext
    {
        protected readonly IConfiguration Configuration;

        public DataContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(Configuration.GetConnectionString("DefaultConnection"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GroupUser>()
                .HasKey(gu => new { gu.GroupId, gu.UserId });
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
    }
}
