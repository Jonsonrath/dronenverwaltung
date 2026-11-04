using DroneInventory.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DroneInventory.Api.Tests.Infrastructure;

// Runs the real API in memory for API tests. The SQLite database is an
// in-memory database that lives as long as this factory and is created by
// the app's own Database.Migrate() at startup.
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<DroneInventoryDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<DroneInventoryDbContext>>();
            services.AddDbContext<DroneInventoryDbContext>(options => options.UseSqlite(_connection));

            // Controllers that exist only in the test assembly.
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
