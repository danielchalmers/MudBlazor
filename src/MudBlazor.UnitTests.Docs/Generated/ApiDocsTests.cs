using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Docs.Pages.Api;
using MudBlazor.Docs.Services;
using MudBlazor.Services;
using MudBlazor.UnitTests.Mocks;
using NUnit.Framework;

namespace MudBlazor.UnitTests.Docs.Generated
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public partial class ApiDocsTests
    {
        private static readonly ServiceDescriptor[] DefaultServices = CreateDefaultServices();
        private BunitContext ctx;

        private static ServiceDescriptor[] CreateDefaultServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<ISnackbar, SnackbarService>();
            services.AddSingleton<IBrowserViewportService, MockBrowserViewportService>();
            services.AddTransient<IScrollManager, MockScrollManager>();
            services.AddTransient<IScrollListenerFactory, MockScrollListenerFactory>();
            services.AddTransient<IJsApiService, MockJsApiService>();
            services.AddTransient<IDocsJsApiService, MockDocsJsApiService>();
            services.AddTransient<IResizeObserverFactory, MockResizeObserverFactory>();
            services.AddTransient<IScrollSpyFactory, MockScrollSpyFactory>();
            services.AddTransient<IEventListenerFactory, MockEventListenerFactory>();
            services.AddTransient<IEventListener, MockEventListener>();
            services.AddSingleton<IDocsNavigationService, DocsNavigationService>();
            services.AddSingleton<IMenuService, MenuService>();
            services.AddSingleton<IPopoverService, MockPopoverService>();
            services.AddSingleton<IKeyInterceptorService, MockKeyInterceptorService>();
            services.AddTransient<IJsEventFactory, MockJsEventFactory>();
            services.AddScoped<IRenderQueueService, RenderQueueService>();
            services.AddScoped<IPointerEventsNoneService, MockPointerEventsNoneService>();
            services.AddTransient<InternalMudLocalizer>();
            services.AddTransient<ILocalizationInterceptor, DefaultLocalizationInterceptor>();
            services.AddTransient<ILocalizationEnumInterceptor, DefaultLocalizationEnumInterceptor>();
            services.AddScoped(sp => new HttpClient());
            return services.ToArray();
        }

        [SetUp]
        public void Setup()
        {
            ctx = new BunitContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            foreach (var descriptor in DefaultServices)
            {
                ctx.Services.Add(descriptor);
            }
        }

        // This shows how to test a docs page with incremental rendering.
        // We are not (yet) testing all docs pages (just the examples), but if we wanted to, this would be the way.
        [Test]
        public async Task AlertPage_Test()
        {
            ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager("https://localhost:2112/", "https://localhost:2112/components/alert"));
            var comp = ctx.Render<MudBlazor.Docs.Pages.Components.Alert.AlertPage>();
            await WaitForRenderQueueAsync();
        }

        /// <summary>
        /// An example of a generated API test.
        /// </summary>
        [Test]
        public async Task MudAlert_API_Test_Example()
        {
            ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager("https://localhost:2112/", "https://localhost:2112/components/MudAlert"));
            var comp = ctx.Render<Api>(parameters => parameters.Add(x => x.TypeName, "MudAlert"));
            await WaitForRenderQueueAsync();
            comp.Markup.Should().NotContain("Sorry, the type").And.NotContain("could not be found");
            var exampleLink = comp.FindComponents<MudLink>().FirstOrDefault(link => link.Instance.Href.StartsWith("/component"));
            exampleLink.Should().NotBeNull();
        }

        [TearDown]
        public async Task TearDown()
        {
            await WaitForRenderQueueAsync();

            if (ctx is not null)
            {
                await ctx.DisposeAsync();
            }
        }

        protected Task WaitForRenderQueueAsync()
        {
            var queueService = ctx.Services.GetRequiredService<IRenderQueueService>();
            return queueService.WaitUntilEmpty();
        }
    }
}
