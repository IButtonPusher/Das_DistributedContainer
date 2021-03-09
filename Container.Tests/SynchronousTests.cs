using System;
using System.Threading.Tasks;
using Container.Tests.Implementations;
using Container.Tests.Interfaces;
using Das.Container;
using Xunit;

// ReSharper disable All

namespace Container.Tests
{
    public class SynchronousTests
    {
        [Fact]
        public void DuplicateInstantiation()
        {
            SlowLoader.Reset();

            var container = new BaseResolver(TimeSpan.FromSeconds(10));
            container.ResolveTo<IResolver>(container);

            container.ResolveTo<ILoadSlowly, SlowLoader>();
            container.ResolveTo<INeedLoadSlowly2, NeedSlowly2>();
            container.ResolveTo<INeedLoadSlowly1, NeedSlowly1>();

            var load1 = container.Resolve<INeedLoadSlowly1>();
            var load2 = container.Resolve<INeedLoadSlowly2>();
        }

        [Fact]
        public void ResolveWithoutMapping()
        {
            var container = new BaseResolver();
            container.ResolveTo<ILog, MegaLog>();
            container.ResolveTo<ILogFileProvider, TestLogFileProvider>();
            //container.ResolveTo<ILogSizeProvider, TestLogSizeProvider>();

            //var testLogFileProvider = container.Resolve<ILogFileProvider>();
            //var fi = testLogFileProvider.GetLogFile();
            //Assert.NotNull(fi);

            var testLogSizeProvider = container.Resolve<TestLogSizeProvider>();
            Assert.NotNull(testLogSizeProvider);

            var size = testLogSizeProvider!.GetLogSize();
            Assert.True(size >= 0);
        }

        [Fact]
        public void SimpleLinearDependencies()
        {
            var container = new BaseResolver();
            container.ResolveTo<ILog, MegaLog>();
            container.ResolveTo<ILogFileProvider, TestLogFileProvider>();
            container.ResolveTo<ILogSizeProvider, TestLogSizeProvider>();

            var testLogFileProvider = container.Resolve<ILogFileProvider>();
            var fi = testLogFileProvider.GetLogFile();
            Assert.NotNull(fi);

            var testLogSizeProvider = container.Resolve(typeof(ILogSizeProvider)) as ILogSizeProvider;
            Assert.NotNull(testLogSizeProvider);

            var size = testLogSizeProvider!.GetLogSize();
            Assert.True(size >= 0);
        }
    }
}
