using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HabitGarden
{
    // Layout and controls use a 390-point portrait canvas; primary controls are 48+ points tall.
    public sealed partial class HabitGardenApp
    {
        void BuildShell()
        {
            var canvas = new GameObject("Interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390,844); scaler.matchWidthOrHeight = 0;
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("Event system",typeof(EventSystem),typeof(StandaloneInputModule));
            var background = Panel(canvas.transform,"Cream background",cream); Stretch(background);
            safe = new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>(); safe.SetParent(canvas.transform,false); ApplySafeArea();
            var header = Panel(safe,"Header",cream); Anchor(header,0,1,1,1,new Vector2(0,-76),Vector2.zero);
            Art(header,"leaf",16,20,30,36,green); Label(header,"habit garden",54,18,210,28,23,true);
            Label(header,"A little every day.",55,45,220,20,11,false,muted);
            var wallet = Panel(header,"Coin balance",peach); Position(wallet,292,22,82,40); wallet.anchorMin = wallet.anchorMax = new Vector2(1,1); wallet.pivot = new Vector2(1,1); wallet.anchoredPosition = new Vector2(-16,-22);
            Art(wallet,"coin",10,11,18,18,new Color(.63f,.40f,.12f)); balance = Label(wallet,"0",34,8,42,24,16,true); balance.alignment = TextAlignmentOptions.MidlineRight;
            body = new GameObject("Screen content",typeof(RectTransform)).GetComponent<RectTransform>(); body.SetParent(safe,false);
            Anchor(body,0,0,1,1,new Vector2(16,86),new Vector2(-16,-80));
            var nav = Panel(safe,"Navigation",cream); Anchor(nav,0,0,1,0,Vector2.zero,new Vector2(0,80));
            var divider = Panel(nav,"Divider",sage); Anchor(divider,0,1,1,1,new Vector2(16,-1),new Vector2(-16,0));
            string[] names = {"Garden","Habits","Shop","Profile"}; string[] icons = {"leaf","check","shop","profile"};
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i]; var b = Button(nav,"",0,0,86,64,() => Navigate(name),cream);
                var rt = b.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2((i+.5f)/4f,.5f); rt.pivot = new Vector2(.5f,.5f); rt.anchoredPosition = Vector2.zero;
                Art(rt,icons[i],31,7,24,24,muted); var label = Label(rt,name,0,37,86,20,11,true,muted); label.alignment = TextAlignmentOptions.Center;
                navigation[name] = b;
            }
            var toast = Panel(safe,"Status message",green); Anchor(toast,0,0,1,0,new Vector2(16,84),new Vector2(-16,156));
            status = Label(toast,"",14,10,330,52,13,false,Color.white); Stretch(status.rectTransform,14,8,14,8); toast.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
        }
        void ApplySafeArea()
        {
            if (safe == null || Screen.width == 0 || Screen.height == 0) return;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);
            safe.anchorMax = new Vector2(area.xMax/Screen.width,area.yMax/Screen.height); safe.offsetMin = safe.offsetMax = Vector2.zero;
        }
        RectTransform Scroll(Transform parent = null)
        {
            var root = Panel(parent ?? body,"Scroll",cream); Stretch(root);
            var viewport = Panel(root,"Viewport",cream); Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(viewport,false); content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f,1); content.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(0,0,8,24); layout.spacing = 12;
            layout.childControlWidth = true; layout.childForceExpandWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = root.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }
        RectTransform Card(RectTransform parent,float height,Color? color = null)
        {
            var card = Panel(parent,"Card",color ?? Color.white); var layout = card.gameObject.AddComponent<LayoutElement>(); layout.minHeight = layout.preferredHeight = height; return card;
        }
        void Heading(RectTransform list,string title,string subtitle)
        {
            var card = Card(list,125,cream); Label(card,title,2,0,Width-4,70,28,true); Label(card,subtitle,2,77,Width-4,44,13,false,muted);
        }
        void Section(RectTransform list,string title,string detail = "")
        {
            var card = Card(list,detail == "" ? 28 : 57,cream); Label(card,title,2,0,Width-4,26,17,true);
            if (detail != "") Label(card,detail,2,29,Width-4,28,12,false,muted);
        }
        void CardButton(RectTransform list,string label,Action action,bool subtle = false)
        {
            var card = Card(list,50,cream); Button(card,label,0,0,Width,50,action,subtle ? sage : green);
        }
        TMP_InputField Field(RectTransform list,string caption,string value,bool numeric = false,string placeholder = "")
        {
            var card = Card(list,100,cream); Label(card,caption,0,0,Width,24,13,true);
            var area = Panel(card,caption,Color.white); Position(area,0,30,Width,60);
            var viewport = new GameObject("Text area",typeof(RectTransform),typeof(RectMask2D)).GetComponent<RectTransform>(); viewport.SetParent(area,false); Stretch(viewport,16,10,16,10);
            var text = Label(viewport,"",0,0,Width-32,40,16); Stretch(text.rectTransform); text.alignment = TextAlignmentOptions.MidlineLeft;
            var hint = Label(viewport,placeholder,0,0,Width-32,40,14,false,muted); Stretch(hint.rectTransform); hint.alignment = TextAlignmentOptions.MidlineLeft;
            var input = area.gameObject.AddComponent<TMP_InputField>(); input.textViewport = viewport; input.textComponent = text; input.placeholder = hint;
            input.targetGraphic = area.GetComponent<Image>(); input.contentType = numeric ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine; input.characterLimit = caption == "Habit name" ? 60 : 10; input.text = value; return input;
        }
        void ProgressBar(Transform parent,float value,float x,float y,float width,Color? fill = null)
        {
            var track = Panel(parent,"Progress track",sage); Position(track,x,y,width,8);
            if (value <= 0) return;
            var bar = Panel(track,"Progress",fill ?? green); Position(bar,0,0,Mathf.Max(8,width*Mathf.Clamp01(value)),8); bar.GetComponent<Image>().raycastTarget = false;
        }
        void Confirm(string title,string detail,Action yes)
        {
            var modal = Modal(title,detail,224);
            Button(modal,"Cancel",16,150,(Width-44)/2,52,CloseOverlay,sage);
            Button(modal,"Confirm",Width/2+6,150,(Width-44)/2,52,() => { Try(yes); CloseOverlay(); if (habits != null) Render(); });
        }
        RectTransform Modal(string title,string detail,float height)
        {
            CloseOverlay(); overlay = Panel(safe,"Dialog backdrop",new Color(.10f,.18f,.12f,.58f)); Stretch(overlay);
            var card = Panel(overlay,title,cream); card.anchorMin = card.anchorMax = new Vector2(.5f,.5f); card.pivot = new Vector2(.5f,.5f); card.sizeDelta = new Vector2(Width,height);
            Label(card,title,16,18,Width-32,36,22,true); Label(card,detail,16,63,Width-32,75,14,false,muted); return card;
        }
        void CloseOverlay() { if (overlay != null) { overlay.gameObject.SetActive(false); Destroy(overlay.gameObject); overlay = null; } }
        RectTransform Panel(Transform parent,string name,Color color)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            var image = go.GetComponent<Image>(); image.color = color; image.sprite = round; image.type = Image.Type.Sliced; return go.GetComponent<RectTransform>();
        }
        TMP_Text Label(Transform parent,string text,float x,float y,float width,float height,int size,bool bold = false,Color? tint = null)
        {
            var go = new GameObject(text == "" ? "Label" : text,typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
            Position(go.GetComponent<RectTransform>(),x,y,width,height); var label = go.GetComponent<TextMeshProUGUI>(); label.font = font; label.text = text;
            label.fontSize = size; label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal; label.color = tint ?? ink; label.richText = false;
            label.raycastTarget = false; label.overflowMode = TextOverflowModes.Ellipsis; label.margin = Vector4.zero; return label;
        }
        Button Button(Transform parent,string text,float x,float y,float width,float height,Action action,Color? tint = null)
        {
            var rt = Panel(parent,text == "" ? "Navigation button" : text,tint ?? green); Position(rt,x,y,width,height);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = rt.GetComponent<Image>(); b.onClick.AddListener(() => action());
            var label = Label(rt,text,12,0,width-24,height,14,true,tint.HasValue ? ink : Color.white); label.alignment = TextAlignmentOptions.Center;
            Stretch(label.rectTransform,12,4,12,4); var colors = b.colors; colors.disabledColor = new Color(.82f,.84f,.80f); b.colors = colors; return b;
        }
        GardenArt Art(Transform parent,string kind,float x,float y,float width,float height,Color? tint = null)
        {
            var go = new GameObject(kind+" illustration",typeof(RectTransform),typeof(GardenArt)); go.transform.SetParent(parent,false); Position(go.GetComponent<RectTransform>(),x,y,width,height);
            var art = go.GetComponent<GardenArt>(); art.kind = kind; art.color = tint ?? green; art.raycastTarget = false; return art;
        }
        static void Position(RectTransform rt,float x,float y,float width,float height)
        { rt.anchorMin = rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0,1); rt.anchoredPosition = new Vector2(x,-y); rt.sizeDelta = new Vector2(width,height); }
        static void Anchor(RectTransform rt,float xmin,float ymin,float xmax,float ymax,Vector2 min,Vector2 max)
        { rt.anchorMin = new Vector2(xmin,ymin); rt.anchorMax = new Vector2(xmax,ymax); rt.offsetMin = min; rt.offsetMax = max; }
        static void Stretch(RectTransform rt,float left = 0,float top = 0,float right = 0,float bottom = 0)
        { Anchor(rt,0,0,1,1,new Vector2(left,bottom),new Vector2(-right,-top)); }
        static Sprite RoundedSprite()
        {
            var texture = new Texture2D(48,48,TextureFormat.RGBA32,false);
            for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
            { float dx = Mathf.Max(0,Mathf.Abs(x-23.5f)-11.5f), dy = Mathf.Max(0,Mathf.Abs(y-23.5f)-11.5f); texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(12.5f-Mathf.Sqrt(dx*dx+dy*dy)))); }
            texture.Apply(); return Sprite.Create(texture,new Rect(0,0,48,48),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
        }
    }
}
