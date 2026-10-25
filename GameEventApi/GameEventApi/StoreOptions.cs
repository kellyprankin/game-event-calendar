namespace GameEventApi;

/// <summary>Store details bound from the "Store" config section.</summary>
public class StoreOptions
{
    public const string Section = "Store";

    public string Name { get; set; } = "";

    /// <summary>Street address used as the location on calendar invites.</summary>
    public string Location { get; set; } = "";
}
