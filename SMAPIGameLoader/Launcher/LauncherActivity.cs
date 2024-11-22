using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using System;
using System.IO;
using System.IO.Compression;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

[Activity(
    Label = "Launcher",
    MainLauncher = true,
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorPortrait
)]
public class LauncherActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        SetContentView(Resource.Layout.layout1);

        Xamarin.Essentials.Platform.Init(this, savedInstanceState);

        // Create your application here
        Button installSMAPIBtn = FindViewById<Button>(Resource.Id.InstallSMAPI);
        installSMAPIBtn.Click += (sender, e) =>
        {
            OnClickInstallSMAPI();
        };
        var startGameBtn = FindViewById<Button>(Resource.Id.StartGame);
        startGameBtn.Click += (sender, e) =>
        {
            OnClickStartGame();
        };
    }

    void OnClickStartGame()
    {
        ToastNotifyTool.Notify("Try Start Game!");
        EntryGameActivity.LaunchGameActivity(this);
    }

    void InstallSMAPIInternal(FileResult pick)
    {
    }
    async void OnClickInstallSMAPI()
    {
        try
        {
            ToastNotifyTool.Notify("Please Pick File SMAPI-4.x.x.zip");
            var pick = await FilePicker.PickAsync();
            if (pick.FileName.StartsWith("SMAPI") == false || pick.FileName.EndsWith(".zip") == false)
            {
                throw new Exception("Please select file SMAPI Android.zip!!");
            }

            var zip = ZipFile.OpenRead(pick.FullPath);
            foreach (var entry in zip.Entries)
            {
                Console.WriteLine("entry : " + entry.FullName);
                var destFilePath = entry.FullName.Replace("SMAPI Android/", "");
                destFilePath = Path.Combine(GameAssemblyManager.AssembliesDirPath, destFilePath);
                //make sure have dir
                Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
                entry.ExtractToFile(destFilePath, true);
                Console.WriteLine("extract file: " + destFilePath);
            }

            ToastNotifyTool.Notify("Successfully Install SMAPI!");
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify(ex.ToString());
        }
    }
}