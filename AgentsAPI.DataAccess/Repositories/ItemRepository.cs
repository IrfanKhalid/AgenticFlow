using AgentsAPI.DataAccess.Models;

namespace AgentsAPI.DataAccess.Repositories
{
    public class ItemRepository : IItemRepository
    {
        private readonly List<Item> _items = new List<Item>();
        private int _nextId = 1;

        public IEnumerable<Item> GetAll()
        {
            return _items;
        }

        public Item GetById(int id)
        {
            return _items.FirstOrDefault(i => i.Id == id);
        }

        public void Add(Item item)
        {
            item.Id = _nextId++;
            _items.Add(item);
        }

        public void Update(Item item)
        {
            var existing = GetById(item.Id);
            if (existing != null)
            {
                existing.Name = item.Name;
                existing.Description = item.Description;
            }
        }

        public void Delete(int id)
        {
            var item = GetById(id);
            if (item != null)
            {
                _items.Remove(item);
            }
        }
    }
}