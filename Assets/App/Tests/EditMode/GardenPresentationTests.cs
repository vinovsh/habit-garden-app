using System;
using System.Linq;
using HabitGarden.Domain;
using NUnit.Framework;

namespace HabitGarden.Tests
{
    public sealed class GardenPresentationTests
    {
        [Test] public void WeeklyChartUsesLocalDatesAcrossMonthBoundaryAndExcludesUndoneDays()
        {
            var state = new AppState();
            state.days.Add(new DailyRecord {date="2026-09-30",complete=true});
            state.days.Add(new DailyRecord {date="2026-10-01",complete=true});
            state.days.Add(new DailyRecord {date="2026-10-01",complete=false});
            state.days.Add(new DailyRecord {date="2026-09-20",complete=true});
            state.days.Add(new DailyRecord {date="2026-10-05",complete=true});
            var week = GardenPresentation.Week(state,new DateTime(2026,10,4));
            Assert.That(week.Length,Is.EqualTo(7));
            Assert.That(week.First().date,Is.EqualTo(new DateTime(2026,9,28)));
            Assert.That(week.Last().date,Is.EqualTo(new DateTime(2026,10,4)));
            Assert.That(week.Sum(point=>point.completed),Is.EqualTo(2));
            Assert.That(week.Single(point=>point.date==new DateTime(2026,10,1)).completed,Is.EqualTo(1));
        }
        [Test] public void EmptyWeekIsZeroRatherThanInventedProgress()
        {
            Assert.That(GardenPresentation.Week(new AppState(),new DateTime(2026,10,14)).All(point=>point.completed==0),Is.True);
        }
        [Test] public void PlantLabelsFollowGrowthLandmarksAndClampOverMaturity()
        {
            Assert.That(GardenPresentation.Stage(0,30),Is.EqualTo("Seed"));
            Assert.That(GardenPresentation.Stage(3,30),Is.EqualTo("Sprout"));
            Assert.That(GardenPresentation.Stage(10,30),Is.EqualTo("Young plant"));
            Assert.That(GardenPresentation.Stage(15,30),Is.EqualTo("Growing"));
            Assert.That(GardenPresentation.Stage(24,30),Is.EqualTo("Budding"));
            Assert.That(GardenPresentation.Stage(27,30),Is.EqualTo("Blooming"));
            Assert.That(GardenPresentation.Stage(31,30),Is.EqualTo("Mature"));
            Assert.Throws<ArgumentOutOfRangeException>(()=>GardenPresentation.Stage(1,0));
        }
        [Test] public void CategoryIsPreservedByStateCopyWithoutSharingHabitObjects()
        {
            var state = new AppState(); state.habits.Add(new Habit {id="H1",category="Learning"});
            var copy = StateCopy.Clone(state); copy.habits[0].category="Health";
            Assert.That(state.habits[0].category,Is.EqualTo("Learning"));
            Assert.That(copy.habits[0].category,Is.EqualTo("Health"));
        }
    }
}
