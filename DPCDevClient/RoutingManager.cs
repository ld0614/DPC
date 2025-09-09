using DPCLibrary.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DPCDevClient
{
    internal class RoutingManager
    {
        private IntPtr RtmRegHandle;
        private RTM_REGN_PROFILE? RegnProfile;

        public void Register()
        {
            IntPtr rtmRegHandle;
            RTM_ENTITY_INFO EntityInfo;
            RTM_REGN_PROFILE regnProfile;
            RasError dwRet = RasError.SUCCESS;

            EntityInfo.RtmInstanceId = 0;
            EntityInfo.AddressFamily = IPAddressFamily.IPv4;
            EntityInfo.EntityId.EntityProtocolId = ProtocolId.PROTO_IP_RIP;
            EntityInfo.EntityId.EntityInstanceId = PROTOCOL_ID(ProtocolType.PROTO_TYPE_UCAST, ProtocolVendor.PROTO_FROM_PROTO_ID, ProtocolId.PROTO_IP_RIP);

            // Register the new entity
            dwRet = RtmRegisterEntity(ref EntityInfo, IntPtr.Zero, IntPtr.Zero, false, out regnProfile, out rtmRegHandle);
            if (dwRet != RasError.SUCCESS)
                throw new Win32Exception(dwRet.ToString());

            RtmRegHandle = rtmRegHandle;
            RegnProfile = regnProfile;
        }

        public void DeregisterFromRoutingManager(IntPtr RtmRegHandle)
        {
            if (RtmRegHandle == IntPtr.Zero)
            {
                //Routing Manager has already been deregistered
                return;
            }
            // Clean-up: Deregister the new entity
            RasError dwRet = RtmDeregisterEntity(RtmRegHandle);
            if (dwRet != RasError.SUCCESS)
                throw new Win32Exception(dwRet.ToString());
            RtmRegHandle = IntPtr.Zero;
            RegnProfile = null;
        }

        public void EnumerateRoutes()
        {
            if (RegnProfile == null || RtmRegHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Routing Manager not registered");
            }
            uint MaxHandles = ((RTM_REGN_PROFILE)RegnProfile).MaxHandlesInEnum;

            RouteHandles = _alloca(MaxHandles * sizeof(HANDLE));

            // Do a "route enumeration" over the whole table
            // by passing a NULL DestHandle in this function.

            IntPtr DestHandle = IntPtr.Zero; // Give a valid handle to enumerate over a particular destination

            RasError Status = RtmCreateRouteEnum(RtmRegHandle,
                                        DestHandle,
                                        RTM_VIEW_SET.RTM_VIEW_MASK_UCAST | RTM_VIEW_SET.RTM_VIEW_MASK_MCAST,
                                        RTM_ENUM_FLAGS.RTM_ENUM_OWN_ROUTES, // Get only your own routes
                                        null,
                                        0,
                                        null,
                                        0,
                                        &EnumHandle2);
            if (Status == RasError.SUCCESS)
            {
                do
                {
                    uint NumHandles = MaxHandles;

                    Status = RtmGetEnumRoutes(RtmRegHandle,
                                              EnumHandle2,
                                              &NumHandles,
                                              RouteHandles);

                    for (int k = 0; k < NumHandles; k++)
                    {
                        Console.WriteLine("Route %d: %p\n", l++, RouteHandles[k]);

                        // Get route information using the route's handle
                        Status = RtmGetRouteInfo(...RouteHandles[k]...);

                        if (Status == RasError.SUCCESS)
                        {
                            // Do whatever you want with the route info
                            //...

                            // Release the route information once you are done
                            RtmReleaseRouteInfo(...);
                        }
                    }

                    RtmReleaseRoutes(RtmRegHandle, NumHandles, RouteHandles);
                }
                while (Status == RasError.SUCCESS);

                // Close the enumeration and release its resources
                RtmDeleteEnumHandle(RtmRegHandle, EnumHandle2);
            }
        }

        static ulong PROTOCOL_ID(ProtocolType Type, ProtocolVendor VendorId, ProtocolId ProtocolId)
        {
            return (((uint)Type & 0x03) << 30) |
                   (((uint)VendorId & 0x3FFF) << 16) |
                   ((ulong)ProtocolId & 0xFFFF);
        }

        [DllImport("rtm.dll", SetLastError = true, CharSet = CharSet.Auto, ThrowOnUnmappableChar = true)]
        internal static extern RasError RtmRegisterEntity(
            [In] ref RTM_ENTITY_INFO RtmEntityInfo,
            [In] IntPtr ExportMethods,
            [In] IntPtr EventCallback,
            [In] bool ReserveOpaquePointer,
            [Out] out RTM_REGN_PROFILE RtmRegProfile,
            [Out] out IntPtr RtmRegHandle
        );

        [DllImport("rtm.dll", SetLastError = true, CharSet = CharSet.Auto, ThrowOnUnmappableChar = true)]
        internal static extern RasError RtmGetEnumRoutes(
  [In] IntPtr RtmRegHandle,
  [In] IntPtr EnumHandle,
  [In, Out] uint NumRoutes,
  [Out] IntPtr RouteHandles
);

        [DllImport("rtm.dll", SetLastError = true, CharSet = CharSet.Auto, ThrowOnUnmappableChar = true)]
        internal static extern RasError RtmCreateRouteEnum(
  [In] IntPtr RtmRegHandle,
  [In] IntPtr DestHandle,
  [In] RTM_VIEW_SET TargetViews,
  [In] RTM_ENUM_FLAGS EnumFlags,
  [In] ref RTM_NET_ADDRESS StartDest,
  [In] RTM_MATCH_FLAGS MatchingFlags,
  [In] ref RTM_ROUTE_INFO CriteriaRoute,
  [In] ulong CriteriaInterface,
  [Out] IntPtr RtmEnumHandle
);

        [DllImport("rtm.dll", SetLastError = true, CharSet = CharSet.Auto, ThrowOnUnmappableChar = true)]
        internal static extern RasError RtmDeregisterEntity(
            [In] IntPtr RtmRegHandle
        );
    }
}
