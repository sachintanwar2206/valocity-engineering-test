namespace GildedRose.Console.Updaters;

/// <summary>
/// "Sulfuras" is legendary: it never has to be sold and never changes
/// quality (fixed at 80), so every update step is a no-op.
/// </summary>
public class SulfurasUpdater : ItemUpdater
{
    protected override void UpdateQuality(Item item)
    {
    }

    protected override void UpdateSellIn(Item item)
    {
    }
}
