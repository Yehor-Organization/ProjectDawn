using Microsoft.EntityFrameworkCore;
using ProjectDawnApi;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Core --------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger (with JWT support)
builder.Services.AddProjectDawnSwagger();

// -------------------- Database --------------------
builder.Services.AddDbContext<ProjectDawnDbContext>(options =>
    options.UseSqlite("Data Source=projectdawn.db"));

// -------------------- Auth --------------------
builder.Services.AddProjectDawnAuthentication(builder.Configuration);

// -------------------- SignalR --------------------
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });

// -------------------- App Services --------------------
builder.Services.AddProjectDawnServices();

var app = builder.Build();

// -------------------- Seed --------------------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProjectDawnDbContext>();
    DbInitializer.Initialize(context);
}

// -------------------- Middleware --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // stays here
app.UseAuthorization();

// -------------------- Endpoints --------------------
app.MapControllers();
app.MapProjectDawnHubs();

app.Run();