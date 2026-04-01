using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

var connectionString = config.GetConnectionString("DefaultConnection");

try
{
    await EnsureDatabaseCreated(connectionString);
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
}

// STEP 2: Run all scripts
try
{
    await RunScripts(connectionString);
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
}

async Task EnsureDatabaseCreated(string connStr)
{
    var builder = new SqlConnectionStringBuilder(connStr);
    var dbName = builder.InitialCatalog;

    builder.InitialCatalog = "master";

    using var conn = new SqlConnection(builder.ConnectionString);
    await conn.OpenAsync();

    var cmd = new SqlCommand(
        $"IF DB_ID('{dbName}') IS NULL CREATE DATABASE [{dbName}]",
        conn);

    await cmd.ExecuteNonQueryAsync();

    Console.WriteLine("Database ensured.");
}

async Task RunScripts(string connStr)
{
    var scriptsPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts");

    if (!Directory.Exists(scriptsPath))
    {
        Console.WriteLine("Scripts folder not found.");
        return;
    }

    var scripts = Directory.GetFiles(scriptsPath, "*.sql")
                           .OrderBy(x => x)
                           .ToList();
    var failedScripts = new List<string>();

    foreach (var scriptFile in scripts)
    {
        var scriptName = Path.GetFileName(scriptFile);
        Console.WriteLine($"Executing {scriptName}...");

        try
        {
            var script = await File.ReadAllTextAsync(scriptFile);

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(script, conn);
            await cmd.ExecuteNonQueryAsync();

            Console.WriteLine($"{scriptName} executed.");
        }
        catch (Exception ex)
        {
            failedScripts.Add(scriptName);
            Console.WriteLine($"{scriptName} failed: {ex.Message}");
        }
    }

    if (failedScripts.Count == 0)
    {
        Console.WriteLine("All scripts executed.");
        return;
    }

    Console.WriteLine($"Completed with {failedScripts.Count} failed script(s): {string.Join(", ", failedScripts)}");
}
