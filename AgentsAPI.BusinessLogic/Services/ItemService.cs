using AgentsAPI.DataAccess.Models;
using AgentsAPI.DataAccess.Repositories;

namespace AgentsAPI.BusinessLogic.Services
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepository;

        public ItemService(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public IEnumerable<Item> GetAllItems()
        {
            return _itemRepository.GetAll();
        }

        public Item GetItemById(int id)
        {
            return _itemRepository.GetById(id);
        }

        public void CreateItem(Item item)
        {
            // Add any business logic here, e.g., validation
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                throw new ArgumentException("Name is required.");
            }
            _itemRepository.Add(item);
        }

        public void UpdateItem(Item item)
        {
            // Business logic
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                throw new ArgumentException("Name is required.");
            }
            _itemRepository.Update(item);
        }

        public void DeleteItem(int id)
        {
            _itemRepository.Delete(id);
        }
    }
}