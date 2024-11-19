using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;

namespace SMAPIGameLoader;
[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Icon = "@drawable/icon",
    Theme = "@style/Theme.Splash",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = (ConfigChanges.Keyboard
        | ConfigChanges.KeyboardHidden | ConfigChanges.Orientation
        | ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize
        | ConfigChanges.UiMode))]
internal class EntryActivity : Activity
{
    static EntryActivity()
    {
        //Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        //foreach (var asm in assemblies)
        //{
        //    Console.WriteLine("already loaded in ctor: " + asm);
        //}

        //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        //AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
    }

    private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
    {
        Console.WriteLine("lloaded asm: " + args.LoadedAssembly.FullName);
    }

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        Console.WriteLine("handle resolve manual: " + args.Name);
        return null;
    }

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        //clone game assets
        GameAssetManager.VerifyAssets();
        GameAssemblyManager.VerifyAssemblies();

        //Load MonoGame.Framework.dll into reference
        //StardewValley.dll wait load at SMAPIActivity
        GameAssemblyManager.LoadAssembly(GameAssemblyManager.MonoGameFrameworkDllFileName);

        LaunchGameActivity();
    }
    void LaunchGameActivity()
    {
        Intent intent = new Intent(this, typeof(SMAPIActivity));
        StartActivity(intent);
        //close this activity
        Finish();
    }
}
