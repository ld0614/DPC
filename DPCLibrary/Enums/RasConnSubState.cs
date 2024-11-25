namespace DPCLibrary.Enums
{
    public enum RasConnSubState
    {
        RASCSS_None = 0,
        RASCSS_Dormant,
        RASCSS_Reconnecting,
        RASCSS_Reconnected = 0x2000, //RASCS_DONE
    }
}