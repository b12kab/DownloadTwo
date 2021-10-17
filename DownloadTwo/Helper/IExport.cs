using System;
namespace DownloadTwo.Helper
{
    public interface IExport
    {
        bool IsPermissionNeeded();

        bool ExportFile(string contents, string filename, bool androidSetPending, out string androidUri);

        bool FileCheck(string filename, out bool matched, out string androidUri, out long androidFileId);

        bool DeleteExportFile(string filename, string androidUri, long androidFileId, out bool deleteOK);

        bool UpdateExportFile(string androidUri, out bool updatedOK);
    }
}
