using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    internal static class ThrowController
    {
        // readonlys
        static readonly float _ThrowStrengthMulti = 1f;
        static readonly float _ThrowSpeedMulti = 1f;

        // vars
        static float _throwLerp = 1f;

        public static void UpdateLerp(float dt)
        {
            _throwLerp = Mathf.Lerp(_throwLerp, 1f, _ThrowSpeedMulti * dt * PrimeMover.ThrowSpeedMulti.Value);
            UtilsTIRL.Log(true, $"throwlerp is {_throwLerp}");
        }

        public static void NewThrow(bool isUnderhand)
        {
            _throwLerp = 0;
        }

        public static Vector3 GetThrowOffset
        {
            get
            {
                float xOffsetThisFrame = PrimeMover.Instance.ThrowVisualCurveX.Evaluate(_throwLerp) * _ThrowStrengthMulti * PrimeMover.ThrowStrengthMulti.Value;
                float yOffsetThisFrame = PrimeMover.Instance.ThrowVisualCurveY.Evaluate(_throwLerp) * _ThrowStrengthMulti * PrimeMover.ThrowStrengthMulti.Value;
                UtilsTIRL.Log(true, $"throw | xOffsetThisFrame {xOffsetThisFrame} | yOffsetThisFrame {yOffsetThisFrame}");
                return new Vector3(-xOffsetThisFrame, yOffsetThisFrame, 0);
            }
        }
    }
}
