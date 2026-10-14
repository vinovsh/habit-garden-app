using System;
using System.Globalization;
using System.Linq;

namespace HabitGarden.Domain
{
    public static class GardenPresentation
    {
        public struct WeekPoint { public DateTime date; public int completed; }
        public static WeekPoint[] Week(AppState state,DateTime today)
        {
            return Enumerable.Range(0,7).Select(index =>
            {
                var date = today.Date.AddDays(index-6); string key = date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
                return new WeekPoint { date = date, completed = state.days.Count(d => d.date == key && d.complete) };
            }).ToArray();
        }
        public static string SpeciesName(string species) => species == "tree" ? "Flowering tree" : species == "succulent" ? "Succulent" : "Sunflower";
        public static string Stage(int units,int maturity)
        {
            if (maturity <= 0) throw new ArgumentOutOfRangeException(nameof(maturity));
            float progress = (float)Math.Max(0,units)/maturity;
            return progress >= 1 ? "Mature" : progress >= .88f ? "Blooming" : progress >= .70f ? "Budding" : progress >= .45f ? "Growing" : progress >= .20f ? "Young plant" : progress >= .05f ? "Sprout" : "Seed";
        }
    }
}
