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
                Vector3 breathOffset = HandBreathController.GetModifiedHandPosForBreath(player);
                Vector3 armStamOffset = HandShakeController.GetHandsShakePosition(player);
                Vector3 poseOffset = HandPoseController.GetModifiedHandPosWithPose(player);
                Vector3 changePoseOffset = HandPoseController.GetModifiedHandPosWithPoseChange(player);

                Vector3 movementZOffsets = HandMovController.GetModifiedHandPosZMovement(player);
                Vector3 unstockedOffset = HandMovController.GetModifiedHandPosForUnstockedMovement(player);



                bool isBreathPos = PrimeMover.IsBreathingEffect.Value;
                bool isPosePos = PrimeMover.IsPoseEffect.Value;
                bool isPoseChangePos = PrimeMover.IsPoseChangeEffect.Value;
                bool isArmShake = PrimeMover.IsArmShakeEffect.Value;

                if (isBreathPos) __instance.HandsContainer.WeaponRoot.localPosition += breathOffset;
                if (isPosePos) __instance.HandsContainer.WeaponRoot.localPosition += poseOffset;
                if (isPoseChangePos) __instance.HandsContainer.WeaponRoot.localPosition += changePoseOffset;
                if (isArmShake) __instance.HandsContainer.WeaponRoot.localPosition += armStamOffset;
                __instance.HandsContainer.WeaponRoot.localPosition += movementZOffsets;
                __instance.HandsContainer.WeaponRoot.localPosition += unstockedOffset;



                Vector3 parallaxPosition = __instance.HandsContainer.WeaponRoot.localPosition;
                Quaternion parallaxRotation = __instance.HandsContainer.WeaponRoot.localRotation;
                HandParallaxController.GetModifiedHandPosRotParallax(player, ref parallaxPosition, ref parallaxRotation);

                __instance.HandsContainer.WeaponRoot.localPosition += parallaxPosition;
                __instance.HandsContainer.WeaponRoot.localRotation *= parallaxRotation;



                if (!PrimeMover.IsWeaponDeadzone.Value)
                {
                    return;
                }

                Vector3 headRotThisFrame = DeadzoneController.GetHeadRotationWithDeadzone(player);
                AccessTools.Field(typeof(ProceduralWeaponAnimation), "_headRotationVec").SetValue(__instance, headRotThisFrame);
            }
        }
    }
}
