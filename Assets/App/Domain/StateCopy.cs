using System.Linq;

namespace HabitGarden.Domain
{
    public static class StateCopy
    {
        public static AppState Clone(AppState s) => new AppState {
            schemaVersion=s.schemaVersion, revision=s.revision, lastDate=s.lastDate,
            settings=new Settings { sound=s.settings.sound, reducedMotion=s.settings.reducedMotion, quality=s.settings.quality, guideSeen=s.settings.guideSeen },
            habits=s.habits.Select(x=>new Habit {id=x.id,title=x.title,category=x.category ?? "Personal",species=x.species,createdUtc=x.createdUtc,target=x.target,weekdays=x.weekdays,paused=x.paused,archived=x.archived,reminderMinutes=x.reminderMinutes,changeDate=x.changeDate,pendingTitle=x.pendingTitle,pendingTarget=x.pendingTarget,pendingWeekdays=x.pendingWeekdays}).ToList(),
            days=s.days.Select(x=>new DailyRecord {habitId=x.habitId,date=x.date,plantId=x.plantId,progress=x.progress,target=x.target,complete=x.complete,completedUtc=x.completedUtc,timezoneOffsetMinutes=x.timezoneOffsetMinutes}).ToList(),
            plants=s.plants.Select(x=>new Plant {id=x.id,habitId=x.habitId,species=x.species,seed=x.seed,plot=x.plot,slot=x.slot,growthUnits=x.growthUnits,maturityDate=x.maturityDate,pot=x.pot,generatorVersion=x.generatorVersion}).ToList(),
            ledger=s.ledger.Select(x=>new LedgerEntry {id=x.id,source=x.source,delta=x.delta,createdUtc=x.createdUtc}).ToList(),
            inventory=s.inventory.ToList(), adSessions=s.adSessions.Select(x=>new AdSession {id=x.id,date=x.date,granted=x.granted,amount=x.amount}).ToList()
        };
    }
}

