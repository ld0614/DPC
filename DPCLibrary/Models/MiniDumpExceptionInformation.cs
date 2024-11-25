using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DPCLibrary.Models
{
    //typedef struct _MINIDUMP_EXCEPTION_INFORMATION {
    //    DWORD ThreadId;
    //    PEXCEPTION_POINTERS ExceptionPointers;
    //    BOOL ClientPointers;
    //} MINIDUMP_EXCEPTION_INFORMATION, *PMINIDUMP_EXCEPTION_INFORMATION;
    [StructLayout(LayoutKind.Sequential, Pack = 4)]  // Pack=4 is important! So it works also for x64!
    struct MiniDumpExceptionInformation : IEquatable<MiniDumpExceptionInformation>
    {
        public uint ThreadId;
        public IntPtr ExceptionPointers;
        [MarshalAs(UnmanagedType.Bool)]
        public bool ClientPointers;

        public override bool Equals(object obj)
        {
            return obj is MiniDumpExceptionInformation information && Equals(information);
        }

        public bool Equals(MiniDumpExceptionInformation other)
        {
            return ThreadId == other.ThreadId &&
                   EqualityComparer<IntPtr>.Default.Equals(ExceptionPointers, other.ExceptionPointers) &&
                   ClientPointers == other.ClientPointers;
        }

        public override int GetHashCode()
        {
            int hashCode = -1059261325;
            hashCode = hashCode * -1521134295 + ThreadId.GetHashCode();
            hashCode = hashCode * -1521134295 + ExceptionPointers.GetHashCode();
            hashCode = hashCode * -1521134295 + ClientPointers.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(MiniDumpExceptionInformation left, MiniDumpExceptionInformation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MiniDumpExceptionInformation left, MiniDumpExceptionInformation right)
        {
            return !(left == right);
        }
    }
}
