using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;

namespace SMAPIGameLoader;
//[Activity(
//    Label = "@string/app_name",
//    Icon = "@drawable/icon",
//    Theme = "@style/Theme.Splash",
//    AlwaysRetainTaskState = true,
//    LaunchMode = LaunchMode.SingleInstance,
//    ScreenOrientation = ScreenOrientation.SensorLandscape,
//    ConfigurationChanges = (ConfigChanges.Keyboard
//        | ConfigChanges.KeyboardHidden | ConfigChanges.Orientation
//        | ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize
//        | ConfigChanges.UiMode))]
internal class EntryGameActivity : Activity
{
    public static void LaunchGameActivity(Activity activity)
    {
#if DEBUG
        ToastNotifyTool.Notify("Error can't start game on Debug Mode");
        return;
#else
        Console.WriteLine("App is in Release mode");
#endif

        ToastNotifyTool.Notify("Starting Game..");
        //check game it's can launch with version
        if (StardewApkTool.IsGameVersionSupport == false)
        {
            ToastNotifyTool.Notify("Not support game version: " + StardewApkTool.CurrentGameVersion + ", please update game");
            return;
        }


        //clone game assets
        GameAssetManager.VerifyAssets();
        GameAssemblyManager.VerifyAssemblies();

        //Load MonoGame.Framework.dll into reference
        //StardewValley.dll wait load at SMAPIActivity
        GameAssemblyManager.LoadAssembly(GameAssemblyManager.MonoGameFrameworkDllFileName);

        var intent = new Intent(activity, typeof(SMAPIActivity));
        activity.StartActivity(intent);
        //close this activity
        activity.Finish();
    }
}
