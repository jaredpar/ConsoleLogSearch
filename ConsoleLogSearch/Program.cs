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
            var fullCount = await helixContext.HelixConsoleLogs.CountAsync();
            Console.WriteLine($"Total log count {fullCount}");

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

            var totalCount = await helixContext.HelixConsoleLogs.CountAsync();
            var importCount = 0;
            var skipCount = 0;
            var allUris = await File.ReadAllLinesAsync(@"c:\users\jaredpar\code\ConsoleLogSearch\ConsoleLogSearch\console.txt");
            DrawStats(null);
            foreach (var uri in allUris)
            {
                try
                {
                    if (!set.Add(uri))
                    {
                        continue;
                    }

                    DrawStats(uri);
                    var message = new HttpRequestMessage(HttpMethod.Get, uri);
                    var response = await httpClient.SendAsync(message);
                    var content = await response.Content.ReadAsStringAsync();
                    if (content.Length > 1_000_000)
                    {
                        skipCount++;
                        continue;
                    }

                    var log = new HelixConsoleLog()
                    {
                        ConsoleLog = content,
                        ConsoleLogUri = uri
                    };
                    helixContext.Add(log);
                    await helixContext.SaveChangesAsync();
                    importCount++;
                    totalCount++;
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

            void DrawClear()
            {
                Console.SetCursorPosition(0, top: 0);
                var buffer = new string(' ', Console.WindowWidth);
                for (int i = 0; i < 6; i++)
                {
                    Console.WriteLine(buffer);
                }
            }

            void DrawStats(string uri)
            {
                DrawClear();
                Console.SetCursorPosition(0, top: 0);
                Console.WriteLine($"Total {totalCount:N0}");
                Console.WriteLine($"Imported {importCount:N0}");
                Console.WriteLine($"Skipped {skipCount:N0}");
                Console.WriteLine($"Remaining {(allUris.Length - (totalCount + importCount + skipCount)):N0}");
                Console.SetCursorPosition(0, top: 4);
                Console.WriteLine("");

                if (uri is {})
                {
                    Console.SetCursorPosition(0, top: 4);
                    Console.WriteLine(uri);
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
