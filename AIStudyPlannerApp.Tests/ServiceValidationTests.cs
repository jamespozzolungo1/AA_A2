using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using AIStudyPlannerApp.Backend;

namespace AIStudyPlannerApp.Tests
{
    [TestFixture]
    public class ServiceValidationTests
    {

        [Test]
        public void AddAssignment_WithEmptyTitle_ThrowsArgumentException()
        {
            Environment.SetEnvironmentVariable("AI_STUDY_PLANNER_DB_PASSWORD", "test-password");
            var service = new AssignmentService(new DatabaseService());

            Assert.ThrowsAsync<ArgumentException> (async () =>
                await service.AddAsync(
                    AssignmentService.TestUserId,
                    "",
                    DateTime.Today));
        }
    }
}
