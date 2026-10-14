using System;
using System.Collections.Generic;
using UnityEngine;

namespace HabitGarden
{
    // Small reusable original meshes. Growth changes organs, never the whole plant's scale.
    public static class PlantGeometry
    {
        public static Mesh Stem(Vector3 end,float radius) {
            const int rings=8,sides=8;
            var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            Vector3 axis=end.normalized; if(axis.sqrMagnitude<.01f)axis=Vector3.up;
            Vector3 right=Vector3.Cross(axis,Mathf.Abs(axis.y)>.95f?Vector3.forward:Vector3.up).normalized;
            Vector3 forward=Vector3.Cross(right,axis).normalized;
            for(int j=0;j<=rings;j++) {
                float t=j/(float)rings;
                Vector3 centre=end*t + right*(Mathf.Sin(t*Mathf.PI)*end.magnitude*.05f);
                float r=radius*Mathf.Lerp(1,.35f,t);
                for(int k=0;k<sides;k++) {float a=2*Mathf.PI*k/sides;v.Add(centre+r*(right*Mathf.Cos(a)+forward*Mathf.Sin(a)));uv.Add(new Vector2(k/(float)sides,t));}
            }
            for(int j=0;j<rings;j++)for(int k=0;k<sides;k++) {
                int a=j*sides+k,b=j*sides+(k+1)%sides,c=a+sides,d=b+sides;
                tris.AddRange(new[]{a,c,b,b,c,d});
            }
            // Both tips are capped to avoid hollow stems during growth.
            int bottom=v.Count;v.Add(Vector3.zero);uv.Add(Vector2.zero);
            int top=v.Count;v.Add(end);uv.Add(Vector2.one);
            for(int k=0;k<sides;k++) {int n=(k+1)%sides;tris.AddRange(new[]{bottom,k,n,top,rings*sides+n,rings*sides+k});}
            var mesh=new Mesh {name="Generated capped stem"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();return mesh;
        }
        public static Mesh Leaf(float curl) {
            const int segments=10;var v=new List<Vector3>();var tris=new List<int>();
            for(int i=0;i<=segments;i++) {
                float t=i/(float)segments,w=Mathf.Sin(t*Mathf.PI)*.32f;
                float y=Mathf.Sin(t*Mathf.PI)*.10f + curl*t*t*.6f;
                v.Add(new Vector3(-w,y,t));v.Add(new Vector3(0,y+.03f,t));v.Add(new Vector3(w,y,t));
            }
            for(int i=0;i<segments;i++)for(int j=0;j<2;j++) {
                int a=i*3+j,b=a+1,c=a+3,d=b+3;
                tris.AddRange(new[]{a,c,b,b,c,d});
            }
            // Separate back vertices prevent front and back normals from cancelling.
            int count=v.Count;var front=tris.ToArray();v.AddRange(v.ToArray());
            for(int i=0;i<front.Length;i+=3)tris.AddRange(new[]{front[i+2]+count,front[i+1]+count,front[i]+count});
            var mesh=new Mesh {name="Generated leaf"};mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();return mesh;
        }
        public static Mesh Pot() {
            const int sides=24;var v=new List<Vector3>();var tris=new List<int>();
            float[] heights={0,.24f,.28f,.28f,.06f};float[] radii={.15f,.21f,.23f,.18f,.12f};
            for(int j=0;j<heights.Length;j++)for(int k=0;k<sides;k++){float a=k*2*Mathf.PI/sides;v.Add(new Vector3(Mathf.Cos(a)*radii[j],heights[j],Mathf.Sin(a)*radii[j]));}
            for(int j=0;j<heights.Length-1;j++)for(int k=0;k<sides;k++){int a=j*sides+k,b=j*sides+(k+1)%sides;tris.AddRange(new[]{a,a+sides,b,b,a+sides,b+sides});}
            var m=new Mesh {name="Generated terracotta pot"};m.SetVertices(v);m.SetTriangles(tris,0);m.RecalculateNormals();return m;
        }
    }
    public static class GardenMaterials
    {
        static readonly Dictionary<string,Material> cache=new Dictionary<string,Material>();
        public static Material Get(string key,Color colour) {
            if(cache.TryGetValue(key,out var material))return material;
            var shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
            material=new Material(shader){name=key};material.color=colour;
            if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.28f);
            cache[key]=material;return material;
        }
        public static void Clear(){foreach(var m in cache.Values)UnityEngine.Object.Destroy(m);cache.Clear();}
    }
}
