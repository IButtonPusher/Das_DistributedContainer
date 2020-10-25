using System;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace Das.Container
{
    public interface IResolver
    {
        /// <summary>
        /// Seeds an existing object of type TObject mapped to type TInterface
        /// </summary>
        /// <typeparam name="TInterface">The type the seeded object can be loaded with</typeparam>
        /// <typeparam name="TObject">The type of the object being seeded</typeparam>
        /// <param name="obj">The object being seeded</param>
        Task ResolveToAsync<TInterface, TObject>(TObject obj)
            where TObject : class, TInterface;

        /// <summary>
        /// Specifies a concrete type to resolve an interface against.  No object of the type is provided
        /// </summary>
        /// <typeparam name="TInterface">The type the seeded object can be loaded with</typeparam>
        /// <typeparam name="TObject">The type of the object being seeded</typeparam>
        Task ResolveToAsync<TInterface, TObject>()
            where TObject : class, TInterface;

        Task<TInterface> ResolveAsync<TObject, TInterface>()
            where TObject : TInterface;

        Task<T> ResolveAsync<T>();

        /// <summary>
        /// Resolves an object by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctorArgs">Arguments that must match, in order constructor arguments
        /// that cannot be resolved</param>
        Task<T> ResolveAsync<T>(params Object[] ctorArgs);

        Task<Object> ResolveAsync(Type type);
    }
}
