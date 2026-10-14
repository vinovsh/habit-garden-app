using UnityEngine;
using UnityEngine.UI;

namespace HabitGarden
{
    // Original small vector illustrations. Geometry is generated only when uGUI rebuilds a graphic.
    public sealed class GardenArt : MaskableGraphic
    {
        public string kind = "leaf";
        VertexHelper mesh;
        Color stem = new Color(.30f,.47f,.30f), leaf = new Color(.52f,.66f,.40f), clay = new Color(.79f,.46f,.32f);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            mesh = vh; mesh.Clear();
            switch (kind)
            {
                case "sunflower": Plant(false); break;
                case "tree": Plant(true); break;
                case "succulent": Succulent(); break;
                case "clay": Pot(clay); break;
                case "leafy": Pot(new Color(.49f,.63f,.42f)); Leaf(.48f,.37f,.14f,.18f,Color.white,-30); break;
                case "rainbow": Pot(new Color(.86f,.68f,.79f)); Ellipse(.5f,.38f,.23f,.045f,new Color(1,.78f,.43f)); Ellipse(.5f,.48f,.25f,.04f,new Color(.61f,.75f,.88f)); break;
                case "fence": Fence(); break;
                case "butterfly": Butterfly(); break;
                case "coin": Ellipse(.5f,.5f,.45f,.45f,color); Ellipse(.5f,.5f,.33f,.33f,new Color(1,.85f,.50f)); Line(.5f,.27f,.5f,.73f,.08f,color); break;
                case "check": Line(.18f,.51f,.41f,.27f,.09f,color); Line(.41f,.27f,.85f,.78f,.09f,color); break;
                case "profile": Ellipse(.5f,.71f,.19f,.20f,color); Ellipse(.5f,.22f,.36f,.23f,color); break;
                case "shop": Box(.15f,.16f,.85f,.69f,color); Box(.25f,.24f,.75f,.57f,new Color(.98f,.97f,.93f)); Line(.33f,.65f,.36f,.87f,.065f,color); Line(.36f,.87f,.64f,.87f,.065f,color); Line(.64f,.87f,.67f,.65f,.065f,color); break;
                default: Leaf(.49f,.53f,.32f,.40f,color,-25); Line(.30f,.17f,.68f,.79f,.04f,new Color(.91f,.95f,.86f)); break;
            }
        }
        void Plant(bool tree)
        {
            Ellipse(.5f,.09f,.38f,.06f,new Color(.24f,.34f,.22f,.10f)); Pot(clay);
            Line(.50f,.34f,.50f,.82f,.035f,stem);
            if (tree)
            {
                Line(.5f,.60f,.31f,.75f,.03f,stem); Line(.5f,.61f,.70f,.80f,.03f,stem);
                Ellipse(.50f,.79f,.26f,.19f,leaf); Ellipse(.28f,.75f,.18f,.15f,leaf); Ellipse(.71f,.78f,.18f,.16f,stem);
                Ellipse(.47f,.88f,.18f,.11f,new Color(.65f,.76f,.48f));
                Ellipse(.28f,.77f,.03f,.025f,new Color(.98f,.76f,.72f)); Ellipse(.62f,.85f,.03f,.025f,new Color(.98f,.76f,.72f));
            }
            else
            {
                Leaf(.35f,.60f,.17f,.06f,leaf,-30); Leaf(.64f,.49f,.17f,.06f,stem,30);
                for (int i = 0; i < 10; i++) { float a = i*Mathf.PI/5; Ellipse(.5f+Mathf.Cos(a)*.13f,.83f+Mathf.Sin(a)*.09f,.085f,.07f,new Color(.96f,.73f,.29f)); }
                Ellipse(.5f,.83f,.08f,.065f,new Color(.49f,.32f,.21f));
            }
        }
        void Succulent()
        {
            Ellipse(.5f,.09f,.38f,.06f,new Color(.24f,.34f,.22f,.10f)); Pot(new Color(.68f,.62f,.79f));
            for (int i = 0; i < 7; i++) { float a = i*Mathf.PI*2/7; Leaf(.5f+Mathf.Cos(a)*.15f,.52f+Mathf.Sin(a)*.13f,.12f,.22f,i%2 == 0 ? leaf : stem,-i*51); }
            Leaf(.50f,.65f,.075f,.19f,new Color(.65f,.77f,.52f),0);
        }
        void Pot(Color tint)
        {
            Quad(new Vector2(.28f,.37f),new Vector2(.72f,.37f),new Vector2(.66f,.12f),new Vector2(.34f,.12f),tint);
            Ellipse(.5f,.39f,.25f,.055f,tint); Ellipse(.5f,.40f,.20f,.025f,new Color(.38f,.29f,.22f));
        }
        void Fence()
        {
            Box(.10f,.32f,.90f,.40f,new Color(.82f,.78f,.65f)); Box(.10f,.57f,.90f,.65f,new Color(.82f,.78f,.65f));
            for (int i = 0; i < 4; i++) { float x = .13f+i*.21f; Box(x,.19f,x+.12f,.76f,new Color(.99f,.97f,.89f)); Triangle(new Vector2(x,.76f),new Vector2(x+.06f,.84f),new Vector2(x+.12f,.76f),new Color(.99f,.97f,.89f)); }
        }
        void Butterfly()
        {
            Ellipse(.30f,.63f,.23f,.22f,new Color(.75f,.61f,.87f)); Ellipse(.70f,.63f,.23f,.22f,new Color(.75f,.61f,.87f));
            Ellipse(.34f,.34f,.18f,.16f,new Color(.89f,.75f,.88f)); Ellipse(.66f,.34f,.18f,.16f,new Color(.89f,.75f,.88f));
            Ellipse(.5f,.49f,.035f,.25f,stem); Line(.5f,.73f,.39f,.87f,.018f,stem); Line(.5f,.73f,.61f,.87f,.018f,stem);
        }
        void Leaf(float x,float y,float rx,float ry,Color tint,float angle)
        {
            const int count = 16; var center = new Vector2(x,y); float radians = angle*Mathf.Deg2Rad;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = LeafPoint(i*Mathf.PI*2/count,rx,ry,radians), b = LeafPoint((i+1)*Mathf.PI*2/count,rx,ry,radians);
                Triangle(center,center+a,center+b,tint);
            }
        }
        static Vector2 LeafPoint(float angle,float rx,float ry,float rotation)
        {
            var p = new Vector2(Mathf.Cos(angle)*rx*Mathf.Abs(Mathf.Sin(angle)),Mathf.Sin(angle)*ry);
            return new Vector2(p.x*Mathf.Cos(rotation)-p.y*Mathf.Sin(rotation),p.x*Mathf.Sin(rotation)+p.y*Mathf.Cos(rotation));
        }
        void Ellipse(float x,float y,float rx,float ry,Color tint)
        {
            for (int i = 0; i < 24; i++) { float a = i*Mathf.PI/12,b = (i+1)*Mathf.PI/12; Triangle(new Vector2(x,y),new Vector2(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry),new Vector2(x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry),tint); }
        }
        void Box(float x,float y,float right,float top,Color tint) { Quad(new Vector2(x,y),new Vector2(right,y),new Vector2(right,top),new Vector2(x,top),tint); }
        void Line(float x,float y,float right,float top,float thickness,Color tint)
        {
            var a = new Vector2(x,y); var b = new Vector2(right,top); var dir = (b-a).normalized; var offset = new Vector2(-dir.y,dir.x)*thickness*.5f;
            Quad(a-offset,b-offset,b+offset,a+offset,tint);
        }
        void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint) { Triangle(a,b,c,tint); Triangle(a,c,d,tint); }
        void Triangle(Vector2 a,Vector2 b,Vector2 c,Color tint)
        {
            var rect = rectTransform.rect; int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.x+a.x*rect.width,rect.y+a.y*rect.height,0),tint,Vector2.zero);
            mesh.AddVert(new Vector3(rect.x+b.x*rect.width,rect.y+b.y*rect.height,0),tint,Vector2.zero);
            mesh.AddVert(new Vector3(rect.x+c.x*rect.width,rect.y+c.y*rect.height,0),tint,Vector2.zero); mesh.AddTriangle(start,start+1,start+2);
        }
    }
}
