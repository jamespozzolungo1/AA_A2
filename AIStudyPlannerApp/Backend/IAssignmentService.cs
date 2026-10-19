using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AIStudyPlannerApp.Backend
{
    public interface IAssignmentService {
        Task<List<AssignmentRecord>> GetAllAsync(
            int userId, CancellationToken cancellationToken = default);
        
        Task<int> AddAsync(
            int userId, string title, DateTime dueDate,
            string courseName = "General", string description = "",
            string priority = "Medium",
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            int userId, int assignmentId, string title, DateTime dueDate,
            string courseName = "General", string description = "",
            string priority = "Medium", string status = "Not Started",
            CancellationToken cancellationToken = default);
        
        Task DeleteAsync(
            int userId, int assignmentId,
            CancellationToken cancellationToken = default);
    }
}