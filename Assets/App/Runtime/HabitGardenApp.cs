using System;
using System.Collections;
using System.IO;
using System.Linq;
using HabitGarden.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HabitGarden
{
    public sealed class HabitGardenApp : MonoBehaviour
    {
        HabitService habits;SaveRepository saves;GardenView garden;ReminderService reminders;AdService ads;AppAudio audioPlayer;
        RectTransform safe,body;Text balance,status;Font font;Sprite round;
        string tab="Garden",lastDay;Vector2 lastScreen;
        readonly Color ink=new Color(.13f,.25f,.18f),green=new Color(.38f,.62f,.28f),cream=new Color(.98f,.96f,.90f),lavender=new Color(.85f,.79f,.96f);
        float Width => body.rect.width>0?body.rect.width:1000;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartApp(){if(FindFirstObjectByType<HabitGardenApp>()==null)new GameObject("Habit Garden App").AddComponent<HabitGardenApp>();}
        void Awake() {
            Application.targetFrameRate=30;Screen.orientation=ScreenOrientation.Portrait;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");round=RoundedSprite();
            BuildShell();
            garden=new GameObject("Garden View").AddComponent<GardenView>();garden.Init();
            reminders=gameObject.AddComponent<ReminderService>();reminders.Initialize();
            ads=gameObject.AddComponent<AdService>();audioPlayer=gameObject.AddComponent<AppAudio>();
            saves=new SaveRepository(Path.Combine(Application.persistentDataPath,"habit-garden"));
            try{habits=new HabitService(saves,new DeviceClock());}
            catch(Exception){Recovery();return;}
            Connect();
        }
        void Connect() {
            habits.Changed+=OnStateChanged;ads.Initialize(habits);ads.ReadyChanged+=()=>{if(tab=="Shop")Render();};ads.Error+=Message;
            lastDay=habits.Today;Try(()=>habits.ReconcileDay());reminders.Refresh(habits.Snapshot);
            if(!habits.Snapshot.settings.guideSeen)tab="Guide";Render();
        }
        void OnStateChanged(){garden.Refresh(habits.Snapshot);reminders.Refresh(habits.Snapshot);Render();}
        void Update() {
            if(lastScreen!=new Vector2(Screen.width,Screen.height)){ApplySafeArea();lastScreen=new Vector2(Screen.width,Screen.height);}
            if(habits!=null && habits.Today!=lastDay){lastDay=habits.Today;Try(()=>habits.ReconcileDay());}
        }
        void OnApplicationPause(bool paused){if(!paused && habits!=null){Try(()=>habits.ReconcileDay());reminders.Refresh(habits.Snapshot);}AudioListener.pause=paused;}
        void BuildShell() {
            var canvas=new GameObject("Interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            if(FindFirstObjectByType<EventSystem>()==null)new GameObject("Event system",typeof(EventSystem),typeof(StandaloneInputModule));
            safe=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();safe.SetParent(canvas.transform,false);ApplySafeArea();
            var header=Panel(safe,"Header",cream);Anchor(header,0,1,1,1,new Vector2(0,-210),Vector2.zero);
            Label(header,"Habit Garden",44,32,420,76,52,true);
            Label(header,"Small habits, happy growth",44,112,700,48,30);
            balance=Label(header,"0 coins",760,40,260,64,38,true);
            body=new GameObject("Screen content",typeof(RectTransform)).GetComponent<RectTransform>();body.SetParent(safe,false);Anchor(body,0,0,1,1,new Vector2(30,190),new Vector2(-30,-220));
            var nav=Panel(safe,"Navigation",Color.white);Anchor(nav,0,0,1,0,Vector2.zero,new Vector2(0,170));
            string[] names={"Garden","Habits","Shop","Profile"};
            for(int i=0;i<names.Length;i++){string n=names[i];var b=Button(nav,n,0,28,240,90,()=>{tab=n;Message("");Render();});var rt=b.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2((i+.5f)/4f,0);rt.pivot=new Vector2(.5f,0);rt.anchoredPosition=new Vector2(0,35);}
            status=Label(safe,"",30,0,1000,70,27);status.rectTransform.anchorMin=status.rectTransform.anchorMax=new Vector2(0,0);status.rectTransform.pivot=new Vector2(0,0);status.rectTransform.anchoredPosition=new Vector2(30,170);
            Canvas.ForceUpdateCanvases();
        }
        void ApplySafeArea(){if(safe==null)return;Rect a=Screen.safeArea;safe.anchorMin=new Vector2(a.xMin/Screen.width,a.yMin/Screen.height);safe.anchorMax=new Vector2(a.xMax/Screen.width,a.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;}
        void Clear(){foreach(Transform t in body){t.gameObject.SetActive(false);Destroy(t.gameObject);}}
        void Render() {
            if(habits==null)return;Clear();var s=habits.Snapshot;balance.text=habits.Balance+" coins";
            garden.Show(tab=="Garden");garden.Refresh(s);
            if(tab=="Garden")GardenScreen(s);else if(tab=="Habits")HabitsScreen(s);else if(tab=="Shop")ShopScreen(s);else if(tab=="Guide")GuideScreen();else ProfileScreen(s);
        }
        void GardenScreen(AppState s) {
            Label(body,"Your garden",30,8,Width-60,70,46,true);
            Label(body,"Plot "+(garden.Plot+1)+"  •  Plants grow when habits are completed",30,85,Width-60,100,30);
            var card=Panel(body,"Today",Color.white);Anchor(card,0,0,1,0,new Vector2(0,0),new Vector2(0,300));
            var due=s.habits.Where(habits.IsDue).ToList();int done=due.Count(h=>s.days.Any(x=>x.habitId==h.id&&x.date==habits.Today&&x.complete));
            Label(card,"Today's progress",32,20,Width-70,60,36,true);
            Label(card,due.Count==0?"No habits due today":done+" of "+due.Count+" habits complete",32,85,Width-70,55,32);
            Button(card,"View habits",32,170,Width-290,85,()=>{tab="Habits";Render();});
            int plots=Math.Max(1,(s.plants.Count+11)/12);Button(card,"Next plot",Width-230,170,198,85,()=>{garden.SetPlot((garden.Plot+1)%plots,s);Render();});
            if(s.plants.Count==0)Label(body,"Plant your first habit in the Habits tab.",40,430,Width-80,120,38,true);
        }
        void HabitsScreen(AppState s) {
            var list=Scroll();CardHeading(list,"Daily Habits","Choose small, repeatable goals.");
            foreach(var h in s.habits.Where(x=>!x.archived)) {
                var day=s.days.SingleOrDefault(x=>x.habitId==h.id&&x.date==habits.Today);bool complete=day!=null&&day.complete;
                var card=Card(list,265);Label(card,h.title,30,20,Width-270,65,39,true);
                string detail=h.paused?"Paused":!habits.IsDue(h)?"Not scheduled today":(day?.progress??0)+" / "+(day?.target??h.target)+" today";
                Label(card,detail+"  •  "+habits.Streak(h.id)+" day streak",30,90,Width-70,50,29);
                string id=h.id;Button(card,"Edit",Width-195,25,150,70,()=>Form(h));
                if(habits.IsDue(h))Button(card,complete?"Undo today":h.target==1?"Complete":"Add 1",30,165,Width-70,78,()=>Try(()=>{habits.Progress(id,1,complete);if(!complete)audioPlayer.Complete(s.settings.sound);}));
            }
            CardButton(list,"+ Add habit",()=>Form(null));
        }
        void ShopScreen(AppState s) {
            var list=Scroll();CardHeading(list,"Garden Shop","Decorations are bought with earned coins.");
            string[] titles={"Leafy Pot","Rainbow Planter","White Fence","Butterfly Decor"};
            for(int i=0;i<Catalog.Items.Length;i++) {
                string item=Catalog.Items[i];bool owned=s.inventory.Contains(item);var c=Card(list,170);
                Label(c,titles[i],30,18,Width-300,65,38,true);Label(c,owned?"Owned":Catalog.Price(item)+" coins",30,92,Width-300,50,30);
                var button=Button(c,owned?"Owned":"Buy",Width-225,45,180,80,()=>Try(()=>habits.Buy(item)));button.interactable=!owned;
            }
            if(ads.Ready&&s.adSessions.Count(x=>x.date==habits.Today&&x.granted)<3) {
                var c=Card(list,210);c.GetComponent<Image>().color=lavender;
                Label(c,"Optional bonus",30,20,Width-70,60,36,true);Label(c,"Watch an ad to earn 30 coins",30,85,Width-300,80,29);
                Button(c,"Watch ad",Width-270,100,225,80,()=>Try(()=>ads.ShowReward()));
            }
        }
        void ProfileScreen(AppState s) {
            var list=Scroll();CardHeading(list,"Your progress",s.days.Count(x=>x.complete)+" completed habit days  •  "+s.plants.Count+" plants");
            CardButton(list,s.settings.sound?"Sound on":"Sound off",()=>Try(()=>habits.SetSettings(!s.settings.sound,s.settings.reducedMotion,s.settings.quality,true)));
            CardButton(list,s.settings.reducedMotion?"Reduced motion on":"Reduced motion off",()=>Try(()=>habits.SetSettings(s.settings.sound,!s.settings.reducedMotion,s.settings.quality,true)));
            CardButton(list,"Graphics: "+s.settings.quality,()=>Try(()=>habits.SetSettings(s.settings.sound,s.settings.reducedMotion,s.settings.quality=="Low"?"Medium":"Low",true)));
            CardButton(list,"Guide",()=>{tab="Guide";Render();});
            CardButton(list,"Export local backup",BackupExport);
            CardButton(list,"Restore local backup",BackupImport);
            CardButton(list,"Privacy choices",()=>ads.PrivacyChoices());
            foreach(var plant in s.plants) {
                var c=Card(list,220);var habit=s.habits.Single(x=>x.id==plant.habitId);string id=plant.id;
                Label(c,habit.title,30,18,Width-70,65,35,true);
                Label(c,plant.species+"  •  "+plant.growthUnits+" / "+Catalog.Maturity(plant.species)+" growth",30,85,Width-70,50,28);
                Button(c,"Change pot",30,140,(Width-100)/2,65,()=>Try(()=>{string[] options=new[]{"clay"}.Concat(s.inventory.Where(x=>x=="leafy"||x=="rainbow")).ToArray();habits.Equip(id,options[(Array.IndexOf(options,plant.pot)+1)%options.Length]);garden.Refresh(habits.Snapshot,true);}));
                if(plant.growthUnits>=Catalog.Maturity(plant.species)&&plant.maturityDate!=habits.Today)Button(c,"Plant another",Width/2,140,(Width-100)/2,65,()=>Try(()=>habits.PlantAnother(plant.habitId)));
            }
            CardButton(list,"Delete all app data",()=>Confirm("Delete habits, garden and coins?",()=>Try(()=>{habits.Reset();saves.PurgeHistory(habits.Snapshot);tab="Guide";Render();})));
        }
        void GuideScreen() {
            var list=Scroll();CardHeading(list,"Welcome to Habit Garden","Your small daily habits grow a garden.");
            var c=Card(list,530);Label(c,"1  Add a habit and choose a plant.\n\n2  Complete the target on scheduled days.\n\n3  Each completed day grows your plant.\n\n4  Earn coins for garden decorations.\n\nEverything above works offline. Optional shop ads are available only when ready.",30,30,Width-70,470,33);
            CardButton(list,"Create my first habit",()=>{habits.SetSettings(true,false,"Low",true);Form(null);});
            CardButton(list,"Start with 3 example habits",()=>Try(()=>{
                if(habits.Snapshot.habits.Count==0){habits.Add("Drink water","sunflower",8);habits.Add("Read a book","tree",15);habits.Add("Take a walk","succulent",20);}
                habits.SetSettings(true,false,"Low",true);tab="Habits";Render();
            }));
        }
        void Form(Habit existing) {
            Clear();garden.Show(false);var list=Scroll();CardHeading(list,existing==null?"Add habit":"Edit habit","Targets and weekdays change from tomorrow when editing.");
            var title=Field(list,"Habit name",existing?.title??"",InputField.ContentType.Standard);
            var target=Field(list,"Daily target",(existing?.target??1).ToString(),InputField.ContentType.IntegerNumber);
            var time=Field(list,"Reminder time HH:mm or leave blank",existing!=null&&existing.reminderMinutes>=0?(existing.reminderMinutes/60).ToString("00")+":"+(existing.reminderMinutes%60).ToString("00"):"",InputField.ContentType.Standard);
            int days=existing?.weekdays??127;var row=Card(list,140);Label(row,"Days",25,8,Width-50,40,30,true);
            string[] names={"Su","Mo","Tu","We","Th","Fr","Sa"};
            for(int i=0;i<7;i++){int bit=1<<i;var b=Button(row,names[i],25+i*(Width-70)/7,62,(Width-100)/7,60,()=>{});b.GetComponent<Image>().color=(days&bit)!=0?green:lavender;b.onClick.AddListener(()=>{days^=bit;b.GetComponent<Image>().color=(days&bit)!=0?green:lavender;});}
            int species=Array.IndexOf(Catalog.Species,existing?.species??"sunflower");Button speciesButton=null;
            if(existing==null){var c=Card(list,110);speciesButton=Button(c,"Plant: "+Catalog.Species[species],25,15,Width-60,75,()=>{species=(species+1)%3;speciesButton.GetComponentInChildren<Text>().text="Plant: "+Catalog.Species[species];});}
            CardButton(list,"Save habit",()=>Try(()=>{
                if(!Int32.TryParse(target.text,out int number))throw new ArgumentException("Enter a whole number target.");
                int minutes=-1;if(!String.IsNullOrWhiteSpace(time.text)) {
                    if(!DateTime.TryParseExact(time.text,"HH:mm",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var parsed))throw new ArgumentException("Use a time such as 18:30.");minutes=parsed.Hour*60+parsed.Minute;
                }
                if(existing==null){string id=habits.Add(title.text,Catalog.Species[species],number,days);if(minutes>=0)habits.Edit(id,title.text,number,days,minutes);}
                else habits.Edit(existing.id,title.text,number,days,minutes);
                if(minutes>=0)StartCoroutine(PermissionThenRefresh());tab="Habits";Render();
            }));
            if(existing!=null){CardButton(list,existing.paused?"Resume habit":"Pause habit",()=>Try(()=>habits.SetPaused(existing.id,!existing.paused)));CardButton(list,"Archive habit",()=>Confirm("Archive this habit? Its garden and history will stay.",()=>Try(()=>habits.Archive(existing.id))));}
            CardButton(list,"Cancel",()=>{tab="Habits";Render();});
        }
        IEnumerator PermissionThenRefresh(){yield return reminders.RequestPermission();reminders.Refresh(habits.Snapshot);}
        void BackupExport(){Try(()=>{
            string text=SaveRepository.Export(habits.Snapshot);
#if UNITY_ANDROID && !UNITY_EDITOR
            using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var bridge=new AndroidJavaClass("com.rendermind.habitgarden.DocumentBridge"))bridge.CallStatic("exportBackup",unity.GetStatic<AndroidJavaObject>("currentActivity"),text);
#else
            File.WriteAllText(Path.Combine(Application.persistentDataPath,"HabitGarden-backup.json"),text);Message("Local backup saved.");
#endif
        });}
        void BackupImport(){Confirm("Restore a backup? Current progress will be replaced.",()=>Try(()=>{
#if UNITY_ANDROID && !UNITY_EDITOR
            using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var bridge=new AndroidJavaClass("com.rendermind.habitgarden.DocumentBridge"))bridge.CallStatic("importBackup",unity.GetStatic<AndroidJavaObject>("currentActivity"));
#else
            OnBackupImported(File.ReadAllText(Path.Combine(Application.persistentDataPath,"HabitGarden-backup.json")));
#endif
        }));}
        public void OnBackupFinished(string message){Message(message);}
        public void OnBackupImported(string text){Try(()=>{var imported=SaveRepository.Import(text);if(habits==null){saves.Checkpoint(imported);habits=new HabitService(saves,new DeviceClock());Connect();}else habits.Restore(imported);tab="Profile";Render();Message("Backup restored.");});}
        void Recovery(){Clear();garden.Show(false);var list=Scroll();CardHeading(list,"Save recovery needed","Your saved progress could not be loaded.");CardButton(list,"Restore backup",BackupImport);CardButton(list,"Reset damaged save",()=>Confirm("Permanently delete the damaged save?",()=>Try(()=>{saves.ExplicitDelete();habits=new HabitService(saves,new DeviceClock());Connect();})));}
        void Confirm(string question,Action yes){Clear();garden.Show(false);var list=Scroll();CardHeading(list,"Please confirm",question);CardButton(list,"Confirm",yes);CardButton(list,"Cancel",()=>{if(habits!=null)Render();else Recovery();});}
        void Try(Action action){try{action();}catch(Exception e){Message(e is IOException?"Could not save. Check free space and try again.":e.Message);}}
        void Message(string text){if(status!=null)status.text=text;}

        RectTransform Scroll() {
            var root=Panel(body,"Scroll",cream);Anchor(root,0,0,1,1,Vector2.zero,Vector2.zero);
            var viewport=Panel(root,"Viewport",cream);Anchor(viewport,0,0,1,1,Vector2.zero,Vector2.zero);viewport.gameObject.AddComponent<RectMask2D>();
            var content=new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter)).GetComponent<RectTransform>();content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            var layout=content.GetComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(0,0,15,35);layout.spacing=24;layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
            content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;return content;
        }
        RectTransform Card(RectTransform parent,float height){var card=Panel(parent,"Card",Color.white);var layout=card.gameObject.AddComponent<LayoutElement>();layout.minHeight=layout.preferredHeight=height;return card;}
        void CardHeading(RectTransform list,string title,string detail){var c=Card(list,210);c.GetComponent<Image>().color=cream;Label(c,title,25,5,Width-60,85,46,true);Label(c,detail,25,95,Width-60,105,29);}
        void CardButton(RectTransform list,string label,Action action){var c=Card(list,110);c.GetComponent<Image>().color=cream;Button(c,label,10,10,Width-25,85,action);}
        InputField Field(RectTransform list,string caption,string value,InputField.ContentType type){var c=Card(list,180);Label(c,caption,25,12,Width-60,45,29,true);var area=Panel(c,"Input",new Color(.94f,.96f,.91f));Position(area,25,68,Width-60,86);var text=Label(area,value,15,8,Width-100,65,33);var input=area.gameObject.AddComponent<InputField>();input.textComponent=text;input.targetGraphic=area.GetComponent<Image>();input.contentType=type;input.characterLimit=caption=="Habit name"?60:10;input.text=value;return input;}
        RectTransform Panel(Transform parent,string name,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.color=color;image.sprite=round;image.type=Image.Type.Sliced;return go.GetComponent<RectTransform>();}
        Text Label(Transform parent,string text,float x,float y,float w,float h,int size,bool bold=false){var go=new GameObject("Label",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();Position(rt,x,y,w,h);var label=go.GetComponent<Text>();label.text=text;label.font=font;label.fontSize=size;label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;label.color=ink;label.supportRichText=false;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;return label;}
        Button Button(Transform parent,string text,float x,float y,float w,float h,Action action){var rt=Panel(parent,text,green);Position(rt,x,y,w,h);var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=rt.GetComponent<Image>();b.onClick.AddListener(()=>action());var label=Label(rt,text,10,0,w-20,h,30,true);label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;return b;}
        static void Position(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Anchor(RectTransform r,float xmin,float ymin,float xmax,float ymax,Vector2 min,Vector2 max){r.anchorMin=new Vector2(xmin,ymin);r.anchorMax=new Vector2(xmax,ymax);r.offsetMin=min;r.offsetMax=max;}
        static Sprite RoundedSprite(){var texture=new Texture2D(48,48,TextureFormat.RGBA32,false);for(int y=0;y<48;y++)for(int x=0;x<48;x++){float dx=Mathf.Max(0,Mathf.Abs(x-23.5f)-11.5f),dy=Mathf.Max(0,Mathf.Abs(y-23.5f)-11.5f);texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(12.5f-Mathf.Sqrt(dx*dx+dy*dy))));}texture.Apply();return Sprite.Create(texture,new Rect(0,0,48,48),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));}
    }
}
