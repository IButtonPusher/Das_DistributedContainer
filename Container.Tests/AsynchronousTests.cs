using System;
using System.Threading.Tasks;
using Container.Tests.Implementations;
using Container.Tests.Interfaces;
using Das.Container;
using Xunit;

// ReSharper disable All

namespace Container.Tests
{
    public class AsynchronousTests
    {
        [Fact]
        public async Task DependenciesContravariant()
        {
            var container = new BaseResolver(TimeSpan.FromSeconds(5));
            await container.ResolveToAsync<IMegaLog, MegaLog>();
            await container.ResolveToAsync<ILogFileProvider, TestLogFileProvider>();

            var fileProvider = await container.ResolveAsync<ILogFileProvider>();

            var fi = fileProvider.GetLogFile();
            Assert.NotNull(fi);
        }

        [Fact]
        public async Task DependenciesContravariant2()
        {
            Logger.TimeInstantiated = 0;
            MegaLog.TimeInstantiated = 0;

            var container = new BaseResolver(TimeSpan.FromSeconds(5));
            await container.ResolveToAsync<IMegaLog, MegaLog>();
            await container.ResolveToAsync<INeedMegaLogger, NeedMegaLogger>();
            await container.ResolveToAsync<INeedLogger1, NeedLogger1>();

            var needMegaLogger = await container.ResolveAsync<INeedMegaLogger>();
            var needLogger = await container.ResolveAsync<INeedLogger1>();

            Assert.True(Logger.TimeInstantiated <= 1);

            Assert.True(MegaLog.TimeInstantiated <= 1);
        }

        [Fact]
        public async Task DependenciesCovariant()
        {
            Logger.TimeInstantiated = 0;
            MegaLog.TimeInstantiated = 0;

            var container = new BaseResolver(TimeSpan.FromHours(5));
            await container.ResolveToAsync<IMegaLog, MegaLog>();
            await container.ResolveToAsync<INeedMegaLogger, NeedMegaLogger>();
            await container.ResolveToAsync<INeedLogger1, NeedLogger1>();

            var needLogger = await container.ResolveAsync<INeedLogger1>();
            var needMegaLogger = await container.ResolveAsync<INeedMegaLogger>();

            Assert.True(Logger.TimeInstantiated <= 1);
            Assert.True(MegaLog.TimeInstantiated <= 1);
        }

        [Fact]
        public async Task DependenciesOutOfOrder()
        {
            var container = new BaseResolver(TimeSpan.FromSeconds(5));
            await container.ResolveToAsync<ILog, MegaLog>();

            var loadingFileProvider = container.ResolveAsync<ILogFileProvider>();

            await container.ResolveToAsync<ILogFileProvider, TestLogFileProvider>();
            await container.ResolveToAsync<ILogSizeProvider, TestLogSizeProvider>();

            var testLogFileProvider = await loadingFileProvider;


            var fi = testLogFileProvider.GetLogFile();
            Assert.NotNull(fi);

            var testLogSizeProvider = await container.ResolveAsync(typeof(ILogSizeProvider)) as ILogSizeProvider;
            Assert.NotNull(testLogSizeProvider);

            var size = testLogSizeProvider!.GetLogSize();
            Assert.True(size >= 0);
        }

        [Fact]
        public async Task DuplicateInstantiation()
        {
            SlowLoader.Reset();

            var container = new BaseResolver(); //TimeSpan.FromSeconds(10));
            container.ResolveTo<IResolver>(container);

            container.ResolveTo<ILoadSlowly, SlowLoader>();
            container.ResolveTo<INeedLoadSlowly2, NeedSlowly2>();
            container.ResolveTo<INeedLoadSlowly1, NeedSlowly1>();


            var load1 = container.ResolveAsync<INeedLoadSlowly1>();
            var load2 = container.ResolveAsync<INeedLoadSlowly2>();

            await load1;
            await load2;
        }

        [Fact]
        public async Task MissingDependencyCannotAutoCtor()
        {
            var container = new BaseResolver(); //TimeSpan.FromSeconds(1));
            var n = new NeedsCannotCtor(container);

            await Task.Delay(1000);

            var cannotCtor = new CannotCtor("bob", 9000);
            await container.ResolveToAsync(cannotCtor);

            while (!n.IsFinished)
                await Task.Delay(10);
        }

        [Fact]
        public async Task MissingDependencyTimeout()
        {
            var container = new BaseResolver(TimeSpan.FromSeconds(1));
            await container.ResolveToAsync<ILog, MegaLog>();

            var loadingFileProvider = container.ResolveAsync<ILogFileProvider>();

            ILogFileProvider? testLogFileProvider = null;

            try
            {
                testLogFileProvider = await loadingFileProvider;
                Assert.False(true);
            }
            catch
            {
                Assert.True(true);
            }
        }

        [Fact]
        public async Task SimpleLinearDependencies()
        {
            var container = new BaseResolver(TimeSpan.FromSeconds(5));
            await container.ResolveToAsync<ILog, MegaLog>();
            await container.ResolveToAsync<ILogFileProvider, TestLogFileProvider>();
            await container.ResolveToAsync<ILogSizeProvider, TestLogSizeProvider>();

            var testLogFileProvider = await container.ResolveAsync<ILogFileProvider>();
            var fi = testLogFileProvider.GetLogFile();
            Assert.NotNull(fi);

            var testLogSizeProvider = await container.ResolveAsync(typeof(ILogSizeProvider)) as ILogSizeProvider;
            Assert.NotNull(testLogSizeProvider);

            var size = testLogSizeProvider!.GetLogSize();
            Assert.True(size >= 0);
        }
    }
}
