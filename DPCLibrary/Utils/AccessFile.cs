using DPCLibrary.Models;
using System;
using System.IO;

namespace DPCLibrary.Utils
{
    public class AccessFile
    {
        public static RemoveProfileResult DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    return new RemoveProfileResult(); //Return true
                }
                catch (Exception e)
                {
                    return new RemoveProfileResult(path, e); //Return false with the attached error code
                }
            }

            return new RemoveProfileResult(false); //Return false as it was not deleted but didn't error
        }

        public static long GetFileSize(string path)
        {
            long fileSizeInBytes = -1; //File does not exist
            if (File.Exists(path))
            {
                // Create a FileInfo object
                FileInfo fileInfo = new FileInfo(path);

                // Get the file size in bytes
                fileSizeInBytes = fileInfo.Length;
            }

            return fileSizeInBytes;
        }
    }
}
