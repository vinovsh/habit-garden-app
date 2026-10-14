using System;
using UnityEngine;

namespace HabitGarden
{
    public sealed class AppAudio : MonoBehaviour
    {
        AudioSource source;AudioClip completion;
        void Awake(){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;completion=Generate();}
        static AudioClip Generate() {
            const int hz=22050;var samples=new float[hz/2];
            for(int i=0;i<samples.Length;i++){float t=i/(float)hz;float fade=Mathf.Exp(-t*9);samples[i]=.14f*fade*(Mathf.Sin(2*Mathf.PI*523.25f*t)+.35f*Mathf.Sin(2*Mathf.PI*783.99f*t));}
            var clip=AudioClip.Create("Original gentle completion",samples.Length,1,hz,false);clip.SetData(samples,0);return clip;
        }
        public void Complete(bool enabled){if(enabled)source.PlayOneShot(completion);}
        void OnApplicationPause(bool paused){if(paused)source.Stop();}
        void OnDestroy(){if(completion!=null)Destroy(completion);}
    }
}
