using Android.App;
using Android.OS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xamarin.Essentials;
using static SMAPIGameLoader.Launcher.GameCloner;

namespace SMAPIGameLoader.Launcher;

internal static class GameCloner
{
    public const string ClonerStateFileName = "cloner_state.json";
    public static string ClonerStateFilePath => Path.Combine(FileTool.ExternalFilesDir, ClonerStateFileName);

    public sealed class ClonerState
    {
        //for detect should Cloner again
        public int LastLauncherBuildCode { get; set; } = 0;
        public string LastGameVersionString { get; set; } = "0.0.0.0";

        public void SaveToFile()
        {
            string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ClonerStateFilePath, jsonString);
            Console.WriteLine("done save ClonerState into file");
        }
        public void MarkCloenGameDone()
        {
            this.LastLauncherBuildCode = ApkTool.LauncherBuildCode;
            this.LastGameVersionString = StardewApkTool.CurrentGameVersion.ToString();
        }
        public bool IsNeedToCloneGame()
        {
            //check if you have edit logic or new launcher version
            Console.WriteLine("last build code: " + LastLauncherBuildCode);
            if (ApkTool.LauncherBuildCode != LastLauncherBuildCode)
                return true;

            //check if has new update game
            if (StardewApkTool.CurrentGameVersion != new Version(LastGameVersionString))
                return true;

            //don't clone game again
            return false;
        }
    }
    public static ClonerState GetClonerState()
    {
        try
        {
            //throw exception here if file never crate
            var jsonString = File.ReadAllText(ClonerStateFilePath);
            var clonerState = JsonConvert.DeserializeObject<ClonerState>(jsonString);
            return clonerState ?? new ClonerState();
        }
        catch (Exception ex)
        {
            //Recreate Game Cloner State always
            return new ClonerState();
        }
    }
    public static void Setup()
    {
        ClonerState clonerState = GetClonerState();
        TaskTool.AddNewLine("Try to assert game clone");

        bool isNeedCloenGame = clonerState.IsNeedToCloneGame();

        //clone each section

        if (isNeedCloenGame)
        {
            TaskTool.AddNewLine("Starting cloner game assets");
            //clone game assets
            GameAssetManager.VerifyAssets();
            TaskTool.AddNewLine("Done verify asset");
            GameAssemblyManager.VerifyAssemblies();
            TaskTool.AddNewLine("Done verify assemblies");
            GameAssemblyManager.VerifyLibs();
            TaskTool.AddNewLine("Done verify libs");
        }

        //Load MonoGame.Framework.dll into reference
        GameAssemblyManager.LoadAssembly(GameAssemblyManager.MonoGameFrameworkDllFileName);

        //Rewrite StardewValley.dll
        if (isNeedCloenGame)
        {
            TaskTool.AddNewLine("Try rewrite StardewValley.dll");
            var stardewDllFilePath = GameAssemblyManager.StardewValleyFilePath;
            StardewGameRewriter.Rewrite(stardewDllFilePath);
            TaskTool.AddNewLine("Done rewriter");
            //Don't load StardewValley assembly here
            //you should load at SMAPIActivity
            //Assembly.LoadFrom(stardewDllFilePath);
        }

        //Finally
        if (isNeedCloenGame)
        {
            //mark
            clonerState.MarkCloenGameDone();
            clonerState.SaveToFile();
            TaskTool.AddNewLine("Done save cloner state to file");
        }

        TaskTool.AddNewLine("Done assert game clone");
    }
}
