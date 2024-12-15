using Android.App;

namespace SMAPIGameLoader.Tool;

internal static class AdbExtraTool
{
    private const string EZ_IsClickStartGame = "IsClickStartGame";

    public static bool IsClickStartGame(Activity activity)
    {
        return activity.Intent.GetBooleanExtra(EZ_IsClickStartGame, false);
    }
}