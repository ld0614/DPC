namespace DPCLibrary.Enums
{
    //Taken from https://docs.microsoft.com/en-us/previous-versions/windows/desktop/legacy/aa377274(v=vs.85)
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\ras.h
    public enum VPNStrategy
    {
        Default = 0, //RAS dials Internet Key Exchange version 2 (IKEv2) first. If IKEv2 fails, RAS subsequently attempts the following protocols: Secure Socket Tunneling Protocol (SSTP), Point-to-Point Tunneling Protocol(PPTP), and Layer 2 Tunneling Protocol (L2TP). Whichever protocol succeeds is tried first in subsequent dials for this entry.
        PptpOnly = 1, //RAS dials only PPTP.
        PptpFirst = 2, //RAS always dials PPTP first followed by Internet Key Exchange version (IKEv2), Secure Socket Tunneling Protocol (SSTP), then Layer 2 Tunneling Protocol (L2TP)
        L2tpOnly = 3, //RAS dials only L2TP.
        L2tpFirst = 4, //RAS always dials L2TP first followed by IKEv2, SSTP, then PPTP
        SstpOnly = 5, //RAS dials only SSTP.
        SstpFirst = 6, //RAS always dials SSTP first followed by IKEv2, PPTP, then L2TP.
        Ikev2Only = 7, //RAS dials only IKEv2.
        Ikev2First = 8, //RAS always dials IKEv2 first followed by SSTP, PPTP, then L2TP.
        GREOnly = 9, //RAS dials GRE.
        PptpSstp = 12, //RAS dials PPTP followed by SSTP.
        L2tpSstp = 13, //RAS dials L2TP followed by SSTP.
        Ikev2Sstp = 14, //RAS dials IKEv2 followed by SSTP.
        ProtocolList = 15 // Win11 Only: Use ProtocolList to determine protocols to connect
    }
}
