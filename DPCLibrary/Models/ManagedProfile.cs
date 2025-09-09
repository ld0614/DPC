using DPCLibrary.Enums;
using DPCLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Xml;

namespace DPCLibrary.Models
{
    public class ManagedProfile : IEquatable<ManagedProfile>
    {
        private string profileXML;
        public string ProfileName { get; set; }
        public string OldProfileName { get; set; }
        public ProfileType ProfileType { get; set; }
        public string ProfileXML
        {
            get => profileXML;
            set
            {
                profileXML = value;
                if (!string.IsNullOrWhiteSpace(profileXML))
                {
                    ProfileObj = new CSPProfile(value, ProfileName); //Automatically populate the ProfileObj when the profileXML is set
                }
                else
                {
                    ProfileObj = null; //Reset in the event we are updating the object
                }
            }
        } //Only set when profile has been generated within DPC, when reading from OS ProfileObj is used instead
        public uint Metric { get; set; }
        public uint MTU { get; set; }
        public VPNStrategy VPNStrategy { get; set; }
        public bool DisableRasCredentials { get; set; }
        public uint NetworkOutageTime { get; set; }
        public bool ProfileDeployed { get; set; }
        public IList<string> MachineEKU { get; set; }
        public IList<string> ProxyExcludeList { get; set; }
        public bool ProxyBypassForLocal { get; set; }
        public VPNProfile ProfileObj { get; set; }

        public Dictionary<string, string> RouteListIPv6;
        public Dictionary<string, string> RouteExcludeListIPv6;

        public override bool Equals(object obj)
        {
            return Equals(obj as ManagedProfile);
        }

        //Don't Check Profile Deployed attribute as this won't be set on incoming profiles
        public bool Equals(ManagedProfile other)
        {
            //Use Existing Profile Objects if they already exist, otherwise generate them based on the XML
            VPNProfile sourceProfile = ProfileObj ?? new CSPProfile(ProfileXML, ProfileName);
            VPNProfile compareProfile = other.ProfileObj ?? new CSPProfile(other.ProfileXML, other.ProfileName);

            return other != null &&
                   ProfileName == other.ProfileName &&
                   ProfileType == other.ProfileType &&
                   VPNProfile.CompareProfiles(sourceProfile, compareProfile) &&
                   Metric == other.Metric &&
                   MTU == other.MTU &&
                   VPNStrategy == other.VPNStrategy &&
                   DisableRasCredentials == other.DisableRasCredentials &&
                   NetworkOutageTime == other.NetworkOutageTime &&
                   MachineEKU == other.MachineEKU &&
                   ProxyExcludeList.EqualsArray(other.ProxyExcludeList);
        }

        public override int GetHashCode()
        {
            int hashCode = 2103379817;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProfileName);
            hashCode = hashCode * -1521134295 + ProfileType.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProfileXML);
            hashCode = hashCode * -1521134295 + Metric.GetHashCode();
            hashCode = hashCode * -1521134295 + MTU.GetHashCode();
            hashCode = hashCode * -1521134295 + VPNStrategy.GetHashCode();
            hashCode = hashCode * -1521134295 + DisableRasCredentials.GetHashCode();
            hashCode = hashCode * -1521134295 + NetworkOutageTime.GetHashCode();
            hashCode = hashCode * -1521134295 + MachineEKU.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(ProxyExcludeList);
            return hashCode;
        }

        public void RemoveWhiteSpace()
        {
            if (!string.IsNullOrWhiteSpace(ProfileXML))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ProfileXML);
                doc.PreserveWhitespace = false;
                ProfileXML = doc.InnerXml;
                ProfileXML = ProfileXML.Trim();
            }
        }

        public static bool operator ==(ManagedProfile left, ManagedProfile right)
        {
            return EqualityComparer<ManagedProfile>.Default.Equals(left, right);
        }

        public static bool operator !=(ManagedProfile left, ManagedProfile right)
        {
            return !(left == right);
        }
    }
}
