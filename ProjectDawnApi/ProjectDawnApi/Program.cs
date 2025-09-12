using Microsoft.EntityFrameworkCore;
using ProjectDawnApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// --- THIS SECTION IS CORRECTED ---

var dbName = "ProjectDawn_InMemoryDb";

// Register the DbContext. By removing the 'ServiceLifetime.Singleton' parameter,
// it defaults to the correct 'Scoped' lifetime.
builder.Services.AddDbContext<ProjectDawnDbContext>(options =>
    options.UseInMemoryDatabase(dbName));

// ------------------------------------

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null; // keep property names as-is
    });
;
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ProjectDawnDbContext>();
    DbInitializer.Initialize(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<FarmHub>("/farmHub");

app.Run();