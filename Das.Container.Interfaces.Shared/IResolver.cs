using System;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace Das.Container
{
    // ReSharper disable once UnusedType.Global
    public interface IResolver
    {
        /// <summary>
        /// Specifies a concrete type to resolve an interface against.  No object of the type is provided
        /// </summary>
        /// <typeparam name="TInterface">The type the seeded object can be loaded with</typeparam>
        /// <typeparam name="TObject">The type of the object being seeded</typeparam>
        Task ResolveToAsync<TInterface, TObject>()
            where TObject : class, TInterface;

        /// <summary>
        /// Specifies a concrete type to resolve an interface against.  No object of the type is provided
        /// </summary>
        /// <typeparam name="TInterface">The type the seeded object can be loaded with</typeparam>
        /// <typeparam name="TObject">The type of the object being seeded</typeparam>
        void ResolveTo<TInterface, TObject>()
            where TObject : class, TInterface;

        /// <summary>
        /// Seeds an existing object mapped to type TInterface
        /// </summary>
        /// <typeparam name="TInterface">The type the seeded object can be loaded with</typeparam>
        /// <param name="instance">The object being seeded</param>
        Task ResolveToAsync<TInterface>(TInterface instance);

        /// <summary>
        /// Seeds an existing object of type TObject mapped to type TInterface
        /// </summary>
        /// <typeparam name="TInterface">The type the seeded object can be loaded with</typeparam>
        /// <param name="instance">The object being seeded</param>
        void ResolveTo<TInterface>(TInterface instance);

        Task<T> ResolveAsync<T>();

        T Resolve<T>();


        /// <summary>
        /// Resolves an object by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctorArgs">Arguments that must match, in order constructor arguments
        /// that cannot be resolved</param>
        Task<T> ResolveAsync<T>(params Object[] ctorArgs);

        /// <summary>
        /// Resolves an object by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctorArgs">Arguments that must match, in order constructor arguments
        /// that cannot be resolved</param>
        T Resolve<T>(params Object[] ctorArgs);

        Task<Object> ResolveAsync(Type type);

        Object Resolve(Type type);
    }
}
