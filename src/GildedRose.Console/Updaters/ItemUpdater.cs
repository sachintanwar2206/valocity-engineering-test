namespace GildedRose.Console.Updaters;

/// <summary>
/// Base class for the daily update of a single item. The update always runs in
/// the same three steps (adjust quality, age the item, apply expiry rules);
/// subclasses override only the steps where their category's rules differ.
/// Updaters mutate Item instances from the outside, so the Item class itself
/// stays untouched.
/// </summary>
public abstract class ItemUpdater
{
    protected const int MaxQuality = 50;
    protected const int MinQuality = 0;

    public void Update(Item item)
    {
        UpdateQuality(item);
        UpdateSellIn(item);

        if (item.SellIn < 0)
        {
            HandleExpired(item);
        }
    }

    /// <summary>Daily quality adjustment while the item is in date.</summary>
    protected abstract void UpdateQuality(Item item);

    /// <summary>By default every item ages by one day.</summary>
    protected virtual void UpdateSellIn(Item item) => item.SellIn--;

    /// <summary>Extra adjustment applied once the sell date has passed.</summary>
    protected virtual void HandleExpired(Item item)
    {
    }

    protected static void IncreaseQuality(Item item, int amount = 1) =>
        item.Quality = Math.Min(MaxQuality, item.Quality + amount);

    protected static void DecreaseQuality(Item item, int amount = 1) =>
        item.Quality = Math.Max(MinQuality, item.Quality - amount);
}
