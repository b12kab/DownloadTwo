using System;
using Xamarin.Forms;

namespace DownloadTwo.Helper
{
    public class ExportData
    {
        public bool CreateExportFile(string contents, string filename, bool androidPendingFlag, out string androidUri)
        {
            return DependencyService.Get<IExport>().ExportFile(contents, filename, androidPendingFlag, out androidUri);
        }

        public bool CheckExportFile(string filename, out bool found, out string androidUri)
        {
            found = false;

            bool worked = DependencyService.Get<IExport>().FileCheck(filename, out found, out androidUri, out long androidFileId);
            if (!worked)
            {
                System.Diagnostics.Debug.WriteLine("File check failed!");
            }

            return worked;
        }

        public bool DeleteExportFile(string filename, out bool deletedOK)
        {
            bool found;
            string androidUri;

            deletedOK = false;

            bool worked = DependencyService.Get<IExport>().FileCheck(filename, out found, out androidUri, out long androidFileId);
            if (!worked)
            {
                System.Diagnostics.Debug.WriteLine("File check failed!");
            }
            else if (found)
            {
                worked = DependencyService.Get<IExport>().DeleteExportFile(filename, androidUri, androidFileId, out deletedOK);
            }

            return worked;
        }

        public bool UpdateExportFile(string androidUri, out bool updatedOK)
        {
            bool worked = DependencyService.Get<IExport>().UpdateExportFile(androidUri, out updatedOK);

            return worked;
        }
    }
}
