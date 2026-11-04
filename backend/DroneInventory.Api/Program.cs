using DroneInventory.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<DroneInventoryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DroneInventory")));

// LoanDate and ReturnDate are taken from TimeProvider so tests can use fixed dates.
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// Create or update the SQLite database to the latest migration.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
