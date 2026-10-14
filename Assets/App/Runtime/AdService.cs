using System;
using System.Collections.Concurrent;
using HabitGarden.Domain;
using UnityEngine;
#if HABIT_GARDEN_ADMOB
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace HabitGarden
{
    public sealed class AdService : MonoBehaviour
    {
        readonly ConcurrentQueue<Action> callbacks=new ConcurrentQueue<Action>();
        public event Action ReadyChanged;
        public event Action<string> Error;
        HabitService habits;
        string activeSession;
        bool showing;
        public bool Ready {get;private set;}
#if HABIT_GARDEN_ADMOB
        RewardedAd ad;
        const string TestUnit="ca-app-pub-3940256099942544/5224354917";
#endif
        public void Initialize(HabitService service) {
            habits=service;
#if HABIT_GARDEN_ADMOB
            ConsentInformation.Update(new ConsentRequestParameters(), error=>callbacks.Enqueue(()=>{
                if(error!=null){Error?.Invoke("Ads are unavailable right now.");return;}
                ConsentForm.LoadAndShowConsentFormIfRequired(formError=>callbacks.Enqueue(()=>{
                    if(ConsentInformation.CanRequestAds())MobileAds.Initialize(status=>callbacks.Enqueue(Load));
                }));
            }));
#endif
        }
#if HABIT_GARDEN_ADMOB
        void Load() {
            if(showing)return;ad?.Destroy();ad=null;Ready=false;
            RewardedAd.Load(TestUnit,new AdRequest(),(loaded,error)=>callbacks.Enqueue(()=>{
                if(error!=null||loaded==null){ReadyChanged?.Invoke();return;}
                ad=loaded;Ready=ad.CanShowAd();ReadyChanged?.Invoke();
                ad.OnAdFullScreenContentClosed+=()=>callbacks.Enqueue(()=>{showing=false;AudioListener.pause=false;Invoke(nameof(Load),1);});
                ad.OnAdFullScreenContentFailed+=e=>callbacks.Enqueue(()=>{showing=false;AudioListener.pause=false;Ready=false;ReadyChanged?.Invoke();Error?.Invoke("The ad could not open.");});
            }));
        }
#endif
        public void ShowReward() {
            if(!Ready||showing)return;
            activeSession=habits.BeginAd();string session=activeSession;Ready=false;showing=true;AudioListener.pause=true;ReadyChanged?.Invoke();
#if HABIT_GARDEN_ADMOB
            ad.Show(reward=>callbacks.Enqueue(()=>{
                try{habits.EarnAd(session);}catch(Exception){Error?.Invoke("Could not save the reward. Please check device storage.");}
            }));
#endif
        }
        public void PrivacyChoices() {
#if HABIT_GARDEN_ADMOB
            ConsentForm.ShowPrivacyOptionsForm(error=>callbacks.Enqueue(()=>{
                Ready=false;ad?.Destroy();ad=null;ReadyChanged?.Invoke();if(ConsentInformation.CanRequestAds())Load();
            }));
#endif
        }
        void Update(){while(callbacks.TryDequeue(out var action))action();}
#if HABIT_GARDEN_ADMOB
        void OnDestroy(){ad?.Destroy();}
#endif
    }
}
