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
        public static ManualLogSource Logger;
        static float _dt = 0;

        static float _updateResolution = 0.1f;

        public static void Log(bool everyFrame, string toPrint)
        {
            if (!PrimeMover.IsLogging.Value)
            {
                return;
            }

            if (everyFrame && !PrimeMover.DebugSpam.Value)
            {
                return;
            }

            Logger.LogError((object)toPrint);
        }

        public static void Update(float dt)
        {
            _dt += dt;
        }
    }

}
