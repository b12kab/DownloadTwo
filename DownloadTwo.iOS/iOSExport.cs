using DownloadTwo.Helper;
using System;
using System.IO;

[assembly: Xamarin.Forms.Dependency(typeof(DownloadTwo.iOS.iOSExport))]
namespace DownloadTwo.iOS
{
    public class iOSExport : IExport
    {
        /// <summary>
        /// iOS will always return true for file write, as all files are local
        /// </summary>
        /// <returns>Permission needed? always true</returns>
        public bool IsPermissionNeeded()
        {
            return true;
        }

        /// <summary>
        /// Write file out to app directory.
        /// </summary>
        /// <param name="contents">File content to write</param>
        /// <param name="filename">Filename to export content into</param>
        /// <param name="androidSetPending">Not used on iOS</param>
        /// <returns>true = worked w/o error or false = failed with system error</returns>
        public bool ExportFile(string contents, string filename, bool androidSetPending, out string androidUri)
        {
            androidUri = null;

            string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Documents folder  

            string filespec = Path.Combine(dir, filename);

            FileInfo newFile = new FileInfo(filespec);
            if (newFile.Exists)
            {
                try
                {
                    newFile.Delete();

                    System.Diagnostics.Debug.WriteLine("existing file deleted: " + filespec);
                    System.Diagnostics.Debug.Flush();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed file delete. Passed in filename: " + filespec + ". Full filespec: " + filespec);
                    System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                    System.Diagnostics.Debug.Flush();
                    return false;
                }
            }

            try
            {
                using (StreamWriter writer = new(filespec))
                {
                    writer.WriteAsync(contents);
                    writer.Close();
                    writer.DisposeAsync();
                }
                System.Diagnostics.Debug.WriteLine("File write succeeded. filename: " + filespec);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed file write. Passed in filename: " + filespec + ". Full filespec: " + filespec);
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                System.Diagnostics.Debug.Flush();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check to see if the file exists int the Download directory
        /// </summary>
        /// <param name="filename">Filename to check</param>
        /// <param name="matched">Found filename</param>
        /// <param name="androidUri">Not used on iOS</param>
        /// <param name="androidFileId">Not used on iOS</param>
        /// <returns>true = worked w/o error or false = failed with system error</returns>
        public bool FileCheck(string filename, out bool matched, out string androidUri, out long androidFileId)
        {
            androidUri = null;
            androidFileId = 0;
            matched = false;

            string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Documents folder  

            string filespec = Path.Combine(dir, filename);

            try
            {
                FileInfo newFile = new FileInfo(filespec);
                matched = newFile.Exists;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed file check. Passed in filename: " + filespec + ". Full filespec: " + filespec);
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                System.Diagnostics.Debug.Flush();
                return false;
            }

            return true;
        }

        /// <summary>
        /// This will try to delete a file
        /// </summary>
        /// <param name="filename">Filename to delete</param>
        /// <param name="androidUri">Not used on iOS</param>
        /// <param name="androidFileId">Not used on iOS</param>
        /// <param name="deleteOK">File actually deleted?</param>
        /// <returns>true = worked w/o error or false = failed with system error</returns>
        public bool DeleteExportFile(string filename, string androidUri, long androidFileId, out bool deleteOK)
        {
            deleteOK = false;

            string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Documents folder  

            string filespec = Path.Combine(dir, filename);

            FileInfo newFile = new FileInfo(filespec);
            if (newFile.Exists)
            {
                try
                {
                    newFile.Delete();
                    deleteOK = true;

                    System.Diagnostics.Debug.WriteLine("existing file deleted: " + filespec);
                    System.Diagnostics.Debug.Flush();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed file delete. Passed in filename: " + filespec + ". Full filespec: " + filespec);
                    System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                    System.Diagnostics.Debug.Flush();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Not used on iOS
        /// </summary>
        /// <param name="androidUri">Not used in iOS</param>
        /// <param name="updatedOK">File actually updated?</param>
        /// <returns>true = worked w/o error or false = failed with system error</returns>
        public bool UpdateExportFile(string androidUri, out bool updatedOK)
        {
            updatedOK = true;

            return true;
        }
    }
}
