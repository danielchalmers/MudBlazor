using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Docs.Services;
using MudBlazor.Services;
using MudBlazor.UnitTests.Docs.Mocks;
using MudBlazor.UnitTests.Mocks;
using NUnit.Framework;

namespace MudBlazor.UnitTests.Docs.Generated
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public partial class ExampleDocsTests
    {
        private static readonly ServiceDescriptor[] DefaultServices = CreateDefaultServices();
        private BunitContext ctx;
        private IRenderQueueService renderQueueService;

        private static ServiceDescriptor[] CreateDefaultServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<NavigationManager, MockNavigationManager>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<ISnackbar, SnackbarService>();
            services.AddSingleton<IBrowserViewportService, MockBrowserViewportService>();
            services.AddTransient<IScrollManager, MockScrollManager>();
            services.AddTransient<IScrollListenerFactory, MockScrollListenerFactory>();
            services.AddTransient<IJsApiService, MockJsApiService>();
            services.AddTransient<IDocsJsApiService, MockDocsJsApiService>();
            services.AddTransient<IResizeObserverFactory, MockResizeObserverFactory>();
            services.AddTransient<IEventListenerFactory, MockEventListenerFactory>();
            services.AddTransient<IEventListener, MockEventListener>();
            services.AddSingleton<IKeyInterceptorService, MockKeyInterceptorService>();
            services.AddTransient<IJsEventFactory, MockJsEventFactory>();
            services.AddSingleton<IPopoverService, MockPopoverService>();
            services.AddScoped<IRenderQueueService, RenderQueueService>();
            services.AddScoped<IPointerEventsNoneService, MockPointerEventsNoneService>();
            services.AddTransient<ILocalizationInterceptor, DefaultLocalizationInterceptor>();
            services.AddTransient<InternalMudLocalizer>();
            services.AddTransient<ILocalizationEnumInterceptor, DefaultLocalizationEnumInterceptor>();
            services.AddTransient<IScrollListener, ScrollListener>();
            services.AddTransient<IResizeObserver, ResizeObserver>();
            services.AddOptions();
            services.AddScoped(sp =>
                new HttpClient(new MockDocsMessageHandler()) { BaseAddress = new Uri("https://localhost/") });
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
            renderQueueService = ctx.Services.GetRequiredService<IRenderQueueService>();
        }

        [TearDown]
        public async Task TearDown()
        {
            if (renderQueueService is not null)
            {
                await renderQueueService.WaitUntilEmpty();
            }

            if (ctx is not null)
            {
                await ctx.DisposeAsync();
            }
        }

        protected async Task RenderExampleAsync<TComponent>()
            where TComponent : IComponent
        {
            ctx.Render<TComponent>();
            await renderQueueService.WaitUntilEmpty();
        }
    }
}
