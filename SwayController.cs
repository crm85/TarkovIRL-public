using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static EFT.Player;

namespace TarkovIRL
{
    public static class SwayController
    {
        private static readonly float _BaseSwayValue = -0.5f;
        private static readonly float _BaseSwayLerpSpeed = 1f;

        // vars
        public static bool IsSwayUpdatedThisFrame = false;
        static float _addedSwayTarget = 0;
        static float _addedSwayLerp = 0;

        public static void UpdateLerp(float dt)
        {
            _addedSwayLerp = Mathf.Lerp(_addedSwayLerp, _addedSwayTarget, dt * _BaseSwayLerpSpeed);
        }

        public static Vector3 GetNewSway(Vector3 newSwayFactors, bool isAiming)
        {
            if (AnimStateController.IsBlindfire)
            {
                return Vector3.zero;
            }

            float weaponWeight = WeaponController.CurrentWeaponWeight;
            bool isFolded = !WeaponController.IsStocked;
            bool isPistol = WeaponController.IsPistol;

            // vertical axis
            newSwayFactors.x *= -0.3f * weaponWeight;
            if (PlayerMotionController.VerticalTrend < 0 && !isPistol)
            {
                newSwayFactors.x *= -1f;
            }

            // z axis
            newSwayFactors.y *= -.2f * weaponWeight;

            // horizontal axis ***
            _addedSwayTarget = _BaseSwayValue * PrimeMover._2_WeaponSwayMulti.Value * WeaponController.GetWeaponMulti(false) * EfficiencyController.EfficiencyModifier;

            if (isAiming)
            {
                _addedSwayTarget *= -2f;
                if (isFolded)
                {
                    _addedSwayTarget *= -0.7f;
                }
                else if (isPistol)
                {
                    _addedSwayTarget *= -3f;
                }
            }
            newSwayFactors.z *= 0;
            //newSwayFactors.z *= _addedSwayLerp;
            //UtilsTIRL.Log($"added sway {_addedSwayLerp}");

            // output
            return newSwayFactors;
        }
    }
}
