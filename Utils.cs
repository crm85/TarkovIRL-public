using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    public static class Utils
    {
        public static ManualLogSource Logger;
        public static float DeltaTime = 0;

        static float _updateResolution = 0.1f;

        public static void Log(bool everyFrame, string toPrint)
        {
            if (everyFrame)
            {
                Logger.LogError((object)toPrint);
            }
            else
            {
                if (DeltaTime > _updateResolution)
                {
                    DeltaTime = 0;
                    Logger.LogError((object)toPrint);
                }
            }
        }

        public static void PumpUpdate(float dt)
        {
            DeltaTime += dt;
        }
    }

}
