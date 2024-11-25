namespace DPCLibrary.Enums
{
    public enum EncryptionMethod
    {
        DES = 0,
        DES3 = 1,
        AES128 = 2,
        AES192 = 3,
        AES256 = 4,
        AES_GCM_128 = 5,
        AES_GCM_256 = 6
        //AES_GCM_192 = 7, //Found at https://docs.microsoft.com/en-us/graph/api/resources/intune-deviceconfig-cryptographysuite?view=graph-rest-beta, doesn't appear to be supported with AOVPN
        //ChaCha20Poly1305 = 8 //Found at https://docs.microsoft.com/en-us/graph/api/resources/intune-deviceconfig-cryptographysuite?view=graph-rest-beta, doesn't appear to be supported with AOVPN
    }
}