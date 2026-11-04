using DroneInventory.Api.Data;
using DroneInventory.Api.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Invalid request bodies give 422 like all other invalid input (design.md §5).
        options.InvalidModelStateResponseFactory = context =>
            new UnprocessableEntityObjectResult(new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status422UnprocessableEntity,
            });
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

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

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
