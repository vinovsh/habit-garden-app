using System;
using System.IO;
using HabitGarden.Domain;
using NUnit.Framework;

namespace HabitGarden.Tests
{
    public sealed class SaveRepositoryTests
    {
        string folder;SaveRepository store;
        [SetUp]public void Setup(){folder=Path.Combine(Path.GetTempPath(),"habit-garden-tests-"+Guid.NewGuid());store=new SaveRepository(folder);}
        [TearDown]public void Cleanup(){Directory.Delete(folder,true);}
        [Test]public void CommittedJournalSurvivesReopen(){var service=new HabitService(store,new DeviceClock());service.Add("Read","tree");Assert.AreEqual(1,new SaveRepository(folder).Load().habits.Count);}
        [Test]public void TornFinalAppendRecoversAndAllowsNextWrite(){var service=new HabitService(store,new DeviceClock());service.Add("Read","tree");File.AppendAllText(Path.Combine(folder,"journal.jsonl"),"{torn");var reopened=new HabitService(new SaveRepository(folder),new DeviceClock());reopened.Add("Walk","succulent");Assert.AreEqual(2,new SaveRepository(folder).Load().habits.Count);}
        [Test]public void BrokenBackupChecksumIsRejected(){string text=SaveRepository.Export(new AppState());Assert.Throws<InvalidDataException>(()=>SaveRepository.Import(text.Replace("schemaVersion", "differentField")));}
        [Test]public void CorruptOnlySaveDoesNotSilentlyReset(){File.WriteAllText(Path.Combine(folder,"state.json"),"bad");Assert.Throws<InvalidDataException>(()=>store.Load());}
        [Test]public void OlderCheckpointDoesNotReplaceNewerJournal(){var service=new HabitService(store,new DeviceClock());service.Add("Read","tree");store.Checkpoint(service.Snapshot);service.Add("Walk","succulent");Assert.AreEqual(2,new SaveRepository(folder).Load().habits.Count);}
        [Test]public void CheckpointAndJournalRemainConsistent(){var service=new HabitService(store,new DeviceClock());service.Add("Read","tree");store.Checkpoint(service.Snapshot);Assert.AreEqual(service.Snapshot.revision,store.Load().revision);}
    }
}
