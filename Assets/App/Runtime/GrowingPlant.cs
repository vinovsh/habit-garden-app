using System;
using System.Collections.Generic;
using HabitGarden.Domain;
using UnityEngine;

namespace HabitGarden
{
    public sealed class GrowingPlant : MonoBehaviour
    {
        sealed class Organ {
            public Transform t,parent;
            public Vector3 anchor,end,baseScale;
            public Quaternion rotation;
            public float birth,full;
            public bool leaf;
        }
        readonly List<Organ> organs=new List<Organ>();
        readonly List<Mesh> ownedMeshes=new List<Mesh>();
        Transform growthRoot;
        Mesh leafOpen,leafCurl;
        float shown,target,start,elapsed=3;
        int seed;
        bool reduced;
        public string Id {get;private set;}
        public void Init(Plant model,bool reducedMotion) {
            Id=model.id;seed=model.seed;reduced=reducedMotion;
            leafOpen=Own(PlantGeometry.Leaf(0));leafCurl=Own(PlantGeometry.Leaf(.8f));
            var openVertices=leafOpen.vertices;var curledVertices=leafCurl.vertices;var delta=new Vector3[openVertices.Length];
            for(int i=0;i<delta.Length;i++)delta[i]=curledVertices[i]-openVertices[i];
            leafOpen.AddBlendShapeFrame("Curl",100,delta,new Vector3[delta.Length],new Vector3[delta.Length]);
            var pot=MeshObject("Pot",transform,Own(PlantGeometry.Pot()),GardenMaterials.Get("pot:"+model.pot,model.pot=="rainbow"?new Color(.8f,.57f,.9f):model.pot=="leafy"?new Color(.52f,.72f,.42f):new Color(.86f,.48f,.29f)));
            var soil=GameObject.CreatePrimitive(PrimitiveType.Cylinder);soil.name="Soil";soil.transform.SetParent(transform,false);soil.transform.localPosition=new Vector3(0,.255f,0);soil.transform.localScale=new Vector3(.36f,.008f,.36f);Destroy(soil.GetComponent<Collider>());
            soil.GetComponent<Renderer>().sharedMaterial=GardenMaterials.Get("soil",new Color(.26f,.16f,.10f));
            growthRoot=new GameObject("Growth anchored to soil").transform;growthRoot.SetParent(transform,false);growthRoot.localPosition=Vector3.up*.26f;
            if(model.species=="tree")BuildTree();else if(model.species=="sunflower")BuildSunflower();else BuildSucculent();
            target=shown=model.growthUnits/(float)Catalog.Maturity(model.species);Pose(shown);
        }
        Mesh Own(Mesh m){ownedMeshes.Add(m);return m;}
        static Transform MeshObject(string name,Transform parent,Mesh mesh,Material material) {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go.transform;
        }
        Organ Stem(string name,Transform parent,Vector3 anchor,Vector3 end,float radius,float birth,float full,Color color) {
            Vector3 localEnd=Vector3.up*end.magnitude;
            var t=MeshObject(name,parent,Own(PlantGeometry.Stem(localEnd,radius)),GardenMaterials.Get(name.StartsWith("Trunk")?"bark":"stem",color));
            var o=new Organ {t=t,parent=parent,anchor=anchor,end=localEnd,birth=birth,full=full,baseScale=Vector3.one,rotation=Quaternion.FromToRotation(Vector3.up,end.normalized)};organs.Add(o);return o;
        }
        void Leaf(Transform parent,Vector3 anchor,float yaw,float tilt,float size,float birth,float full,Color color) {
            var go=new GameObject("Unfolding leaf",typeof(SkinnedMeshRenderer));var t=go.transform;t.SetParent(parent,false);
            var renderer=go.GetComponent<SkinnedMeshRenderer>();renderer.sharedMesh=leafOpen;renderer.sharedMaterial=GardenMaterials.Get("leaf:"+color,color);
            renderer.localBounds=new Bounds(new Vector3(0,.3f,.5f),new Vector3(1,2,2));
            var o=new Organ {t=t,parent=parent,anchor=anchor,birth=birth,full=full,baseScale=Vector3.one*size,rotation=Quaternion.Euler(tilt,yaw,0),leaf=true};organs.Add(o);
        }
        void BuildTree() {
            var brown=new Color(.46f,.29f,.17f);var green=new Color(.30f,.57f,.26f);
            var trunk=Stem("Trunk",growthRoot,Vector3.zero,new Vector3(.025f,1.18f,0),.052f,.01f,.56f,brown);
            var random=new System.Random(seed);
            for(int b=0;b<7;b++) {
                float yaw=b*137.5f+(float)random.NextDouble()*20f;float a=yaw*Mathf.Deg2Rad;
                float y=.32f+b*.105f;float len=.38f+(float)random.NextDouble()*.13f;
                var branch=Stem("Trunk branch",growthRoot,new Vector3(0,y,0),new Vector3(Mathf.Cos(a)*len,.27f,Mathf.Sin(a)*len),.024f,.24f+b*.04f,.68f+b*.035f,brown);
                // Parent organ evaluated first; attachments follow its growth without detached limbs.
                branch.parent=trunk.t;
                for(int k=0;k<5;k++) {
                    float t=.35f+k*.15f;
                    Leaf(branch.t,branch.end*t,yaw+(k%2==0?60:-60),-35,.22f,.46f+b*.03f+k*.012f,.82f+b*.02f+k*.01f,green);
                }
            }
            Leaf(trunk.t,new Vector3(0,1.18f,0),0,-25,.25f,.65f,.92f,green);
        }
        void BuildSunflower() {
            var stem=Stem("Stem",growthRoot,Vector3.zero,new Vector3(0,.86f,0),.023f,.015f,.70f,new Color(.27f,.52f,.19f));
            for(int i=0;i<6;i++)Leaf(stem.t,new Vector3(0,.14f+i*.10f,0),i*137.5f,-25,.28f,.10f+i*.07f,.32f+i*.07f,new Color(.34f,.62f,.25f));
            var head=new GameObject("Bloom").transform;head.SetParent(growthRoot,false);
            var headOrgan=new Organ {t=head,parent=stem.t,anchor=Vector3.up*.86f,birth=.65f,full=.94f,baseScale=Vector3.one,rotation=Quaternion.Euler(-20,0,0)};organs.Add(headOrgan);
            var centre=GameObject.CreatePrimitive(PrimitiveType.Sphere);centre.name="Flower centre";centre.transform.SetParent(head,false);centre.transform.localScale=new Vector3(.19f,.09f,.19f);Destroy(centre.GetComponent<Collider>());centre.GetComponent<Renderer>().sharedMaterial=GardenMaterials.Get("flower centre",new Color(.44f,.26f,.10f));
            for(int i=0;i<14;i++)Leaf(head,new Vector3(0,0,0),i*360f/14f,0,.29f,.76f,.99f,new Color(1f,.76f,.18f));
        }
        void BuildSucculent() {
            for(int ring=0;ring<3;ring++)for(int i=0;i<7;i++) {
                float birth=.015f+ring*.20f+i*.015f;
                Leaf(growthRoot,new Vector3(0,ring*.035f,0),i*360f/7f+ring*25f,-15-ring*12f,.30f-ring*.05f,birth,birth+.33f,new Color(.37f,.69f,.51f));
            }
        }
        public void SetTarget(Plant p,bool reducedMotion) {
            reduced=reducedMotion;float next=Mathf.Clamp01(p.growthUnits/(float)Catalog.Maturity(p.species));
            if(Mathf.Abs(next-target)>.00001f){start=shown;target=next;elapsed=0;}
            if(reduced){shown=target;elapsed=3;Pose(shown);}
        }
        void Update() {
            if(elapsed<3){elapsed+=Time.unscaledDeltaTime;shown=Mathf.Lerp(start,target,Mathf.SmoothStep(0,1,elapsed/3));Pose(shown);}
            if(growthRoot!=null)growthRoot.localRotation=reduced?Quaternion.identity:Quaternion.Euler(0,0,Mathf.Sin(Time.time*.7f+(seed%100)*.1f)*.8f);
        }
        void Pose(float progress) {
            foreach(var o in organs) {
                float g=Mathf.SmoothStep(0,1,Mathf.InverseLerp(o.birth,o.full,progress));
                o.t.gameObject.SetActive(g>.0001f);if(g<=.0001f)continue;
                // Anchor transformed by the parent's current local growth scale.
                Vector3 world=o.parent.TransformPoint(o.anchor);o.t.position=world;
                o.t.localRotation=o.rotation*(o.leaf?Quaternion.Euler(-55*(1-g),0,0):Quaternion.identity);
                o.t.localScale=o.leaf?o.baseScale*Mathf.Lerp(.02f,1,g):new Vector3(Mathf.Lerp(.15f,1,g),g,Mathf.Lerp(.15f,1,g));
                if(o.leaf)o.t.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(0,(1-g)*100);
            }
        }
        void OnDestroy(){foreach(var m in ownedMeshes)if(m!=null)Destroy(m);}
    }
}
