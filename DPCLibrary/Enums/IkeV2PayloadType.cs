namespace DPCLibrary.Enums
{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\ras.h look for: enum _IKEV2_ID_PAYLOAD_TYPE
    public enum IKEV2_ID_PAYLOAD_TYPE
    {
        INVALID = 0,
        IPV4_ADDR = 1, // A single four (4) octet IPv4 address
        FQDN = 2, // A fully-qualified domain name string, e.g., "example.com"
        RFC822_ADDR = 3, // A fully-qualified RFC 822 email address string, e.g., "jsmith@example.com".
        RESERVED1 = 4, // Reserved-Not used
        IPV6_ADDR = 5, // A single sixteen (16) octet IPv6 address.
        RESERVED2 = 6, // Reserved-Not used
        RESERVED3 = 7, // Reserved-Not used
        RESERVED4 = 8, // Reserved-Not used
        DER_ASN1_DN = 9, // The binary Distinguished Encoding Rules (DER) encoding of an ASN.1 X.500 Distinguished Name [PKIX].
        DER_ASN1_GN = 10,// The binary DER encoding of an ASN.1 X.509 GeneralName [PKIX
        KEY_ID = 11,// Reserved-Not used
        MAX
    }
}
