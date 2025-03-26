using DatabaseService.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseService.Data
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

        public DbSet<MyEntity> MyEntities { get; set; }
    }
}
