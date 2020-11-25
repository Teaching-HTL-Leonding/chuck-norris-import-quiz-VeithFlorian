using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


var factory = new ChuckNorrisContextFactory();
await using var dbContext = factory.CreateDbContext(args);
const int MAXRETRIES = 10;


int jokesToImport = 5;
if (args.Length > 0)
{
    if (args[0] == "clean")
    {
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Jokes");
        Environment.Exit(0);
    }
    jokesToImport = int.Parse(args[0]);
    if (jokesToImport < 1 || jokesToImport > 10)
    {
        Console.WriteLine("Maximum number of jokes is 10");
        Environment.Exit(1);
    }
}

var client = new HttpClient();

try	
{
    using var transaction = await dbContext.Database.BeginTransactionAsync();
    for (int i = 0; i < jokesToImport; i++)
    {
        int retries = 0;
        bool newJokeFoundOrExplicit = false;
        do
        {
            HttpResponseMessage response = await client.GetAsync("https://api.chucknorris.io/jokes/random");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var apiJoke = JsonSerializer.Deserialize<Joke>(responseBody);

            if (apiJoke == null || apiJoke.Categories.Contains("explicit"))
            {
                Console.WriteLine("Explicit joke");
                //jokesToImport++;
                newJokeFoundOrExplicit = true;
                continue;
            }
            var chuckNorrisJoke = new ChuckNorrisJoke()
                {ChuckNorrisId = apiJoke.Id, Url = apiJoke.Url, Joke = apiJoke.Value};
            if (!dbContext.Jokes.Any(joke => joke.ChuckNorrisId == chuckNorrisJoke.ChuckNorrisId))
            {
                dbContext.Jokes.Add(chuckNorrisJoke);
                newJokeFoundOrExplicit = true;
            }
            else
            {
                retries++;
                Console.WriteLine($"Retry {retries}/{MAXRETRIES}");
                continue;
            }

            if (retries == MAXRETRIES)
            {
                Console.WriteLine("All jokes importet!");
                Environment.Exit(0);
            }
        } while (!newJokeFoundOrExplicit);
    }
    await dbContext.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch (SqlException ex)
{
    Console.WriteLine($"Error importing joke: {ex.Message}");
    Environment.Exit(1);
}
catch(HttpRequestException e)
{
    Console.WriteLine($"Error downloading joke: {e.Message}");
}


class ChuckNorrisJoke
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string ChuckNorrisId { get; set; }

    [MaxLength(1024)]
    public string Url { get; set; }
    
    public string Joke { get; set; }
}

class ChuckNorrisContext : DbContext
{
    public DbSet<ChuckNorrisJoke> Jokes { get; set; }

    public ChuckNorrisContext(DbContextOptions<ChuckNorrisContext> options)
        :base(options)
    { }
}

class ChuckNorrisContextFactory : IDesignTimeDbContextFactory<ChuckNorrisContext>
{
    public ChuckNorrisContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        var optionsBuilder = new DbContextOptionsBuilder<ChuckNorrisContext>();
        optionsBuilder
            // Uncomment the following line if you want to print generated
            // SQL statements on the console.
            //.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

        return new ChuckNorrisContext(optionsBuilder.Options);
    }
}
