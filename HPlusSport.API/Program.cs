using Asp.Versioning;
using HPlusSport.API;
using HPlusSport.API.Data;
using HPlusSport.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("api-version"), // ?api-version=2.0
        new HeaderApiVersionReader("X-API-Version"), // X-API-Version: 2.0
        new MediaTypeApiVersionReader("ver")); // Accept: application/json;ver=2.0
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<SwaggerDefaultValues>();
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
});


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ShopDbContext>(options =>
{
    options.UseInMemoryDatabase("Shop");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var descriptions = app.DescribeApiVersions();

        foreach (var description in descriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//app.NewApiVersionSet()
//    .HasApiVersion(new ApiVersion(1, 0))
//    .HasApiVersion(new ApiVersion(2, 0))
//    .ReportApiVersions()
//    .Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/products", async (ShopDbContext _context) =>
{
    return await _context.Products.ToListAsync();
});

app.MapGet("/products/{id}", async (int id, ShopDbContext _context) =>
{
    var product = await _context.Products.FindAsync(id);
    if (product == null)
        return Results.NotFound();
    return Results.Ok(product);
});

app.MapGet("/products/available", async (ShopDbContext _context) =>
{
    var products = await _context.Products.Where(p => p.IsAvailable).ToArrayAsync();

    return Results.Ok(products);
});

app.MapPost("/products", async (ShopDbContext _context, Product product) =>
{
    _context.Products.Add(product);
    await _context.SaveChangesAsync();

    return Results.CreatedAtRoute(
        "GetProduct",
        new { id = product.Id },
        product
        );
});

app.MapPut("/products/{id}", async (ShopDbContext _context, int id, Product product) =>
{
    if (id != product.Id)
    {
        return Results.BadRequest();
    }

    _context.Entry(product).State = EntityState.Modified;

    try
    {
        await _context.SaveChangesAsync();
    } catch (DbUpdateConcurrencyException)
    {
        if (!_context.Products.Any(p => p.Id == id))
        {
            return Results.NotFound();
        } else
        {
            throw;
        }
    }

    return Results.NoContent();
});

app.MapDelete("/products/{id}", async (ShopDbContext _context, int id) =>
{
    var product = await _context.Products.FindAsync(id);
    if (product == null)
    {
        return Results.NotFound();
    }

    _context.Remove(product);
    await _context.SaveChangesAsync();

    return Results.Ok();
});

app.MapPost("/products/delete", async (ShopDbContext _context, int[] ids) =>
{
    var products = new List<Product>();
    foreach (var id in ids)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return Results.NotFound();
        }

        products.Add(product);
    }

    _context.Products.RemoveRange(products);
    await _context.SaveChangesAsync();

    return Results.NoContent();
});

app.Run();
