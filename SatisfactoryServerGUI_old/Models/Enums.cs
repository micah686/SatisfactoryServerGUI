namespace SatisfactoryServerGUI.Models;

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

public enum ConsoleEntrySeverity
{
    Info,
    Command,
    Response,
    Warning,
    Error
}

public enum LocalServerState
{
    Stopped,
    Installing,
    Starting,
    Running,
    Stopping
}
