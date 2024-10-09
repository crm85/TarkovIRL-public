using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    public static class UtilsTIRL
    {
        public enum E_DEBUG_PRIORITY { ERROR, SPAM, TESTING };

        public static ManualLogSource Logger;
        static float _dt = 0;

        static float _updateResolution = 0.1f;

        public static void Log(string toPrint)
        {
            Logger.LogError((object)toPrint);
        }

        public static void Update(float dt)
        {
            _dt += dt;
        }

        public static bool IsPriority(int priority)
        {
            if (priority == 1)
            {
                return true;
            }
            else if (priority == 2)
            {
                if (PrimeMover.IsLogging.Value && PrimeMover.DebugSpam.Value)
                {
                    return true;
                }
            }
            else if (priority == 3)
            {
                if (PrimeMover.IsLogging.Value)
                {
                    return true;
                }
            }
            return false;
        }
    }

}
