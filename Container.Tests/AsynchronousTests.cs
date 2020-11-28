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
        public async Task MissingDependencyTimeout()
        {
            var container = new BaseResolver(TimeSpan.FromSeconds(1));
            await container.ResolveToAsync<ILog, NullLog>();

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
            var container = new BaseResolver();
            await container.ResolveToAsync<ILog, NullLog>();
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

        [Fact]
        public async Task SimpleLinearDependenciesOutOfOrder()
        {
            var container = new BaseResolver();
            await container.ResolveToAsync<ILog, NullLog>();

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
        public async Task SimpleLinearDependenciesContravariant()
        {
            //var container = new BaseResolver(TimeSpan.FromSeconds(1));
            var container = new BaseResolver();
            await container.ResolveToAsync<IMegaLog, NullLog>();
            await container.ResolveToAsync<ILogFileProvider, TestLogFileProvider>();

            var fileProvider = await container.ResolveAsync<ILogFileProvider>();

            var fi = fileProvider.GetLogFile();
            Assert.NotNull(fi);
        }
    }
}