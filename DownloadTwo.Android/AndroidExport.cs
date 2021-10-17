using Android.Content;
using Android.Database;
using DownloadTwo.Helper;
using Plugin.CurrentActivity;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Essentials;

[assembly: Xamarin.Forms.Dependency(typeof(DownloadTwo.Droid.AndroidExport))]
namespace DownloadTwo.Droid
{
    public class AndroidExport : IExport
    {
        /// <summary>
        /// After version 10 (API 29) cannot write directly to Downloads directory, so permissions not needed.
        /// Before that, permission is needed.
        /// </summary>
        /// <returns></returns>
        public bool IsPermissionNeeded()
        {
            Version version = DeviceInfo.Version;

            if (version.Major < 10)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Write file out to app directory.
        /// </summary>
        /// <param name="contents">File content to write</param>
        /// <param name="filename">Filename to export content into</param>
        /// <param name="androidSetPending">Set is_pending flag to 0, if this is true and API 30 or above. Note: only needed for network creation download</param>
        /// <param name="androidUri">Android URI of created file</param>
        /// <returns>true = worked w/o error or false = failed with system error</returns>
        public bool ExportFile(string contents, string filename, bool androidSetPending, out string androidUri)
        {
            Version version = DeviceInfo.Version;

            // After version 10 (API 29) cannot write directly to Downloads directory
            // and must do it via the Mediastore API due to security changes
            if (version.Major < 10)
            {
                androidUri = null;
                return WriteAPI28AndBelow(contents, filename);
            }
            else
            {
                bool pending = false;
                if (version.Major > 10 && androidSetPending)
                {
                    pending = true;
                }

                return WriteAPI29AndAbove(contents, filename, pending, out androidUri);
            }
        }

        /// <summary>
        /// Check to see if the file exists int the Download directory
        /// </summary>
        /// <param name="filename">Filename to check</param>
        /// <param name="matched">Was filename found?</param>
        /// <param name="androidUri">Android URI of found file</param>
        /// <param name="androidFileId">Android file id of found file</param>
        /// <returns>true = worked w/o error or false = failed with system error</returns>
        public bool FileCheck(string filename, out bool matched, out string androidUri, out long androidFileId)
        {
            matched = false;
            androidUri = null;
            androidFileId = 0;

            /**
             * A key concept when working with Android [ContentProvider]s is something called
             * "projections". A projection is the list of columns to request from the provider,
             * and can be thought of (quite accurately) as the "SELECT ..." clause of a SQL
             * statement.
             *
             * It's not _required_ to provide a projection. In this case, one could pass `null`
             * in place of `projection` in the call to [ContentResolver.query], but requesting
             * more data than is required has a performance impact.
             *
             * For this sample, we only use a few columns of data, and so we'll request just a
             * subset of columns.
             */
            var projection = new List<string>()
            {
                Android.Provider.MediaStore.Downloads.InterfaceConsts.Id,
                Android.Provider.MediaStore.Downloads.InterfaceConsts.DisplayName,
                Android.Provider.MediaStore.Downloads.InterfaceConsts.DateAdded,
                Android.Provider.MediaStore.Downloads.InterfaceConsts.Title,
                Android.Provider.MediaStore.Downloads.InterfaceConsts.RelativePath,
                Android.Provider.MediaStore.Downloads.InterfaceConsts.MimeType,
            }.ToArray();

            /**
             * The `selection` is the "WHERE ..." clause of a SQL statement. It's also possible
             * to omit this by passing `null` in its place, and then all rows will be returned.
             * In this case we're using a selection based on the date the image was taken.
             *
             * Note that we've included a `?` in our selection. This stands in for a variable
             * which will be provided by the next variable. Note: I modified this to not use
             * the parameter, but just hard code the value - which is the text mime type.
             */
            string selection = Android.Provider.MediaStore.Downloads.InterfaceConsts.MimeType + " = ?";

            /**
             * The `selectionArgs` is a list of values that will be filled in for each `?`
             * in the `selection`.
             */
            var selectionArgs = new List<string>()
            {
                "text/plain"
            };

            ICursor cursor;

            ////https://github.com/android/storage-samples/blob/main/MediaStore/app/src/main/java/com/android/samples/mediastore/MainActivityViewModel.kt

            try
            {
                ContentResolver contentResolver = CrossCurrentActivity.Current.AppContext.ContentResolver;
                cursor = contentResolver.Query(Android.Provider.MediaStore.Downloads.ExternalContentUri, projection, selection, selectionArgs.ToArray(), null);

                if (cursor == null)
                {
                    // If this does happen, then there's a problem somewhere
                    return false;
                }
                else if (cursor != null && cursor.Count > 0)
                {
                    int idColumn = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Downloads.InterfaceConsts.Id);
                    int dispNameColumn = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Downloads.InterfaceConsts.DisplayName);
                    int addedColumn = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Downloads.InterfaceConsts.DateAdded);
                    int titleColumn = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Downloads.InterfaceConsts.Title);
                    int relativePathColumn = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Downloads.InterfaceConsts.RelativePath);

                    cursor.MoveToFirst();

                    System.Diagnostics.Debug.WriteLine("Cursor found " + cursor.Count + " rows");

                    do
                    {
                        long id = cursor.GetLong(idColumn);
                        string displayName = cursor.GetString(dispNameColumn);
                        long addedTimeInSeconds = cursor.GetLong(addedColumn);
                        //DateTime dateTime = new DateTime(TimeUnit.SECONDS.toMillis());
                        string title = cursor.GetString(titleColumn);
                        string relativePath = cursor.GetString(relativePathColumn);

                        if (displayName.Equals(filename))
                        {
                            Android.Net.Uri uri1 = Android.Provider.MediaStore.Downloads.ExternalContentUri.BuildUpon().AppendPath(id.ToString()).Build();
                            androidUri = uri1.ToString();
                            matched = true;
                            androidFileId = id;
                            var junk = contentResolver.OpenFileDescriptor(uri1, "r");
                            
                            bool isUriDocument = Android.Provider.DocumentsContract.IsDocumentUri(CrossCurrentActivity.Current.AppContext, uri1);
                            if (isUriDocument)
                            {
                                Android.OS.Bundle metadata = Android.Provider.DocumentsContract.GetDocumentMetadata(contentResolver, uri1);
                            }

                            break;
                        }
                    }
                    while (cursor.MoveToNext());

                    cursor.Close();
                    cursor.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to get data back from content resolver. Filename: " + filename);
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
        /// <param name="androidUri">URI to delete</param>
        /// <param name="androidFileId">File id to delete</param>
        /// <param name="deleteOK">Delete worked</param>
        /// <returns>true = worked w/o error or false = failed with system error</returns>
        public bool DeleteExportFile(string filename, string androidUri, long androidFileId, out bool deleteOK)
        {
            Version version = DeviceInfo.Version;

            // After version 10 (API 29) cannot write directly to Downloads directory
            // and must do it via the Mediastore API due to security changes
            if (version.Major < 10)
            {
                return DeleteAPI28AndBelow(filename, out deleteOK);
            }
            else
            {
                return DeleteAPI29AndAbove(filename, androidUri, androidFileId, out deleteOK);
            }
        }

        /// <summary>
        /// This will try to update a file.
        /// </summary>
        /// <param name="androidUri">URI to delete</param>
        /// <param name="updatedOK">Update worked</param>
        /// <returns>true = worked w/o error or false = failed with system error</returns>
        public bool UpdateExportFile(string androidUri, out bool updatedOK)
        {
            updatedOK = false;

            Version version = DeviceInfo.Version;

            // After version 10 (API 29) cannot write directly to Downloads directory.
            // Update is only supported on version 11 (API 30).
            if (version.Major > 10)
            {
                return UpdateAPI30AndAbove(androidUri, out updatedOK);
            }

            return true;
        }

        #region delete file

        private bool DeleteAPI29AndAbove(string filename, string androidUri, long androidFileId, out bool deleteOK)
        {
            deleteOK = false;

            /**
             * The `selection` is the "WHERE ..." clause of a SQL statement. It's also possible
             * to omit this by passing `null` in its place, and then all rows will be returned.
             * In this case we're using a selection based on the date the image was taken.
             *
             * Note that we've included a `?` in our selection. This stands in for a variable
             * which will be provided by the next variable.
             */
            var selection = Android.Provider.MediaStore.Downloads.InterfaceConsts.Id + " = ?";
 
            /**
             * The `selectionArgs` is a list of values that will be filled in for each `?`
             * in the `selection`.
             */
            var selectionArgs = new List<string>()
            {
                androidFileId.ToString()
            };

            try
            {
                ContentResolver contentResolver = CrossCurrentActivity.Current.AppContext.ContentResolver;
                int count = contentResolver.Delete(Android.Provider.MediaStore.Downloads.ExternalContentUri, selection, selectionArgs.ToArray());

                if (count > 0)
                {
                    deleteOK = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to get data back from content resolver. Filename: " + filename);
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                System.Diagnostics.Debug.Flush();
                return false;
            }

            return true;
        }

        private bool DeleteAPI28AndBelow(string filename, out bool deleteWorked)
        {
            string directory;

            deleteWorked = false;

#pragma warning disable CS0618 // Type or member is obsolete
            directory = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, Android.OS.Environment.DirectoryDownloads);
#pragma warning restore CS0618 // Type or member is obsolete

            string filespec = Path.Combine(directory, filename);

            FileInfo newFile = new FileInfo(filespec);
            if (newFile.Exists)
            {
                try
                {
                    newFile.Delete();  // ensures we create a new workbook
                    deleteWorked = true;

                    System.Diagnostics.Debug.WriteLine("existing file deleted: " + filespec);
                    System.Diagnostics.Debug.Flush();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to delete existing filespec: " + filename);
                    System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                    System.Diagnostics.Debug.Flush();
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region write file

        private bool WriteAPI28AndBelow(string contents, string filename)
        {
            string directory;

#pragma warning disable CS0618 // Type or member is obsolete
            directory = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, Android.OS.Environment.DirectoryDownloads);
#pragma warning restore CS0618 // Type or member is obsolete

            string filespec = Path.Combine(directory, filename);

            //FileInfo newFile = new FileInfo(filespec);
            //if (newFile.Exists)
            //{
            //    try
            //    {
            //        newFile.Delete();  // ensures we create a new workbook
            //
            //        System.Diagnostics.Debug.WriteLine("existing file deleted: " + filespec);
            //        System.Diagnostics.Debug.Flush();
            //    }
            //    catch (Exception ex)
            //    {
            //        System.Diagnostics.Debug.WriteLine("Failed to delete existing filespec: " + filename);
            //        System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
            //        System.Diagnostics.Debug.Flush();
            //        return false;
            //    }
            //}

            bool worked = DeleteAPI28AndBelow(filename, out bool deleteOK);

            if (!worked)
            {
                return false;
            }

            try
            {
                if (!deleteOK)
                {
                    return false;
                }

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

        private bool WriteAPI29AndAbove(string contents, string filename, bool setPending, out string androidUri)
        {
            androidUri = null;

            string fileNameWithoutExt = Path.ChangeExtension(filename, null);

            int fileSize = (int)contents.Length;

            ContentValues values = new ContentValues();
            ContentResolver contentResolver = CrossCurrentActivity.Current.AppContext.ContentResolver;

            values.Put(Android.Provider.MediaStore.IMediaColumns.Title, filename);
            values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, "text/plain");
            values.Put(Android.Provider.MediaStore.IMediaColumns.Size, fileSize);
            values.Put(Android.Provider.MediaStore.Downloads.InterfaceConsts.DisplayName, fileNameWithoutExt);

            // Note: This file creation doesn't need this, as this isn't downloading the file
            //       from the internet; however, it's here for an example purposes.
            if (setPending)
            {
                values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 1);
            }

            Android.Net.Uri newUri;
            System.IO.Stream saveStream;

            try
            {
                newUri = contentResolver.Insert(Android.Provider.MediaStore.Downloads.ExternalContentUri, values);
                androidUri = newUri.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to get data back from content resolver. Filename: " + filename);
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                System.Diagnostics.Debug.Flush();
                return false;
            }

            try
            {
                saveStream = contentResolver.OpenOutputStream(newUri);

                using (StreamWriter writer = new(saveStream))
                {
                    writer.WriteAsync(contents);
                    writer.Close();
                    writer.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed file write: " + filename);
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                System.Diagnostics.Debug.Flush();
                return false;
            }

            return true;
        }

        #endregion

        #region update file

        private bool UpdateAPI30AndAbove(string androidUri, out bool updateOK)
        {
            updateOK = false;

            ContentValues values = new ContentValues();

            values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 0);

            try
            {
                ContentResolver contentResolver = CrossCurrentActivity.Current.AppContext.ContentResolver;

                Android.Net.Uri updateUri = Android.Net.Uri.Parse(androidUri);
                int count = contentResolver.Update(updateUri, values, null, null);

                if (count > 0)
                {
                    updateOK = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to get data back from content resolver.");
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                System.Diagnostics.Debug.Flush();
                return false;
            }

            return true;
        }

        #endregion
    }
}
