
using MyVexBooks.Models.Entities;

namespace MyVexBooks.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {

        public Repository(LibrosvexContext context)
        {
            Context = context;
        }

        public LibrosvexContext Context { get; }


        public virtual T? Get(object id)
        {
            return
                Context.Find<T>(id);
        }
        public virtual IEnumerable<T> GetAll()
        {
            return Context.Set<T>();
        }
        public virtual void Insert(T entity)
        {
            Context.Add(entity);
            Context.SaveChanges();
        }
        public virtual void Update(T entity)
        {
            Context.Update(entity);
            Context.SaveChanges();
        }


        public virtual void Delete(object id)
        {
            var entity = Context.Find<T>(id);
            if (entity != null)
            {
                Context.Remove(entity);
                Context.SaveChanges();
            }
        }
    }
}
