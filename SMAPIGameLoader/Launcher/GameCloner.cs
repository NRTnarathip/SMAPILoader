using Android.App;
using Android.OS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.Json;
using SMAPIGameLoader.Game.Rewriter;
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
            this.LastGameVersionString = GameApkTool.CurrentGameVersion.ToString();
        }
        public bool IsNeedToCloneGame()
        {
            //check if you have edit logic or new launcher version
            Console.WriteLine("last build code: " + LastLauncherBuildCode);
            if (ApkTool.LauncherBuildCode != LastLauncherBuildCode)
                return true;

            //check if has new update game
            if (GameApkTool.CurrentGameVersion != new Version(LastGameVersionString))
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
        bool isNeedCloenGame = clonerState.IsNeedToCloneGame();

        TaskTool.NewLine(isNeedCloenGame
            ? "Game files need an update, this may take a minute"
            : "Game files are up to date");

        //clone each section
        if (isNeedCloenGame)
        {
            TaskTool.NewLine("Copying game assets");
            GameAssetManager.VerifyAssets();
            TaskTool.NewLine("Copying game assemblies");
            GameAssemblyManager.VerifyAssemblies();
            TaskTool.NewLine("Copying native libraries");
            GameAssemblyManager.VerifyLibs();
        }

        //Load MonoGame.Framework.dll into reference
        GameAssemblyManager.LoadAssembly(GameAssemblyManager.MonoGameDLLFileName);

        //Rewrite StardewValley.dll
        if (isNeedCloenGame)
        {
            TaskTool.NewLine("Patching StardewValley.dll");
            using (var stardewAssemblyStream = File.Open(GameAssemblyManager.StardewValleyFilePath,
                FileMode.Open, FileAccess.ReadWrite))
            {
                var stardewAssemblyDef = StardewGameRewriter.ReadAssembly(stardewAssemblyStream);
                StardewGameRewriter.Rewrite(stardewAssemblyDef);
                StardewAudioRewriter.Rewrite(stardewAssemblyDef);

                TaskTool.NewLine("Saving patched assembly");
                stardewAssemblyDef.Write();
            }

            //Don't load StardewValley assembly here
            //you should load at SMAPIActivity
            //Assembly.LoadFrom(stardewDllFilePath);
        }

        //Finally
        if (isNeedCloenGame)
        {
            clonerState.MarkCloenGameDone();
            clonerState.SaveToFile();
            TaskTool.NewLine("Game files ready");
        }
    }
}
