using System;
using System.Globalization;
using System.Linq;
using HabitGarden.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HabitGarden
{
    public sealed partial class HabitGardenApp
    {
        void GardenScreen(AppState state)
        {
            var list = Scroll();
            Heading(list,"Your little garden",DateTime.Now.ToString("dddd, d MMMM",CultureInfo.InvariantCulture)+"  •  Room to grow");
            var due = state.habits.Where(habits.IsDue).ToList();
            int done = due.Count(h => state.days.Any(d => d.habitId == h.id && d.date == habits.Today && d.complete));
            var summary = Card(list,143,sage);
            Label(summary,"TODAY'S LITTLE WINS",16,14,Width-32,22,11,true,green);
            Label(summary,due.Count == 0 ? "A day at your own pace" : done+" of "+due.Count+" habits complete",16,43,Width-32,28,20,true);
            ProgressBar(summary,due.Count == 0 ? 0 : (float)done/due.Count,16,86,Width-32);
            int bestStreak = state.habits.Where(h => !h.archived && !h.paused).Select(h => habits.Streak(h.id)).DefaultIfEmpty(0).Max();
            Label(summary,bestStreak+" day best active streak  •  Keep growing gently",16,107,Width-32,26,11,false,green);
            var view = Card(list,285,new Color(.93f,.95f,.87f));
            Label(view,"PLOT "+(garden.Plot+1).ToString("00"),16,14,100,20,11,true,green);
            Label(view,"Tap a plant to say hello",16,39,Width-32,22,12,false,muted);
            var imageGo = new GameObject("Interactive garden",typeof(RectTransform),typeof(RawImage),typeof(GardenPointer)); imageGo.transform.SetParent(view,false);
            var imageRect = imageGo.GetComponent<RectTransform>(); Position(imageRect,6,65,Width-12,210);
            imageGo.GetComponent<RawImage>().texture = garden.Texture; imageGo.GetComponent<GardenPointer>().garden = garden;
            if (state.plants.Count == 0)
            {
                imageGo.SetActive(false); Art(view,"sunflower",Width/2-43,79,86,120);
                Label(view,"A fresh start is a beautiful thing.",16,200,Width-32,32,14,true);
                Button(view,"Plant your first habit",16,231,Width-32,48,() => Navigate("Habits"));
            }
            int plots = Math.Max(1,state.plants.Select(p => p.plot+1).DefaultIfEmpty(1).Max());
            if (plots > 1)
            {
                var row = Card(list,50,cream);
                Button(row,"Previous plot",0,0,(Width-12)/2,50,() => { garden.SetPlot((garden.Plot+plots-1)%plots,state); selectedPlant = null; Render(); },sage);
                Button(row,"Next plot",Width/2+6,0,(Width-12)/2,50,() => { garden.SetPlot((garden.Plot+1)%plots,state); selectedPlant = null; Render(); },sage);
            }
            var chosen = state.plants.FirstOrDefault(p => p.id == selectedPlant && p.plot == garden.Plot);
            if (chosen != null) PlantCard(list,chosen,state,true);
            CardButton(list,"View today's habits",() => Navigate("Habits"));
            Section(list,"Growing with you","Your plants never die. Missed days simply pause growth.");
            foreach (var plant in state.plants.Where(p => p.plot == garden.Plot)) PlantCard(list,plant,state,false);
        }
        void PlantCard(RectTransform list,Plant plant,AppState state,bool selected)
        {
            var linked = state.habits.Single(h => h.id == plant.habitId); float value = (float)plant.growthUnits/Catalog.Maturity(plant.species);
            var card = Card(list,selected ? 208 : 118,selected ? sage : Color.white);
            Art(card,plant.species,14,17,60,77);
            Label(card,linked.title,88,15,Width-106,40,16,true);
            Label(card,GardenPresentation.Stage(plant.growthUnits,Catalog.Maturity(plant.species))+"  •  "+plant.growthUnits+" / "+Catalog.Maturity(plant.species)+" days",88,59,Width-106,24,12,false,muted);
            ProgressBar(card,value,88,95,Width-108);
            if (selected)
            {
                Button(card,"View linked habit",16,124,Width-32,48,() => { habitFilter = "All"; Navigate("Habits"); },green);
                Label(card,"Growing a "+GardenPresentation.SpeciesName(plant.species).ToLowerInvariant()+" with every completed day.",16,181,Width-32,21,11,false,muted);
            }
            else
            {
                var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = card.GetComponent<Image>();
                button.onClick.AddListener(() => { selectedPlant = plant.id; Render(); });
            }
        }
        void HabitsScreen(AppState state)
        {
            var list = Scroll(); Heading(list,"Small steps. Big growth.","Make a little space for the things that matter.");
            var filters = Card(list,48,cream); string[] names = {"Today","All","Paused"};
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i]; Button(filters,name,i*(Width+6)/3,0,(Width-12)/3,48,() => { habitFilter = name; Render(); },habitFilter == name ? sage : Color.white);
            }
            var visible = state.habits.Where(h => !h.archived && (habitFilter == "All" || (habitFilter == "Paused" ? h.paused : habits.IsDue(h)))).ToList();
            if (visible.Count == 0)
            {
                var empty = Card(list,214); Art(empty,"leaf",Width/2-26,24,52,62);
                Label(empty,habitFilter == "Paused" ? "Nothing on pause" : habitFilter == "Today" ? "A little breathing room" : "Let's plant a good habit",16,100,Width-32,32,20,true);
                Label(empty,habitFilter == "Today" ? "No habits are scheduled today. See all your habits or plant a new one below." : "Choose something small. A glass of water, a page of a book, a moment outside.",16,143,Width-32,54,13,false,muted);
            }
            foreach (var habit in visible)
            {
                var record = state.days.SingleOrDefault(d => d.habitId == habit.id && d.date == habits.Today);
                bool complete = record != null && record.complete; int target = record?.target ?? habit.target, progress = record?.progress ?? 0;
                var card = Card(list,232,complete ? new Color(.94f,.97f,.91f) : Color.white);
                var icon = Panel(card,"Plant badge",habit.species == "tree" ? sage : habit.species == "succulent" ? lavender : peach); Position(icon,14,14,48,54); Art(icon,habit.species,8,4,32,44);
                Label(card,habit.title,75,14,Width-140,50,17,true);
                Button(card,"Edit",Width-62,13,48,48,() => Form(habit),cream);
                string description = habit.paused ? "Paused • Grow again when you're ready" : !habits.IsDue(habit) ? "Not scheduled today" : complete ? "Complete today  •  +10 coins earned" : progress+" / "+target+" today";
                Label(card,description,16,83,Width-32,28,13,false,complete ? green : muted);
                ProgressBar(card,(float)progress/target,16,124,Width-32);
                Label(card,habits.Streak(habit.id)+" day streak  •  "+(habit.category ?? "Personal"),16,145,Width-32,24,11,false,muted);
                if (habits.IsDue(habit))
                {
                    string id = habit.id;
                    Button(card,complete ? "Completed ✓  ·  Undo" : target == 1 ? "Complete habit" : "+ Add 1",16,178,Width-32,48,() => Try(() =>
                    {
                        habits.Progress(id,1,complete);
                        if (!complete && habits.Snapshot.days.Any(d => d.habitId == id && d.date == habits.Today && d.complete))
                        { audioPlayer.Complete(state.settings.sound); Message("A little win! +10 coins. Your plant is growing."); }
                    }),complete ? sage : green);
                }
                else Button(card,habit.paused ? "Resume habit" : "View schedule",16,178,Width-32,48,() => { if (habit.paused) Try(() => habits.SetPaused(habit.id,false)); else Form(habit); },sage);
            }
            CardButton(list,"+ Plant a new habit",() => Form(null));
            Section(list,"Gentle consistency","Each completed scheduled day earns 10 coins and one growth day.");
        }
        void ShopScreen(AppState state)
        {
            var list = Scroll(); Heading(list,"A little garden joy","Lovely things for the garden you're growing.");
            var intro = Card(list,86,peach); Art(intro,"coin",16,25,32,32,new Color(.63f,.40f,.12f));
            Label(intro,habits.Balance+" coins to make it yours",62,18,Width-78,27,17,true);
            Label(intro,"Earn 10 coins with each completed habit.",62,50,Width-78,23,11,false,muted);
            string[] titles = {"Leafy pot","Rainbow planter","Garden fence","Butterfly friend"};
            string[] details = {"A fresh little home for your plant.","A splash of colour, a lot of happiness.","A cosy border for your growing world.","A tiny visitor with lavender wings."};
            for (int i = 0; i < Catalog.Items.Length; i++)
            {
                string item = Catalog.Items[i], title = titles[i], detail = details[i]; bool owned = state.inventory.Contains(item);
                var card = Card(list,178); var preview = Panel(card,"Preview",i%2 == 0 ? sage : lavender); Position(preview,14,14,96,148); Art(preview,item,8,20,80,100);
                Label(card,title,126,18,Width-142,30,18,true); Label(card,detail,126,56,Width-142,32,12,false,muted);
                Label(card,owned ? "In your collection ✓" : Catalog.Price(item)+" coins",126,91,Width-142,24,13,true,green);
                Button(card,owned ? "View owned item" : "Preview",126,125,Width-142,48,() => ShopPreview(item,title,detail),sage);
                // The entire preview area is also a 96 x 148 point target.
                var previewButton = preview.gameObject.AddComponent<Button>(); previewButton.targetGraphic = preview.GetComponent<Image>(); previewButton.onClick.AddListener(() => ShopPreview(item,title,detail));
            }
            if (ads.Ready && state.adSessions.Count(a => a.date == habits.Today && a.granted) < 3)
            {
                var bonus = Card(list,166,lavender); Label(bonus,"A little optional bonus",16,16,Width-32,28,18,true);
                Label(bonus,"Watch an ad for 30 coins. Your habits and plants work without ads.",16,54,Width-32,44,13,false,muted);
                Button(bonus,"Watch ad  ·  +30 coins",16,108,Width-32,48,() => Try(() => ads.ShowReward()));
            }
            Section(list,"Made for you, earned by you","Decorations are cosmetic. Every plant grows through your habits.");
        }
        void ShopPreview(string item,string title,string detail)
        {
            bool owned = habits.Snapshot.inventory.Contains(item); int price = Catalog.Price(item);
            var modal = Modal(title,detail,400); var backdrop = Panel(modal,"Item preview",item == "rainbow" || item == "butterfly" ? lavender : sage);
            Position(backdrop,16,136,Width-32,162); Art(backdrop,item,(Width-152)/2,10,120,138);
            Label(modal,owned ? "Already in your collection" : price+" coins  •  Balance: "+habits.Balance,16,307,Width-32,25,13,true,green);
            Button(modal,"Close",16,343,(Width-44)/2,48,CloseOverlay,sage);
            var buy = Button(modal,owned ? "Owned ✓" : habits.Balance < price ? "Need "+(price-habits.Balance)+" coins" : "Buy for "+price,Width/2+6,343,(Width-44)/2,48,() => Try(() =>
            {
                habits.Buy(item); CloseOverlay(); Render(); Message(title+" is yours! Find pots in your collection.");
            })); buy.interactable = !owned && habits.Balance >= price;
        }
        void ProfileScreen(AppState state)
        {
            var list = Scroll(); Heading(list,"Look how far you've grown","Your garden is a collection of little victories.");
            var stats = Card(list,106,sage);
            int mature = state.plants.Count(p => p.growthUnits >= Catalog.Maturity(p.species));
            Stat(stats,state.days.Count(d => d.complete).ToString(),"habit days",0,Width/3);
            Stat(stats,state.plants.Count.ToString(),"plants grown",Width/3,Width/3);
            Stat(stats,mature.ToString(),"mature plants",2*Width/3,Width/3);
            WeeklyChart(list,state);
            Section(list,"Your plant collection","Change a pot or begin a new plant after maturity.");
            if (state.plants.Count == 0)
            {
                var empty = Card(list,74); Label(empty,"Your first plant is waiting for you.",16,15,Width-32,44,14,false,muted);
            }
            foreach (var plant in state.plants)
            {
                var card = Card(list,178); var habit = state.habits.Single(h => h.id == plant.habitId);
                Art(card,plant.species,12,12,52,76); Label(card,habit.title,80,15,Width-96,36,16,true);
                Label(card,GardenPresentation.Stage(plant.growthUnits,Catalog.Maturity(plant.species))+"  •  "+plant.growthUnits+" / "+Catalog.Maturity(plant.species)+" days",80,56,Width-96,24,12,false,muted);
                ProgressBar(card,(float)plant.growthUnits/Catalog.Maturity(plant.species),16,99,Width-32);
                Button(card,"Choose pot",16,117,(Width-44)/2,48,() => PotPicker(plant),sage);
                bool canPlant = !habit.archived && plant == state.plants.Last(p => p.habitId == plant.habitId) && plant.growthUnits >= Catalog.Maturity(plant.species) && String.CompareOrdinal(plant.maturityDate,habits.Today) < 0;
                if (canPlant) Button(card,"Plant another",Width/2+6,117,(Width-44)/2,48,() => Try(() => habits.PlantAnother(plant.habitId)));
                else Label(card,"Plot "+(plant.plot+1)+" · "+GardenPresentation.SpeciesName(plant.species),Width/2+6,128,(Width-44)/2,32,11,false,muted);
            }
            Section(list,"Make yourself comfortable");
            Setting(list,"Sound","Soft taps and completion chimes",state.settings.sound,() => Try(() => habits.SetSettings(!state.settings.sound,state.settings.reducedMotion,state.settings.quality,true)));
            Setting(list,"Reduced motion","Disable wind and growth animation",state.settings.reducedMotion,() => Try(() => habits.SetSettings(state.settings.sound,!state.settings.reducedMotion,state.settings.quality,true)));
            CardButton(list,"Graphics: "+state.settings.quality,() => Try(() => habits.SetSettings(state.settings.sound,state.settings.reducedMotion,state.settings.quality == "Low" ? "Medium" : "Low",true)),true);
            CardButton(list,"Manage habit reminders",() => { habitFilter = "All"; Navigate("Habits"); },true);
            var reminderNote = Card(list,75,cream); Label(reminderNote,"Enable a reminder inside a habit's edit screen. Permission is requested only when you choose a time; your phone may delay delivery.",8,4,Width-16,67,12,false,muted);
            Section(list,"Your garden stays with you","Habits and backups live locally. No account needed.");
            CardButton(list,"Export a local backup",BackupExport,true); CardButton(list,"Restore a local backup",BackupImport,true);
            CardButton(list,"How your garden grows",() => Navigate("Guide"),true);
            CardButton(list,"Ad privacy choices",() => ads.PrivacyChoices(),true);
            CardButton(list,"Delete all app data",() => Confirm("Start fresh?","This removes habits, plants and coins from this app. Exported backups stay with you.",() => Try(() => { habits.Reset(); saves.PurgeHistory(habits.Snapshot); tab = "Guide"; })),true);
        }
        void Stat(RectTransform parent,string value,string caption,float x,float width)
        {
            var a = Label(parent,value,x,18,width,40,29,true,green); a.alignment = TextAlignmentOptions.Center;
            var b = Label(parent,caption,x,65,width,25,11,false,muted); b.alignment = TextAlignmentOptions.Center;
        }
        void WeeklyChart(RectTransform list,AppState state)
        {
            var points = GardenPresentation.Week(state,DateTime.ParseExact(habits.Today,"yyyy-MM-dd",CultureInfo.InvariantCulture));
            var card = Card(list,222); Label(card,"Your last 7 days",16,15,Width-32,28,18,true);
            Label(card,points.Sum(p => p.completed)+" completed habit days this week",16,51,Width-32,24,12,false,muted);
            int max = Math.Max(1,points.Max(p => p.completed)); float slot = (Width-32)/7;
            for (int i = 0; i < points.Length; i++)
            {
                var point = points[i]; float height = 8+68f*point.completed/max;
                var bar = Panel(card,"Completed: "+point.completed,point.completed == 0 ? sage : i == 6 ? green : new Color(.61f,.74f,.51f)); Position(bar,16+i*slot+slot*.23f,172-height,slot*.54f,height);
                var value = Label(card,point.completed.ToString(),16+i*slot,82,slot,22,12,true); value.alignment = TextAlignmentOptions.Center;
                var day = Label(card,point.date.ToString("ddd",CultureInfo.InvariantCulture),16+i*slot,184,slot,22,10,false,muted); day.alignment = TextAlignmentOptions.Center;
            }
        }
        void Setting(RectTransform list,string title,string detail,bool enabled,Action action)
        {
            var card = Card(list,91); Label(card,title,16,13,Width-110,25,16,true); Label(card,detail,16,44,Width-110,36,11,false,muted);
            Button(card,enabled ? "On ✓" : "Off",Width-83,20,67,50,action,enabled ? sage : cream);
        }
        void PotPicker(Plant plant)
        {
            var state = habits.Snapshot; var modal = Modal("A new home for your plant","Choose a pot you own. Your growth stays the same.",392);
            string[] options = {"clay","leafy","rainbow"};
            for (int i = 0; i < options.Length; i++)
            {
                string pot = options[i]; bool owned = pot == "clay" || state.inventory.Contains(pot); float y = 136+i*62;
                Art(modal,pot,17,y,44,50);
                var b = Button(modal,(pot == "clay" ? "Terracotta" : pot == "leafy" ? "Leafy pot" : "Rainbow planter")+(plant.pot == pot ? " ✓" : owned ? "" : " · Shop"),76,y,Width-92,52,() => Try(() =>
                { habits.Equip(plant.id,pot); garden.Refresh(habits.Snapshot,true); CloseOverlay(); Render(); }),plant.pot == pot ? sage : Color.white); b.interactable = owned;
            }
            Button(modal,"Done",16,335,Width-32,48,CloseOverlay);
        }
        void GuideScreen()
        {
            var list = Scroll(); Heading(list,"Good things take a little care.","Welcome to your very own habit garden.");
            var hero = Card(list,172,sage); Art(hero,"tree",Width/2-105,8,88,150); Art(hero,"sunflower",Width/2-20,23,66,133); Art(hero,"succulent",Width/2+43,55,74,100);
            string[] titles = {"Plant a small intention","Show up, a little each day","Watch your garden become yours"};
            string[] details = {"Pick a habit, its days and a plant. Start small; you can always change it.","Complete a target to earn 10 coins and one growth day. Miss a day? Your plant waits.","Collect mature plants and use earned coins for lovely pots and decorations."};
            for (int i = 0; i < titles.Length; i++)
            {
                var card = Card(list,124); var n = Label(card,(i+1).ToString("00"),16,19,32,30,19,true,green);
                Label(card,titles[i],64,15,Width-80,43,16,true); Label(card,details[i],64,66,Width-80,50,12,false,muted);
            }
            CardButton(list,"Plant my first habit",() => Try(() => { var s = habits.Snapshot.settings; habits.SetSettings(s.sound,s.reducedMotion,s.quality,true); Form(null); }));
            if (habits.Snapshot.habits.Count == 0) CardButton(list,"Try 3 gentle example habits",() => Try(() =>
            {
                habits.Add("Drink water","sunflower",8); habits.Add("Read a book","tree",15); habits.Add("Take a walk","succulent",20);
                var s = habits.Snapshot.settings; habits.SetSettings(s.sound,s.reducedMotion,s.quality,true); habitFilter = "Today"; Navigate("Habits");
            }),true);
            else CardButton(list,"Back to my garden",() => Navigate("Garden"),true);
            Section(list,"Offline, at your pace","Your garden works without internet. Optional ads appear only in the shop when available.");
        }
        void Form(Habit existing)
        {
            Clear(); renderedTab = "Form"; garden.Show(false); var list = Scroll();
            Heading(list,existing == null ? "Plant a new habit" : "Care for your habit",existing == null ? "Small and repeatable is a lovely place to start." : "Name, target and day changes apply tomorrow.");
            var title = Field(list,"Habit name",existing != null && existing.changeDate != "" ? existing.pendingTitle : existing?.title ?? "",false,"e.g. Read a few pages");
            string category = existing?.category ?? "Personal";
            string[] categories = {"Personal","Health","Mindfulness","Learning","Movement"};
            var categoryCard = Card(list,106); Label(categoryCard,"Category",16,12,Width-32,23,13,true);
            var categoryButton = Button(categoryCard,category,16,44,Width-32,48,() => {},sage);
            categoryButton.onClick.AddListener(() => { category = categories[(Array.IndexOf(categories,category)+1)%categories.Length]; categoryButton.GetComponentInChildren<TMP_Text>().text = category; });
            var target = Field(list,"Daily target",(existing != null && existing.pendingTarget > 0 ? existing.pendingTarget : existing?.target ?? 1).ToString(),true,"1 for a simple yes / no habit");
            var explainer = Card(list,54,sage); Label(explainer,"Target 1 is a simple check-off. Higher targets add progress one step at a time.",12,8,Width-24,40,12,false,muted);
            int days = existing != null && existing.changeDate != "" ? existing.pendingWeekdays : existing?.weekdays ?? 127;
            var dayCard = Card(list,185); Label(dayCard,"When will you show up?",16,13,Width-32,26,16,true);
            string[] daysShort = {"Sun","Mon","Tue","Wed","Thu","Fri","Sat"};
            for (int i = 0; i < 7; i++)
            {
                int bit = 1 << i; string dayName = daysShort[i]; float w = (Width-50)/4; float x = 16+(i%4)*(w+6), y = 53+(i/4)*61;
                var b = Button(dayCard,daysShort[i]+((days&bit) != 0 ? " ✓" : ""),x,y,w,52,() => {},(days&bit) != 0 ? sage : cream);
                b.onClick.AddListener(() => { days ^= bit; b.GetComponent<Image>().color = (days&bit) != 0 ? sage : cream; b.GetComponentInChildren<TMP_Text>().text = dayName+((days&bit) != 0 ? " ✓" : ""); });
            }
            int species = Math.Max(0,Array.IndexOf(Catalog.Species,existing?.species ?? "sunflower"));
            if (existing == null)
            {
                var plantCard = Card(list,218); Label(plantCard,"Choose your little companion",16,14,Width-32,28,16,true);
                Button[] choices = new Button[3];
                for (int i = 0; i < 3; i++)
                {
                    int index = i; float w = (Width-44)/3; float x = 16+i*(w+6);
                    choices[i] = Button(plantCard,"",x,57,w,143,() => { species = index; for (int j = 0; j < 3; j++) choices[j].GetComponent<Image>().color = j == species ? sage : cream; },i == species ? sage : cream);
                    var rt = choices[i].GetComponent<RectTransform>(); Art(rt,Catalog.Species[i],(w-48)/2,10,48,70);
                    var caption = Label(rt,GardenPresentation.SpeciesName(Catalog.Species[i]),2,91,w-4,22,11,true); caption.alignment = TextAlignmentOptions.Center;
                    var maturity = Label(rt,Catalog.Maturity(Catalog.Species[i])+" growth days",2,116,w-4,20,10,false,muted); maturity.alignment = TextAlignmentOptions.Center;
                }
            }
            var time = Field(list,"Optional reminder",existing != null && existing.reminderMinutes >= 0 ? (existing.reminderMinutes/60).ToString("00")+":"+(existing.reminderMinutes%60).ToString("00") : "",false,"18:30 · leave blank for no reminder");
            CardButton(list,existing == null ? "Plant this habit" : "Save changes",() => Try(() =>
            {
                if (!Int32.TryParse(target.text,out int number)) throw new ArgumentException("Enter a whole number target.");
                if (days == 0) throw new ArgumentException("Choose at least one day.");
                int minutes = -1;
                if (!String.IsNullOrWhiteSpace(time.text))
                {
                    if (!DateTime.TryParseExact(time.text.Trim(),"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var parsed)) throw new ArgumentException("Use a reminder time such as 18:30.");
                    minutes = parsed.Hour*60+parsed.Minute;
                }
                if (existing == null) { string id = habits.Add(title.text,Catalog.Species[species],number,days,category); if (minutes >= 0) habits.Edit(id,title.text,number,days,minutes); }
                else habits.Edit(existing.id,title.text,number,days,minutes,category);
                if (minutes >= 0) StartCoroutine(PermissionThenRefresh()); habitFilter = "All"; tab = "Habits"; Render(); Message(existing == null ? "A new habit, a new beginning." : "Saved. Target and day changes begin tomorrow.");
            }));
            if (existing != null)
            {
                CardButton(list,existing.paused ? "Resume habit" : "Pause habit",() => Try(() => { habits.SetPaused(existing.id,!existing.paused); Navigate("Habits"); }),true);
                CardButton(list,"Archive habit",() => Confirm("Archive this habit?","Its history and plant will stay in your garden. It will leave the active habit list.",() => Try(() => { habits.Archive(existing.id); tab = "Habits"; })),true);
            }
            CardButton(list,"Cancel",() => Navigate("Habits"),true);
        }
    }
}
