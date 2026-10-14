using System;
using System.Collections.Generic;
using System.Linq;
using HabitGarden.Domain;
using UnityEngine;

namespace HabitGarden
{
    public sealed class GardenView : MonoBehaviour
    {
        readonly Dictionary<string,GrowingPlant> plants=new Dictionary<string,GrowingPlant>();
        Transform world;
        Camera cameraView;
        Transform decorations;
        string decorationKey="";
        int plot;
        public int Plot => plot;
        RenderTexture texture;
        public RenderTexture Texture => texture;
        public event Action<string> Selected;
        public void Pick(Vector2 position)
        {
            if (!cameraView.enabled) return;
            var ray = cameraView.ViewportPointToRay(new Vector3(position.x,position.y,0));
            if (Physics.Raycast(ray,out var hit,20))
            {
                var target = hit.collider.GetComponent<GardenPlantTarget>();
                if (target != null) Selected?.Invoke(target.plantId);
            }
        }
        public void Init() {
            world=new GameObject("Garden diorama").transform;world.SetParent(transform,false);
            cameraView=new GameObject("Garden camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();cameraView.transform.SetParent(transform,false);
            texture=new RenderTexture(768,466,16);texture.name="Garden card render";texture.Create();cameraView.targetTexture=texture;
            cameraView.tag="MainCamera";cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=new Color(.98f,.96f,.90f);
            cameraView.orthographic=true;cameraView.orthographicSize=2.1f;
            cameraView.transform.position=new Vector3(3,4,-4);cameraView.transform.LookAt(new Vector3(0,.45f,0));
            var light=new GameObject("Sunlight",typeof(Light)).GetComponent<Light>();light.transform.SetParent(transform,false);light.type=LightType.Directional;light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(40,-30,0);
            RenderSettings.ambientLight=new Color(.75f,.79f,.68f);
            Shape(PrimitiveType.Cube,"Garden bed",new Vector3(0,-.12f,0),new Vector3(2.9f,.24f,2.3f),new Color(.56f,.72f,.39f));
            for(int i=0;i<9;i++)Shape(PrimitiveType.Sphere,"Edge stone",new Vector3(-1.4f+i*.35f,-.03f,-1.2f),new Vector3(.32f,.22f,.26f),new Color(.78f,.73f,.60f));
        }
        void Shape(PrimitiveType type,string name,Vector3 pos,Vector3 size,Color c) {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(world,false);go.transform.localPosition=pos;go.transform.localScale=size;Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=GardenMaterials.Get(name,c);
        }
        public void Show(bool enabled){world.gameObject.SetActive(enabled);cameraView.enabled=enabled;cameraView.rect=new Rect(0,0,1,1);}
        public void SetPlot(int index,AppState state){plot=Mathf.Max(0,index);Refresh(state,true);}
        public void Refresh(AppState state,bool rebuild=false) {
            string key=string.Join(",",state.inventory);
            if(key!=decorationKey){decorationKey=key;RebuildDecorations(state);}
            var shown=state.plants.Where(x=>x.plot==plot).ToList();
            foreach(var id in plants.Keys.ToList())if(rebuild || !shown.Any(x=>x.id==id)){Destroy(plants[id].gameObject);plants.Remove(id);}
            foreach(var model in shown) {
                if(!plants.TryGetValue(model.id,out var view)) {
                    var go=new GameObject("Plant "+model.species);go.transform.SetParent(world,false);go.transform.localPosition=new Vector3(-.98f+(model.slot%4)*.65f,0,-.65f+(model.slot/4)*.65f);
                    go.transform.localScale=Vector3.one*.70f;
                    var target=go.AddComponent<GardenPlantTarget>();target.plantId=model.id;
                    var collider=go.AddComponent<SphereCollider>();collider.center=new Vector3(0,.40f,0);collider.radius=.43f;
                    view=go.AddComponent<GrowingPlant>();view.Init(model,state.settings.reducedMotion);plants[model.id]=view;
                }else view.SetTarget(model,state.settings.reducedMotion);
            }
            Application.targetFrameRate=state.settings.quality=="Medium"?60:30;
        }
        void RebuildDecorations(AppState state) {
            if(decorations!=null)Destroy(decorations.gameObject);
            decorations=new GameObject("Owned decorations").transform;decorations.SetParent(world,false);
            if(state.inventory.Contains("fence"))for(int i=0;i<10;i++) {
                var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name="Fence picket";post.transform.SetParent(decorations,false);post.transform.localPosition=new Vector3(-1.35f+i*.3f,.2f,1.17f);post.transform.localScale=new Vector3(.12f,.4f,.08f);Destroy(post.GetComponent<Collider>());post.GetComponent<Renderer>().sharedMaterial=GardenMaterials.Get("fence",new Color(.98f,.96f,.86f));
            }
            if(state.inventory.Contains("butterfly"))for(int i=0;i<2;i++) {
                var wing=GameObject.CreatePrimitive(PrimitiveType.Sphere);wing.name="Butterfly wing";wing.transform.SetParent(decorations,false);wing.transform.localPosition=new Vector3(1.1f+(i==0?-.09f:.09f),.6f,-.8f);wing.transform.localScale=new Vector3(.18f,.25f,.04f);wing.transform.localRotation=Quaternion.Euler(0,0,i==0?-25:25);Destroy(wing.GetComponent<Collider>());wing.GetComponent<Renderer>().sharedMaterial=GardenMaterials.Get("butterfly",new Color(.76f,.56f,.90f));
            }
        }
        void OnDestroy(){if(texture!=null){cameraView.targetTexture=null;texture.Release();Destroy(texture);}GardenMaterials.Clear();}
    }
}

