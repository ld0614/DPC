using System;

namespace DPCLibrary.Models
{
    public interface IPAddress : IEquatable<IPAddress>
    {
        void LoadFromPBKHex(string pbkHex);

        void LoadFromString(string address);
    }
}