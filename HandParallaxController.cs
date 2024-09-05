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
    public static class HandParallaxController
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

        static float _avgSetSizeMulti = 0.5f;
        static bool _aimingLastFrame = false;

        public static void GetModifiedHandPosRotParallax(Player player, ref Vector3 position, ref Quaternion rotation)
        {
            float weaponMulti = WeaponController.CurrentWeaponWeight * (1f - WeaponController.CurrentWeaponErgoNorm);
            float efficiencyMulti = WeaponController.GetEfficiencyModifier(player);
            float poseLevel = Mathf.Clamp(player.PoseLevel, 0.5f, 1f);
            float speedMulti = PlayerMotionController.IsPlayerMovement ? PlayerMotionController.GetNormalSpeed(player) : 0.5f;

            AdsTimer.UpdateLerps();

            if (WeaponController.IsStocked)
            {
                bool isNewAds = !_aimingLastFrame && player.ProceduralWeaponAnimation.IsAiming;
                bool isFinishAds = _aimingLastFrame && !player.ProceduralWeaponAnimation.IsAiming;
                _aimingLastFrame = player.ProceduralWeaponAnimation.IsAiming;

                float adsEfficiencyMulti = 1f / efficiencyMulti;
                float adsMulti = weaponMulti * adsEfficiencyMulti * poseLevel * speedMulti;
                if (isNewAds)
                {
                    AdsTimer.StartNewAds(true, adsMulti);
                }
                else if (isFinishAds)
                {
                    AdsTimer.StartNewAds(false, adsMulti);
                }
            }

            Vector2 rotationalMotionThisFrame = _playerRotationLastFrame - player.Rotation;
            _playerRotationLastFrame = player.Rotation;

            _rotAvgXSet += rotationalMotionThisFrame.x;
            _rotAvgYSet += rotationalMotionThisFrame.y;
            _rotAvgXSet -= _rotAvgX;
            _rotAvgYSet -= _rotAvgY;
            _rotAvgXSet = Mathf.Clamp(_rotAvgXSet, -_avgSetSizeMulti, _avgSetSizeMulti);
            _rotAvgYSet = Mathf.Clamp(_rotAvgYSet, -_avgSetSizeMulti, _avgSetSizeMulti);
            _rotAvgX = _rotAvgXSet * player.DeltaTime;
            _rotAvgY = _rotAvgYSet * player.DeltaTime;

            _parallaxWeightADS = AdsTimer.ParallaxWeight;

            float dt = player.DeltaTime;
            float parallaxMulti = PrimeMover.ParallaxMulti.Value * weaponMulti * efficiencyMulti * poseLevel * speedMulti;

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
