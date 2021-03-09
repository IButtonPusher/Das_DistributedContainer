using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Global

namespace Das.Container
{
    // ReSharper disable once UnusedType.Global
    public interface IResolver
    {
        T Resolve<T>();

        /// <summary>
        ///     Resolves an object by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctorArgs">
        ///     Arguments that must match, in order constructor arguments
        ///     that cannot be resolved
        /// </param>
        T Resolve<T>(params Object[] ctorArgs);

        Object Resolve(Type type);

        Task<T> ResolveAsync<T>();

        Task<T> ResolveAsync<T>(CancellationToken cancellation);


        /// <summary>
        ///     Resolves an object by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctorArgs">
        ///     Arguments that must match, in order constructor arguments
        ///     that cannot be resolved
        /// </param>
        Task<T> ResolveAsync<T>(params Object[] ctorArgs);

        Task<Object> ResolveAsync(Type type);

        Task<Object> ResolveAsync(Type type,
                                  CancellationToken cancellation);

        /// <summary>
        ///     Specifies a concrete type to resolve an interface against.  No object of the type is provided
        /// </summary>
        /// <typeparam name="TContract">The type the seeded object can be loaded with</typeparam>
        /// <typeparam name="TObject">The type of the object being seeded</typeparam>
        void ResolveTo<TContract, TObject>()
            where TObject : class, TContract;

        /// <summary>
        ///     Seeds an existing object of type TObject mapped to type TContract
        /// </summary>
        /// <typeparam name="TContract">The type the seeded object can be loaded with</typeparam>
        /// <param name="instance">The object being seeded</param>
        void ResolveTo<TContract>(TContract instance);

        /// <summary>
        ///     Specifies a concrete type to resolve an interface against.  No object of the type is provided
        /// </summary>
        /// <typeparam name="TContract">The type the seeded object can be loaded with</typeparam>
        /// <typeparam name="TObject">The type of the object being seeded</typeparam>
        Task ResolveToAsync<TContract, TObject>()
            where TObject : class, TContract;

        /// <summary>
        ///     Specifies a concrete type to resolve an interface against.  No object of the type is provided
        /// </summary>
        /// <typeparam name="TContract">The type the seeded object can be loaded with</typeparam>
        /// <typeparam name="TObject">The type of the object being seeded</typeparam>
        Task ResolveToAsync<TContract, TObject>(TObject instance)
            where TObject : class, TContract;

        /// <summary>
        ///     Seeds an existing object mapped to type TContract
        /// </summary>
        /// <typeparam name="TInstance">
        ///     The type the seeded object can be loaded with.
        ///     Can be the actual type or a base class/interface
        /// </typeparam>
        /// <param name="instance">The object being seeded</param>
        Task ResolveToAsync<TInstance>(TInstance instance);

        Boolean TryResolve<TInstance>(Type type,
                                      out TInstance resolved);
    }
}
