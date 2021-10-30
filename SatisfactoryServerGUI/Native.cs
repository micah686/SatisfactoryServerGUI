using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SatisfactoryServerGUI
{

    public class Native
    {
        //https://www.programmerall.com/article/51711595824/
        public class ConsoleContentRead
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool AttachConsole(uint dwProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool FreeConsole(uint dwProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern IntPtr GetStdHandle(int nStdHandle);

            [StructLayout(LayoutKind.Sequential)]
            public struct Coord
            {
                public short X;
                public short Y;
            }

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            static extern bool ReadConsoleOutputCharacter(IntPtr hConsoleOutput, [Out] StringBuilder lpCharacter, uint length, Coord bufferCoord, out uint lpNumberOfCharactersRead);

            public static string ReadCharacterAt(int x, int y, int length)
            {
                IntPtr consoleHandle = GetStdHandle(-11);

                if (consoleHandle == IntPtr.Zero)
                {
                    return null;
                }
                Coord position = new Coord
                {
                    X = (short)x,
                    Y = (short)y
                };
                StringBuilder result = new StringBuilder(length);
                uint read = 0;
                if (ReadConsoleOutputCharacter(consoleHandle, result, (uint)length, position, out read))
                {
                    return result.ToString();
                }
                else
                {
                    return null;
                }
            }

            public static string GetContent(int pid, int x, int y, int length)
            {
                //Note that the Windows application can be ATTACHCONSOLE
                var flag = AttachConsole((uint)pid);
                string content = ReadCharacterAt(x, y, length);
                var freeflag = FreeConsole((uint)pid);
                return content;
            }
        }
    }
}
