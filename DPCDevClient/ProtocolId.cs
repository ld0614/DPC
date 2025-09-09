using System;

namespace DPCDevClient
{
    [Flags]
    public enum ProtocolId : ulong
    {
        PROTO_IP_OTHER=   1,
        PROTO_IP_LOCAL=   2,
        PROTO_IP_NETMGMT= 3,
        PROTO_IP_ICMP=    4,
        PROTO_IP_EGP=     5,
        PROTO_IP_GGP=     6,
        PROTO_IP_HELLO=   7,
        PROTO_IP_RIP=     8,
        PROTO_IP_IS_IS=   9,
        PROTO_IP_ES_IS=  10,
        PROTO_IP_CISCO=  11,
        PROTO_IP_BBN=    12,
        PROTO_IP_OSPF=   13,
        PROTO_IP_BGP=    14,
        PROTO_IP_IDPR=   15,
        PROTO_IP_EIGRP=  16,
        PROTO_IP_DVMRP=  17,
        PROTO_IP_RPL=    18,
        PROTO_IP_DHCP=   19,

 PROTO_IP_MSDP    =    9,
 PROTO_IP_IGMP   =    10,
 PROTO_IP_BGMP   =    11,

        //
        // The IPRTRMGR_PID is 10000 // 0x00002710
        //

 PROTO_IP_VRRP           =    112,
 PROTO_IP_BOOTP         =     9999 ,   // 0x0000270F

        //included for DHCPv6 Relay Agent
 PROTO_IPV6_DHCP 		=	999	,    // 0x000003E7

 PROTO_IP_NT_AUTOSTATIC =     10002  , // 0x00002712
 PROTO_IP_DNS_PROXY      =    10003  , // 0x00002713
 PROTO_IP_DHCP_ALLOCATOR =    10004  , // 0x00002714
 PROTO_IP_NAT          =      10005  , // 0x00002715
 PROTO_IP_NT_STATIC     =     10006  , // 0x00002716
 PROTO_IP_NT_STATIC_NON_DOD=  10007  , // 0x00002717
 PROTO_IP_DIFFSERV       =    10008  , // 0x00002718
 PROTO_IP_MGM         =       10009   ,// 0x00002719
 PROTO_IP_ALG          =      10010  , // 0x0000271A
 PROTO_IP_H323        =       10011 ,  // 0x0000271B
 PROTO_IP_FTP         =       10012,   // 0x0000271C
 PROTO_IP_DTP         =       10013   // 0x0000271D
    }
}