using System.Threading;
using System.Threading.Tasks;

namespace AgentsAPI.Agents
{
    public interface ICrawlerAgent
    {
        Task<string> SearchAsync(string query, CancellationToken cancellationToken = default);
    }
}