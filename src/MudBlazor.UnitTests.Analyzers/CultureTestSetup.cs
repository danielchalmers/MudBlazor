using System.Globalization;
using NUnit.Framework;

[SetUpFixture]
public sealed class CultureTestSetup
{
    private const string TestCultureEnvironmentVariableName = "MUD_TEST_CULTURE";

    [OneTimeSetUp]
    public void ConfigureCulture()
    {
        var cultureName = Environment.GetEnvironmentVariable(TestCultureEnvironmentVariableName);

        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return;
        }

        var culture = string.Equals(cultureName, "invariant", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.InvariantCulture
            : CultureInfo.GetCultureInfo(cultureName);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var effectiveCultureName = string.IsNullOrEmpty(culture.Name) ? "invariant" : culture.Name;
        TestContext.Progress.WriteLine($"Running tests using culture '{effectiveCultureName}'.");
    }
}
