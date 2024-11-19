using HarmonyLib;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static StardewValley.LocalizedContentManager;

namespace SMAPIGameLoader.Mod;

[HarmonyPatch]
internal class LocalizedModManager
{
    static LocalizedModManager Instance;
    internal static void Setup()
    {
        new LocalizedModManager();
    }
    LocalizedModManager()
    {
        Instance = this;
    }
    public class TranslatorDictionary
    {
        public string assetName;
        public Dictionary<string, string> strings = new();
        public string assetFullPath;
    }
    static Dictionary<string, TranslatorDictionary> TranslatorDictionaryMap = new();

    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(LocalizedContentManager),
        "LoadStringReturnNullIfNotFound",
        [typeof(string), typeof(bool)]
    )]
    static bool PrefixLoadStringReturnNullIfNotFound(
        LocalizedContentManager __instance,
        ref string __result,
        string path, bool localeFallback)
    {
        var content = __instance;
        parseStringPath(path, out var assetName, out var key);
        var translatorDict = LoadTranslatorDictionaryType(assetName);
        if (translatorDict == null)
        {
            //skip does not exist mod assets
            return true;
        }

        string text = GetString(translatorDict.strings, key) ?? (localeFallback ? LoadBaseStringOrNull(path) : null);
        __result = PreprocessString(text);
        Console.WriteLine("PrefixLoadStringReturnNullIfNotFound: result=" + __result + ", path: " + path);
         
        return false;
    }
    public static string PreprocessString(string text)
    {
        if (text == null)
            return null;

        var gender = Game1.player?.Gender ?? Gender.Male;
        text = Dialogue.applyGenderSwitchBlocks(gender, text);
        text = Dialogue.applyGenderSwitch(gender, text, altTokenOnly: true);
        return text;
    }
    public static string LoadBaseStringOrNull(string path)
    {
        parseStringPath(path, out var assetName, out var key);
        var translatorDict = LoadTranslatorDictionaryType(assetName);
        if (translatorDict != null)
            return GetString(translatorDict.strings, key);
        return null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LocalizedContentManager), "LoadBaseStringOrNull", [typeof(string)])]
    static bool PrefixLoadBaseStringOrNull(ref string __result, string path)
    {
        var baseString = LoadBaseStringOrNull(path);
        if (baseString == null)
            return true;
        __result = baseString;
        Console.WriteLine("PrefixLoadBaseStringOrNull: result=" +__result);
        return false;
    }
    static TranslatorDictionary LoadTranslatorDictionaryType(string assetName)
    {
        assetName = FileTool.SafePath(assetName);
        assetName = $"Content/{assetName}.json";

        var assetFullPath = Path.Combine(AssetsModsManager.AssetsModsDir, assetName);
        if (Path.Exists(assetFullPath) == false)
        {
            return null;
        }
        if (TranslatorDictionaryMap.TryGetValue(assetName, out var translatorDict) == false)
        {
            var jsonContent = File.ReadAllText(assetFullPath);
            translatorDict = new TranslatorDictionary();
            translatorDict.strings = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
            translatorDict.assetName = assetName;
            translatorDict.assetFullPath = assetFullPath;
        }
        return translatorDict;
    }
    static string GetString(Dictionary<string, string> strings, string key)
    {
        if (strings == null)
        {
            return null;
        }
        if (strings.TryGetValue(key + ".mobile", out var value))
        {
            return value;
        }
        if (!strings.TryGetValue(key, out value))
        {
            return null;
        }
        return value;
    }
    static void parseStringPath(string path, out string assetName, out string key)
    {
        int colonIndex = path.IndexOf(':');
        if (colonIndex == -1)
            throw new ContentLoadException("Unable to parse string path: " + path);

        assetName = path.Substring(0, colonIndex);
        key = path.Substring(colonIndex + 1, path.Length - colonIndex - 1);
    }
}
