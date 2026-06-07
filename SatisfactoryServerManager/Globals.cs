using System;
using System.IO;

namespace SatisfactoryServerManager;

public static class Globals
{
    public static readonly string? DataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
}