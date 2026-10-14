using System;
using System.Collections;
using System.IO;
using HabitGarden.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HabitGarden
{
    public sealed partial class HabitGardenApp : MonoBehaviour
    {
        HabitService habits; SaveRepository saves; GardenView garden; ReminderService reminders; AdService ads; AppAudio audioPlayer;
        RectTransform safe, body, overlay; TMP_Text balance, status; TMP_FontAsset font; Sprite round;
        string tab = "Garden", lastDay, selectedPlant, habitFilter = "Today", renderedTab = "";
        Vector2 lastScreen; Rect lastSafeArea; float messageUntil;
        readonly Color ink = new Color(.14f,.25f,.19f), green = new Color(.24f,.39f,.28f),
            cream = new Color(.98f,.97f,.93f), sage = new Color(.87f,.92f,.83f),
            lavender = new Color(.91f,.87f,.97f), peach = new Color(1f,.89f,.80f), muted = new Color(.36f,.43f,.36f);
        readonly System.Collections.Generic.Dictionary<string,Button> navigation = new System.Collections.Generic.Dictionary<string,Button>();
        float Width => body.rect.width > 0 ? body.rect.width : 358;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartApp() { if (FindFirstObjectByType<HabitGardenApp>() == null) new GameObject("Habit Garden App").AddComponent<HabitGardenApp>(); }
        void Awake()
        {
            Application.targetFrameRate = 30; Screen.orientation = ScreenOrientation.Portrait;
            font = Resources.Load<TMP_FontAsset>("GardenFont");
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (font == null) font = TMP_FontAsset.CreateFontAsset(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            round = RoundedSprite(); BuildShell();
            garden = new GameObject("Garden View").AddComponent<GardenView>(); garden.Init();
            garden.Selected += id => { selectedPlant = id; Render(); };
            reminders = gameObject.AddComponent<ReminderService>(); reminders.Initialize();
            ads = gameObject.AddComponent<AdService>(); audioPlayer = gameObject.AddComponent<AppAudio>();
            saves = new SaveRepository(Path.Combine(Application.persistentDataPath,"habit-garden"));
            try { habits = new HabitService(saves,new DeviceClock()); }
            catch (Exception) { Recovery(); return; }
            Connect();
        }
        void Connect()
        {
            habits.Changed += OnStateChanged;
            ads.Initialize(habits); ads.ReadyChanged += () => { if (tab == "Shop" && overlay == null) Render(); }; ads.Error += Message;
            lastDay = habits.Today; Try(() => habits.ReconcileDay()); reminders.Refresh(habits.Snapshot);
            if (!habits.Snapshot.settings.guideSeen) tab = "Guide";
            Render();
        }
        void OnStateChanged() { garden.Refresh(habits.Snapshot); reminders.Refresh(habits.Snapshot); if (overlay == null) Render(); }
        void Update()
        {
            if (lastScreen != new Vector2(Screen.width,Screen.height) || lastSafeArea != Screen.safeArea)
            { ApplySafeArea(); lastScreen = new Vector2(Screen.width,Screen.height); lastSafeArea = Screen.safeArea; }
            if (Input.GetKeyDown(KeyCode.Escape))
            { if (overlay != null) CloseOverlay(); else if (renderedTab == "Form") Navigate("Habits"); else if (tab != "Garden") Navigate("Garden"); }
            if (habits != null && habits.Today != lastDay) { lastDay = habits.Today; Try(() => habits.ReconcileDay()); }
            if (status != null && status.transform.parent.gameObject.activeSelf && Time.unscaledTime > messageUntil) status.transform.parent.gameObject.SetActive(false);
        }
        void OnApplicationPause(bool paused)
        {
            if (!paused && habits != null) { Try(() => habits.ReconcileDay()); reminders.Refresh(habits.Snapshot); }
            AudioListener.pause = paused;
        }
        void OnDestroy() { if (habits != null) habits.Changed -= OnStateChanged; }
        void Navigate(string destination) { CloseOverlay(); renderedTab = ""; tab = destination; Message(""); Render(); }
        void Clear() { foreach (Transform child in body) { child.gameObject.SetActive(false); Destroy(child.gameObject); } }
        void Render()
        {
            if (habits == null) return;
            var previous = body.GetComponentInChildren<ScrollRect>();
            float scrollY = renderedTab == tab && previous != null ? previous.content.anchoredPosition.y : 0;
            Clear(); renderedTab = tab; var state = habits.Snapshot; balance.text = habits.Balance.ToString();
            foreach (var entry in navigation)
            {
                bool active = entry.Key == tab;
                entry.Value.GetComponent<Image>().color = active ? sage : cream;
                foreach (var text in entry.Value.GetComponentsInChildren<TMP_Text>()) text.color = active ? green : muted;
                entry.Value.GetComponentInChildren<GardenArt>().color = active ? green : muted;
            }
            garden.Show(tab == "Garden"); garden.Refresh(state);
            switch (tab)
            {
                case "Garden": GardenScreen(state); break;
                case "Habits": HabitsScreen(state); break;
                case "Shop": ShopScreen(state); break;
                case "Guide": GuideScreen(); break;
                default: ProfileScreen(state); break;
            }
            Canvas.ForceUpdateCanvases();
            var next = body.GetComponentInChildren<ScrollRect>();
            if (next != null) next.content.anchoredPosition = new Vector2(0,Mathf.Clamp(scrollY,0,Mathf.Max(0,next.content.rect.height-next.viewport.rect.height)));
        }
        IEnumerator PermissionThenRefresh() { yield return reminders.RequestPermission(); reminders.Refresh(habits.Snapshot); }
        void BackupExport(){Try(()=>{
            string text=SaveRepository.Export(habits.Snapshot);
#if UNITY_ANDROID && !UNITY_EDITOR
            using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var bridge=new AndroidJavaClass("com.rendermind.habitgarden.DocumentBridge"))bridge.CallStatic("exportBackup",unity.GetStatic<AndroidJavaObject>("currentActivity"),text);
#else
            File.WriteAllText(Path.Combine(Application.persistentDataPath,"HabitGarden-backup.json"),text);Message("Local backup saved.");
#endif
        });}
        void BackupImport(){Confirm("Restore a backup?","Current progress will be replaced after the backup is validated.",()=>Try(()=>{
#if UNITY_ANDROID && !UNITY_EDITOR
            using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var bridge=new AndroidJavaClass("com.rendermind.habitgarden.DocumentBridge"))bridge.CallStatic("importBackup",unity.GetStatic<AndroidJavaObject>("currentActivity"));
#else
            OnBackupImported(File.ReadAllText(Path.Combine(Application.persistentDataPath,"HabitGarden-backup.json")));
#endif
        }));}
        public void OnBackupFinished(string message){Message(message);}
        public void OnBackupImported(string text){Try(()=>{var imported=SaveRepository.Import(text);if(habits==null){saves.Checkpoint(imported);habits=new HabitService(saves,new DeviceClock());Connect();}else habits.Restore(imported);tab="Profile";Render();Message("Backup restored.");});}

        void Recovery()
        {
            Clear(); garden.Show(false); var list = Scroll(); Heading(list,"A little care needed","Your saved garden couldn't be loaded. Restore a backup or explicitly reset it.");
            CardButton(list,"Restore a backup",BackupImport);
            CardButton(list,"Reset damaged save",() => Confirm("Reset this save?","This permanently removes the damaged save. You can restore an exported backup later.",() => Try(() => { saves.ExplicitDelete(); habits = new HabitService(saves,new DeviceClock()); Connect(); })),true);
        }
        void Try(Action action)
        {
            try { action(); }
            catch (Exception e) { Message(e is IOException ? "Couldn't save. Check free space and try again." : e.Message); }
        }
        void Message(string text)
        {
            if (status == null) return;
            status.text = text; status.transform.parent.SetAsLastSibling(); status.transform.parent.gameObject.SetActive(!String.IsNullOrEmpty(text));
            messageUntil = Time.unscaledTime + 6;
        }
    }
}
