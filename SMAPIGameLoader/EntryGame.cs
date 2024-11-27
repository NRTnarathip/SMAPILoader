using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using HarmonyLib;
using System;
using System.Reflection;

namespace SMAPIGameLoader;
internal static class EntryGame
{
    public static void LaunchGameActivity(Activity launcherActivity)
    {
        TaskTool.Run(() =>
        {
            LaunchGameActivityInternal(launcherActivity);
        });
    }

    static void LaunchGameActivityInternal(Activity launcherActivity)
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

        try
        {
            //clone game assets
            GameAssetManager.VerifyAssets();
            GameAssemblyManager.VerifyAssemblies();

            //Load MonoGame.Framework.dll into reference
            GameAssemblyManager.LoadAssembly(GameAssemblyManager.MonoGameFrameworkDllFileName);

            //patch rewrite StardewValley.dll & Load
            var stardewDllFilePath = GameAssemblyManager.StardewValleyFilePath;
            StardewGameRewriter.Rewrite(stardewDllFilePath, out var isRewrite);
            Assembly.LoadFrom(stardewDllFilePath);


            var intent = new Intent(launcherActivity, typeof(SMAPIActivity));
            launcherActivity.StartActivity(intent);
            launcherActivity.Finish();
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("Error:LaunchGameActivity: " + ex.ToString());
        }
    }
}
