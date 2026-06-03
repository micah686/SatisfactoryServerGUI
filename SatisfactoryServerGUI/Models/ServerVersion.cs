namespace SatisfactoryServerGUI.Models;

public enum ServerVersion
{
    Public,
    Experimental
}

public enum LogChoice
{
    Satisfactory,
    SteamCmd,
    None
}

public enum FactoryLogPrefix
{
    All,
    LogInit,
    LogPakFile,
    LogMemory,
    LogWindows,
    LogStreaming,
    LogNetCore,
    LogNet,
    LogLoad,
    LogReplicationGraph,
    LogGameState,
    LogGameMode,
    LogNavigation,
    LogGame,
    LogSave,
    LogOnline,
    LogSteamShared,
    LogGenericPlatformMisc,
    LogWorld,
    LogNiagara
}
