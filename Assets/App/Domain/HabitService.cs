using System;
using System.Globalization;
using System.Linq;

namespace HabitGarden.Domain
{
    public sealed class HabitService
    {
        readonly IStateStore store;
        readonly IClock clock;
        AppState state;
        public event Action Changed;
        public AppState Snapshot => StateCopy.Clone(state);
        public string Today => clock.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public int Balance => state.ledger.Sum(x=>x.delta);
        public HabitService(IStateStore store, IClock clock) { this.store=store; this.clock=clock; state=store.Load(); Validate(state); }

        public bool IsDue(Habit h) => !h.paused && !h.archived && (h.weekdays & (1 << (int)clock.Now.DayOfWeek)) != 0;
        void Apply(Action<AppState> action)
        {
            var next=StateCopy.Clone(state);
            if (String.CompareOrdinal(Today,next.lastDate)<0) throw new InvalidOperationException("Your phone date moved backwards. Correct the date before recording progress.");
            foreach(var h in next.habits.Where(x=>x.changeDate!="" && String.CompareOrdinal(x.changeDate,Today)<=0)) {
                h.title=h.pendingTitle; h.target=h.pendingTarget; h.weekdays=h.pendingWeekdays; h.changeDate="";
            }
            next.lastDate=Today;
            action(next); next.revision=state.revision+1; Validate(next);
            store.Commit(next); state=next; Changed?.Invoke();
        }
        public void ReconcileDay()
        {
            if(state.lastDate!=Today || state.habits.Any(x=>x.changeDate!="" && String.CompareOrdinal(x.changeDate,Today)<=0)) Apply(s=>{});
        }
        public string Add(string title, string species, int target=1, int weekdays=127)
        {
            CheckHabit(title,species,target,weekdays);
            string id=Guid.NewGuid().ToString("N");
            Apply(s=>{
                if(s.habits.Count(x=>!x.archived)>=20) throw new InvalidOperationException("The limit is 20 active habits.");
                s.habits.Add(new Habit {id=id,title=title.Trim(),species=species,target=target,weekdays=weekdays,createdUtc=clock.Now.UtcDateTime.ToString("O")});
                AddPlant(s,id,species);
            }); return id;
        }
        void AddPlant(AppState s,string habitId,string species)
        {
            int index=s.plants.Count;
            s.plants.Add(new Plant {id=Guid.NewGuid().ToString("N"),habitId=habitId,species=species,seed=Guid.NewGuid().GetHashCode() & int.MaxValue,plot=index/12,slot=index%12});
        }
        public void Edit(string id,string title,int target,int weekdays,int reminderMinutes)
        {
            var h=state.habits.Single(x=>x.id==id); CheckHabit(title,h.species,target,weekdays);
            if(reminderMinutes < -1 || reminderMinutes>=1440) throw new ArgumentException("Invalid reminder time");
            Apply(s=>{var x=s.habits.Single(v=>v.id==id); x.pendingTitle=title.Trim(); x.pendingTarget=target; x.pendingWeekdays=weekdays;
                x.changeDate=clock.Now.Date.AddDays(1).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture); x.reminderMinutes=reminderMinutes;});
        }
        public void SetPaused(string id,bool paused) => Apply(s=>s.habits.Single(x=>x.id==id).paused=paused);
        public void Archive(string id) => Apply(s=>s.habits.Single(x=>x.id==id).archived=true);
        public void Progress(string id,int amount,bool undo=false)
        {
            Apply(s=>{
                var h=s.habits.Single(x=>x.id==id);
                if(!IsDue(h)) throw new InvalidOperationException("This habit is not due today.");
                var r=s.days.SingleOrDefault(x=>x.habitId==id && x.date==Today);
                if(r==null) { var plant=s.plants.Last(x=>x.habitId==id); r=new DailyRecord {habitId=id,date=Today,target=h.target,plantId=plant.id}; s.days.Add(r); }
                bool was=r.complete;
                r.progress=undo?0:Math.Min(r.target,Math.Max(0,r.progress+Math.Max(0,amount)));
                r.complete=r.progress>=r.target;
                r.timezoneOffsetMinutes=(int)clock.Now.Offset.TotalMinutes;
                if(r.complete) r.completedUtc=clock.Now.UtcDateTime.ToString("O");
                if(was!=r.complete) {
                    Ledger(s,"completion:"+id+":"+Today,r.complete?10:-10);
                    var plant=s.plants.Single(x=>x.id==r.plantId);
                    plant.growthUnits+=r.complete?1:-1;
                    if(plant.growthUnits<Catalog.Maturity(plant.species))plant.maturityDate="";
                    else if(plant.maturityDate=="")plant.maturityDate=Today;
                }
            });
        }
        void Ledger(AppState s,string source,int delta) => s.ledger.Add(new LedgerEntry {id=Guid.NewGuid().ToString("N"),source=source,delta=delta,createdUtc=clock.Now.UtcDateTime.ToString("O")});
        public void PlantAnother(string habitId) => Apply(s=>{
            var h=s.habits.Single(x=>x.id==habitId);
            if(h.archived) throw new InvalidOperationException("Archived habit");
            var p=s.plants.Last(x=>x.habitId==habitId);
            if(p.growthUnits<Catalog.Maturity(p.species) || String.CompareOrdinal(p.maturityDate,Today)>=0) throw new InvalidOperationException("Plant again on the day after maturity.");
            AddPlant(s,habitId,h.species);
        });
        public void Buy(string item) => Apply(s=>{
            int price=Catalog.Price(item);
            if(s.inventory.Contains(item)) throw new InvalidOperationException("Already owned");
            if(s.ledger.Sum(x=>x.delta)<price) throw new InvalidOperationException("Not enough coins yet.");
            Ledger(s,"purchase:"+item,-price); s.inventory.Add(item);
        });
        public void Equip(string plantId,string pot) => Apply(s=>{
            if(pot!="clay" && pot!="leafy" && pot!="rainbow") throw new ArgumentException("Invalid pot");
            if(pot!="clay" && !s.inventory.Contains(pot)) throw new InvalidOperationException("Buy this pot first");
            s.plants.Single(x=>x.id==plantId).pot=pot;
        });
        public string BeginAd()
        {
            string id=Guid.NewGuid().ToString("N");
            Apply(s=>{
                if(s.adSessions.Count(x=>x.date==Today && x.granted)>=3) throw new InvalidOperationException("Today's bonus limit reached.");
                s.adSessions.Add(new AdSession {id=id,date=Today});
            }); return id;
        }
        public void EarnAd(string id)
        {
            if(state.adSessions.Any(x=>x.id==id && x.granted)) return;
            Apply(s=>{
                var a=s.adSessions.Single(x=>x.id==id);
                if(s.adSessions.Count(x=>x.date==a.date && x.granted)>=3) throw new InvalidOperationException("Bonus limit reached");
                a.granted=true; Ledger(s,"ad:"+id,a.amount);
            });
        }
        public void SetSettings(bool sound,bool reduced,string quality,bool guideSeen) => Apply(s=>{
            if(quality!="Low" && quality!="Medium") throw new ArgumentException("Invalid quality");
            s.settings.sound=sound;s.settings.reducedMotion=reduced;s.settings.quality=quality;s.settings.guideSeen=guideSeen;
        });
        public void Restore(AppState candidate) { Validate(candidate); var next=StateCopy.Clone(candidate); next.revision=state.revision+1;store.Commit(next);state=next;Changed?.Invoke(); }
        public void Reset() { var next=new AppState {revision=state.revision+1};store.Commit(next);state=next;Changed?.Invoke(); }
        public int Streak(string habitId)
        {
            var h=state.habits.Single(x=>x.id==habitId); int count=0;
            var date=clock.Now.Date;
            if(!state.days.Any(x=>x.habitId==habitId && x.date==Today && x.complete)) date=date.AddDays(-1);
            for(int i=0;i<36600;i++,date=date.AddDays(-1)) {
                if((h.weekdays & (1<<(int)date.DayOfWeek))==0) continue;
                if(!state.days.Any(x=>x.habitId==habitId && x.date==date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture) && x.complete)) break;
                count++;
            }return count;
        }
        static void CheckHabit(string title,string species,int target,int days)
        {
            if(String.IsNullOrWhiteSpace(title) || title.Trim().Length>60) throw new ArgumentException("Use a title between 1 and 60 characters.");
            if(!Catalog.Species.Contains(species) || target<1 || target>10000 || days<1 || days>127) throw new ArgumentException("Invalid habit target, species or weekdays.");
        }
        public static void Validate(AppState s)
        {
            if(s==null || s.schemaVersion!=1 || s.revision<0 || s.settings==null || s.habits==null || s.days==null || s.plants==null || s.ledger==null || s.inventory==null || s.adSessions==null) throw new InvalidOperationException("Unsupported or invalid save.");
            if(s.habits.Select(x=>x.id).Distinct().Count()!=s.habits.Count || s.plants.Select(x=>x.id).Distinct().Count()!=s.plants.Count || s.ledger.Select(x=>x.id).Distinct().Count()!=s.ledger.Count || s.adSessions.Select(x=>x.id).Distinct().Count()!=s.adSessions.Count) throw new InvalidOperationException("Duplicate save identifiers.");
            foreach(var h in s.habits) {CheckHabit(h.title,h.species,h.target,h.weekdays); if(String.IsNullOrEmpty(h.id)) throw new InvalidOperationException("Invalid habit ID");}
            if(s.days.Select(x=>x.habitId+":"+x.date).Distinct().Count()!=s.days.Count) throw new InvalidOperationException("Duplicate daily record");
            foreach(var r in s.days) {
                if(!s.habits.Any(x=>x.id==r.habitId) || !s.plants.Any(x=>x.id==r.plantId && x.habitId==r.habitId) || r.target<1 || r.progress<0 || r.progress>r.target || r.complete!=(r.progress>=r.target)) throw new InvalidOperationException("Invalid daily record");
                DateTime.ParseExact(r.date,"yyyy-MM-dd",CultureInfo.InvariantCulture);
            }
            foreach(var plant in s.plants) if(!s.habits.Any(x=>x.id==plant.habitId) || !Catalog.Species.Contains(plant.species) || plant.growthUnits!=s.days.Count(x=>x.plantId==plant.id && x.complete) || plant.slot<0 || plant.slot>=12 || plant.plot<0) throw new InvalidOperationException("Invalid plant record");
            if(s.inventory.Any(x=>!Catalog.Items.Contains(x)) || s.inventory.Distinct().Count()!=s.inventory.Count) throw new InvalidOperationException("Invalid inventory");
            foreach(var a in s.adSessions) if(a.amount!=30 || (a.granted && s.ledger.Count(x=>x.source=="ad:"+a.id)!=1)) throw new InvalidOperationException("Invalid ad entitlement");
        }
    }
}
