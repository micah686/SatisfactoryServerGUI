using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using MessageBox = AdonisUI.Controls.MessageBox;

namespace SatisfactoryServerGUI
{
    internal class ServerStateHelper
    {
        private const string FACTORY_SERVER = "FactoryServer";
        private const string UNREAL_ENGINE = "UnrealServer-Win64-Shipping";   
        internal static FileSystemWatcher SaveWatcher = new FileSystemWatcher();
        public delegate void ChangedEventHandler(object source, FileSystemEventArgs e);
        public static ChangedEventHandler OnChangedHandler;

        internal static void StopServer()
        {
            Process.GetProcessesByName(FACTORY_SERVER).FirstOrDefault()?.CloseMainWindow();
            Process.GetProcessesByName(FACTORY_SERVER).FirstOrDefault()?.Close();
            Process.GetProcessesByName(UNREAL_ENGINE).FirstOrDefault()?.CloseMainWindow();
            Process.GetProcessesByName(UNREAL_ENGINE).FirstOrDefault()?.Close();

            int count = 20;
            bool serverStopped = false;
            while (count > 0)
            {
                if (!IsServerRunning())
                {
                    serverStopped = true;
                    break;
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(250));
                    count--;
                }                
            }
            if(serverStopped == false)
            {
                Process.GetProcessesByName(FACTORY_SERVER).FirstOrDefault()?.Kill(true);
                Process.GetProcessesByName(UNREAL_ENGINE).FirstOrDefault()?.Kill(true);
            }            
        }

        internal static bool IsServerRunning()
        {
            var factoryServer = Process.GetProcessesByName(FACTORY_SERVER).FirstOrDefault();
            var unrealEngine = Process.GetProcessesByName(UNREAL_ENGINE).FirstOrDefault();

            bool factoryEngineStopped = factoryServer == null || factoryServer.HasExited;
            bool unrealStopped = unrealEngine == null || unrealEngine.HasExited;

            return !(factoryEngineStopped && unrealStopped);
        }
        
    }
}
