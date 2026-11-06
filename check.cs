#:project Issue14134.Shapes/Issue14134.Shapes.csproj

// CoreCLR sanity check: every case should print 181.
foreach (var name in Issue14134.Cases.Names)
{
    Console.WriteLine($"{name}: {await Issue14134.Cases.RunAsync(name)}");
}
