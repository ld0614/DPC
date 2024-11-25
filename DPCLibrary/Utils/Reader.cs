using System;
using System.Threading;

namespace DPCLibrary.Utils
{
    public static class Reader
    {
        private static readonly Thread InputThread = InitaliseInputThread();
        private static readonly AutoResetEvent getInput = new AutoResetEvent(false);
        private static readonly AutoResetEvent gotInput = new AutoResetEvent(false);
        private static string input;

        static Thread InitaliseInputThread()
        {
            Thread inputThread = new Thread(ReadValue)
            {
                IsBackground = true
            };
            inputThread.Start();
            return inputThread;
        }

        private static void ReadValue()
        {
            while (true)
            {
                getInput.WaitOne();
                input = Console.ReadLine();
                gotInput.Set();
            }
        }

        // omit the parameter to read a line without a timeout
        public static string ReadLine(int timeOutMillisecs)
        {
            getInput.Set();
            bool success = gotInput.WaitOne(timeOutMillisecs);
            if (success)
                return input;
            else
                throw new TimeoutException("User did not provide input within the time limit.");
        }
    }
}
