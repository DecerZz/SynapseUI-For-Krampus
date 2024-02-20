using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Synapse_UI_WPF.Interfaces
{
    public static class CInterface
    {
        
        private static uint InteractPtr;

        
        private static uint GetKey(uint Func)
        {
            var Addr = InteractPtr;
            Addr ^= RotateRight(0x3b53904e, Convert.ToInt32(Addr % 16));
            return RotateLeft(Addr, Convert.ToInt32(Func % 16));
        }

        
        public static void Init()
        {
        }

        
        public static void Inject(string Path, string D3DPath, string XInputPath, int Proc, bool AutoLaunch)
        {
        }

        
        public static string GetHwid()
        {
            return "iwannakms";
        }

        
        public static string Sign(string Info)
        {
            return "epic";
        }

        
        private static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }

        
        private static uint RotateRight(uint value, int count)
        {
            return (value >> count) | (value << (32 - count));
        }
    }
}
