using DPCLibrary.Models;
using System;
using System.IO;

namespace DPCLibrary.Utils
{
    internal class AccessFile
    {
        public static RemoveProfileResult DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    return new RemoveProfileResult(); //Return true
                } catch (Exception e)
                {
                    return new RemoveProfileResult(path, e); //Return false with the attached error code
                }
            }

            return new RemoveProfileResult(false); //Return false as it was not deleted but didn't error
        }
    }
}
