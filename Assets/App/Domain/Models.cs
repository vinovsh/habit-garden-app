using System;
using System.Collections.Generic;

namespace HabitGarden.Domain
{
    [Serializable] public sealed class AppState
    {
        public int schemaVersion = 1;
        public long revision;
        public string lastDate = "";
        public List<Habit> habits = new List<Habit>();
        public List<DailyRecord> days = new List<DailyRecord>();
        public List<Plant> plants = new List<Plant>();
        public List<LedgerEntry> ledger = new List<LedgerEntry>();
        public List<string> inventory = new List<string>();
        public List<AdSession> adSessions = new List<AdSession>();
        public Settings settings = new Settings();
    }
    [Serializable] public sealed class Settings
    {
        public bool sound = true, reducedMotion, guideSeen;
        public string quality = "Low";
    }
    [Serializable] public sealed class Habit
    {
        public string id, title, species, createdUtc;
        public string category = "Personal";
        public int target = 1, weekdays = 127;
        public bool paused, archived;
        public int reminderMinutes = -1;
        public string changeDate = "", pendingTitle = "";
        public int pendingTarget, pendingWeekdays;
    }
    [Serializable] public sealed class DailyRecord
    {
        public string habitId, date, plantId, completedUtc = "";
        public int progress, target, timezoneOffsetMinutes;
        public bool complete;
    }
    [Serializable] public sealed class Plant
    {
        public string id, habitId, species, maturityDate = "", pot = "clay";
        public int seed, plot, slot, growthUnits, generatorVersion = 1;
    }
    [Serializable] public sealed class LedgerEntry
    {
        public string id, source, createdUtc;
        public int delta;
    }
    [Serializable] public sealed class AdSession
    {
        public string id, date;
        public bool granted;
        public int amount = 30;
    }
    public interface IClock { DateTimeOffset Now { get; } }
    public sealed class DeviceClock : IClock { public DateTimeOffset Now => DateTimeOffset.Now; }
    public interface IStateStore
    {
        AppState Load();
        void Commit(AppState state);
    }
    public static class Catalog
    {
        public static readonly string[] Species = { "sunflower", "tree", "succulent" };
        public static readonly string[] Items = { "leafy", "rainbow", "fence", "butterfly" };
        public static int Maturity(string species) => species == "tree" ? 30 : species == "sunflower" ? 14 : 10;
        public static int Price(string item) => item == "leafy" ? 50 : item == "rainbow" ? 80 : item == "fence" ? 70 : item == "butterfly" ? 60 : throw new ArgumentException("Unknown item");
    }
}

