using System;
using System.Threading.Tasks;
using Das.Container;

namespace Container.Tests.Implementations
{
    public class NeedsCannotCtor
    {
        public Boolean IsFinished { get; private set; }

        public NeedsCannotCtor(IResolver resolver)
        {

            var promise = resolver.ResolveAsync<CannotCtor>();
            promise.ContinueWith(OnPromiseKept);
        }

        private void OnPromiseKept(Task<CannotCtor> obj)
        {
            if (!obj.IsCompleted)
                throw new Exception();

            var res = obj.Result;

            if (res == null)
                throw new Exception();

            IsFinished = true;
        }
    }
}
