using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Net8.JWTTest.Data
{
    public class Net8JWTTestContext : DbContext
    {
        public Net8JWTTestContext(DbContextOptions<Net8JWTTestContext> options)
            : base(options)
        {
        }

        public DbSet<User> User { get; set; } = default!;
    }
}
