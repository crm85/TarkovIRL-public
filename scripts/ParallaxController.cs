using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using EFT;
using EFT.InventoryLogic;

namespace TarkovIRL
{
    public static class ParallaxController
    {
        private static readonly float _ParallaxSetSizeFixed = 0.2f;

        private static float _rotAvgXSet = 0;
        private static float _rotAvgYSet = 0;
        private static float _rotAvgX = 0;
        private static float _rotAvgY = 0;

        private static float _posLerpXTarget = 0;
        private static float _posLerpYTarget = 0;
        private static float _rotLerpXTarget = 0;
        private static float _rotLerpYTarget = 0;

        private static float _posLerpX = 0;
        private static float _posLerpY = 0;
        private static float _rotLerpX = 0;
        private static float _rotLerpY = 0;

        private static float _lerpToLerp = 0;

        private static Vector2 _playerRotationLastFrame = Vector2.zero;
        private static float _parallaxWeightADS = 1f;

        static bool _aimingLastFrame = false;

        public static void GetModifiedHandPosRotParallax(Player player, ref Vector3 position, ref Quaternion rotation)
        {
            if (AnimStateController.IsBlindfire)
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return;
            }

            float inverseWeaponMulti = WeaponController.GetWeaponMulti(true);
            float inverseEfficiencyMulti = EfficiencyController.EfficiencyModifierInverse;
            float parallaxEfficiencyMulti = inverseWeaponMulti * inverseEfficiencyMulti;
            bool isAiming = player.ProceduralWeaponAnimation.IsAiming;

            ParallaxAdsController.UpdateLerps();

            if (WeaponController.IsStocked)
            {
                bool isNewAds = !_aimingLastFrame && isAiming;
                bool isFinishAds = _aimingLastFrame && !isAiming;
                _aimingLastFrame = isAiming;

                if (isNewAds)
                {
                    ParallaxAdsController.StartNewAds(true, parallaxEfficiencyMulti);
                }
                else if (isFinishAds)
                {
                    ParallaxAdsController.StartNewAds(false, parallaxEfficiencyMulti);
                }
            }

            //float dt = player.DeltaTime;
            float dt = PrimeMover.Instance.DeltaTime;

            Vector2 rotationalMotionThisFrame = _playerRotationLastFrame - player.Rotation;
            rotationalMotionThisFrame *= dt;
            _playerRotationLastFrame = player.Rotation;
            float sizeMultiFinal = _ParallaxSetSizeFixed * PrimeMover.ParallaxSetSizeMulti.Value;

            _rotAvgXSet += rotationalMotionThisFrame.x;
            _rotAvgYSet += rotationalMotionThisFrame.y;
            _rotAvgXSet -= _rotAvgX;
            _rotAvgYSet -= _rotAvgY;
            _rotAvgXSet = Mathf.Clamp(_rotAvgXSet, -sizeMultiFinal, sizeMultiFinal);
            _rotAvgYSet = Mathf.Clamp(_rotAvgYSet, -sizeMultiFinal, sizeMultiFinal);
            _rotAvgX = _rotAvgXSet * dt;
            _rotAvgY = _rotAvgYSet * dt;

            _parallaxWeightADS = ParallaxAdsController.ParallaxWeight;

            // up
            float extraPistolParallax = WeaponController.IsPistol ? PrimeMover.PistolSpecificParallax.Value : 1f;
            float parallaxMulti = PrimeMover.ParallaxMulti.Value * WeaponController.GetWeaponMulti(false) * extraPistolParallax;

            // down
            float inverseRotationDeltaMulti = 1f / PlayerMotionController.RotationDelta;
            float zeroLerpTime = dt * inverseEfficiencyMulti * inverseRotationDeltaMulti * PrimeMover.ParallaxRotationRecenterMulti.Value;
            if (UtilsTIRL.IsPriority(2)) UtilsTIRL.Log($"zeroLerpTime {zeroLerpTime}, inverseEfficiencyMulti {inverseEfficiencyMulti}, inverseRotationDeltaMulti {inverseRotationDeltaMulti}");


            //
            // calc the position lerps
            //

            _posLerpXTarget = Mathf.Lerp(_posLerpXTarget, _rotAvgX * parallaxMulti, dt);
            _posLerpXTarget = Mathf.Lerp(_posLerpXTarget, 0, zeroLerpTime);
            //
            _posLerpYTarget = Mathf.Lerp(_posLerpYTarget, _rotAvgY * parallaxMulti, dt);
            _posLerpYTarget = Mathf.Lerp(_posLerpYTarget, 0, zeroLerpTime);

            //
            // calc the rotation lerps
            //

            _rotLerpXTarget = Mathf.Lerp(_rotLerpXTarget, _rotAvgX * parallaxMulti, dt);
            _rotLerpXTarget = Mathf.Lerp(_rotLerpXTarget, 0, zeroLerpTime);
            //
            _rotLerpYTarget = Mathf.Lerp(_rotLerpYTarget, _rotAvgY * parallaxMulti, dt);
            _rotLerpYTarget = Mathf.Lerp(_rotLerpYTarget, 0, zeroLerpTime);

            //
            // final lerp
            //
            _rotLerpX = Mathf.Lerp(_rotLerpX, _rotLerpXTarget, dt);
            _posLerpX = Mathf.Lerp(_posLerpX, _posLerpXTarget, dt);
            //
            _rotLerpY = Mathf.Lerp(_rotLerpY, _rotLerpYTarget, dt);
            _posLerpY = Mathf.Lerp(_posLerpY, _posLerpYTarget, dt);

            //
            // fill the references
            //
            rotation = Quaternion.Euler(0, -_rotLerpY * 100f * _parallaxWeightADS, -_rotLerpX * 100f * _parallaxWeightADS);
            position = new Vector3(_posLerpX * _parallaxWeightADS, -_posLerpY * _parallaxWeightADS, 0);
        }
    }
}
