using System;
using System.Threading.Tasks;

namespace Das.Container
{
    public interface IDeferredLoader<TValue> : IDeferredLoader
    {
        Boolean TrySetResult(TValue value);
    }

    public interface IDeferredLoader
    {
        
        //void NotifyOfLoad<T>(T obj);

        /// <summary>
        /// If this loader has a dependency on T then this may indirectly set the result
        /// </summary>
        void NotifyOfLoad(Object obj,
                          Type contractType);

        Task<Object> GetAwaiter();
    }
}
