using DPCLibrary.Enums;
using DPCLibrary.Models;
using System;
using System.Runtime.InteropServices;

namespace DPCLibrary.Utils
{
    internal static class NativeMethods
    {
        //Get a Callback when a Profile is connected or disconnected
        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasConnectionNotification(
            IntPtr hRasConn,
            IntPtr Handle,
            ConnectionEvent ConnType
        );

        //List all active VPN Connections
        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasEnumConnections(
            [In, Out] RasConnection[] rasconn,
            [In, Out] ref int cb,
            [Out] out int connections
        );

        //Delete Specified VPN Profile
        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasDeleteEntry(
            [In] string lpszPhonebook,
            [In] string lpszEntry
        );

        //List all Entries in a PBK File
        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasEnumEntries(
            [In] IntPtr reserved,
            [In] string lpszPhonebook,
            [In, Out] RasEntryName[] lprasentryname,
            [In, Out] ref int lpcb,
            [Out] out int lpcEntries
        );

        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasGetEntryDialParams(
          [In] string lpszPhonebook,
          [In, Out] ref RasDialParams rasDialParams,
          [Out] out bool passwordRetrieved
        );

        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasGetEapUserIdentity(
          [In] string lpszPhonebook,
          [In] string lpszEntry,
          [In] RasEapDialFlags eapDialFlags,
          [In] IntPtr hwnd,
          [Out] out RasEapUserIdentity userDetails
        );

        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern void RasFreeEapUserIdentity(
          [In] RasEapUserIdentity userDetails
        );

        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasDial(
          [In] RasDialExtensions rasDialExtensions,
          [In] string lpszPhonebook,
          [In, Out] ref RasDialParams rasDialParams,
          [In] uint notifierVersion,
          [In] IntPtr notifierFunction, //Should be RasDialFunc2
          [Out] out IntPtr hrasconn
        );

        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasHangUp(
            [In] IntPtr hrasconn
        );

        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasGetConnectStatus(
            [In] IntPtr hrasconn,
            [In, Out] ref RasConnStatus status
        );

        //Validate Profile Names
        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasValidateEntryName(
            [In] string lpszPhonebook,
            [In] string lpszEntry
        );

        //Get Details about a specific VPN Profile
        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasGetEntryProperties(
            [In] string lpszPhonebook,
            [In] string lpszEntry,
            [In, Out] ref RasEntry lpRasEntry,
            [In, Out] ref int dwEntryInfoSize,
            [Out] IntPtr lpbDeviceInfo,
            [In, Out] IntPtr dwDeviceInfoSize
        );

        //Update a specific VPN Profile Details
        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError RasSetEntryProperties(
            [In] string lpszPhonebook,
            [In] string lpszEntry,
            [In] ref RasEntry lpRasEntry,
            [In] int dwEntryInfoSize,
            [In] IntPtr lpbDeviceInfo,
            [In] int dwDeviceInfoSize
        );

        //BOOL
        //WINAPI
        //MiniDumpWriteDump(
        //    __in HANDLE hProcess,
        //    __in DWORD ProcessId,
        //    __in HANDLE hFile,
        //    __in MINIDUMP_TYPE DumpType,
        //    __in_opt PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam,
        //    __in_opt PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam,
        //    __in_opt PMINIDUMP_CALLBACK_INFORMATION CallbackParam
        //    );
        [DllImport("dbghelp.dll",
          EntryPoint = "MiniDumpWriteDump",
          CallingConvention = CallingConvention.StdCall,
          CharSet = CharSet.Unicode,
          ThrowOnUnmappableChar = true,
          ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool MiniDumpWriteDump(
          IntPtr hProcess,
          uint processId,
          IntPtr hFile,
          uint dumpType,
          ref MiniDumpExceptionInformation expParam,
          IntPtr userStreamParam,
          IntPtr callbackParam);

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
        internal static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcess", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcessId", ExactSpelling = true)]
        internal static extern uint GetCurrentProcessId();

        //Get a system notification when group policy has updated
        [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern bool RegisterGPNotification(
            [In] IntPtr Handle,
            [In] bool bMachine
        );

        //Get the last error set in the thread
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        internal static extern RasError GetLastError();
    }
}
