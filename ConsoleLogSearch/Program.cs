using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleLogSearch
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // await Populate();
            await SearchAsync();
        }

        public static async Task SearchAsync()
        {
            var helixContext = GetHelixContext();
            while (true)
            {
                Console.WriteLine("Enter a search term");
                var search = Console.ReadLine();
                if (string.IsNullOrEmpty(search))
                {
                    continue;
                }

                try
                {
                    if (search.Contains(' '))
                    {
                        search = $@"""{search}""";
                    }

                    var query = helixContext
                        .HelixConsoleLogs
                        .Where(x => EF.Functions.Contains(x.ConsoleLog, search));

                    var watch = new Stopwatch();
                    watch.Start();
                    var count = await query.CountAsync();
                    var top = await query.Select(x => x.ConsoleLogUri).Take(5).ToListAsync();
                    var elapsed = watch.Elapsed;

                    Console.WriteLine($"Elapsed: {elapsed}");
                    Console.WriteLine($"Total: {count}");
                    foreach (var item in top)
                    {
                        Console.WriteLine(item);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

        }

        public static async Task Populate()
        {
            var helixContext = GetHelixContext();
            var httpClient = new HttpClient();
            var set = new HashSet<string>();
            foreach (var uri in await helixContext.HelixConsoleLogs.Select(x => x.ConsoleLogUri).ToListAsync())
            {
                set.Add(uri);
            }

            foreach (var line in await File.ReadAllLinesAsync(@"c:\users\jaredpar\code\ConsoleLogSearch\ConsoleLogSearch\console.csv"))
            {
                try
                {
                    var parts = line.Split(new[] { ',' }, count: 2);
                    var size = int.Parse(parts[0]);
                    if (size >= 500_000)
                    {
                        continue;
                    }

                    var uri = parts[1];
                    if (!set.Add(uri))
                    {
                        continue;
                    }

                    Console.WriteLine(uri);
                    var message = new HttpRequestMessage(HttpMethod.Get, uri);
                    var response = await httpClient.SendAsync(message);
                    var content = await response.Content.ReadAsStringAsync();
                    var log = new HelixConsoleLog()
                    {
                        ConsoleLog = content,
                        ConsoleLogUri = uri
                    };
                    helixContext.Add(log);
                    await helixContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    while (ex.InnerException is { } ie)
                    {
                        Console.WriteLine(ie.Message);
                        ex = ie;
                    }
                }
            }
        }

        public static HelixContext GetHelixContext()
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            var builder = new DbContextOptionsBuilder<HelixContext>();
            var connectionString = configuration["ConnectionString"];
            builder.UseSqlServer(connectionString, opts => opts.CommandTimeout((int)TimeSpan.FromMinutes(.5).TotalSeconds));
            return new HelixContext(builder.Options);
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
