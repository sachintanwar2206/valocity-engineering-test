namespace GildedRose.Console.Updaters;

/// <summary>
/// Conjured items degrade in quality twice as fast as normal items: two per
/// day, and a further two per day once the sell date has passed. Quality
/// never drops below zero.
/// </summary>
public class ConjuredItemUpdater : ItemUpdater
{
    protected override void UpdateQuality(Item item) => DecreaseQuality(item, 2);

    protected override void HandleExpired(Item item) => DecreaseQuality(item, 2);
}
