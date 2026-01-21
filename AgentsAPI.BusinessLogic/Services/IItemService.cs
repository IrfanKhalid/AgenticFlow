using AgentsAPI.DataAccess.Models;

namespace AgentsAPI.BusinessLogic.Services
{
    public interface IItemService
    {
        IEnumerable<Item> GetAllItems();
        Item GetItemById(int id);
        void CreateItem(Item item);
        void UpdateItem(Item item);
        void DeleteItem(int id);
    }
}