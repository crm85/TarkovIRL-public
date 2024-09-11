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
        private static float _rotAvgXSet = 0;
        private static float _rotAvgYSet = 0;
        private static float _rotAvgX = 0;
        private static float _rotAvgY = 0;

        private static float _posLerpX = 0;
        private static float _posLerpY = 0;
        private static float _rotLerpX = 0;
        private static float _rotLerpY = 0;
        private static Vector2 _playerRotationLastFrame = Vector2.zero;
        private static float _parallaxWeightADS = 1f;

        static bool _aimingLastFrame = false;

        public static void GetModifiedHandPosRotParallax(Player player, ref Vector3 position, ref Quaternion rotation)
        {
            float weaponMulti = WeaponController.GetWeaponMulti();
            float efficiencyMulti = EfficiencyController.GetEfficiencyModifier;

            ParallaxTimer.UpdateLerps();

            if (WeaponController.IsStocked)
            {
                bool isNewAds = !_aimingLastFrame && player.ProceduralWeaponAnimation.IsAiming;
                bool isFinishAds = _aimingLastFrame && !player.ProceduralWeaponAnimation.IsAiming;
                _aimingLastFrame = player.ProceduralWeaponAnimation.IsAiming;

                float adsEfficiencyMulti = 1f / efficiencyMulti;
                float adsMulti = weaponMulti * adsEfficiencyMulti;
                if (isNewAds)
                {
                    ParallaxTimer.StartNewAds(true, adsMulti);
                }
                else if (isFinishAds)
                {
                    ParallaxTimer.StartNewAds(false, adsMulti);
                }
            }

            Vector2 rotationalMotionThisFrame = _playerRotationLastFrame - player.Rotation;
            _playerRotationLastFrame = player.Rotation;
            float sizeMultiFinal = PrimeMover.ParallaxSetSizeMulti.Value * weaponMulti;

            _rotAvgXSet += rotationalMotionThisFrame.x;
            _rotAvgYSet += rotationalMotionThisFrame.y;
            _rotAvgXSet -= _rotAvgX;
            _rotAvgYSet -= _rotAvgY;
            _rotAvgXSet = Mathf.Clamp(_rotAvgXSet, -sizeMultiFinal, sizeMultiFinal);
            _rotAvgYSet = Mathf.Clamp(_rotAvgYSet, -sizeMultiFinal, sizeMultiFinal);
            _rotAvgX = _rotAvgXSet * player.DeltaTime;
            _rotAvgY = _rotAvgYSet * player.DeltaTime;

            _parallaxWeightADS = ParallaxTimer.ParallaxWeight;

            float dt = player.DeltaTime;
            float parallaxMulti = PrimeMover.ParallaxMulti.Value * weaponMulti * efficiencyMulti;

            //
            // calc the position lerps
            //
            _posLerpX = Mathf.Lerp(_posLerpX, _rotAvgX * parallaxMulti, dt);
            _posLerpX = Mathf.Lerp(_posLerpX, 0, dt);

            _posLerpY = Mathf.Lerp(_posLerpY, _rotAvgY * parallaxMulti, dt);
            _posLerpY = Mathf.Lerp(_posLerpY, 0, dt);

            //
            // calc the rotation lerps
            //
            _rotLerpX = Mathf.Lerp(_rotLerpX, _rotAvgX * parallaxMulti, dt);
            _rotLerpX = Mathf.Lerp(_rotLerpX, 0, dt);

            _rotLerpY = Mathf.Lerp(_rotLerpY, _rotAvgY * parallaxMulti, dt);
            _rotLerpY = Mathf.Lerp(_rotLerpY, 0, dt);

            //
            // fill the references
            //
            rotation = Quaternion.Euler(0, -_rotLerpY * 100f * _parallaxWeightADS, -_rotLerpX * 100f * _parallaxWeightADS);
            position = new Vector3(_posLerpX * _parallaxWeightADS, -_posLerpY * _parallaxWeightADS, 0);
        }
    }
}
