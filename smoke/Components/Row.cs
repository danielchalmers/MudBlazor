namespace MudSmoke.Components;

public sealed class Row
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    public DateTime Added { get; set; }

    public bool Active { get; set; }

    public string Zone { get; set; } = "a";
}
