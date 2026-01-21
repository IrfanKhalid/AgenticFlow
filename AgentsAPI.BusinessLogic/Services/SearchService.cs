using System.Threading;
using System.Threading.Tasks;

namespace AgentsAPI.BusinessLogic.Services
{
    public class SearchService : ISearchService
    {
        private readonly AgentsAPI.Agents.ICrawlerAgent _crawlerAgent;

        public SearchService(AgentsAPI.Agents.ICrawlerAgent crawlerAgent)
        {
            _crawlerAgent = crawlerAgent;
        }

        public Task<string> SearchWithGoogleAsync(string query)
        {
            return _crawlerAgent.SearchAsync(query, CancellationToken.None);
        }
    }
}