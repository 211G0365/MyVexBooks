namespace MyVexBooks.Repositories
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetAll();
        T? Get(object id);
        void Insert(T entity);
        void Update(T entity);
        void Delete(object id);
    }
}
