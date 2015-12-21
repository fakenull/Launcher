using System;
using System.IO;
using Launcher.WindowsAPI;

namespace Launcher
{
    class Lineage
    {
        private static Kernel32.ProcessInformation _processInfo;

        private static bool ByteArrayCompare(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;

            for (var i = 0; i < a1.Length; i++)
                if (a1[i] != a2[i])
                    return false;

            return true;
        }

        public static void Run(Settings settings, string bin, long ip, ushort port)
        {
            var binpath = Path.Combine(settings.ClientDirectory, bin);

            var startupInfo = new Kernel32.Startupinfo();
            _processInfo = new Kernel32.ProcessInformation();

            //TODO -- what to do if !success?
            var success = Kernel32.CreateProcess(binpath, string.Format("\"{0}\" {1} {2}", binpath.Trim(), ip, port),
                IntPtr.Zero, IntPtr.Zero, false,
                Kernel32.ProcessCreationFlags.CreateSuspended | Kernel32.ProcessCreationFlags.CreateDefaultErrorMode,
                IntPtr.Zero, null, ref startupInfo, out _processInfo);

            var tHandle = _processInfo.HThread;

            // TODO: need a better way to hook/suspend the client after themida unpack
            var tries = 10;
            byte[] patchWatchFor = { 0x75, 0x3B };
            var patchWatchBuff = new byte[2];

            Kernel32.ResumeThread(tHandle);
            System.Threading.Thread.Sleep(500);

            while (tries > 0)
            {
                Kernel32.SuspendThread(tHandle);
                Kernel32.ReadProcessMemory(_processInfo.HProcess, (IntPtr)0x0045CF2F, patchWatchBuff, (uint)patchWatchBuff.Length, 0);

                if (ByteArrayCompare(patchWatchBuff, patchWatchFor))
                {
                    // Fix Runtime Expired
                    Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x0045CF2F, new byte[] { 0xEB }, 1, 0);

                    // Fix GameGuard
                    Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x0045E3AC, new byte[] { 0x90, 0xE9 }, 2, 0);
                    Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x004DE45A, new byte[] { 0x90, 0x90 }, 2, 0);
                    Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x0045BA71, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }, 6, 0);
                    Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x0045EABA, new byte[] { 0xEB }, 1, 0);

                    // Don't let Lin.bin install NPKCMSVC Windows Service
                    Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x00474AC4, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x84, 0xC0, 0x5E, 0x5B, 0xEB }, 10, 0);

                    // Remove darkness
                    if (settings.DisableDark)
                    {
                        byte[] write7 = { 0x90, 0xE9 };
                        Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x0046690B, new byte[] { 0x90, 0xE9 }, 2, 0);
                    }

                    // TODO: A checkbox to enable/disable like darkness?
                    // Mob name with color
                    Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x0046786E, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }, 6, 0);

                    // Load list.spr with lancemaster fix
                    string zelgoPakPath = Path.Combine(settings.ClientDirectory, (string)"Zelgo.bin");
                    if (File.Exists(zelgoPakPath))
                    {
                        byte[] zelgoPak = File.ReadAllBytes(zelgoPakPath);
                        Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x004B6CE0, new byte[] { 0xEB }, 1, 0);
                        Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x00504538, zelgoPak, (uint)zelgoPak.Length, 0);
                        Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x006DA508, new byte[] { 0x0F, 0x27 }, (uint)2, 0);
                    }

                    // Codepage??
                    Kernel32.WriteProcessMemory(_processInfo.HProcess, (IntPtr)0x00483B8E, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }, 6, 0);

                    Kernel32.ResumeThread(tHandle);
                    break;
                }

                Kernel32.ResumeThread(tHandle);
                System.Threading.Thread.Sleep(500);
                tries--;
            }

            Kernel32.ResumeThread(tHandle);
        }
    }
}
