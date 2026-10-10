namespace GildedRose.Console.Updaters;

/// <summary>
/// Maps an item to the updater implementing its category's rules. Updaters
/// are stateless, so a single shared instance of each is reused. Adding a
/// new category means adding an updater class and one line here; no
/// existing rule needs to be touched.
/// </summary>
public static class ItemUpdaterFactory
{
    public const string AgedBrie = "Aged Brie";
    public const string Sulfuras = "Sulfuras, Hand of Ragnaros";
    public const string BackstagePasses = "Backstage passes to a TAFKAL80ETC concert";

    // Conjured items are a category from a supplier rather than a single
    // product, so they are recognized by name prefix (e.g. "Conjured Mana
    // Cake", "Conjured Sword") instead of an exact match.
    public const string ConjuredPrefix = "Conjured";

    private static readonly ItemUpdater Normal = new NormalItemUpdater();
    private static readonly ItemUpdater Brie = new AgedBrieUpdater();
    private static readonly ItemUpdater Legendary = new SulfurasUpdater();
    private static readonly ItemUpdater BackstagePass = new BackstagePassUpdater();
    private static readonly ItemUpdater Conjured = new ConjuredItemUpdater();

    public static ItemUpdater ForItem(Item item) => item.Name switch
    {
        AgedBrie => Brie,
        Sulfuras => Legendary,
        BackstagePasses => BackstagePass,
        _ when item.Name.StartsWith(ConjuredPrefix, StringComparison.Ordinal) => Conjured,
        _ => Normal
    };
}
