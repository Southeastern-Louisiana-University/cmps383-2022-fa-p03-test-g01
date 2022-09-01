using System.Net;
using FA22.P03.Tests.Web.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FA22.P03.Tests.Web;

[TestClass]
public class Phase3Tests
{
    private WebTestContext context = new();

    [TestInitialize]
    public void Init()
    {
        context = new WebTestContext();
    }

    [TestCleanup]
    public void Cleanup()
    {
        context.Dispose();
    }

    [TestMethod]
    public async Task ListAllProducts_Returns200AndData()
    {
        //arrange
        var webClient = context.GetStandardWebClient();

        //act
        var httpResponse = await webClient.GetAsync("/api/products");

        //assert
        await AssertProductListAllFunctions(httpResponse);
    }

    [TestMethod]
    public async Task GetProductById_Returns200AndData()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var target = await GetProduct(webClient);
        if (target == null)
        {
            Assert.Fail("Make List All products work first");
            return;
        }

        //act
        var httpResponse = await webClient.GetAsync($"/api/products/{target.Id}");

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK, "we expect an HTTP 200 when calling GET /api/products/{id} ");
        var resultDto = await httpResponse.Content.ReadAsJsonAsync<ProductDto>();
        resultDto.Should().BeEquivalentTo(target, "we expect get product by id to return the same data as the list all product endpoint");
    }

    [TestMethod]
    public async Task GetProductById_NoSuchId_Returns404()
    {
        //arrange
        var webClient = context.GetStandardWebClient();

        //act
        var httpResponse = await webClient.GetAsync("/api/products/999999");

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "we expect an HTTP 404 when calling GET /api/products/{id} with an invalid id");
    }

    [TestMethod]
    public async Task CreateProduct_NoName_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ProductDto
        {
            Description = "asd",
        };

        //act
        var httpResponse = await webClient.PostAsJsonAsync("/api/products", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling POST /api/products with no name");
    }

    [TestMethod]
    public async Task CreateProduct_NameTooLong_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ProductDto
        {
            Name = "a".PadLeft(121, '0'),
            Description = "asd",
        };

        //act
        var httpResponse = await webClient.PostAsJsonAsync("/api/products", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling POST /api/products with a name that is too long");
    }

    [TestMethod]
    public async Task CreateProduct_NoDescription_ReturnsError()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var target = await GetProduct(webClient);
        if (target == null)
        {
            Assert.Fail("You are not ready for this test");
            return;
        }
        var request = new ProductDto
        {
            Name = "asd",
        };

        //act
        var httpResponse = await webClient.PostAsJsonAsync("/api/products", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling POST /api/products with no description");
    }

    [TestMethod]
    public async Task CreateProduct_Returns201AndData()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ProductDto
        {
            Name = "a",
            Description = "asd",
        };

        //act
        var httpResponse = await webClient.PostAsJsonAsync("/api/products", request);

        //assert
        await AssertCreateProductFunctions(httpResponse, request, webClient);
    }

    [TestMethod]
    public async Task UpdateProduct_NoName_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ProductDto
        {
            Name = "a",
            Description = "desc",
        };
        await using var target = await CreateProduct(webClient, request);
        if (target == null)
        {
            Assert.Fail("You are not ready for this test");
        }

        request.Name = null;

        //act
        var httpResponse = await webClient.PutAsJsonAsync($"/api/products/{request.Id}", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling PUT /api/products/{id} with a missing name");
    }

    [TestMethod]
    public async Task UpdateProduct_NameTooLong_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ProductDto
        {
            Name = "a",
            Description = "desc",
        };
        await using var target = await CreateProduct(webClient, request);
        if (target == null)
        {
            Assert.Fail("You are not ready for this test");
        }

        request.Name = "a".PadLeft(121, '0');

        //act
        var httpResponse = await webClient.PutAsJsonAsync($"/api/products/{request.Id}", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling PUT /api/products/{id} with a name that is too long");
    }

    [TestMethod]
    public async Task UpdateProduct_NoDescription_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ProductDto
        {
            Name = "a",
            Description = "desc",
        };
        await using var target = await CreateProduct(webClient, request);
        if (target == null)
        {
            Assert.Fail("You are not ready for this test");
        }

        request.Description = null;

        //act
        var httpResponse = await webClient.PutAsJsonAsync($"/api/products/{request.Id}", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling PUT /api/products/{id} with a missing description");
    }

    [TestMethod]
    public async Task UpdateProduct_Valid_Returns200()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ProductDto
        {
            Name = "a",
            Description = "desc",
        };
        await using var target = await CreateProduct(webClient, request);
        if (target == null)
        {
            Assert.Fail("You are not ready for this test");
        }

        request.Description = "cool new description";

        //act
        var httpResponse = await webClient.PutAsJsonAsync($"/api/products/{request.Id}", request);

        //assert
        await AssertProductUpdateFunctions(httpResponse, request, webClient);
    }

    [TestMethod]
    public async Task DeleteProduct_NoSuchItem_ReturnsNotFound()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ProductDto
        {
            Description = "asd",
            Name = "asd"
        };
        await using var itemHandle = await CreateProduct(webClient, request);
        if (itemHandle == null)
        {
            Assert.Fail("You are not ready for this test");
            return;
        }
        //act
        var httpResponse = await webClient.DeleteAsync($"/api/products/{request.Id + 21}");

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "we expect an HTTP 404 when calling DELETE /api/products/{id} with an invalid Id");
    }

    [TestMethod]
    public async Task DeleteProduct_ValidItem_ReturnsOk()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ProductDto
        {
            Description = "asd",
            Name = "asd",
        };
        await using var itemHandle = await CreateProduct(webClient, request);
        if (itemHandle == null)
        {
            Assert.Fail("You are not ready for this test");
            return;
        }
        //act
        var httpResponse = await webClient.DeleteAsync($"/api/products/{request.Id}");

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK, "we expect an HTTP 200 when calling DELETE /api/products/{id} with a valid id");
    }

    [TestMethod]
    public async Task DeleteProduct_SameItemTwice_ReturnsNotFound()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ProductDto
        {
            Description = "asd",
            Name = "asd",
        };
        await using var itemHandle = await CreateProduct(webClient, request);
        if (itemHandle == null)
        {
            Assert.Fail("You are not ready for this test");
            return;
        }
        //act
        await webClient.DeleteAsync($"/api/products/{request.Id}");
        var httpResponse = await webClient.DeleteAsync($"/api/products/{request.Id}");

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "we expect an HTTP 404 when calling DELETE /api/products/{id} on the same item twice");
    }

    [TestMethod]
    public async Task CreateItem_NoProductId_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ItemDto
        {
            ProductId = null,
            Condition = "Good",
        };

        //act
        var httpResponse = await webClient.PostAsJsonAsync("/api/items", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling POST /api/items with no name");
    }

    [TestMethod]
    public async Task CreateItem_InvalidProductId_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ItemDto
        {
            ProductId = 9999,
            Condition = "Good",
        };

        //act
        var httpResponse = await webClient.PostAsJsonAsync("/api/items", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling POST /api/items with an invalid product id");
    }

    [TestMethod]
    public async Task CreateItem_NoCondition_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var productDto = await GetProduct(webClient);
        if (productDto == null)
        {
            Assert.Fail("You are not ready for this test - make product listing work first");
        }
        var request = new ItemDto
        {
            ProductId = productDto.Id,
        };

        //act
        var httpResponse = await webClient.PostAsJsonAsync("/api/items", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling POST /api/items with no condition");
    }

    [TestMethod]
    public async Task CreateItem_Valid_Returns201()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var productDto = await GetProduct(webClient);
        if (productDto == null)
        {
            Assert.Fail("You are not ready for this test - make product listing work first");
        }
        var request = new ItemDto
        {
            ProductId = productDto.Id,
            Condition = "Good"
        };

        //act
        var httpResponse = await webClient.PostAsJsonAsync("/api/items", request);

        //assert
        await AssertCreateItemFunctions(httpResponse, request, webClient);
    }

    [TestMethod]
    public async Task UpdateItem_NoProductId_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var productDto = await GetProduct(webClient);
        if (productDto == null)
        {
            Assert.Fail("You are not ready for this test - make product listing work first");
        }
        var request = new ItemDto
        {
            ProductId = productDto.Id,
            Condition = "Good"
        };
        await using var handle = await CreateItem(webClient, request);
        if (handle == null)
        {
            Assert.Fail("You are not ready for this test - make item create work first");
        }

        request.ProductId = null;

        //act
        var httpResponse = await webClient.PutAsJsonAsync($"/api/items/{request.Id}", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling PUT /api/items/{id} with a missing product id");
    }

    [TestMethod]
    public async Task UpdateItem_BadProductId_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var productDto = await GetProduct(webClient);
        if (productDto == null)
        {
            Assert.Fail("You are not ready for this test - make product listing work first");
        }
        var request = new ItemDto
        {
            ProductId = productDto.Id,
            Condition = "Good"
        };
        await using var handle = await CreateItem(webClient, request);
        if (handle == null)
        {
            Assert.Fail("You are not ready for this test - make item create work first");
        }

        request.ProductId = 9999;

        //act
        var httpResponse = await webClient.PutAsJsonAsync($"/api/items/{request.Id}", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling PUT /api/items/{id} with a invalid product id");
    }

    [TestMethod]
    public async Task UpdateItem_NoCondition_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var productDto = await GetProduct(webClient);
        if (productDto == null)
        {
            Assert.Fail("You are not ready for this test - make product listing work first");
        }
        var request = new ItemDto
        {
            ProductId = productDto.Id,
            Condition = "Good"
        };
        await using var handle = await CreateItem(webClient, request);
        if (handle == null)
        {
            Assert.Fail("You are not ready for this test - make item create work first");
        }

        request.Condition = null;

        //act
        var httpResponse = await webClient.PutAsJsonAsync($"/api/items/{request.Id}", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling PUT /api/items/{id} with a missing product id");
    }

    [TestMethod]
    public async Task UpdateItem_Valid_Returns200()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var productDto = await GetProduct(webClient);
        if (productDto == null)
        {
            Assert.Fail("You are not ready for this test - make product listing work first");
        }
        var request = new ItemDto
        {
            ProductId = productDto.Id,
            Condition = "Good"
        };
        await using var handle = await CreateItem(webClient, request);
        if (handle == null)
        {
            Assert.Fail("You are not ready for this test - make item create work first");
        }

        request.Condition = "Bad";

        //act
        var httpResponse = await webClient.PutAsJsonAsync($"/api/items/{request.Id}", request);

        //assert
        await AssertItemUpdateFunctions(httpResponse, request, webClient);
    }

    [TestMethod]
    public async Task DeleteItem_Valid_Returns200()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var productDto = await GetProduct(webClient);
        if (productDto == null)
        {
            Assert.Fail("You are not ready for this test - make product listing work first");
        }
        var request = new ItemDto
        {
            ProductId = productDto.Id,
            Condition = "Good"
        };
        await using var handle = await CreateItem(webClient, request);
        if (handle == null)
        {
            Assert.Fail("You are not ready for this test - make item create work first");
        }

        //act
        var httpResponse = await webClient.DeleteAsync($"/api/items/{request.Id}");

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK, "we expect an HTTP 200 when calling DELETE /api/items/{id} with a valid id");
    }

    [TestMethod]
    public async Task DeleteItem_TwiceOnSameId_Returns404()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var productDto = await GetProduct(webClient);
        if (productDto == null)
        {
            Assert.Fail("You are not ready for this test - make product listing work first");
        }
        var request = new ItemDto
        {
            ProductId = productDto.Id,
            Condition = "Good"
        };
        await using var handle = await CreateItem(webClient, request);
        if (handle == null)
        {
            Assert.Fail("You are not ready for this test - make item create work first");
        }

        //act
        await webClient.DeleteAsync($"/api/items/{request.Id}");
        var httpResponse = await webClient.DeleteAsync($"/api/items/{request.Id}");

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "we expect an HTTP 404 when calling DELETE /api/items/{id} with a valid id twice in a row");
    }

    [TestMethod]
    public async Task CreateListing_Valid_Return201()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ListingDto
        {
            Name = "Good games",
            Description = "Stuff",
            Price = 999,
            StartUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2)),
            EndUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1)),
        };

        //act
        var httpResponse = await webClient.PostAsJsonAsync("/api/listings/", request);

        //assert
        await AssertCreateListingFunctions(httpResponse, request, webClient);
    }

    [TestMethod]
    public async Task CreateListing_ValidAndActive_Returns201()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ListingDto
        {
            Name = "Good games",
            Description = "Stuff",
            Price = 999,
            StartUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1)),
            EndUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(1)),
        };

        //act
        var httpResponse = await webClient.PostAsJsonAsync("/api/listings/", request);

        //assert
        await AssertCreateListingFunctions(httpResponse, request, webClient);
    }

    [TestMethod]
    public async Task GetListingById_InvalidId_Returns404()
    {
        //arrange
        var webClient = context.GetStandardWebClient();

        //act
        var httpResponse = await webClient.GetAsync("/api/listings/9999");

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "we expect an HTTP 404 when calling GET /api/listings/{id} with an invalid id");
    }

    [TestMethod]
    public async Task CreateListing_EndBeforeStart_Returns400()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ListingDto
        {
            Name = "Good games",
            Description = "Stuff",
            Price = 999,
            EndUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1)),
            StartUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(1)),
        };

        //act
        var httpResponse = await webClient.PostAsJsonAsync("/api/listings/", request);

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "we expect an HTTP 400 when calling POST /api/listings with a listing that has a start date after the end date");
    }

    [TestMethod]
    public async Task DeleteListing_Valid_Returns200()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ListingDto
        {
            Name = "Good games",
            Description = "Stuff",
            Price = 999,
            StartUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2)),
            EndUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1)),
        };
        await using var handle = await CreateListing(webClient, request);
        if (handle == null)
        {
            Assert.Fail("You are not ready for this test");
        }

        //act
        var httpResponse = await webClient.DeleteAsync($"/api/listings/{request.Id}");

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK, "we expect an HTTP 200 when calling DELETE /api/listings/{id} with a valid id");
    }

    [TestMethod]
    public async Task DeleteListing_SameIdTwice_Returns404()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var request = new ListingDto
        {
            Name = "Good games",
            Description = "Stuff",
            Price = 999,
            StartUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2)),
            EndUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1)),
        };
        await using var handle = await CreateListing(webClient, request);
        if (handle == null)
        {
            Assert.Fail("You are not ready for this test");
        }

        //act
        await webClient.DeleteAsync($"/api/listings/{request.Id}");
        var httpResponse = await webClient.DeleteAsync($"/api/listings/{request.Id}");

        //assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "we expect an HTTP 404 when calling DELETE /api/listings/{id} with the same id twice");
    }

    [TestMethod]
    public async Task SetListingItems_AllProducts_Returns204()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var listingDto = new ListingDto
        {
            Name = "Good games",
            Description = "Stuff",
            Price = 999,
            StartUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2)),
            EndUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1)),
        };
        await using var handle = await CreateListing(webClient, listingDto);
        if (handle == null)
        {
            Assert.Fail("You are not ready for this test");
        }

        var products = await GetProducts(webClient);
        if (products == null || !products.Any())
        {
            Assert.Fail("You are not ready for this test");
        }

        var items = new List<ItemDto>();
        foreach (var productDto in products)
        {
            var item = new ItemDto
            {
                Condition = "GReat",
                ProductId = productDto.Id
            };
            if (await CreateItem(webClient, item) == null)
            {
                Assert.Fail("You are not ready for this test");
            }
            items.Add(new ItemDto
            {
                Id = item.Id
            });
        }

        //act
        var httpResponse = await webClient.PutAsJsonAsync($"/api/listings/{listingDto.Id}/items", items);

        //assert
        await AssertSetListingItemsFunctions(httpResponse, items, listingDto, webClient);
    }

    [TestMethod]
    public async Task SetListingItems_ActiveListingAndAllProducts_Returns204()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var listingDto = new ListingDto
        {
            Name = "Good games",
            Description = "Stuff",
            Price = 999,
            StartUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2)),
            EndUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(1)),
        };
        await using var handle = await CreateListing(webClient, listingDto);
        if (handle == null)
        {
            Assert.Fail("You are not ready for this test");
        }

        var products = await GetProducts(webClient);
        if (products == null || !products.Any())
        {
            Assert.Fail("You are not ready for this test");
        }

        var items = new List<ItemDto>();
        foreach (var productDto in products)
        {
            var item = new ItemDto
            {
                Condition = "GReat",
                ProductId = productDto.Id
            };
            if (await CreateItem(webClient, item) == null)
            {
                Assert.Fail("You are not ready for this test");
            }
            items.Add(new ItemDto
            {
                Id = item.Id
            });
        }

        //act
        var httpResponse = await webClient.PutAsJsonAsync($"/api/listings/{listingDto.Id}/items", items);

        //assert
        await AssertSetListingItemsFunctions(httpResponse, items, listingDto, webClient);
    }

    [TestMethod]
    public async Task SetListingItems_TwoDifferntSets_LastOneWins()
    {
        //arrange
        var webClient = context.GetStandardWebClient();
        var listingDto = new ListingDto
        {
            Name = "Good games",
            Description = "Stuff",
            Price = 999,
            StartUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2)),
            EndUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1)),
        };
        await using var handle = await CreateListing(webClient, listingDto);
        if (handle == null)
        {
            Assert.Fail("You are not ready for this test");
        }

        var products = await GetProducts(webClient);
        if (products == null || !products.Any())
        {
            Assert.Fail("You are not ready for this test");
        }

        var items = new List<ItemDto>();
        foreach (var productDto in products)
        {
            var item = new ItemDto
            {
                Condition = "GReat",
                ProductId = productDto.Id
            };
            if (await CreateItem(webClient, item) == null)
            {
                Assert.Fail("You are not ready for this test");
            }
            items.Add(new ItemDto
            {
                Id = item.Id
            });
        }

        //first time
        var itemsA = items.Take(1).ToList();
        var httpResponseA = await webClient.PutAsJsonAsync($"/api/listings/{listingDto.Id}/items", itemsA);
        await AssertSetListingItemsFunctions(httpResponseA, itemsA, listingDto, webClient);

        //second time
        var itemsB = items.Skip(1).ToList();
        var httpResponseB = await webClient.PutAsJsonAsync($"/api/listings/{listingDto.Id}/items", itemsB);
        await AssertSetListingItemsFunctions(httpResponseB, itemsB, listingDto, webClient);
    }

    private static async Task AssertSetListingItemsFunctions(HttpResponseMessage httpResponse, List<ItemDto> items, ListingDto listingDto, HttpClient webClient)
    {
        httpResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, "we expect an HTTP 204 when calling PUT /api/listings/{id}/item with valid data to set the listing items");

        var getListingItems = await webClient.GetAsync($"/api/listings/{listingDto.Id}/items");
        getListingItems.StatusCode.Should().Be(HttpStatusCode.OK, "we should get back a list (even if it is empty) of the listing items for sale when calling GET /api/listings/{id}/items");
        var listingItems = await getListingItems.Content.ReadAsJsonAsync<List<ItemDto>>();
        Assert.IsNotNull(listingItems, "we should get back a list (even if it is empty) of the listing items for sale when calling GET /api/listings/{id}/items");

        listingItems.Should().HaveCount(items.Count, "counts should match calling GET /api/listings/{id}/items after setting the items for sale via PUT /api/listings/{id}/items");
        foreach (var itemDto in items)
        {
            listingItems.Should().Contain(x => x.Id == itemDto.Id, "GET /api/listings/{id}/items should return the set of items marked for sale in the provided listing id");
        }

        if (listingDto.StartUtc <= DateTimeOffset.UtcNow && DateTimeOffset.UtcNow <= listingDto.EndUtc)
        {
            foreach (var itemDto in listingItems)
            {
                var getProductListing = await webClient.GetAsync($"/api/products/{itemDto.ProductId}/listings");
                var productListingData = await getProductListing.Content.ReadAsJsonAsync<List<ListingDto>>();
                Assert.IsNotNull(productListingData, "we expect json data when calling GET /api/products/{id}/listings");

                productListingData.Should().ContainEquivalentOf(listingDto, "we expect that an active listing with some item has a relevant product listing under GET /api/products/{id}/listings");

                if (productListingData.GroupBy(x => x.Id).Any(x => x.Count() > 1))
                {
                    Assert.Fail("Your GET /api/products/{id}/listings endpoint is returning the same ListingDto more than once");
                }
            }
        }
    }

    private static async Task<IAsyncDisposable?> CreateListing(HttpClient webClient, ListingDto request)
    {
        try
        {
            var httpResponse = await webClient.PostAsJsonAsync("/api/listings", request);
            var resultDto = await AssertCreateListingFunctions(httpResponse, request, webClient);
            request.Id = resultDto.Id;
            return new DeleteListing(resultDto, webClient);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<ListingDto> AssertCreateListingFunctions(HttpResponseMessage httpResponse, ListingDto request, HttpClient webClient)
    {
        httpResponse.StatusCode.Should().Be(HttpStatusCode.Created, "we expect an HTTP 201 when calling POST /api/listings with valid data to create a new listing");

        var resultDto = await httpResponse.Content.ReadAsJsonAsync<ListingDto>();
        Assert.IsNotNull(resultDto, "We expect json data when calling POST /api/listings");

        resultDto.Id.Should().BeGreaterOrEqualTo(1, "we expect a newly created listing to return with a positive Id");
        resultDto.Should().BeEquivalentTo(request, x => x.Excluding(y => y.Id), "We expect the create listing endpoint to return the result");

        httpResponse.Headers.Location.Should().NotBeNull("we expect the 'location' header to be set as part of a HTTP 201");
        httpResponse.Headers.Location.Should().Be($"http://localhost/api/listings/{resultDto.Id}", "we expect the location header to point to the get listing by id endpoint");

        var getByIdResult = await webClient.GetAsync($"/api/listings/{resultDto.Id}");
        getByIdResult.StatusCode.Should().Be(HttpStatusCode.OK, "we should be able to get the newly created listing by id as GET /api/listings/{id}");
        var dtoById = await getByIdResult.Content.ReadAsJsonAsync<ListingDto>();
        dtoById.Should().BeEquivalentTo(resultDto, "we expect the same result to be returned by a create listing as what you'd get from get listing by id");

        if (request.StartUtc <= DateTimeOffset.UtcNow && DateTimeOffset.UtcNow <= request.EndUtc)
        {
            var getAllRequest = await webClient.GetAsync("/api/listings/active");

            var listAllData = await getAllRequest.Content.ReadAsJsonAsync<List<ListingDto>>();
            Assert.IsNotNull(listAllData, "We expect json data when calling GET /api/listings/active");
            var matchingItem = listAllData.Where(x => x.Id == resultDto.Id).ToArray();
            matchingItem.Should().HaveCount(1, "we should be a be able to find this newly created listing by id in the list all endpoint as it was created while being active");
            matchingItem[0].Should().BeEquivalentTo(resultDto, "we expect the same result from get all active listings compared to a create listing");
        }

        return resultDto;
    }

    private static async Task<IAsyncDisposable?> CreateItem(HttpClient webClient, ItemDto request)
    {
        try
        {
            var httpResponse = await webClient.PostAsJsonAsync("/api/items", request);
            var resultDto = await AssertCreateItemFunctions(httpResponse, request, webClient);
            request.Id = resultDto.Id;
            request.ProductName = resultDto.ProductName;
            return new DeleteItem(resultDto, webClient);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task AssertItemUpdateFunctions(HttpResponseMessage httpResponse, ItemDto request, HttpClient webClient)
    {
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK, "we expect an HTTP 200 when calling PUT /api/items/{id} with valid data to update a item");
        var resultDto = await httpResponse.Content.ReadAsJsonAsync<ItemDto>();
        resultDto.Should().BeEquivalentTo(request, x => x.Excluding(y => y.ProductName), "We expect the update item endpoint to return the result");

        var getByIdResult = await webClient.GetAsync($"/api/items/{request.Id}");
        getByIdResult.StatusCode.Should().Be(HttpStatusCode.OK, "we should be able to get the updated item by id");
        var dtoById = await getByIdResult.Content.ReadAsJsonAsync<ItemDto>();
        dtoById.Should().BeEquivalentTo(request, "we expect the same result to be returned by a update item as what you'd get from get item by id");
    }

    private static async Task<ItemDto> AssertCreateItemFunctions(HttpResponseMessage httpResponse, ItemDto request, HttpClient webClient)
    {
        httpResponse.StatusCode.Should().Be(HttpStatusCode.Created, "we expect an HTTP 201 when calling POST /api/items with valid data to create a new item");

        var resultDto = await httpResponse.Content.ReadAsJsonAsync<ItemDto>();
        Assert.IsNotNull(resultDto, "We expect json data when calling POST /api/items");

        resultDto.Id.Should().BeGreaterOrEqualTo(1, "we expect a newly created item to return with a positive Id");
        resultDto.Should().BeEquivalentTo(request, x => x.Excluding(y => y.Id).Excluding(y => y.ProductName), "We expect the create item endpoint to return the result");

        httpResponse.Headers.Location.Should().NotBeNull("we expect the 'location' header to be set as part of a HTTP 201");
        httpResponse.Headers.Location.Should().Be($"http://localhost/api/items/{resultDto.Id}", "we expect the location header to point to the get item by id endpoint");

        var getByIdResult = await webClient.GetAsync($"/api/items/{resultDto.Id}");
        getByIdResult.StatusCode.Should().Be(HttpStatusCode.OK, "we should be able to get the newly created item by id");
        var dtoById = await getByIdResult.Content.ReadAsJsonAsync<ItemDto>();
        dtoById.Should().BeEquivalentTo(resultDto, "we expect the same result to be returned by a create item as what you'd get from get item by id");

        return resultDto;
    }

    private async Task AssertProductUpdateFunctions(HttpResponseMessage httpResponse, ProductDto request, HttpClient webClient)
    {
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK, "we expect an HTTP 200 when calling PUT /api/products/{id} with valid data to update a product");
        var resultDto = await httpResponse.Content.ReadAsJsonAsync<ProductDto>();
        resultDto.Should().BeEquivalentTo(request, "We expect the update product endpoint to return the result");

        var getByIdResult = await webClient.GetAsync($"/api/products/{request.Id}");
        getByIdResult.StatusCode.Should().Be(HttpStatusCode.OK, "we should be able to get the updated product by id");
        var dtoById = await getByIdResult.Content.ReadAsJsonAsync<ProductDto>();
        dtoById.Should().BeEquivalentTo(request, "we expect the same result to be returned by a update product as what you'd get from get product by id");

        var getAllRequest = await webClient.GetAsync("/api/products");
        await AssertProductListAllFunctions(getAllRequest);

        var listAllData = await getAllRequest.Content.ReadAsJsonAsync<List<ProductDto>>();
        Assert.IsNotNull(listAllData, "We expect json data when calling GET /api/products");
        listAllData.Should().NotBeEmpty("list all should have something if we just updated a product");
        var matchingItem = listAllData.Where(x => x.Id == request.Id).ToArray();
        matchingItem.Should().HaveCount(1, "we should be a be able to find the newly created product by id in the list all endpoint");
        matchingItem[0].Should().BeEquivalentTo(request, "we expect the same result to be returned by a updated product as what you'd get from get getting all products");
    }

    private async Task<IAsyncDisposable?> CreateProduct(HttpClient webClient, ProductDto request)
    {
        try
        {
            var httpResponse = await webClient.PostAsJsonAsync("/api/products", request);
            var resultDto = await AssertCreateProductFunctions(httpResponse, request, webClient);
            request.Id = resultDto.Id;
            return new DeleteProduct(resultDto, webClient);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<ProductDto> AssertCreateProductFunctions(HttpResponseMessage httpResponse, ProductDto request, HttpClient webClient)
    {
        httpResponse.StatusCode.Should().Be(HttpStatusCode.Created, "we expect an HTTP 201 when calling POST /api/products with valid data to create a new product");

        var resultDto = await httpResponse.Content.ReadAsJsonAsync<ProductDto>();
        Assert.IsNotNull(resultDto, "We expect json data when calling POST /api/products");

        resultDto.Id.Should().BeGreaterOrEqualTo(1, "we expect a newly created product to return with a positive Id");
        resultDto.Should().BeEquivalentTo(request, x => x.Excluding(y => y.Id), "We expect the create product endpoint to return the result");

        httpResponse.Headers.Location.Should().NotBeNull("we expect the 'location' header to be set as part of a HTTP 201");
        httpResponse.Headers.Location.Should().Be($"http://localhost/api/products/{resultDto.Id}", "we expect the location header to point to the get product by id endpoint");

        var getByIdResult = await webClient.GetAsync($"/api/products/{resultDto.Id}");
        getByIdResult.StatusCode.Should().Be(HttpStatusCode.OK, "we should be able to get the newly created product by id");
        var dtoById = await getByIdResult.Content.ReadAsJsonAsync<ProductDto>();
        dtoById.Should().BeEquivalentTo(resultDto, "we expect the same result to be returned by a create product as what you'd get from get product by id");

        var getAllRequest = await webClient.GetAsync("/api/products");
        await AssertProductListAllFunctions(getAllRequest);

        var listAllData = await getAllRequest.Content.ReadAsJsonAsync<List<ProductDto>>();
        Assert.IsNotNull(listAllData, "We expect json data when calling GET /api/products");
        listAllData.Should().NotBeEmpty("list all should have something if we just created a product");
        var matchingItem = listAllData.Where(x => x.Id == resultDto.Id).ToArray();
        matchingItem.Should().HaveCount(1, "we should be a be able to find the newly created product by id in the list all endpoint");
        matchingItem[0].Should().BeEquivalentTo(resultDto, "we expect the same result to be returned by a created product as what you'd get from get getting all products");

        return resultDto;
    }

    private static async Task<List<ProductDto>?> GetProducts(HttpClient webClient)
    {
        try
        {
            var getAllRequest = await webClient.GetAsync("/api/products");
            var getAllResult = await AssertProductListAllFunctions(getAllRequest);
            return getAllResult.ToList();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<ProductDto?> GetProduct(HttpClient webClient)
    {
        try
        {
            var getAllRequest = await webClient.GetAsync("/api/products");
            var getAllResult = await AssertProductListAllFunctions(getAllRequest);
            return getAllResult.OrderByDescending(x => x.Id).First();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<List<ProductDto>> AssertProductListAllFunctions(HttpResponseMessage httpResponse)
    {
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK, "we expect an HTTP 200 when calling GET /api/products");
        var resultDto = await httpResponse.Content.ReadAsJsonAsync<List<ProductDto>>();
        Assert.IsNotNull(resultDto, "We expect json data when calling GET /api/products");
        resultDto.Should().HaveCountGreaterThan(2, "we expect at least 3 products");
        resultDto.All(x => !string.IsNullOrWhiteSpace(x.Name)).Should().BeTrue("we expect all products to have names");
        resultDto.All(x => !string.IsNullOrWhiteSpace(x.Description)).Should().BeTrue("we expect all products to have descriptions");
        resultDto.All(x => x.Id > 0).Should().BeTrue("we expect all products to have an id");
        var ids = resultDto.Select(x => x.Id).ToArray();
        ids.Should().HaveSameCount(ids.Distinct(), "we expect Id values to be unique for every product");
        return resultDto;
    }

    private class ProductDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    private class ItemDto
    {
        public int Id { get; set; }
        public string? Condition { get; set; }
        public string? ProductName { get; set; }
        public int? ProductId { get; set; }
    }

    private class ListingDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public DateTimeOffset? StartUtc { get; set; }
        public DateTimeOffset? EndUtc { get; set; }
    }

    private sealed class DeleteProduct : IAsyncDisposable
    {
        private readonly ProductDto request;
        private readonly HttpClient webClient;

        public DeleteProduct(ProductDto request, HttpClient webClient)
        {
            this.request = request;
            this.webClient = webClient;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await webClient.DeleteAsync($"/api/products/{request.Id}");
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    private class DeleteItem : IAsyncDisposable
    {
        private readonly ItemDto request;
        private readonly HttpClient webClient;

        public DeleteItem(ItemDto request, HttpClient webClient)
        {
            this.request = request;
            this.webClient = webClient;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await webClient.DeleteAsync($"/api/items/{request.Id}");
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    private class DeleteListing : IAsyncDisposable
    {
        private readonly ListingDto request;
        private readonly HttpClient webClient;

        public DeleteListing(ListingDto request, HttpClient webClient)
        {
            this.request = request;
            this.webClient = webClient;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await webClient.DeleteAsync($"/api/listings/{request.Id}");
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
