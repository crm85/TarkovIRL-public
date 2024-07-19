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
    internal class CalculateCameraPositionPatch : ModulePatch
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
                Vector3 addedBreathPosition = HandsMovController.GetModifiedHandPosForBreath(player);
                Vector3 addedPosePosition = HandsMovController.GetModifiedHandPosWithPose(player);
                Vector3 addedChangePosePos = HandsMovController.GetModifiedHandPosWithPoseChange(player);

                bool isBreathPos = PrimeMover.IsBreathingEffect.Value;
                bool isPosePos = PrimeMover.IsPoseEffect.Value;
                bool isPoseChangePos = PrimeMover.IsPoseChangeEffect.Value;

                if (isBreathPos) __instance.HandsContainer.WeaponRoot.localPosition += addedBreathPosition;
                if (isPosePos) __instance.HandsContainer.WeaponRoot.localPosition += addedPosePosition;
                if (isPoseChangePos) __instance.HandsContainer.WeaponRoot.localPosition += addedChangePosePos;
            }
        }
    }
}
