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
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();

    var scriptsPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts");

    if (!Directory.Exists(scriptsPath))
    {
        Console.WriteLine("Scripts folder not found.");
        return;
    }

    var scripts = Directory.GetFiles(scriptsPath, "*.sql")
                           .OrderBy(x => x)
                           .ToList();

    foreach (var scriptFile in scripts)
    {
        var scriptName = Path.GetFileName(scriptFile);
        Console.WriteLine($"Executing {scriptName}...");
    
        var script = await File.ReadAllTextAsync(scriptFile);

        using var cmd = new SqlCommand(script, conn);
        await cmd.ExecuteNonQueryAsync();

        Console.WriteLine($"{scriptName} executed.");
    }
}