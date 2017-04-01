using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;

namespace Demo
{
    public class AppListener : WakefulIntentService.WakefulIntentService.IAlarmListener
    {
        public void ScheduleAlarms(AlarmManager alarmManager, PendingIntent pendingIntent, Context context)
        {
            //alarmManager.SetInexactRepeating(AlarmType.ElapsedRealtimeWakeup, SystemClock.ElapsedRealtime() + 6000, AlarmManager.IntervalFifteenMinutes, pendingIntent);
            var dt = new DateTime()
                     .AddMinutes(1);
                 
            var cal = Calendar.GetInstance(Locale.English);
            cal.TimeInMillis = Java.Lang.JavaSystem.CurrentTimeMillis();
            cal.Set(CalendarField.Hour, dt.Hour);
            cal.Set(CalendarField.Minute, dt.Minute);
            cal.Set(CalendarField.Second, dt.Second);

            alarmManager.SetRepeating(AlarmType.RtcWakeup, cal.TimeInMillis,
                                1000 * 60 , pendingIntent);
        }

        public void SendWakefulWork(Context context)
        {
            WakefulIntentService.WakefulIntentService.SendWakefulWork(context, typeof(AppService));
        }

        public long GetMaxAge()
        {
            return (AlarmManager.IntervalFifteenMinutes*2);
        }
    }
}