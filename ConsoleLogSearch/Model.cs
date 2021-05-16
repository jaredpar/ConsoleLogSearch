using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleLogSearch
{
    public sealed class HelixContext : DbContext
    {
        public DbSet<HelixConsoleLog> HelixConsoleLogs { get; set; }

        public HelixContext(DbContextOptions<HelixContext> options)
            :base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }

    public sealed class HelixConsoleLog
    {
        public int Id { get; set; }

        [Column(TypeName = "text")]
        [Required]
        public string ConsoleLog { get; set; }

        [Column(TypeName = "varchar(1000)")]
        [Required]
        public string ConsoleLogUri { get; set; }
    }
}
