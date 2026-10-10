namespace GildedRose.Console.Updaters;

/// <summary>
/// Ordinary items lose one quality per day, and a further one per day once
/// the sell date has passed (twice as fast overall). Quality never drops
/// below zero.
/// </summary>
public class NormalItemUpdater : ItemUpdater
{
    protected override void UpdateQuality(Item item) => DecreaseQuality(item);

    protected override void HandleExpired(Item item) => DecreaseQuality(item);
}
