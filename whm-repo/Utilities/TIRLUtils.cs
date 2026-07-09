using BepInEx.Logging;
using UnityEngine;

namespace TarkovIRL
{
    public static class TIRLUtils
    {
        public enum E_DEBUG_PRIORITY { LOG, SPAM_LOG, ALWAYS_LOG };

        public static ManualLogSource Logger;
        static float _dt = 0;
        static float _spamTimer = 0;

        public static void LogError(string toPrint)
        {
            Logger.LogError((object)toPrint);
        }

        public static void Log(string toPrint, bool spam)
        {
            if (!PrimeMover.IsLogging.Value)
            {
                return;
            }
            if (spam)
                Logger.LogInfo((object)toPrint);
            else
            {
                _spamTimer += _dt;
                if (_spamTimer > PrimeMover.LoggingUpdateFrequency.Value)
                {
                    _spamTimer = 0;
                    Logger.LogInfo((object)toPrint);
                }
            }
        }

        public static void LogPriority(E_DEBUG_PRIORITY priority, string toPrint)
        {
            if (priority == E_DEBUG_PRIORITY.SPAM_LOG)
            {
                if (PrimeMover.IsLogging.Value && PrimeMover.DebugSpam.Value)
                {
                    Logger.LogError((object)toPrint);
                }
            }
            else if (priority == E_DEBUG_PRIORITY.LOG)
            {
                if (PrimeMover.IsLogging.Value && !PrimeMover.DebugSpam.Value)
                {
                    Logger.LogError((object)toPrint);
                }
            }
            else if (priority == E_DEBUG_PRIORITY.ALWAYS_LOG)
            {
                Logger.LogError((object)toPrint);
            }
        }

        public static void Update(float dt)
        {
            _dt = dt;
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        public static float FiniteOr(float value, float fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        public static Vector2 FiniteOr(Vector2 value, Vector2 fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        public static Vector3 FiniteOr(Vector3 value, Vector3 fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        public static float FulfilledLerp(float value, float target, float step)
        {
            value += step;
            if (value > target)
            {
                value = target;
            }
            return value;
        }

        public static Quaternion GetQuatFromV3(Vector3 v)
        {
            v = FiniteOr(v, Vector3.zero);
            Quaternion result = Quaternion.identity;
            result.x = v.x;
            result.y = v.y;
            result.z = v.z;

            result = NormalizeQuaternion(result);

            return result;
        }

        public static Quaternion NormalizeQuaternion(Quaternion quaternion)
        {
            float magnitudeSqr = quaternion.x * quaternion.x
                + quaternion.y * quaternion.y
                + quaternion.z * quaternion.z
                + quaternion.w * quaternion.w;

            if (!IsFinite(magnitudeSqr) || magnitudeSqr <= 0.000001f)
            {
                return Quaternion.identity;
            }

            float inverseMagnitude = 1f / Mathf.Sqrt(magnitudeSqr);
            return new Quaternion(
                quaternion.x * inverseMagnitude,
                quaternion.y * inverseMagnitude,
                quaternion.z * inverseMagnitude,
                quaternion.w * inverseMagnitude);
        }
    }

}
