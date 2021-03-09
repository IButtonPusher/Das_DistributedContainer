using System;
using System.Threading;
using System.Threading.Tasks;

#if NET40
using SemaphoreSlim = System.Threading.AsyncSemaphore;
#endif

namespace Das.Container
{
    public static class SemaphoreExtensions
    {
        public static TOutput RunLockedFunc<TInput1, TInput2, TOutput>(
            this SemaphoreSlim semaphore,
            TInput1 input1,
            TInput2 input2,
            Func<TInput1, TInput2, TOutput> action)
        {
            semaphore.Wait();

            try
            {
                return action(input1, input2);
            }
            finally
            {
                semaphore.Release();
            }
        }


        public static TOutput RunLockedFunc<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput>(
            this SemaphoreSlim semaphore,
            TInput1 input1,
            TInput2 input2,
            TInput3 input3,
            TInput4 input4,
            TInput5 input5,
            Func<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput> action)
        {
            semaphore.Wait();

            try
            {
                return action(input1, input2, input3, input4, input5);
            }
            finally
            {
                semaphore.Release();
            }
        }


        public static async Task<TOutput> RunLockedFuncAsync<TInput1, TInput2, TInput3, TInput4, TOutput>(
            this SemaphoreSlim semaphore,
            TInput1 input1,
            TInput2 input2,
            TInput3 input3,
            TInput4 input4,
            Func<TInput1, TInput2, TInput3, TInput4, TOutput> func,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return func(input1, input2, input3, input4);
            }
            finally
            {
                semaphore.Release();
            }
        }


        public static async Task<TOutput> RunLockedFuncAsync<TInput1, TInput2, TInput3, TOutput>(
            this SemaphoreSlim semaphore,
            TInput1 input1,
            TInput2 input2,
            TInput3 input3,
            Func<TInput1, TInput2, TInput3, TOutput> func,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return func(input1, input2, input3);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static async Task<TOutput> RunLockedFuncAsync<TInput1, TInput2, TOutput>(
            this SemaphoreSlim semaphore,
            Func<TInput1, TInput2, TOutput> func,
            TInput1 input1,
            TInput2 input2,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return func(input1, input2);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static async Task<TOutput> RunLockedFuncAsync<TInput, TOutput>(
            this SemaphoreSlim semaphore,
            Func<TInput, TOutput> func,
            TInput input,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return func(input);
            }
            finally
            {
                semaphore.Release();
            }
        }


        public static async Task<TOutput> RunLockedFuncAsync<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput>(
            this SemaphoreSlim semaphore,
            Func<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput> func,
            TInput1 input1,
            TInput2 input2,
            TInput3 input3,
            TInput4 input4,
            TInput5 input5,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return func(input1, input2, input3, input4, input5);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
