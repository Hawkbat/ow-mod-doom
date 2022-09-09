using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagedDoom
{
    public static class Logger
    {
        public static bool Enabled;

        public static void Log(string str)
        {
            if (Enabled)
            {
                UnityEngine.Debug.Log(str);
            }
        }
    }
}
