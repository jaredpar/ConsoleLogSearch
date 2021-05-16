using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace ConsoleLogSearch
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        // This entry point exists so that `dotnet ef database` and `migrations` has an 
        // entry point to create TriageDbContext
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<HelixContext>(options => Config(options));
                });

            static void Config(DbContextOptionsBuilder builder)
            {
                var configuration = CreateConfiguration();
                var connectionString = configuration["ConnectionString"];
                builder.UseSqlServer(connectionString, opts => opts.CommandTimeout((int)TimeSpan.FromMinutes(145).TotalSeconds).EnableRetryOnFailure());
            }

            static IConfiguration CreateConfiguration()
            {
                var config = new ConfigurationBuilder()
                    .AddUserSecrets<Program>()
                    .Build();
                return config;
            }
        }
    }
}
