using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.UnitTests.TestComponents.NavLink;
using NUnit.Framework;

namespace MudBlazor.UnitTests.Components
{
    [TestFixture]
    public class NavLinkTests : BunitTest
    {
        /// <summary>
        /// When Target is not empty, rel attribute should be equals to "noopener noreferrer" on the a element
        /// </summary>
        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("_self", "noopener noreferrer")]
        [TestCase("_blank", "noopener noreferrer")]
        [TestCase("_parent", "noopener noreferrer")]
        [TestCase("_top", "noopener noreferrer")]
        [TestCase("myFrameName", "noopener noreferrer")]
        public void NavLink_CheckRelAttribute(string target, string expectedRel)
        {
            var comp = Context.Render<MudNavLink>(parameters => parameters.Add(x => x.Target, target));
            // print the generated html
            // select elements needed for the test
            comp.Find("a").GetAttribute("rel").Should().Be(expectedRel);
        }

        [TestCase(null)]
        [TestCase("_blank")]
        public void NavLink_Rel_ReplacesTheDefault(string target)
        {
            var comp = Context.Render<MudNavLink>(parameters => parameters
                .Add(x => x.Href, "/dashboard")
                .Add(x => x.Target, target)
                .Add(x => x.Rel, "nofollow"));

            comp.Find("a").GetAttribute("rel").Should().Be("nofollow");
        }

        [TestCase("href")]
        [TestCase("HREF")]
        [TestCase("target")]
        public void NavLink_Disabled_DropsNavigationAttributesAssignedDirectly(string attributeName)
        {
            // UserAttributes is a parameter, so assigning the dictionary directly skips the per-key matching that routes href/target to Href/Target.
            // A disabled link must stay inert anyway.
            var comp = Context.Render<MudNavLink>(parameters => parameters
                .Add(x => x.Disabled, true)
                .Add(x => x.UserAttributes, new Dictionary<string, object>
                {
                    [attributeName] = "/dashboard",
                    ["data-testid"] = "nav-disabled"
                }));

            var anchor = comp.Find("a");
            anchor.HasAttribute(attributeName).Should().BeFalse();
            anchor.HasAttribute("href").Should().BeFalse();
            anchor.GetAttribute("data-testid").Should().Be("nav-disabled", because: "only navigation attributes are dropped");
        }

        [Test]
        public void NavLink_Enabled_KeepsNavigationAttributesAssignedDirectly()
        {
            // The href here is supplied through UserAttributes rather than the Href parameter, so this exercises the same path as the disabled cases and shows the guard is scoped to Disabled.
            var comp = Context.Render<MudNavLink>(parameters => parameters
                .Add(x => x.Href, "/dashboard")
                .Add(x => x.UserAttributes, new Dictionary<string, object>
                {
                    ["href"] = "/override",
                    ["data-testid"] = "nav-enabled"
                }));

            var anchor = comp.Find("a");
            anchor.GetAttribute("href").Should().Be("/override", because: "a caller assigning href directly still overrides Href when the link is enabled");
            anchor.GetAttribute("data-testid").Should().Be("nav-enabled");
        }

        [Test]
        public void NavLink_Rel_IsOmittedWhenDisabled()
        {
            var comp = Context.Render<MudNavLink>(parameters => parameters
                .Add(x => x.Href, "/dashboard")
                .Add(x => x.Target, "_blank")
                .Add(x => x.Rel, "nofollow")
                .Add(x => x.Disabled, true));

            comp.Find("a").HasAttribute("rel").Should().BeFalse();
        }

        [Test]
        public async Task NavLink_CheckOnClickEvent()
        {
            var clicked = false;
            var comp = Context.Render<MudNavLink>(parameters => parameters.Add(x => x.OnClick, (MouseEventArgs args) => { clicked = true; }));
            // print the generated html
            comp.FindAll("a").Should().BeEmpty();
            await comp.Find(".mud-nav-link").ClickAsync();
            clicked.Should().BeTrue();
        }

        [Test]
        public async Task NavLink_Active()
        {
            const string activeClass = "Custom__nav_active_css";
            var comp = Context.Render<MudNavLink>(parameters => parameters.Add(x => x.ActiveClass, activeClass));
            await comp.Find(".mud-nav-link").ClickAsync();
            comp.Markup.Should().Contain(activeClass);
        }

        [Test]
        public async Task NavLink_Enabled_CheckNavigation()
        {
            var comp = Context.Render<NavLinkDisabledTest>(parameters => parameters.Add(x => x.Disabled, false));
            await comp.Find("a").ClickAsync();
            comp.Instance.IsNavigated.Should().BeTrue();
        }

        [Test]
        public async Task NavLink_Disabled_CheckNoNavigation()
        {
            var comp = Context.Render<NavLinkDisabledTest>(parameters => parameters.Add(x => x.Disabled, true));
            await comp.Find("a").ClickAsync();
            comp.Instance.IsNavigated.Should().BeFalse();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NavLink_UserAttributes_AppearOnAnchor(bool disabled)
        {
            var comp = Context.Render<MudNavLink>(parameters => parameters
                .Add(x => x.Href, "/dashboard")
                .Add(x => x.Disabled, disabled)
                .AddUnmatched("aria-label", "Dashboard")
                .AddUnmatched("data-testid", "nav-dashboard"));
            comp.Find("a.mud-nav-link").GetAttribute("aria-label").Should().Be("Dashboard");
            comp.Find("a.mud-nav-link").GetAttribute("data-testid").Should().Be("nav-dashboard");
            comp.Find(".mud-nav-item").HasAttribute("aria-label").Should().BeFalse();
        }

        [Test]
        public void NavLink_UserAttributes_AppearOnClickableElement()
        {
            var comp = Context.Render<MudNavLink>(parameters => parameters
                .Add(x => x.OnClick, (MouseEventArgs _) => { })
                .AddUnmatched("aria-label", "Dashboard")
                .AddUnmatched("data-testid", "nav-dashboard"));
            comp.FindAll("a").Should().BeEmpty();
            comp.Find(".mud-nav-link").GetAttribute("aria-label").Should().Be("Dashboard");
            comp.Find(".mud-nav-link").GetAttribute("data-testid").Should().Be("nav-dashboard");
            comp.Find(".mud-nav-item").HasAttribute("aria-label").Should().BeFalse();
        }

        [Test]
        public async Task NavLinkOnClickErrorContentCaughtException()
        {
            var comp = Context.Render<NavLinkErrorContenCaughtException>();
            IElement AlertText() => MudAlert().Find("div.mud-alert-message");
            IRenderedComponent<MudAlert> MudAlert() => comp.FindComponent<MudAlert>();
            IReadOnlyList<IElement> Links() => comp.FindAll(".mud-nav-link");
            IElement MudLink() => Links()[0];

            await MudLink().ClickAsync(new MouseEventArgs());

            AlertText().InnerHtml.Should().Be("Oh my! We caught an error and handled it!");
        }

        /// <summary>
        /// IconSize forwards to the leading icon, which renders the matching size class.
        /// </summary>
        [TestCase(Size.Small, "mud-icon-size-small")]
        [TestCase(Size.Medium, "mud-icon-size-medium")]
        [TestCase(Size.Large, "mud-icon-size-large")]
        public void NavLink_IconSize_AppliesSizeClass(Size size, string expectedClass)
        {
            var comp = Context.Render<MudNavLink>(parameters => parameters
                .Add(x => x.Href, "/dashboard")
                .Add(x => x.Icon, Icons.Material.Filled.Home)
                .Add(x => x.IconSize, size));

            comp.Find("svg.mud-nav-link-icon").ClassList.Should().Contain(expectedClass);
        }

        /// <summary>
        /// An unset IconSize keeps the medium icon that links rendered before the parameter existed.
        /// </summary>
        [Test]
        public void NavLink_IconSize_DefaultsToMedium()
        {
            var comp = Context.Render<MudNavLink>(parameters => parameters
                .Add(x => x.Href, "/dashboard")
                .Add(x => x.Icon, Icons.Material.Filled.Home));

            comp.Find("svg.mud-nav-link-icon").ClassList.Should().Contain("mud-icon-size-medium");
        }

        /// <summary>
        /// IconSize also reaches the icon on the OnClick branch, which renders no anchor.
        /// </summary>
        [Test]
        public void NavLink_IconSize_AppliesOnClickBranch()
        {
            var comp = Context.Render<MudNavLink>(parameters => parameters
                .Add(x => x.OnClick, (MouseEventArgs _) => { })
                .Add(x => x.Icon, Icons.Material.Filled.Home)
                .Add(x => x.IconSize, Size.Large));

            comp.FindAll("a").Should().BeEmpty();
            comp.Find("svg.mud-nav-link-icon").ClassList.Should().Contain("mud-icon-size-large");
        }
    }
}
