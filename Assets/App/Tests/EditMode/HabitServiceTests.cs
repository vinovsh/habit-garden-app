using System;
using System.Linq;
using HabitGarden.Domain;
using NUnit.Framework;

namespace HabitGarden.Tests
{
    public sealed class HabitServiceTests
    {
        sealed class Clock:IClock {public DateTimeOffset now=new DateTimeOffset(2026,10,14,18,0,0,TimeSpan.FromHours(5.5));public DateTimeOffset Now=>now;}
        sealed class Store:IStateStore {
            public AppState state=new AppState();public bool fail;
            public AppState Load()=>StateCopy.Clone(state);
            public void Commit(AppState s){if(fail)throw new System.IO.IOException("Injected failure");state=StateCopy.Clone(s);}
        }
        Clock clock;Store store;HabitService service;
        [SetUp]public void Setup(){clock=new Clock();store=new Store();service=new HabitService(store,clock);}
        [Test]public void DuplicateCompletionDoesNotDuplicateCoins(){string id=service.Add("Read","tree");service.Progress(id,1);service.Progress(id,1);Assert.AreEqual(10,service.Balance);Assert.AreEqual(1,service.Snapshot.plants[0].growthUnits);}
        [Test]public void NumericProgressEarnsOnlyAtTarget(){string id=service.Add("Water","sunflower",8);service.Progress(id,7);Assert.AreEqual(0,service.Balance);service.Progress(id,1);Assert.AreEqual(10,service.Balance);}
        [Test]public void UndoAndRecompleteRestoreOneEntitlement(){string id=service.Add("Read","tree");service.Progress(id,1);service.Progress(id,0,true);Assert.AreEqual(0,service.Balance);service.Progress(id,1);Assert.AreEqual(10,service.Balance);Assert.AreEqual(1,service.Snapshot.plants[0].growthUnits);}
        [Test]public void FailedSaveDoesNotChangeVisibleState(){string id=service.Add("Read","tree");store.fail=true;Assert.Throws<System.IO.IOException>(()=>service.Progress(id,1));Assert.AreEqual(0,service.Balance);Assert.AreEqual(0,service.Snapshot.days.Count);}
        [Test]public void UnscheduledDayCannotComplete(){string id=service.Add("Read","tree",1,1<<(int)DayOfWeek.Monday);Assert.Throws<InvalidOperationException>(()=>service.Progress(id,1));}
        [Test]public void PausedHabitCannotComplete(){string id=service.Add("Read","tree");service.SetPaused(id,true);Assert.Throws<InvalidOperationException>(()=>service.Progress(id,1));}
        [Test]public void AdCallbackIsIdempotent(){string id=service.BeginAd();service.EarnAd(id);service.EarnAd(id);Assert.AreEqual(30,service.Balance);}
        [Test]public void RewardLimitIsEnforced(){for(int i=0;i<3;i++)service.EarnAd(service.BeginAd());Assert.Throws<InvalidOperationException>(()=>service.BeginAd());Assert.AreEqual(90,service.Balance);}
        [Test]public void DuplicatePurchaseDoesNotSpendAgain(){for(int i=0;i<2;i++)service.EarnAd(service.BeginAd());service.Buy("leafy");Assert.Throws<InvalidOperationException>(()=>service.Buy("leafy"));Assert.AreEqual(10,service.Balance);}
        [Test]public void CannotPurchaseWithInsufficientFunds(){Assert.Throws<InvalidOperationException>(()=>service.Buy("rainbow"));Assert.AreEqual(0,service.Balance);}
        [Test]public void MidnightProducesANewEntitlement(){string id=service.Add("Read","tree");service.Progress(id,1);clock.now=clock.now.AddDays(1);service.Progress(id,1);Assert.AreEqual(20,service.Balance);Assert.AreEqual(2,service.Snapshot.days.Count);}
        [Test]public void ClockRollbackCannotCreateAnEarlierDay(){string id=service.Add("Read","tree");clock.now=clock.now.AddDays(-1);Assert.Throws<InvalidOperationException>(()=>service.Progress(id,1));}
        [Test]public void ScheduleChangesAreDeferred(){string id=service.Add("Read","tree");service.Edit(id,"Reading",3,127,-1);Assert.AreEqual(1,service.Snapshot.habits[0].target);clock.now=clock.now.AddDays(1);service.ReconcileDay();Assert.AreEqual(3,service.Snapshot.habits[0].target);}
        [Test]public void SnapshotCannotMutateState(){service.Add("Read","tree");var s=service.Snapshot;s.habits.Clear();Assert.AreEqual(1,service.Snapshot.habits.Count);}
        [Test]public void MaturityCannotReplantOnSameDay(){string id=service.Add("Walk","succulent");for(int i=0;i<10;i++){service.Progress(id,1);if(i<9)clock.now=clock.now.AddDays(1);}Assert.Throws<InvalidOperationException>(()=>service.PlantAnother(id));clock.now=clock.now.AddDays(1);service.PlantAnother(id);Assert.AreEqual(2,service.Snapshot.plants.Count);}
        [Test]public void PlantHistoryCannotBeForgedInBackup(){string id=service.Add("Read","tree");var s=service.Snapshot;s.plants[0].growthUnits=5;Assert.Throws<InvalidOperationException>(()=>HabitService.Validate(s));}
    }
}
