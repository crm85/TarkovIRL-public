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
        //
        // Old sway class. Only used for the axial rotation and vertical
        // sway effects. Could be replaced in future.
        //

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

            float weaponWeight = WeaponController.GetWeaponMulti(false);
            bool isPistol = WeaponController.IsPistol;

            // vertical axis
            newSwayFactors.x *= -0.3f * weaponWeight;
            if (PlayerMotionController.VerticalTrend < 0 && !isPistol)
            {
                newSwayFactors.x *= -1f;
            }

            // z axis
            newSwayFactors.y *= -.2f * weaponWeight;

            // output
            return newSwayFactors;
        }
    }
}
