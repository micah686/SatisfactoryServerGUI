using System.ComponentModel;
using System.Threading.Tasks;

namespace SatisfactoryServerGUI.Services;

public interface ISettingsService : INotifyPropertyChanged
{
    string ServerPath { get; set; }
    string ServerIp { get; set; }
    int ServerApiPort { get; set; }
    string? AuthToken { get; set; }
    bool TrustSelfSignedCerts { get; set; }
    int ServerQueryPort { get; set; }
    int BeaconPort { get; set; }
    int Port { get; set; }
    string? MultiHome { get; set; }
    bool DisablePacketRouting { get; set; }
    bool DisableSeasonalEvents { get; set; }
    bool NoSteam { get; set; }
    bool NoVisibleConsole { get; set; }
    string ServerVersion { get; set; }
    string ServerExePath { get; }
    string ServerShPath { get; }
    Task LoadAsync();
    Task SaveAsync();
}
