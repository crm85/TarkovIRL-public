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
        private static readonly float _RotationTrigger = 0.0002f;

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

            Vector2 rotationalMotionThisFrame = _playerRotationLastFrame - player.Rotation;
            _playerRotationLastFrame = player.Rotation;
            float sizeMultiFinal = _ParallaxSetSizeFixed * PrimeMover.ParallaxSetSizeMulti.Value;

            _rotAvgXSet += rotationalMotionThisFrame.x;
            _rotAvgYSet += rotationalMotionThisFrame.y;
            _rotAvgXSet -= _rotAvgX;
            _rotAvgYSet -= _rotAvgY;
            _rotAvgXSet = Mathf.Clamp(_rotAvgXSet, -sizeMultiFinal, sizeMultiFinal);
            _rotAvgYSet = Mathf.Clamp(_rotAvgYSet, -sizeMultiFinal, sizeMultiFinal);
            _rotAvgX = _rotAvgXSet * player.DeltaTime;
            _rotAvgY = _rotAvgYSet * player.DeltaTime;

            _parallaxWeightADS = ParallaxAdsController.ParallaxWeight;

            // up
            float dt = player.DeltaTime;
            float extraPistolParallax = WeaponController.IsPistol ? PrimeMover.PistolSpecificParallax.Value : 1f;
            float parallaxMulti = PrimeMover.ParallaxMulti.Value * WeaponController.GetWeaponMulti(false) * extraPistolParallax;

            // down
            float pistolZeroFactor = (WeaponController.IsPistol && PlayerMotionController.RotationDelta <= _RotationTrigger) ? PrimeMover.PistolSpecificParallax.Value * 2f : 0.5f;
            float zeroDt = dt * PrimeMover.ParallaxReturnToCenterMulti.Value * inverseEfficiencyMulti * pistolZeroFactor;

            //
            // calc the position lerps
            //
            _posLerpXTarget = Mathf.Lerp(_posLerpXTarget, _rotAvgX * parallaxMulti, dt);
            _posLerpXTarget = Mathf.Lerp(_posLerpXTarget, 0, zeroDt);
            //
            _posLerpYTarget = Mathf.Lerp(_posLerpYTarget, _rotAvgY * parallaxMulti, dt);
            _posLerpYTarget = Mathf.Lerp(_posLerpYTarget, 0, zeroDt);

            //
            // calc the rotation lerps
            //
            _rotLerpXTarget = Mathf.Lerp(_rotLerpXTarget, _rotAvgX * parallaxMulti, dt);
            _rotLerpXTarget = Mathf.Lerp(_rotLerpXTarget, 0, zeroDt);
            _rotLerpYTarget = Mathf.Lerp(_rotLerpYTarget, _rotAvgY * parallaxMulti, dt);
            _rotLerpYTarget = Mathf.Lerp(_rotLerpYTarget, 0, zeroDt);

            //
            // final lerp
            //
            _rotLerpX = Mathf.Lerp(_rotLerpX, _rotLerpXTarget, dt);
            _rotLerpY = Mathf.Lerp(_rotLerpY, _rotLerpYTarget, dt);
            //
            _posLerpX = Mathf.Lerp(_posLerpX, _posLerpXTarget, dt);
            _posLerpY = Mathf.Lerp(_posLerpY, _posLerpYTarget, dt);

            //
            // fill the references
            //
            rotation = Quaternion.Euler(0, -_rotLerpY * 100f * _parallaxWeightADS, -_rotLerpX * 100f * _parallaxWeightADS);
            position = new Vector3(_posLerpX * _parallaxWeightADS, -_posLerpY * _parallaxWeightADS, 0);
        }
    }
}
