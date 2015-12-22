using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiDirectW10.Engine
{
    public static class Globals
    {
        public static readonly byte[] CustomOui = { 0xAA, 0xBB, 0xCC };
        public static readonly byte CustomOuiType = 0xDD;
        public static readonly byte[] WfaOui = { 0x50, 0x6F, 0x9A };
        public static readonly byte[] MsftOui = { 0x00, 0x50, 0xF2 };
        public static readonly string strServerPort = "50001";
    }
}
