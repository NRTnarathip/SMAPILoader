using Android.App;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Tool;

internal static class ActivityTool
{
    public static Activity CurrentActivity => GetTopActivity();
    static List<Activity> _ActivityList = new();

    public static List<Activity> GetAllActivity()
    {
        _ActivityList = _ActivityList.Where(x => x != null).ToList();
        return _ActivityList;
    }

    static Activity GetTopActivity()
    {
        var top = GetAllActivity().Where(item => item.IsDestroyed == false).LastOrDefault();
        Console.WriteLine("OnGetTop: " + top);
        return top;
    }

    public static void Init(Activity activity)
    {
        if (_ActivityList.Contains(activity))
            return;

        _ActivityList.Add(activity);
    }
    public static void SwapActivity<T>(Activity currentActivity, bool closeCurrentActivity = true)
    {
        var intent = new Intent(currentActivity, typeof(T));
        currentActivity.StartActivity(intent);
        if (closeCurrentActivity)
            currentActivity.Finish();
    }
}
