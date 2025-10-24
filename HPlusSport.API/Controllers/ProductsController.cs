using HPlusSport.API.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HPlusSport.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HPlusSport.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ShopDbContext _context;

        public ProductsController(ShopDbContext context)
        {
            _context = context;
            _context.Database.EnsureCreated();
        }

        [HttpGet]
        public async Task<ActionResult> GetAllProducts([FromQuery] ProductQueryParameters queryParams)
        {
            IQueryable<Product> products = _context.Products;

            if (queryParams.MinPrice != null)
            {
                products = products.Where(p => p.Price >= queryParams.MinPrice.Value);
            }

            if (queryParams.MaxPrice != null)
            {
                products = products.Where(p => p.Price <= queryParams.MaxPrice.Value);
            }

            if (!string.IsNullOrEmpty(queryParams.Sku))
            {
                products = products.Where(p => p.Sku
                    .ToLower()
                    .Contains(queryParams.Sku.ToLower())
                );
            }

            if (!string.IsNullOrEmpty(queryParams.Name))
            {
                products = products.Where(p => p.Name
                    .ToLower()
                    .Contains(queryParams.Name.ToLower())
                );
            }

            if (!string.IsNullOrEmpty(queryParams.SortBy))
            {
                if (typeof(Product).GetProperty(queryParams.SortBy) != null)
                {
                    products = products.OrderByCustom(
                        queryParams.SortBy,
                        queryParams.SortOrder);
                }
            }

            products = products
                .Skip(queryParams.Size * (queryParams.Page - 1))
                .Take(queryParams.Size);

            var paginatedProducts = await products.ToArrayAsync();
            return Ok(paginatedProducts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();
            return Ok(product);
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<Product>>> GetAvailableProducts()
        {
            var products = await _context.Products.Where(p => p.IsAvailable).ToArrayAsync();

            return Ok(products);
        }

        [HttpPost]
        public async Task<ActionResult> CreateProduct([FromBody] Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                    nameof(GetProduct),
                    new { id = product.Id},
                    product
                );
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(p => p.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("Delete")]
        public async Task<ActionResult> DeleteMultiple([FromQuery] int[] ids)
        {
            var products = new List<Product>();
            foreach (var id in ids)
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                products.Add(product);
            }

            _context.Products.RemoveRange(products);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
