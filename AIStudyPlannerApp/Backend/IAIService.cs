using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIStudyPlannerApp.Backend
{
    public interface IAIService
    {
        Task<string> GetReplyAsync(
            List<(string Role, string Content)> history,
            string? studyContext = null);
    }
}