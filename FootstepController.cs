using EFT;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    static internal class FootstepController
    {
        // readonlys
        static readonly float _UpdateMulti = 12f;
        static readonly float _StepIntensityMulti = 0.0025f;

        // vars
        static float _stepLerp = 0;
        static float _currentSpeed = 0;

        public static void UpdateStep(float dt)
        {
            float speedAdjusted = 1f + _currentSpeed;
            _stepLerp = Mathf.Lerp(_stepLerp, 1f, dt * _UpdateMulti * speedAdjusted * PrimeMover.FootstepLerpMulti.Value);
        }

        public static void NewStep(Player player)
        {
            if (_stepLerp < 0.95f)
            {
                return;
            }
            _stepLerp = 0;
            _currentSpeed = player.Speed;
        }

        public static Vector3 GetModifiedHandPosFootstep
        {
            get 
            {
                float speedAdjusted = 0.2f + _currentSpeed;
                float stepValue = PrimeMover.Instance.FootStepCurve.Evaluate(_stepLerp) * _StepIntensityMulti * PrimeMover.FootstepIntesnityMulti.Value * EfficiencyController.EfficiencyModifier * speedAdjusted;
                return new Vector3(0, stepValue, 0); 
            }
        }
    }
}
