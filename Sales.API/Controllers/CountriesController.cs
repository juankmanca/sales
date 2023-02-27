using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales.API.Data;
using Sales.Share.entities;

namespace Sales.API.Controllers
{
    [Route("/api/countries")]
    [ApiController]
    public class CountriesController: ControllerBase
    {
        private readonly DataContext _dataContext;
        public CountriesController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(Country country)
        {
            try
            {
                _dataContext.Add(country);
                await _dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (DbUpdateException dbUpdateException)
            {
                if(dbUpdateException.InnerException!.Message.Contains("duplicate"))
                {
                    return BadRequest("Ya existe un país con el mismo nombre.");
                }

                return BadRequest(dbUpdateException.Message);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message); 
            }
        }

        [HttpPut()]
        public async Task<IActionResult> PutAsync(Country country)
        {
            try
            {
                _dataContext.Update(country);
                await _dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (DbUpdateException dbUpdateException)
            {
                if (dbUpdateException.InnerException!.Message.Contains("duplicate"))
                {
                    return BadRequest("Ya existe un país con el mismo nombre.");
                }

                return BadRequest(dbUpdateException.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            // include == Inner Join with States
            return Ok(await _dataContext.Countries
                .Include(country => country.States)
                .ToListAsync());
        }

        [HttpGet("full")]
        public async Task<IActionResult> GetFullAsync()
        {
            // include == Inner Join with States
            return Ok(await _dataContext.Countries
                .Include(x => x.States!)
                .ThenInclude(x => x.Cities)
                .ToListAsync());
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var country = await _dataContext.Categories.FirstOrDefaultAsync(x => x.Id == id);
            if(country == null)
            {
                return NotFound();
            }

            _dataContext.Categories.Remove(country);
            await _dataContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAsync(int id)
        {
            var country = await _dataContext.Countries
                .Include(x => x.States!)
                .ThenInclude(x => x.Cities)
                .FirstOrDefaultAsync(x => x.Id == id);
            if(country == null)
            {
                return NotFound();
            }
            return Ok(country);
        }
    }
}
