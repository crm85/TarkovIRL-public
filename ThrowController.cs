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
        static readonly float _OverhandYExtraMulti = 4f;

        // vars
        static float _throwLerp = 1f;
        static bool _isThrowing = false;
        static bool _isUnderhand = false;

        public static void UpdateLerp(float dt)
        {
            if (!_isThrowing) return;

            _throwLerp = Mathf.Lerp(_throwLerp, 1f, dt * PrimeMover.ThrowSpeedMulti.Value);
            if (_throwLerp >= 0.98f)
            {
                _isThrowing = false;
                _throwLerp = 1f;
            }
            UtilsTIRL.Log(true, $"throwlerp is {_throwLerp}");
        }

        public static void NewThrow(bool isUnderhand)
        {
            _isThrowing = true;
            _throwLerp = 0;
            _isUnderhand = isUnderhand;
        }

        public static Vector3 GetThrowOffset
        {
            get
            {
                float reverseY = _isUnderhand ? -1f : 1f;
                AnimationCurve xCurve = _isUnderhand ? PrimeMover.Instance.ThrowVisualUnderhandCurveX : PrimeMover.Instance.ThrowVisualCurveX;
                AnimationCurve yCurve = _isUnderhand ? PrimeMover.Instance.ThrowVisualUnderhandCurveY : PrimeMover.Instance.ThrowVisualCurveY;
                float xOffsetThisFrame = xCurve.Evaluate(_throwLerp) * EfficiencyController.EfficiencyModifierInverse * PrimeMover.ThrowStrengthMulti.Value;
                float yOffsetThisFrame = yCurve.Evaluate(_throwLerp) * EfficiencyController.EfficiencyModifierInverse * _OverhandYExtraMulti * PrimeMover.ThrowStrengthMulti.Value * reverseY;
                UtilsTIRL.Log(true, $"throw | xOffsetThisFrame {xOffsetThisFrame} | yOffsetThisFrame {yOffsetThisFrame}");



                return new Vector3(-xOffsetThisFrame, -yOffsetThisFrame, 0);
            }
        }
    }
}
