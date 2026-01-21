using System.Collections.Generic;

namespace AgentsAPI.BusinessLogic.Services
{
    public interface ISearchService
    {
        Task<string> SearchWithGoogleAsync(string query);
    }
}