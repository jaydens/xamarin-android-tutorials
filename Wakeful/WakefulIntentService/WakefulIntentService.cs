using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Util;
using Java.Lang;
using Java.Security;
using Exception = System.Exception;

namespace WakefulIntentService
{
    abstract public class WakefulIntentService : IntentService
    {
        abstract protected void DoWakefulWork(Intent intent);
        public static string NAME = "com.intellidrive.wakeful.WakefulIntentService";
        public static string LAST_ALARM = "lastAlarm";
        public static string TAG = "WakefulIntentService";
        private static volatile PowerManager.WakeLock lockStatic = null;

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static PowerManager.WakeLock GetLock(Context context)
        {
            if (lockStatic == null)
            {
                PowerManager manager = (PowerManager)context.GetSystemService(Context.PowerService);

                lockStatic = manager.NewWakeLock(WakeLockFlags.Partial, NAME);
                lockStatic.SetReferenceCounted(true);
            }
            return (lockStatic);
        }

        public static void SendWakefulWork(Context context, Intent intent)
        {
            GetLock(context.ApplicationContext); //Possibly use of acquire here
            context.StartService(intent);
        }

        public static void SendWakefulWork(Context context, Type classService)
        {
            SendWakefulWork(context, new Intent(context, classService));
        }

        public static void ScheduleAlarms(IAlarmListener alarmListener, Context context)
        {
            ScheduleAlarms(alarmListener, context, true);
        }

        public static void ScheduleAlarms(IAlarmListener alarmListener, Context context, bool force)
        {
            Log.Info(TAG, "Scheduling Alarm");
            ISharedPreferences preferences = context.GetSharedPreferences(NAME, 0);
            long lastAlarm = preferences.GetLong(LAST_ALARM, 0);

            if (lastAlarm == 0 || force ||
                (DateTime.Now.Millisecond > lastAlarm &&
                 DateTime.Now.Millisecond - lastAlarm > alarmListener.GetMaxAge()))
            {
                Log.Info(TAG, "OK - Passed Checks, Setting Manager Intents and Pending Intents");
                AlarmManager manager = (AlarmManager)context.GetSystemService(Context.AlarmService);
                Log.Debug(TAG, $"Manager: {manager.ToString()}");
                //Intent intent = new Intent(context, typeof(AlarmReceiver));
                Intent intent = new Intent("com.intellidrive.wakeful.ALARM_TRIG");
                Log.Debug(TAG, $"Intent: {intent.ToString()}");
                PendingIntent pendingIntent = PendingIntent.GetBroadcast(context, 0, intent, 0);
                Log.Debug(TAG, $"Pending Intent: {pendingIntent.ToString()}");
                alarmListener.ScheduleAlarms(manager, pendingIntent, context);
                Log.Info(TAG, "Scheduled Alarm..");
                Log.Debug(TAG, $"Manager: {manager.ToString()}");
            }
        }

        public static void CancelAlarms(Context context)
        {
            AlarmManager manager = (AlarmManager)context.GetSystemService(Context.AlarmService);
            Intent intent = new Intent(context, typeof(AlarmReceiver));
            PendingIntent pendingIntent = PendingIntent.GetBroadcast(context, 0, intent, 0);
            manager.Cancel(pendingIntent);
            context.GetSharedPreferences(NAME, 0).Edit().Remove(LAST_ALARM).Commit();
        }

        public WakefulIntentService(string name) : base(name)
        {
            SetIntentRedelivery(true);
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            PowerManager.WakeLock wakeLock = GetLock(this.ApplicationContext);

            if (!lockStatic.IsHeld || (flags & StartCommandFlags.Redelivery) != 0)
            {
                wakeLock.Acquire();
            }


            base.OnStartCommand(intent, flags, startId);
            return (StartCommandResult.RedeliverIntent);
        }

        protected override void OnHandleIntent(Intent intent)
        {
            try
            {
                DoWakefulWork(intent);
            }
            finally
            {
                PowerManager.WakeLock wakeLock = GetLock(this.ApplicationContext);

                if (wakeLock.IsHeld)
                {
                    try
                    {
                        wakeLock.Release();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(Class.SimpleName, "Exception when releasing wakelock", ex);
                        //Log exception when releasing wakelock
                    }
                }
            }
        }

        public interface IAlarmListener
        {
            void ScheduleAlarms(AlarmManager manager, PendingIntent pendingIntent, Context context);
            void SendWakefulWork(Context context);
            long GetMaxAge();
        }
    }
}