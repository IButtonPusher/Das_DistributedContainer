using System;
using System.Threading.Tasks;

namespace Das.Container
{
    /// <summary>
    ///     Implemented by types that require asynchronous loading to be performed
    ///     after the constructor finishes
    /// </summary>
    public interface IInitializeAsync
    {
        /// <summary>
        ///     Called to perform additional asynchronous initialization logic.
        ///     After this completes, the object is considered to be fully instantiated.
        /// </summary>
        Task InitializeAsync();
    }
}