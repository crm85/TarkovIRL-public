using SPT.Reflection.Patching;
using EFT;
using EFT.Animations;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    internal class CalculateCameraPositionPatch_HandLayers : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        private static float _rotationAvgSetX = 0;
        private static float _rotationAvgSetY = 0;
        private static float _rotationAvgX = 0;
        private static float _rotationAvgY = 0;

        private static float _rotationLerpPosX = 0;
        private static float _rotationLerpPosY = 0;

        private static float _rotationLerpRotX = 0;

        private static Vector2 _playerRotationLastFrame = Vector2.zero;



        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("CalculateCameraPosition", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ProceduralWeaponAnimation __instance)
        {
            if ((UnityEngine.Object)(object)__instance == (UnityEngine.Object)null)
            {
                return;
            }
            Player.FirearmController firearmController = (Player.FirearmController)fcField.GetValue(__instance);
            if ((UnityEngine.Object)(object)firearmController == (UnityEngine.Object)null)
            {
                return;
            }
            Player player = (Player)playerField.GetValue(firearmController);
            if ((UnityEngine.Object)(object)player != (UnityEngine.Object)null && player.IsYourPlayer)
            {
                Vector3 addedBreathPosition = HandBreathController.GetModifiedHandPosForBreath(player);
                Vector3 addedPosePosition = HandMovController.GetModifiedHandPosWithPose(player);
                Vector3 addedChangePosePos = HandMovController.GetModifiedHandPosWithPoseChange(player);
                Vector3 addedArmJitterPos = HandShakeController.GetHandsShakePosition(player);

                bool isBreathPos = PrimeMover.IsBreathingEffect.Value;
                bool isPosePos = PrimeMover.IsPoseEffect.Value;
                bool isPoseChangePos = PrimeMover.IsPoseChangeEffect.Value;
                bool isArmShake = PrimeMover.IsArmShakeEffect.Value;

                if (isBreathPos) __instance.HandsContainer.WeaponRoot.localPosition += addedBreathPosition;
                if (isPosePos) __instance.HandsContainer.WeaponRoot.localPosition += addedPosePosition;
                if (isPoseChangePos) __instance.HandsContainer.WeaponRoot.localPosition += addedChangePosePos;
                if (isArmShake) __instance.HandsContainer.WeaponRoot.localPosition += addedArmJitterPos;

                Vector3 addedZPos = HandMovController.GetModifiedHandPosForRotSpeed(player);
                Vector3 addedStockedMovPos = HandMovController.GetModifiedHandPosForUnstockedMovement();

                __instance.HandsContainer.WeaponRoot.localPosition += addedZPos;
                __instance.HandsContainer.WeaponRoot.localPosition += addedStockedMovPos;


                // testing
                Vector2 rotationalMotionThisFrame = _playerRotationLastFrame - player.Rotation;
                _playerRotationLastFrame = player.Rotation;

                _rotationAvgSetX += rotationalMotionThisFrame.x;
                _rotationAvgSetY += rotationalMotionThisFrame.y;
                _rotationAvgSetX -= _rotationAvgX;
                _rotationAvgSetY -= _rotationAvgY;
                _rotationAvgSetX = Mathf.Clamp(_rotationAvgSetX, -PrimeMover.DevTestFloat1.Value, PrimeMover.DevTestFloat1.Value);
                _rotationAvgSetY = Mathf.Clamp(_rotationAvgSetY, -PrimeMover.DevTestFloat1.Value, PrimeMover.DevTestFloat1.Value);
                _rotationAvgX = _rotationAvgSetX * player.DeltaTime;
                _rotationAvgY = _rotationAvgSetY * player.DeltaTime;


                float lerpRate = player.DeltaTime * 2f;
                _rotationLerpPosX = Mathf.Lerp(_rotationLerpPosX, _rotationAvgX, lerpRate * PrimeMover.DevTestFloat3.Value);
                //_rotLerpY = Mathf.Lerp(_rotLerpY, _rotAvgY, lerpRate);
                __instance.HandsContainer.WeaponRoot.localPosition = __instance.HandsContainer.WeaponRoot.localPosition + new Vector3(_rotationLerpPosX * PrimeMover.DevTestFloat4.Value, 0, 0);
                // this works ^ 


                _rotationLerpRotX = Mathf.Lerp(_rotationLerpRotX, _rotationAvgX, lerpRate * PrimeMover.DevTestFloat.Value);
                //__instance.HandsContainer.Weapon.localRotation = __instance.HandsContainer.Weapon.localRotation * Quaternion.Euler(0, PrimeMover.DevTestFloat1.Value, 0);
                __instance.HandsContainer.Weapon.localRotation = __instance.HandsContainer.Weapon.localRotation * Quaternion.Euler(0, 0, -_rotationLerpRotX * PrimeMover.DevTestFloat2.Value);
            }
        }
    }
}
