using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Exception = System.Exception;
using Android.Util;

namespace WakefulIntentService
{
    [BroadcastReceiver(Enabled=true, Exported = true)]
    [IntentFilter(new []{ Android.Content.Intent.ActionBootCompleted, "com.intellidrive.wakeful.ALARM_TRIG"} )]
    [MetaData("com.intellidrive.wakeful", Resource = "@xml/wakeful")]
    public class AlarmReceiver : BroadcastReceiver
    {
        private static string WAKEFUL_META_DATA = "com.intellidrive.wakeful";

        public override void OnReceive(Context context, Intent intent)
        {
            Log.Debug(WakefulIntentService.TAG, "In Alarm Reciever - On Recieve");
            WakefulIntentService.IAlarmListener alarmListener = GetListener(context);

            if (alarmListener != null)
            {
                if (intent.Action != null)
                {
                    Log.Debug(WakefulIntentService.TAG, $"Intent Action Is: {intent.Action.ToString()}");
                    ISharedPreferences preferences = context.GetSharedPreferences(WakefulIntentService.NAME, 0);

                    preferences.Edit().PutLong(WakefulIntentService.LAST_ALARM, System.DateTime.Now.Millisecond)
                        .Commit();
                    Log.Debug(WakefulIntentService.TAG, $"Sending Wakeful Work To Context: {context.ToString()}");
                    alarmListener.SendWakefulWork(context);
                }
                else
                {
                    Log.Debug(WakefulIntentService.TAG, "Intent Action Was Null... Scheduling New Alarm");
                    WakefulIntentService.ScheduleAlarms(alarmListener, context, true);
                }
            }
            else
                Log.Debug(WakefulIntentService.TAG, "Alarm Listener Is NULL");
        }


        private WakefulIntentService.IAlarmListener GetListener(Context context)
        {
            PackageManager packageManager = context.PackageManager;
            ComponentName componentName = new ComponentName(context, Class);

            try
            {
                ActivityInfo activityInfo = packageManager.GetReceiverInfo(componentName, PackageInfoFlags.MetaData);
                XmlReader reader = activityInfo.LoadXmlMetaData(packageManager, WAKEFUL_META_DATA);

                while (!reader.EOF)
                {
                    if (reader.IsStartElement())
                    {
                        if (reader.Name == "WakefulIntentService")
                        {
                            // Get First Attribute... ensure is listener =
                            reader.MoveToFirstAttribute();
                            if (reader.Name != "listener")
                                throw new Exception("XML Element Does Not Contain A Listener Property");
                                
                            string className = reader.Value;
                            Class cls = Java.Lang.Class.ForName(className);
                            return ((WakefulIntentService.IAlarmListener)cls.NewInstance());
                        }
                    }
                    reader.MoveToNextAttribute();
                }
            }
            catch (Exception ex)
            {
                //Figure this out later.
                throw;
            }
            return (null);
        }
    }
}