using DPCLibrary.Enums;
using DPCLibrary.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace DPCLibrary.Utils
{
    public static class AccessRasApi
    {
        private static readonly object profileActionLock = new object();

        public delegate void RasCallback();
        public static void Start(ConnectionEvent connEvent, CancellationToken token, RasCallback callback)
        {
            using (EventWaitHandle RasCallback = new EventWaitHandle(false, EventResetMode.ManualReset, null))
            {
                RasError result = NativeMethods.RasConnectionNotification(RasConstants.INVALID_HANDLE_VALUE, RasCallback.SafeWaitHandle.DangerousGetHandle(), connEvent);

                if (result != RasError.SUCCESS)
                {
                    throw new Win32Exception(result.ToString());
                }

                while (!token.IsCancellationRequested)
                {
                    if (RasCallback.WaitOne(500))
                    {
                        callback();
                        RasCallback.Reset();
                    }
                }
            }
        }

        public static RemoveProfileResult RemoveProfile(ProfileInfo profile)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                try
                {
                    RasError nRet = RasError.PENDING;
                    int timeoutInSeconds = 30;
                    DateTime startTime = DateTime.UtcNow;
                    do
                    {
                        nRet = NativeMethods.RasDeleteEntry(profile.PBKPath, profile.ProfileName);
                        if (nRet != RasError.SUCCESS && nRet != RasError.ERROR_CANNOT_DELETE)
                        {
                            throw new Win32Exception(nRet.ToString());
                        }
                        else if (nRet == RasError.ERROR_CANNOT_DELETE)
                        {
                            bool hangUpResult = HangUpConnection(profile);
                            if (!hangUpResult)
                            {
                                //Couldn't hang up the connection so likely in a connecting state, wait for connection to fail before attempting to delete again
                                Thread.Sleep(2000); //Sleep for 2 seconds
                            }
                        }
                    } while (nRet != RasError.SUCCESS && (DateTime.UtcNow - startTime).TotalSeconds < timeoutInSeconds);

                    if (nRet == RasError.SUCCESS)
                    {
                        return new RemoveProfileResult(); //Return true
                    }
                    else
                    {
                        throw new Win32Exception(nRet.ToString());
                    }
                }
                catch (Exception e)
                {
                    return new RemoveProfileResult(e); //Return false
                }
            }
        }

        public static bool ValidateProfileName(string profileName)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                RasError returnValue = NativeMethods.RasValidateEntryName(null, profileName);

                if (returnValue == RasError.SUCCESS || returnValue == RasError.ERROR_ALREADY_EXISTS)
                {
                    return true;
                }

                return false;
            }
        }

        public static IList<string> ListConnectedProfiles()
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                IList<RasConnection> profiles = ConnectedProfiles();

                IList<string> profileNameList = new List<string>();
                foreach (RasConnection connection in profiles)
                {
                    profileNameList.Add(connection.szEntryName);
                }

                return profileNameList;
            }
        }

        public static IList<RasConnection> ConnectedProfiles()
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                // create 1 item array.
                RasConnection[] connections = new RasConnection[1];
                connections[0].dwSize = Marshal.SizeOf(typeof(RasConnection));
                //Get entries count
                int cb = Marshal.SizeOf(typeof(RasConnection));
                RasError nRet = NativeMethods.RasEnumConnections(connections, ref cb, out int connectionsCount);
                if (nRet != RasError.SUCCESS && nRet != RasError.ERROR_BUFFER_TOO_SMALL)
                    throw new Win32Exception((int)nRet);
                if (connectionsCount == 0)
                {
                    return new List<RasConnection>();
                }

                // create array with specified entries number
                connections = new RasConnection[connectionsCount];
                for (int i = 0; i < connections.Length; i++)
                {
                    connections[i].dwSize = Marshal.SizeOf(typeof(RasConnection));
                }
                nRet = NativeMethods.RasEnumConnections(connections, ref cb, out _);
                if (nRet != RasError.SUCCESS)
                    throw new Win32Exception(nRet.ToString());

                return connections;
            }
        }

        public static IntPtr DialConnection(ProfileInfo profile, bool throwOnFailure)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                RasDialParams dialParams = new RasDialParams()
                {
                    szEntryName = profile.ProfileName,
                    dwSize = Marshal.SizeOf(typeof(RasDialParams))
                };

                RasError EapIdentityResult = NativeMethods.RasGetEapUserIdentity(profile.PBKPath, profile.ProfileName, RasEapDialFlags.RASEAPF_NonInteractive, IntPtr.Zero, out RasEapUserIdentity userIdentity);
                RasError dialParamsResult = NativeMethods.RasGetEntryDialParams(profile.PBKPath, ref dialParams, out bool success); //Retrieve Password data as required

                IntPtr connectionHandle = IntPtr.Zero;
                RasError rasDialResult = NativeMethods.RasDial(null, profile.PBKPath, ref dialParams, 2, IntPtr.Zero, out connectionHandle);
                NativeMethods.RasFreeEapUserIdentity(userIdentity); //Clean up user Identity Memory
                if (throwOnFailure && rasDialResult != RasError.SUCCESS)
                {
                    throw new Win32Exception(rasDialResult.ToString());
                }

                return connectionHandle;
            }
        }

        public static RasConnStatus GetRasConnectionStatus(ProfileInfo profile)
        {
            RasConnStatus status = new RasConnStatus()
            {
                dwSize = Marshal.SizeOf(typeof(RasConnStatus))
            };

            IntPtr connectionHandle = DialConnection(profile, false);
            if (connectionHandle != IntPtr.Zero)
            {
                RasError nRet = NativeMethods.RasGetConnectStatus(connectionHandle, ref status);
                if (nRet == RasError.SUCCESS)
                {
                    return status;
                }
                status.dwError = nRet;
            }
            else
            {
                status.dwError = RasError.ERROR_INVALID_HANDLE;
            }

            status.rasConnState = RasConnState.RASCS_Disconnected;

            return status;
        }

        public static bool HangUpConnection(ProfileInfo profile)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                IList<RasConnection> profiles = ConnectedProfiles();
                IList<IntPtr> connectionHandles = profiles.Where(c => c.szEntryName == profile.ProfileName && c.hrasconn != IntPtr.Zero).Select(c => c.hrasconn).ToList();
                if (connectionHandles.Count == 0) //Connection may not be fully initialized and/or stuck
                {
                    IntPtr connectionHandle = DialConnection(profile, false);
                    if (connectionHandle != IntPtr.Zero)
                    {
                        connectionHandles.Add(connectionHandle);
                    }
                }

                if (connectionHandles.Count == 0)
                {
                    return false;
                }

                foreach (IntPtr connectionHandle in connectionHandles)
                {
                    HangUpConnection(connectionHandle);
                }

                return true;
            }
        }

        public static void HangUpConnection(IntPtr rasconn)
        {
            int timeoutInSeconds = 120; //2 mins
            DateTime startTime = DateTime.UtcNow;

            if (rasconn == IntPtr.Zero)
            {
                throw new Win32Exception(RasError.ERROR_INVALID_HANDLE.ToString());
            }

            RasConnStatus status = new RasConnStatus()
            {
                dwSize = Marshal.SizeOf(typeof(RasConnection))
            };

            RasError nRet = NativeMethods.RasHangUp(rasconn);
            if (nRet == RasError.ERROR_NO_CONNECTION) return; //Hang Up already complete
            if (nRet != RasError.SUCCESS)
                throw new Win32Exception(nRet.ToString());

            do
            {
                nRet = NativeMethods.RasGetConnectStatus(rasconn, ref status);
                //Loop until the connection fails as per documentation https://learn.microsoft.com/en-us/windows/win32/api/ras/nf-ras-rashangupa
            } while (nRet == RasError.SUCCESS && (DateTime.UtcNow - startTime).TotalSeconds < timeoutInSeconds);

            if (nRet == RasError.SUCCESS && status.rasConnState != RasConnState.RASCS_Disconnected)
            {
                throw new TimeoutException("Unable to disconnect profile");
            }
        }

        public static IList<string> ListProfilesFromDirectory(string pbkPath)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                if (string.IsNullOrWhiteSpace(pbkPath)) return new List<string>();
                // create 1 item array.
                RasEntryName[] entries = new RasEntryName[1];
                entries[0].dwSize = Marshal.SizeOf(typeof(RasEntryName));
                //Get entries count
                int cb = Marshal.SizeOf(typeof(RasEntryName));
                RasError nRet = NativeMethods.RasEnumEntries(IntPtr.Zero, pbkPath, entries, ref cb, out int entryCount);
                if (nRet != RasError.SUCCESS && nRet != RasError.ERROR_BUFFER_TOO_SMALL)
                    throw new Win32Exception((int)nRet);
                if (entryCount == 0)
                {
                    return new List<string>();
                }

                if (entryCount > 1)
                {
                    // create array with specified entries number
                    entries = new RasEntryName[entryCount];
                    for (int i = 0; i < entries.Length; i++)
                    {
                        entries[i].dwSize = Marshal.SizeOf(typeof(RasEntryName));
                    }
                    nRet = NativeMethods.RasEnumEntries(IntPtr.Zero, pbkPath, entries, ref cb, out _);
                    if (nRet != RasError.SUCCESS)
                        throw new Win32Exception(nRet.ToString());
                }

                List<string> profileNameList = new List<string>();
                foreach (RasEntryName profileEntry in entries)
                {
                    profileNameList.Add(profileEntry.szEntryName);
                }

                return profileNameList;
            }
        }

        public static RasEntry GetVPNProperties(string ConnectionName)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                RasEntry connectionDetails = new RasEntry
                {
                    dwSize = (uint)Marshal.SizeOf(typeof(RasEntry))
                };

                int cb = Marshal.SizeOf(typeof(RasEntry));
                RasError nRet = NativeMethods.RasGetEntryProperties(null, ConnectionName, ref connectionDetails, ref cb, IntPtr.Zero, IntPtr.Zero);
                if (nRet != RasError.SUCCESS)
                    throw new Win32Exception(nRet.ToString());

                return connectionDetails;
            }
        }

        public static void SetVPNProperties(string connectionName, RasEntry updatedProperties)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                updatedProperties.dwSize = (uint)Marshal.SizeOf(updatedProperties);

                RasError nRet = NativeMethods.RasSetEntryProperties(null, connectionName, ref updatedProperties, Marshal.SizeOf(updatedProperties), IntPtr.Zero, 0);
                if (nRet != RasError.SUCCESS)
                    throw new Win32Exception(nRet.ToString());
            }
        }

        public static bool GetDisableRasCredStatus(string connectionName)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                RasEntry props = GetVPNProperties(connectionName);
                return props.dwfOptions2.HasFlag(RasOptions2.RASEO2_DontUseRasCredentials);
            }
        }

        public static bool SetDisableRasCredFlag(string connectionName, bool State)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                RasEntry props = GetVPNProperties(connectionName);
                if (props.dwfOptions2.HasFlag(RasOptions2.RASEO2_DontUseRasCredentials) != State)
                {
                    if (State)
                    {
                        AddOptions2Flag(ref props, RasOptions2.RASEO2_DontUseRasCredentials);
                    }
                    else
                    {
                        RemoveOptions2Flag(ref props, RasOptions2.RASEO2_DontUseRasCredentials);
                    }
                    SetVPNProperties(connectionName, props);
                    return true;
                }

                return false;
            }
        }

        public static uint GetNetworkOutageTime(string connectionName)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                RasEntry props = GetVPNProperties(connectionName);
                if (props.dwfOptions2.HasFlag(RasOptions2.RASEO2_DisableMobility))
                {
                    //Disable Mobility = 1 means NetworkOutageTime must be 0 or is ignored (meaning effectively 0)
                    return 0;
                }
                else
                {
                    return props.dwNetworkOutageTime;
                }
            }
        }

        public static bool SetNetworkOutageTime(string connectionName, uint networkOutageTime)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                RasEntry props = GetVPNProperties(connectionName);
                if (GetNetworkOutageTime(connectionName) != networkOutageTime)
                {
                    if (networkOutageTime != 0)
                    {
                        RemoveOptions2Flag(ref props, RasOptions2.RASEO2_DisableMobility);
                        props.dwNetworkOutageTime = networkOutageTime;
                    }
                    else
                    {
                        AddOptions2Flag(ref props, RasOptions2.RASEO2_DisableMobility);
                        props.dwNetworkOutageTime = 0;
                    }
                    SetVPNProperties(connectionName, props);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static VPNStrategy GetVPNStrategy(string connectionName)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                RasEntry props = GetVPNProperties(connectionName);
                return props.dwVpnStrategy;
            }
        }

        public static bool SetVPNStrategy(string connectionName, VPNStrategy updatedStrategy)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                if (updatedStrategy == VPNStrategy.GREOnly || updatedStrategy == VPNStrategy.ProtocolList)
                {
                    //Setting these values breaks the WMI interface used to retrieve profiles from the OS for comparison
                    throw new InvalidOperationException(updatedStrategy + " is not supported in DPC due to bugs in Windows");
                }
                RasEntry props = GetVPNProperties(connectionName);
                if (props.dwVpnStrategy != updatedStrategy)
                {
                    props.dwVpnStrategy = updatedStrategy;
                    SetVPNProperties(connectionName, props);
                    return true;
                }

                return false;
            }
        }

        public static uint GetVPNIPv4Metric(string connectionName)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                RasEntry props = GetVPNProperties(connectionName);
                return props.dwIPv4InterfaceMetric;
            }
        }

        public static uint GetVPNIPv6Metric(string connectionName)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                RasEntry props = GetVPNProperties(connectionName);
                return props.dwIPv6InterfaceMetric;
            }
        }

        public static bool SetVPNMetric(string connectionName, uint? ipv4Metric)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                uint? ipv6Metric = ipv4Metric; //Treat as a single value but set on both unless we need expose separately
                bool update = false;
                RasEntry props = GetVPNProperties(connectionName);
                if (ipv4Metric != null)
                {
                    if (ipv4Metric == 0 && props.dwIPv4InterfaceMetric != 0)
                    {
                        //Remove Custom Metric
                        props.dwIPv4InterfaceMetric = 0;
                        RemoveOptions2Flag(ref props, RasOptions2.RASEO2_IPv4ExplicitMetric);
                        update = true;
                    }
                    else if (props.dwIPv4InterfaceMetric != ipv4Metric)
                    {
                        //Add Custom Metric
                        props.dwIPv4InterfaceMetric = (uint)ipv4Metric;
                        AddOptions2Flag(ref props, RasOptions2.RASEO2_IPv4ExplicitMetric);
                        update = true;
                    }
                }

                if (ipv6Metric != null)
                {
                    if (ipv6Metric == 0 && props.dwIPv6InterfaceMetric != 0)
                    {
                        //Remove Custom Metric
                        props.dwIPv6InterfaceMetric = 0;
                        RemoveOptions2Flag(ref props, RasOptions2.RASEO2_IPv6ExplicitMetric);
                        update = true;
                    }
                    else if (props.dwIPv6InterfaceMetric != ipv6Metric)
                    {
                        //Add Custom Metric
                        props.dwIPv6InterfaceMetric = (uint)ipv6Metric;
                        AddOptions2Flag(ref props, RasOptions2.RASEO2_IPv6ExplicitMetric);
                        update = true;
                    }
                }

                if (update)
                {
                    SetVPNProperties(connectionName, props);
                    return true;
                }

                return false;
            }
        }

        internal static void RemoveOptions2Flag(ref RasEntry entry, RasOptions2 flag)
        {
            entry.dwfOptions2 &= ~flag; //Remove the explicit metric flag
        }

        internal static void AddOptions2Flag(ref RasEntry entry, RasOptions2 flag)
        {
            entry.dwfOptions2 |= flag; //Remove the explicit metric flag
        }
    }
}