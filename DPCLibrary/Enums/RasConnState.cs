namespace DPCLibrary.Enums
{
    public enum RasConnState
    {
        RASCS_OpenPort = 0,
        RASCS_PortOpened,
        RASCS_ConnectDevice,
        RASCS_DeviceConnected,
        RASCS_AllDevicesConnected,
        RASCS_Authenticate,
        RASCS_AuthNotify,
        RASCS_AuthRetry,
        RASCS_AuthCallback,
        RASCS_AuthChangePassword,
        RASCS_AuthProject,
        RASCS_AuthLinkSpeed,
        RASCS_AuthAck,
        RASCS_ReAuthenticate,
        RASCS_Authenticated,
        RASCS_PrepareForCallback,
        RASCS_WaitForModemReset,
        RASCS_WaitForCallback,
        RASCS_Projected,
        RASCS_StartAuthentication,
        RASCS_CallbackComplete,
        RASCS_LogonNetwork,
        RASCS_SubEntryConnected,
        RASCS_SubEntryDisconnected,
        RASCS_ApplySettings,
        RASCS_Interactive = 0x1000, //RASCS_PAUSED
        RASCS_RetryAuthentication,
        RASCS_CallbackSetByCaller,
        RASCS_PasswordExpired,
        RASCS_InvokeEapUI,
        RASCS_Connected = 0x2000, //RASCS_DONE
        RASCS_Disconnected
    }
}