namespace GildedRose.Console.Updaters;

/// <summary>
/// "Aged Brie" increases in quality as it ages: by one per day, and by a
/// further one per day once the sell date has passed, capped at 50.
/// </summary>
public class AgedBrieUpdater : ItemUpdater
{
    protected override void UpdateQuality(Item item) => IncreaseQuality(item);

    protected override void HandleExpired(Item item) => IncreaseQuality(item);
}
