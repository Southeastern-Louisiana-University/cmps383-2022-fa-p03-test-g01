using FA22.P03.Web.Features.Products;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var currentId = 1;
var products = new List<ProductDto>
{
    new ProductDto
    {
        Id = currentId++,
        Name = "Super Mario World",
        Description = "Super Nintendo (SNES) System. Mint Condition",
        Price = 259.99m,
    },
    new ProductDto
    {
        Id = currentId++,
        Name = "Donkey Kong 64",
        Description = "Moderate Condition Donkey Kong 64 cartridge for the Nintendo 64",
        Price = 199m,
    },
    new ProductDto
    {
        Id = currentId++,
        Name = "Half-Life 2: Collector's Edition",
        Description = "Good condition with all 5 CDs, booklets, and material from original",
        Price = 559.99m
    }
};

app.MapGet("/api/products", () =>
    {
        return products;
    })
    .Produces(200, typeof(ProductDto[]));

app.MapGet("/api/products/{id}", (int id) =>
    {
        var result = products.FirstOrDefault(x => x.Id == id);
        if (result == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(result);
    })
    .WithName("GetProductById")
    .Produces(404)
    .Produces(200, typeof(ProductDto));

app.MapPost("/api/products", (ProductDto product) =>
    {
        if (string.IsNullOrWhiteSpace(product.Name) ||
            product.Name.Length > 120 ||
            product.Price <= 0 ||
            string.IsNullOrWhiteSpace(product.Description))
        {
            return Results.BadRequest();
        }

        product.Id = currentId++;
        products.Add(product);
        return Results.CreatedAtRoute("GetProductById", new { id = product.Id }, product);
    })
    .Produces(400)
    .Produces(201, typeof(ProductDto));

app.MapPut("/api/products/{id}", (int id, ProductDto product) =>
    {
        if (string.IsNullOrWhiteSpace(product.Name) ||
            product.Name.Length > 120 ||
            product.Price <= 0 ||
            string.IsNullOrWhiteSpace(product.Description))
        {
            return Results.BadRequest();
        }

        var current = products.FirstOrDefault(x => x.Id == id);
        if (current == null)
        {
            return Results.NotFound();
        }

        current.Name = product.Name;
        current.Name = product.Name;
        current.Price = product.Price;
        current.Description = product.Description;

        return Results.Ok(current);
    })
    .Produces(400)
    .Produces(404)
    .Produces(200, typeof(ProductDto));

app.MapDelete("/api/products/{id}", (int id) =>
    {
        var current = products.FirstOrDefault(x => x.Id == id);
        if (current == null)
        {
            return Results.NotFound();
        }

        products.Remove(current);

        return Results.Ok();
    })
    .Produces(400)
    .Produces(404)
    .Produces(200, typeof(ProductDto));


app.Run();

//see: https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0
// Hi 383 - this is added so we can test our web project automatically. More on that later
public partial class Program { }
