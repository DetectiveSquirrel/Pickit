using System.Runtime.InteropServices;

namespace Pickit.Utilities
{
    public class WinApi
    {
        [DllImport("user32.dll")]
        public static extern bool BlockInput(bool fBlockIt);
    }
}