using GildedRose.Console;
using Xunit;

namespace GildedRose.Tests;

/// <summary>
/// Characterization tests for UpdateQuality, written BEFORE refactoring.
/// They pin down the existing behavior (as specified in the README rules)
/// so the refactor can be verified to change structure, not behavior.
/// </summary>
public class UpdateQualityTests
{
    private const string AgedBrie = "Aged Brie";
    private const string Sulfuras = "Sulfuras, Hand of Ragnaros";
    private const string BackstagePasses = "Backstage passes to a TAFKAL80ETC concert";

    /// <summary>
    /// Runs a single day's update over one item and returns it.
    /// </summary>
    private static Item UpdateItem(string name, int sellIn, int quality)
    {
        var app = new Program
        {
            Items = new List<Item> { new Item { Name = name, SellIn = sellIn, Quality = quality } }
        };
        app.UpdateQuality();
        return app.Items[0];
    }

    public class NormalItems
    {
        [Fact]
        public void Quality_and_sellIn_both_decrease_by_one_before_sell_date()
        {
            var item = UpdateItem("+5 Dexterity Vest", sellIn: 10, quality: 20);
            Assert.Equal(9, item.SellIn);
            Assert.Equal(19, item.Quality);
        }

        [Fact]
        public void Quality_degrades_twice_as_fast_once_sell_date_has_passed()
        {
            var item = UpdateItem("+5 Dexterity Vest", sellIn: 0, quality: 20);
            Assert.Equal(-1, item.SellIn);
            Assert.Equal(18, item.Quality);
        }

        [Theory]
        [InlineData(10, 0)] // before sell date
        [InlineData(0, 0)]  // on/after sell date (would otherwise drop by 2)
        [InlineData(-5, 1)] // after sell date with only 1 quality left
        public void Quality_is_never_negative(int sellIn, int quality)
        {
            var item = UpdateItem("+5 Dexterity Vest", sellIn, quality);
            Assert.True(item.Quality >= 0);
        }
    }

    public class AgedBrieItems
    {
        [Fact]
        public void Quality_increases_by_one_before_sell_date()
        {
            var item = UpdateItem(AgedBrie, sellIn: 2, quality: 0);
            Assert.Equal(1, item.SellIn);
            Assert.Equal(1, item.Quality);
        }

        [Fact]
        public void Quality_increases_by_two_once_sell_date_has_passed()
        {
            var item = UpdateItem(AgedBrie, sellIn: 0, quality: 10);
            Assert.Equal(-1, item.SellIn);
            Assert.Equal(12, item.Quality);
        }

        [Theory]
        [InlineData(5, 50)]  // already at cap before sell date
        [InlineData(0, 50)]  // already at cap after sell date
        [InlineData(0, 49)]  // would pass cap mid-update (+2) but must stop at 50
        public void Quality_never_exceeds_fifty(int sellIn, int quality)
        {
            var item = UpdateItem(AgedBrie, sellIn, quality);
            Assert.True(item.Quality <= 50);
        }
    }

    public class SulfurasItems
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(-1)] // even past its sell date
        public void Never_changes_quality_or_sellIn(int sellIn)
        {
            var item = UpdateItem(Sulfuras, sellIn, quality: 80);
            Assert.Equal(sellIn, item.SellIn);
            Assert.Equal(80, item.Quality);
        }
    }

    public class BackstagePassItems
    {
        [Fact]
        public void Quality_increases_by_one_when_more_than_ten_days_remain()
        {
            var item = UpdateItem(BackstagePasses, sellIn: 15, quality: 20);
            Assert.Equal(14, item.SellIn);
            Assert.Equal(21, item.Quality);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(6)]
        public void Quality_increases_by_two_when_ten_days_or_less_remain(int sellIn)
        {
            var item = UpdateItem(BackstagePasses, sellIn, quality: 20);
            Assert.Equal(22, item.Quality);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(1)]
        public void Quality_increases_by_three_when_five_days_or_less_remain(int sellIn)
        {
            var item = UpdateItem(BackstagePasses, sellIn, quality: 20);
            Assert.Equal(23, item.Quality);
        }

        [Fact]
        public void Quality_drops_to_zero_after_the_concert()
        {
            var item = UpdateItem(BackstagePasses, sellIn: 0, quality: 20);
            Assert.Equal(-1, item.SellIn);
            Assert.Equal(0, item.Quality);
        }

        [Theory]
        [InlineData(15, 50)] // at cap in the +1 window
        [InlineData(10, 50)] // at cap in the +2 window
        [InlineData(5, 49)]  // +3 would pass the cap; must stop at 50
        public void Quality_never_exceeds_fifty(int sellIn, int quality)
        {
            var item = UpdateItem(BackstagePasses, sellIn, quality);
            Assert.True(item.Quality <= 50);
        }
    }

    public class ConjuredItems
    {
        [Fact]
        public void Quality_degrades_twice_as_fast_as_a_normal_item()
        {
            var item = UpdateItem("Conjured Mana Cake", sellIn: 3, quality: 6);
            Assert.Equal(2, item.SellIn);
            Assert.Equal(4, item.Quality);
        }

        [Fact]
        public void Quality_degrades_by_four_once_sell_date_has_passed()
        {
            var item = UpdateItem("Conjured Mana Cake", sellIn: 0, quality: 10);
            Assert.Equal(-1, item.SellIn);
            Assert.Equal(6, item.Quality);
        }

        [Theory]
        [InlineData(5, 1)]  // -2 would go below zero
        [InlineData(0, 3)]  // -4 would go below zero
        public void Quality_is_never_negative(int sellIn, int quality)
        {
            var item = UpdateItem("Conjured Mana Cake", sellIn, quality);
            Assert.Equal(0, item.Quality);
        }

        [Fact]
        public void Any_item_whose_name_starts_with_Conjured_is_treated_as_conjured()
        {
            var item = UpdateItem("Conjured Sword of a Thousand Truths", sellIn: 10, quality: 10);
            Assert.Equal(8, item.Quality);
        }
    }

    public class MultipleItems
    {
        [Fact]
        public void Every_item_in_the_list_is_updated_in_a_single_pass()
        {
            var app = new Program
            {
                Items = new List<Item>
                {
                    new Item { Name = "+5 Dexterity Vest", SellIn = 10, Quality = 20 },
                    new Item { Name = AgedBrie, SellIn = 2, Quality = 0 },
                    new Item { Name = Sulfuras, SellIn = 0, Quality = 80 }
                }
            };

            app.UpdateQuality();

            Assert.Equal(19, app.Items[0].Quality);
            Assert.Equal(1, app.Items[1].Quality);
            Assert.Equal(80, app.Items[2].Quality);
        }
    }
}
