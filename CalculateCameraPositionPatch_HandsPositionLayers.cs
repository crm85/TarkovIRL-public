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
    internal class CalculateCameraPositionPatch_HandsPositionLayers : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        private static float _rotAvgXSet = 0;
        private static float _rotAvgYSet = 0;
        private static float _rotAvgX = 0;
        private static float _rotAvgY = 0;
        private static Vector2 _rotAvgVector = Vector2.zero;

        private static float _rotLerpX = 0;
        private static float _rotLerpY = 0;
        private static Vector2 _playerRotationLastFrame = Vector2.zero;


        private static float _rotLerpXforRot = 0;

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
                Vector3 addedBreathPosition = HandsMovController.GetModifiedHandPosForBreath(player);
                Vector3 addedPosePosition = HandsMovController.GetModifiedHandPosWithPose(player);
                Vector3 addedChangePosePos = HandsMovController.GetModifiedHandPosWithPoseChange(player);
                Vector3 addedArmJitterPos = HandsMovController.GetModifiedHandPosForArmStam(player);

                bool isBreathPos = PrimeMover.IsBreathingEffect.Value;
                bool isPosePos = PrimeMover.IsPoseEffect.Value;
                bool isPoseChangePos = PrimeMover.IsPoseChangeEffect.Value;
                bool isArmShake = PrimeMover.IsArmJitterEffect.Value;

                if (isBreathPos) __instance.HandsContainer.WeaponRoot.localPosition += addedBreathPosition;
                if (isPosePos) __instance.HandsContainer.WeaponRoot.localPosition += addedPosePosition;
                if (isPoseChangePos) __instance.HandsContainer.WeaponRoot.localPosition += addedChangePosePos;
                if (isArmShake) __instance.HandsContainer.WeaponRoot.localPosition += addedArmJitterPos;

                Vector3 addedZPos = HandsMovController.GetModifiedHandPosForRotSpeed();
                Vector3 addedStockedMovPos = HandsMovController.GetModifiedHandPosForUnstockedMovement();

                __instance.HandsContainer.WeaponRoot.localPosition += addedZPos;
                __instance.HandsContainer.WeaponRoot.localPosition += addedStockedMovPos;


                // testing
                Vector2 rotationalMotionThisFrame = _playerRotationLastFrame - player.Rotation;
                _playerRotationLastFrame = player.Rotation;

                _rotAvgXSet += rotationalMotionThisFrame.x;
                _rotAvgYSet += rotationalMotionThisFrame.y;
                _rotAvgXSet -= _rotAvgX;
                _rotAvgYSet -= _rotAvgY;
                _rotAvgXSet = Mathf.Clamp(_rotAvgXSet, -PrimeMover.DevTestFloat1.Value, PrimeMover.DevTestFloat1.Value);
                _rotAvgYSet = Mathf.Clamp(_rotAvgYSet, -PrimeMover.DevTestFloat1.Value, PrimeMover.DevTestFloat1.Value);
                _rotAvgX = _rotAvgXSet * player.DeltaTime;
                _rotAvgY = _rotAvgYSet * player.DeltaTime;


                float lerpRate = player.DeltaTime * 2f;
                _rotLerpX = Mathf.Lerp(_rotLerpX, _rotAvgX, lerpRate * PrimeMover.DevTestFloat3.Value);
                //_rotLerpY = Mathf.Lerp(_rotLerpY, _rotAvgY, lerpRate);
                __instance.HandsContainer.WeaponRoot.localPosition = __instance.HandsContainer.WeaponRoot.localPosition + new Vector3(_rotLerpX * PrimeMover.DevTestFloat4.Value, 0, 0);
                // this works ^ 


                _rotLerpXforRot = Mathf.Lerp(_rotLerpXforRot, _rotAvgX, lerpRate * PrimeMover.DevTestFloat.Value);
                //__instance.HandsContainer.Weapon.localRotation = __instance.HandsContainer.Weapon.localRotation * Quaternion.Euler(0, PrimeMover.DevTestFloat1.Value, 0);
                __instance.HandsContainer.Weapon.localRotation = __instance.HandsContainer.Weapon.localRotation * Quaternion.Euler(0, 0, -_rotLerpXforRot * PrimeMover.DevTestFloat2.Value);
            }
        }
    }
}
