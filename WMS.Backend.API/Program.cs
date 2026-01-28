
using Microsoft.EntityFrameworkCore;
using WMS.Backend.API.Data;
using WMS.Backend.API.Repositories.Interfaces;
using WMS.Backend.API.Repositories;
using WMS.Backend.API.Services;

namespace WMS.Backend.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Configure Entity Framework DbContext
        builder.Services.AddDbContext<WMSDbContext>(options =>
            options.UseSqlite(
                builder.Configuration.GetConnectionString("WMSDatabase")
            )
        );

        // Register Repositories
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<ISKURepository, SKURepository>();
        builder.Services.AddScoped<IAllocationRepository, AllocationRepository>();

        // Register Services
        builder.Services.AddScoped<StockAllocationService>();
        builder.Services.AddScoped<OrderCancellationService>();
        builder.Services.AddScoped<SKUCorrectionService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        // Auto-migrate database on startup (Optional - for development only)
        //using (var scope = app.Services.CreateScope())
        //{
        //    var dbContext = scope.ServiceProvider.GetRequiredService<WMSDbContext>();
        //    dbContext.Database.Migrate();
        //}

        app.Run();
    }
}
