using System;
using System.Collections.Generic;

namespace SatisfactoryServerGUI.Models;

/// <summary>Root object persisted to settings.json.</summary>
public class AppSettings
{
    public List<ServerProfile> Profiles { get; set; } = new();
    public Guid? LastActiveProfileId { get; set; }

    /// <summary>HTTPS QueryServerState poll interval, seconds.</summary>
    public int HttpsPollSeconds { get; set; } = 8;

    /// <summary>Lightweight UDP poll interval, seconds.</summary>
    public int UdpPollSeconds { get; set; } = 3;
}
