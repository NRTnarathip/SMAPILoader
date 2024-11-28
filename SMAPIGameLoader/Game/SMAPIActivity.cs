using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Text;
using Android.Util;
using Android.Views;
using HarmonyLib;
using Java.Util;
using Microsoft.Xna.Framework;
using SMAPIGameLoader.Tool;
using StardewValley;
using StardewValley.Mobile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace SMAPIGameLoader;

[Activity(
    Label = "@string/app_name",
    Icon = "@drawable/icon",
    Theme = "@style/Theme.Splash",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = (ConfigChanges.Keyboard
        | ConfigChanges.KeyboardHidden | ConfigChanges.Orientation
        | ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize
        | ConfigChanges.UiMode))]
public class SMAPIActivity : AndroidGameActivity
{
    public static SMAPIActivity Instance { get; private set; }

    Bundle currentBundle;
    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(currentBundle);
        Console.WriteLine("SMAPIActivity.OnCreate()");

        //init sdk
        Instance = this;
        currentBundle = bundle;
        ActivityTool.Init(this);

        //ready
        ToastNotifyTool.Notify("On SMAPIActivity.OnCreate(bundle)");

        LaunchGame();
    }
    void LaunchGame()
    {
        Console.WriteLine("try launch game");
        try
        {
            //ready to use all assemblies & references
            var harmony = new Harmony("SMAPIGameLoader");
            harmony.PatchAll();
            Console.WriteLine("harmony.PatchAll()");

            //Prepare Assemblies && fix bug
            GameAssemblyManager.LoadAssembly(GameAssemblyManager.StardewDllName);

            //setup Activity
            Console.WriteLine("try integrate main activity");
            IntegrateStardewMainActivity();

            //ready
            Console.WriteLine("Stardew Activity Ready");
            Stardew_OnCreate();
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }
    void IntegrateStardewMainActivity()
    {
        Console.WriteLine("try get instance field");
        var instance_Field = typeof(MainActivity).GetField("instance", BindingFlags.Static | BindingFlags.Public);
        Console.WriteLine("try set field");
        instance_Field.SetValue(null, this);
        Console.WriteLine("done setup MainActivity.instance with: " + instance_Field.GetValue(null));
        MainActivityPatcher.Apply();
    }

    //override method
    void Stardew_OnCreate()
    {
        Log.It("MainActivity.OnCreate");
        RequestWindowFeature(WindowFeatures.NoTitle);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
        {
            Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
        }
        Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
        Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
        //base.OnCreate(currentBundle);
        CheckAppPermissions();
    }

    //Start Instance Game
    void OnCreatePartTwo()
    {
        Log.It("MainActivity.OnCreatePartTwo");
        SetupDisplaySettings();
        SetPaddingForMenus();

        var err = StartGameWithSMAPI();
        if (err != null)
        {
            ToastNotifyTool.Notify("error try run SMAPI: " + err.ToString());
            ErrorDialogTool.Show(err);
        }
        ToastNotifyTool.Notify("Done OnCreatePartTwo()");
    }

    Game1 _game1 => Game1.game1;
    protected override void OnResume()
    {
        Log.It("MainActivity.OnResume");
        base.OnResume();
        if (_game1 != null)
            _game1.OnAppResume();

        RequestedOrientation = ScreenOrientation.SensorLandscape;
        SetImmersive();
    }
    protected override void OnPause()
    {
        Log.It("MainActivity.OnPause");
        if (_game1 != null)
            _game1.OnAppPause();

        Game1.emergencyBackup();
        base.OnPause();
    }
    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
        {
            RequestedOrientation = ScreenOrientation.SensorLandscape;
            SetImmersive();
        }
    }

    void SetImmersive()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
        {
            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)5894;
        }
    }
    public bool CheckStorageMigration()
    {
        Console.WriteLine("Bypass Farm Migration");
        return false;
    }

    public void LogPermissions()
    {
        Log.It("MainActivity.LogPermissions method PackageManager: , AccessNetworkState:" + PackageManager.CheckPermission("android.permission.ACCESS_NETWORK_STATE", PackageName).ToString() + ", AccessWifiState:" + PackageManager.CheckPermission("android.permission.ACCESS_WIFI_STATE", PackageName).ToString() + ", Internet:" + PackageManager.CheckPermission("android.permission.INTERNET", PackageName).ToString() + ", Vibrate:" + PackageManager.CheckPermission("android.permission.VIBRATE", PackageName));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            Log.It("MainActivity.LogPermissions: , AccessNetworkState:" + CheckSelfPermission("android.permission.ACCESS_NETWORK_STATE").ToString() + ", AccessWifiState:" + CheckSelfPermission("android.permission.ACCESS_WIFI_STATE").ToString() + ", Internet:" + CheckSelfPermission("android.permission.INTERNET").ToString() + ", Vibrate:" + CheckSelfPermission("android.permission.VIBRATE"));
        }
    }
    public bool HasPermissions
    {
        get
        {
            if (PackageManager.CheckPermission("android.permission.ACCESS_NETWORK_STATE", PackageName) == Permission.Granted
                && PackageManager.CheckPermission("android.permission.ACCESS_WIFI_STATE", PackageName) == Permission.Granted
                && PackageManager.CheckPermission("android.permission.INTERNET", PackageName) == Permission.Granted
                && PackageManager.CheckPermission("android.permission.VIBRATE", PackageName) == Permission.Granted)
            {
                return true;
            }
            return false;
        }
    }

    public void CheckAppPermissions()
    {
        LogPermissions();
        if (HasPermissions)
        {
            Log.It("MainActivity.CheckAppPermissions permissions already granted.");
            OnCreatePartTwo();
        }
        else
        {
            Log.It("MainActivity.CheckAppPermissions PromptForPermissions C");
            PromptForPermissionsWithReasonFirst();
        }
    }
    public static string PermissionMessageA(string languageCode)
    {
        return languageCode switch
        {
            "de" => "Du musst die Erlaubnis zum Lesen/Schreiben auf dem externen Speicher geben, um das Spiel zu speichern und Speicherstände auf andere Plattformen übertragen zu können. Bitte gib diese Genehmigung, um spielen zu können.",
            "es" => "Para guardar la partida y transferir partidas guardadas a y desde otras plataformas, se necesita permiso para leer/escribir en almacenamiento externo. Concede este permiso para poder jugar.",
            "ja" => "外部機器への読み込み/書き出しの許可が、ゲームのセーブデータの保存や他プラットフォームとの双方向のデータ移行実行に必要です。プレイを続けるには許可をしてください。",
            "pt" => "Para salvar o jogo e transferir jogos salvos entre plataformas é necessário permissão para ler/gravar em armazenamento externo. Forneça essa permissão para jogar.",
            "ru" => "Для сохранения игры и переноса сохранений с/на другие платформы нужно разрешение на чтение-запись на внешнюю память. Дайте разрешение, чтобы начать играть.",
            "ko" => "게임을 저장하려면 외부 저장공간에 대한 읽기/쓰기 권한이 필요합니다. 또한 저장 데이터 이전 기능을 허용해 다른 플랫폼에서 게임 진행상황을 가져올 때에도 권한이 필요합니다. 게임을 플레이하려면 권한을 허용해 주십시오.",
            "tr" => "Oyunu kaydetmek ve kayıtları platformlardan platformlara taşımak için harici depolamada okuma/yazma izni gereklidir. Lütfen oynayabilmek için izin verin.",
            "fr" => "Une autorisation de lecture / écriture sur un stockage externe est requise pour sauvegarder le jeu et vous permettre de transférer des sauvegardes vers et depuis d'autres plateformes. Veuillez donner l'autorisation afin de jouer.",
            "hu" => "A játék mentéséhez, és ahhoz, hogy a különböző platformok között hordozhasd a játékmentést, engedélyezned kell a külső tárhely olvasását/írását, Kérjük, a játékhoz engedélyezd ezeket.",
            "it" => "È necessaria l'autorizzazione a leggere/scrivere su un dispositivo di memorizzazione esterno per salvare la partita e per consentire di trasferire i salvataggi da e su altre piattaforme. Concedi l'autorizzazione per giocare.",
            "zh" => "《星露谷物语》请求获得授权用来保存游戏数据以及访问线上功能。",
            _ => "Read/write to external storage permission is required to save the game, and to allow to you transfer saves to and from other platforms. Please give permission in order to play.",
        };
    }
    public static string PermissionMessageB(string languageCode)
    {
        return languageCode switch
        {
            "de" => "Bitte geh in die Handy-Einstellungen > Apps > Stardew Valley > Berechtigungen und aktiviere den Speicher, um das Spiel zu spielen.",
            "es" => "En el teléfono, ve a Ajustes > Aplicaciones > Stardew Valley > Permisos y activa Almacenamiento para jugar al juego.",
            "ja" => "設定 > アプリ > スターデューバレー > 許可の順に開いていき、ストレージを有効にしてからゲームをプレイしましょう。",
            "pt" => "Acesse Configurar > Aplicativos > Stardew Valley > Permissões e ative Armazenamento para jogar.",
            "ru" => "Перейдите в меню Настройки > Приложения > Stardew Valley > Разрешения и дайте доступ к памяти, чтобы начать играть.",
            "ko" => "휴대전화의 설정 > 어플리케이션 > 스타듀 밸리 > 권한 에서 저장공간을 활성화한 뒤 게임을 플레이해 주십시오.",
            "tr" => "Lütfen oyunu oynayabilmek için telefonda Ayarlar > Uygulamalar > Stardew Valley > İzinler ve Depolamayı etkinleştir yapın.",
            "fr" => "Veuillez aller dans les Paramètres du téléphone> Applications> Stardew Valley> Autorisations, puis activez Stockage pour jouer.",
            "hu" => "Lépje be a telefonodon a Beállítások > Alkalmazások > Stardew Valley > Engedélyek menübe, majd engedélyezd a Tárhelyet a játékhoz.",
            "it" => "Nel telefono, vai su Impostazioni > Applicazioni > Stardew Valley > Autorizzazioni e attiva Memoria archiviazione per giocare.",
            "zh" => "可在“设置-权限隐私-按应用管理权限-星露谷物语”进行设置，并打开“电话”、“读取位置信息”、“存储”权限。",
            _ => "Please go into phone Settings > Apps > Stardew Valley > Permissions, and enable Storage to play the game.",
        };

    }
    public static string GetOKString(string languageCode)
    {
        return languageCode switch
        {
            "de" => "OK",
            "es" => "DE ACUERDO",
            "ja" => "OK",
            "pt" => "Está bem",
            "ru" => "Хорошо",
            "ko" => "승인",
            "tr" => "tamam",
            "fr" => "D'accord",
            "hu" => "rendben",
            "it" => "ok",
            "zh" => "好",
            _ => "OK",
        };
    }

    private void PromptForPermissionsWithReasonFirst()
    {
        Log.It("MainActivity.PromptForPermissionsWithReasonFirst...");
        if (!HasPermissions)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            string languageCode = Locale.Default.Language.Substring(0, 2);
            builder.SetMessage(PermissionMessageA(languageCode));
            builder.SetCancelable(false);
            builder.SetPositiveButton(GetOKString(languageCode), delegate
            {
                Log.It("MainActivity.PromptForPermissionsWithReasonFirst PromptForPermissions A");
                PromptForPermissions();
            });
            Dialog dialog = builder.Create();
            if (!IsFinishing)
            {
                dialog.Show();
            }
        }
        else
        {
            Log.It("MainActivity.PromptForPermissionsWithReasonFirst PromptForPermissions B");
            PromptForPermissions();
        }
    }
    private string[] requiredPermissions => [
        "android.permission.ACCESS_NETWORK_STATE", "android.permission.ACCESS_WIFI_STATE",
        "android.permission.INTERNET", "android.permission.VIBRATE"
    ];
    private string[] deniedPermissionsArray
    {
        get
        {
            List<string> list = new List<string>();
            string[] array = requiredPermissions;
            for (int i = 0; i < array.Length; i++)
            {
                if (PackageManager.CheckPermission(array[i], PackageName) != 0)
                {
                    list.Add(array[i]);
                }
            }
            return list.ToArray();
        }
    }
    public void PromptForPermissions()
    {
        Log.It("MainActivity.PromptForPermissions requesting permissions...deniedPermissionsArray:" + deniedPermissionsArray.Length);
        string[] array = deniedPermissionsArray;
        if (array.Length != 0)
        {
            Log.It("PromptForPermissions permissionsArray:" + array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Log.It("PromptForPermissions permissionsArray[" + i + "]: " + array[i]);
            }
            RequestPermissions(array, 0);
        }
    }

    static string GetSMAPIFilePath => Path.Combine(GameAssemblyManager.AssembliesDirPath, "StardewModdingAPI.dll");
    public Exception StartGameWithSMAPI()
    {
        Console.WriteLine("try start game with SMAPI");
        Exception exOut = null;
        try
        {
            //setup patch game vanilla
            Log.Setup();

            var smapiFilePath = GetSMAPIFilePath;
            Console.WriteLine("smapi path to load: " + smapiFilePath);
            if (File.Exists(smapiFilePath) == false)
            {
                Console.WriteLine("error StardewModdingAPI.dll file not found");
                exOut = new Exception($"Error file: {smapiFilePath} not found");
            }

            var smapi = Assembly.LoadFrom(smapiFilePath);
            Console.WriteLine(smapi);
            var programType = smapi.GetType("StardewModdingAPI.Program");
            var mainMethod = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.Public);
            var args = new object[] { new string[] { } };
            Console.WriteLine("try invoke SMAPI Program.Main()");
            mainMethod.Invoke(null, args);
            Console.WriteLine("done run SMAPI Program.Main()");
        }
        catch (Exception err)
        {
            Console.WriteLine("failed start SMAPI");
            Console.WriteLine(err);
            exOut = err;
        }
        return exOut;
    }
    public void StartGameVanilla()
    {
        Console.WriteLine("try start game with vanilla");
        try
        {
            //setup game patch
            Log.Setup();

            //ready create instance game
            var gameRunner = new GameRunner();
            GameRunner.instance = gameRunner;
            SetContentView((View)GameRunner.instance.Services.GetService(typeof(View)));
            Console.WriteLine("done set content view");
            Console.WriteLine("try run Game Runner: " + GameRunner.instance);
            GameRunner.instance.Run();
            Console.WriteLine("done GameRunner.Run()");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public int GetBuild()
    {
        Context context = Application.Context;
        PackageManager packageManager = context.PackageManager;
        PackageInfo packageInfo = packageManager.GetPackageInfo(context.PackageName, (PackageInfoFlags)0);
        return packageInfo.VersionCode;
    }
    public void SetPaddingForMenus()
    {
        Log.It("MainActivity.SetPaddingForMenus build:" + GetBuild());
        if (Build.VERSION.SdkInt >= BuildVersionCodes.P && Window != null && Window.DecorView != null && Window.DecorView.RootWindowInsets != null && Window.DecorView.RootWindowInsets.DisplayCutout != null)
        {
            DisplayCutout displayCutout = Window.DecorView.RootWindowInsets.DisplayCutout;
            Log.It("MainActivity.SetPaddingForMenus DisplayCutout:" + displayCutout);
            if (displayCutout.SafeInsetLeft > 0 || displayCutout.SafeInsetRight > 0)
            {
                int num = Math.Max(displayCutout.SafeInsetLeft, displayCutout.SafeInsetRight);
                Game1.xEdge = Math.Min(90, num);
                Game1.toolbarPaddingX = num;
                Log.It("MainActivity.SetPaddingForMenus CUT OUT toolbarPaddingX:" + Game1.toolbarPaddingX + ", xEdge:" + Game1.xEdge);
                return;
            }
        }
        string manufacturer = Build.Manufacturer;
        string model = Build.Model;
        DisplayMetrics displayMetrics = new DisplayMetrics();
        WindowManager.DefaultDisplay.GetRealMetrics(displayMetrics);
        if (displayMetrics.HeightPixels >= 1920 || displayMetrics.WidthPixels >= 1920)
        {
            Game1.xEdge = 20;
            Game1.toolbarPaddingX = 20;
        }
    }
    public static void SetupDisplaySettings()
    {
        var MobileDisplayType = typeof(MainActivity).Assembly.GetType("StardewValley.Mobile.MobileDisplay");
        MobileDisplayType.GetMethod("SetupDisplaySettings", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
    }
}
