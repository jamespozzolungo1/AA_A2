
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIStudyPlannerApp.Backend
{
    public interface IRepository<T> where T : StudyItem
    {
        IReadOnlyList<T> GetAll();

        T? GetById(Guid id);

        void Add(T item);

        bool Update(T item);

        bool Delete(Guid id);
    }

    public interface IDataService
    {
        Task SaveAsync<T>(string fileName, IReadOnlyCollection<T> items);

        Task<List<T>> LoadAsync<T>(string fileName);
    }
}
