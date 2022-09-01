namespace FA22.P03.Web.Features.Products;

public class ProductDto
{
    public int Id { get; set; }

    public string? Name { get; set; }
    public string? Description { get; set; }

    public decimal Price { get; set; }
}