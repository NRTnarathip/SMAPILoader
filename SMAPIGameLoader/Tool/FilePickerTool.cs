using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class FilePickerTool
{
    public static FilePickerFileType FileTypeZip = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.Android, new[] { "application/zip" } },
    });
    public static async Task<FileResult> PickZipFile(string title = null)
    {
        if (title == null)
            title = "Please select zip file";

        //TODO check permission
        //android 6 --> 10 need to request External read write storage
        var activity = SMAPIGameLoader.Launcher.LauncherActivity.Instance;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M && Build.VERSION.SdkInt <= BuildVersionCodes.Q)
        {
            if (ContextCompat.CheckSelfPermission(activity, Manifest.Permission.ReadExternalStorage) != Permission.Granted
                || ContextCompat.CheckSelfPermission(activity, Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            {
                ToastNotifyTool.Notify("Please Click Allow File Access Permission");
                // Request the permissions
                ActivityCompat.RequestPermissions(activity,
                    [Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage],
                    1000); // 100 is a request code (you can choose any integer)
                return null;
            }
        }

        var options = new PickOptions
        {
            PickerTitle = title,
            FileTypes = FileTypeZip,
        };
        return await FilePicker.PickAsync(options);
    }

    const int CreateDocumentRequestCode = 4001;
    static TaskCompletionSource<Android.Net.Uri> createDocumentTcs;

    // Xamarin.Essentials 1.7 has no save picker, so this drives ACTION_CREATE_DOCUMENT directly.
    public static Task<Android.Net.Uri> PickSaveLocation(string fileName, string mimeType)
    {
        var intent = new Intent(Intent.ActionCreateDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType(mimeType);
        intent.PutExtra(Intent.ExtraTitle, fileName);

        // a second tap while the picker is open must not start another picker
        if (createDocumentTcs is { Task.IsCompleted: false })
            return Task.FromResult<Android.Net.Uri>(null);

        // start first: if it throws (no Files app), no pending picker is left behind to block later exports
        SMAPIGameLoader.Launcher.LauncherActivity.Instance.StartActivityForResult(intent, CreateDocumentRequestCode);
        createDocumentTcs = new TaskCompletionSource<Android.Net.Uri>();
        return createDocumentTcs.Task;
    }

    internal static void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        if (requestCode != CreateDocumentRequestCode)
            return;

        var uri = resultCode == Result.Ok ? data?.Data : null;
        if (uri != null && !Android.Provider.DocumentsContract.IsDocumentUri(Application.Context, uri))
            createDocumentTcs?.TrySetException(new System.IO.IOException($"The picker returned a location that is not a document: {uri}"));
        else
            createDocumentTcs?.TrySetResult(uri);
        createDocumentTcs = null;
    }
}
