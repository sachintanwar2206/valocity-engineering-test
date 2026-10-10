namespace GildedRose.Console.Updaters;

/// <summary>
/// Backstage passes gain quality as the concert approaches: +1 per day,
/// +2 with ten days or less to go, +3 with five days or less, capped at 50.
/// After the concert they are worthless.
/// </summary>
public class BackstagePassUpdater : ItemUpdater
{
    protected override void UpdateQuality(Item item)
    {
        IncreaseQuality(item);

        if (item.SellIn <= 10)
        {
            IncreaseQuality(item);
        }

        if (item.SellIn <= 5)
        {
            IncreaseQuality(item);
        }
    }

    protected override void HandleExpired(Item item) => item.Quality = 0;
}
