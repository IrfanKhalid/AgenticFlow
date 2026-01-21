using AgentsAPI.BusinessLogic.Services;
using AgentsAPI.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;

namespace AgentsAPI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly IItemService _itemService;

        public ItemsController(IItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Item>> GetAll()
        {
            return Ok(_itemService.GetAllItems());
        }

        [HttpGet("{id}")]
        public ActionResult<Item> GetById(int id)
        {
            var item = _itemService.GetItemById(id);
            if (item == null)
            {
                return NotFound();
            }
            return Ok(item);
        }

        [HttpPost]
        public ActionResult<Item> Create([FromBody] Item item)
        {
            _itemService.CreateItem(item);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Item item)
        {
            if (id != item.Id)
            {
                return BadRequest();
            }
            _itemService.UpdateItem(item);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _itemService.DeleteItem(id);
            return NoContent();
        }
    }
}