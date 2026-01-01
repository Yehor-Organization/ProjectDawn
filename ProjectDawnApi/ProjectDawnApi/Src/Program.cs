using Microsoft.EntityFrameworkCore;
using ProjectDawnApi;

var builder = WebApplication.CreateBuilder(args);

// ==================================================
// Core
// ==================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProjectDawnSwagger();

// ==================================================
// Database (SQLite / MySQL switch)
// ==================================================
var dbProvider = Environment.GetEnvironmentVariable("DB_PROVIDER")?.ToLower() ?? "sqlite";

if (dbProvider == "mysql")
{
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME");
    var dbUser = Environment.GetEnvironmentVariable("DB_USER");
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

    Require(dbHost, "DB_HOST");
    Require(dbName, "DB_NAME");
    Require(dbUser, "DB_USER");
    Require(dbPassword, "DB_PASSWORD");

    var mysqlConnection =
        $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};" +
        "SslMode=None;AllowPublicKeyRetrieval=True;";

    builder.Services.AddDbContext<ProjectDawnDbContext>(options =>
        options.UseMySql(
            mysqlConnection,
            ServerVersion.AutoDetect(mysqlConnection)
        ));
}
else if (dbProvider == "sqlite")
{
    var sqlitePath =
        Environment.GetEnvironmentVariable("SQLITE_PATH") ??
        "Data Source=projectdawn.db";

    builder.Services.AddDbContext<ProjectDawnDbContext>(options =>
        options.UseSqlite(sqlitePath));
}
else
{
    throw new InvalidOperationException($"Unknown DB_PROVIDER '{dbProvider}'");
}

// ==================================================
// Auth
// ==================================================
builder.Services.AddProjectDawnAuthentication(builder.Configuration);

// ==================================================
// SignalR
// ==================================================
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });

// ==================================================
// App Services
// ==================================================
builder.Services.AddProjectDawnServices();

var app = builder.Build();

// ==================================================
// Database migrations (auto-create tables)
// ==================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProjectDawnDbContext>();

    const int retries = 10;
    for (int i = 0; i < retries; i++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception ex) when (DatabaseMigrationHelper.IsTransientDbError(ex) && i < retries - 1)
        {
            Console.WriteLine($"DB not ready, retrying... ({i + 1}/{retries})");
            Thread.Sleep(3000);
        }
    }
}

// ==================================================
// Middleware
// ==================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS breaks in containers unless explicitly configured
if (!Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?.Equals("true") ?? true)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// ==================================================
// Endpoints
// ==================================================
app.MapControllers();
app.MapProjectDawnHubs();

app.Run();

// ==================================================
// Helpers
// ==================================================
static void Require(string? value, string name)
{
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Missing required env var: {name}");
}