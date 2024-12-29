using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

internal static class SaveManager
{
    public static void OnClickImportSaveZip()
    {

    }
    public static bool ImportSaveZip(FileResult pick)
    {
        if (pick.FileName.EndsWith(".zip"))
            return false;


        return true;
    }
}
