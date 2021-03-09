using System;
using System.Threading.Tasks;
using Container.Tests.Interfaces;
using Das.Container;
// ReSharper disable All

namespace Container.Tests.Implementations
{
    public class NeedSlowly2 : INeedLoadSlowly2
    {
        private readonly IResolver _resolver;
        //private readonly INeedLoadSlowly1 _slow1;
        private ILoadSlowly? _lodr;

        public NeedSlowly2(IResolver resolver)
                           //INeedLoadSlowly1 slow1)
        {
            _resolver = resolver;
            //_slow1 = slow1;
            LoadEventually().ConfigureAwait(false);
        }

        private async Task LoadEventually()
        {
            await Task.Delay(3000).ConfigureAwait(false);
            var _ = _resolver.ResolveAsync<ILoadSlowly>()
                             .ContinueWith(OnFinished).ConfigureAwait(false);
        }

        private void OnFinished(Task<ILoadSlowly> obj)
        {
            _lodr = obj.Result;
        }
    }
}
