using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using EFT;
using EFT.InventoryLogic;
using RealismMod;

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

            float weaponBalanceMulti = 1f + (Mathf.Pow(WeaponStats.Balance, 2f) * 0.001f);

            float sizeMultiFinal = _ParallaxSetSizeFixed * PrimeMover.ParallaxSetSizeMulti.Value * weaponBalanceMulti;

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

            float newValue = Mathf.Pow(1f - Mathf.Clamp01(PlayerMotionController.RotationDelta / 0.1f), 2f);


            float parallaxMulti = PrimeMover.ParallaxMulti.Value * WeaponController.GetWeaponMulti(false) * EfficiencyController.EfficiencyModifier * extraPistolParallax * newValue;
            parallaxMulti = Mathf.Pow(parallaxMulti, 2f) / 100f;
            //UtilsTIRL.Log($"newValue {newValue}, parallaxMulti {parallaxMulti}");

            // down

            // calc the position lerps
            _posLerpXTarget = Mathf.Lerp(_posLerpXTarget, _rotAvgX * parallaxMulti, dt * PrimeMover.ParallaxDTMulti.Value);
            _posLerpYTarget = Mathf.Lerp(_posLerpYTarget, _rotAvgY * parallaxMulti, dt * PrimeMover.ParallaxDTMulti.Value);

            // calc the rotation lerps
            _rotLerpXTarget = Mathf.Lerp(_rotLerpXTarget, _rotAvgX * parallaxMulti, dt * PrimeMover.ParallaxDTMulti.Value);
            _rotLerpYTarget = Mathf.Lerp(_rotLerpYTarget, _rotAvgY * parallaxMulti, dt * PrimeMover.ParallaxDTMulti.Value);

            float clampValueRot = WeaponController.IsPistol ? PrimeMover.ParallaxHardClampPistols.Value : PrimeMover.ParallaxHardClamp.Value;
            float clampValuePos = WeaponController.IsPistol ? PrimeMover.ParallaxHardClampPistols.Value * 0.5f : PrimeMover.ParallaxHardClamp.Value * 0.5f;

            _rotLerpXTarget = Mathf.Clamp(_rotLerpXTarget, -clampValueRot, clampValueRot);
            _rotLerpYTarget = Mathf.Clamp(_rotLerpYTarget, -clampValueRot, clampValueRot);
            _posLerpXTarget = Mathf.Clamp(_posLerpXTarget, -clampValuePos, clampValuePos);
            _posLerpYTarget = Mathf.Clamp(_posLerpYTarget, -clampValuePos, clampValuePos);

            // smoothing lerp
            _rotLerpX = Mathf.Lerp(_rotLerpX, _rotLerpXTarget, dt * PrimeMover.ParallaxRotationSmoothingMulti.Value);
            _rotLerpY = Mathf.Lerp(_rotLerpY, _rotLerpYTarget, dt * PrimeMover.ParallaxRotationSmoothingMulti.Value);
            _posLerpX = Mathf.Lerp(_posLerpX, _posLerpXTarget, dt * PrimeMover.ParallaxRotationSmoothingMulti.Value);
            _posLerpY = Mathf.Lerp(_posLerpY, _posLerpYTarget, dt * PrimeMover.ParallaxRotationSmoothingMulti.Value);

            //UtilsTIRL.Log($"_rotLerpX {_rotLerpX}, _rotLerpY {_rotLerpY}, _posLerpX {_posLerpX}, _posLerpY {_posLerpY}");


            // fill the references
            rotation = Quaternion.Euler(0, -_rotLerpY * 100f * _parallaxWeightADS, -_rotLerpX * 100f * _parallaxWeightADS);
            position = new Vector3(_posLerpX * _parallaxWeightADS, -_posLerpY * _parallaxWeightADS, 0);
        }
    }
}
