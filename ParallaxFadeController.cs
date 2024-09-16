using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    internal static class ParallaxFadeController
    {
        // readonlys
        static readonly float _RotationThresh = 0.0002f;

        // vars
        static float _currentFadeValue = 0;

        public static void UpdateLerp(float dt)
        {
            //
            // lerping to 1 is lerping INTO the parallax
            //

            bool lerpToOne = PlayerMotionController.RotationDelta > _RotationThresh;
            float weaponMulti = 1f / WeaponController.GetWeaponMulti();
            float efficiencyMulti = 1f / EfficiencyController.GetEfficiencyModifier;
            float finalMulti = weaponMulti * efficiencyMulti;
            if (lerpToOne)
            {
                finalMulti = 1f / finalMulti;
            }
            float lerpRate = dt * PrimeMover.DevTestFloat3.Value * finalMulti;
            _currentFadeValue = Mathf.Lerp(_currentFadeValue, lerpToOne ? 1 : 0, lerpRate);
        }

        public static float FadeValue
        {
            get 
            { 
                float fadeValueAdjusted = PrimeMover.Instance.SmoothEdgesCurve.Evaluate(_currentFadeValue);
                return fadeValueAdjusted; 
            }
        }
    }
}
