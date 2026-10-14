using System;
using System.Collections;
using System.Linq;
using HabitGarden.Domain;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

namespace HabitGarden
{
    public sealed class ReminderService : MonoBehaviour
    {
        public void Initialize() {
#if UNITY_ANDROID
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel {
                Id="habit_reminders",Name="Habit reminders",Importance=Importance.Default,Description="Your chosen daily habit reminders"});
#endif
        }
        public IEnumerator RequestPermission() {
#if UNITY_ANDROID && !UNITY_EDITOR
            var request=new PermissionRequest();while(request.Status==PermissionStatus.RequestPending)yield return null;
#else
            yield return null;
#endif
        }
        public void Refresh(AppState state) {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            if(AndroidNotificationCenter.UserPermissionToPost!=PermissionStatus.Allowed)return;
            foreach(var h in state.habits.Where(x=>!x.paused&&!x.archived&&x.reminderMinutes>=0))for(int i=0;i<7;i++) {
                var day=DateTime.Now.Date.AddDays(i);
                if((h.weekdays&(1<<(int)day.DayOfWeek))==0)continue;
                var time=day.AddMinutes(h.reminderMinutes);if(time<=DateTime.Now)continue;
                string key=day.ToString("yyyy-MM-dd");if(state.days.Any(x=>x.habitId==h.id&&x.date==key&&x.complete))continue;
                AndroidNotificationCenter.SendNotification(new AndroidNotification {
                    Title="Habit Garden",Text="A small habit is ready to grow.",FireTime=time,IntentData=h.id
                },"habit_reminders");
            }
#endif
        }
    }
}
