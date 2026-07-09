using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace TarkovIRL
{
    internal class Patch_ShiftWeaponRoot_RunPose : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PlayerBones).GetMethod("ShiftWeaponRoot", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(PlayerBones __instance, EPointOfView pv)
        {
            if ((Object)(object)__instance == (Object)null || __instance.Weapon_Root_Anim == null)
            {
                return;
            }

            Player player = __instance.Player;
            if ((Object)(object)player == (Object)null || !player.IsYourPlayer)
            {
                return;
            }

            if (pv != EPointOfView.FirstPerson)
            {
                RunningFadeController.Reset();
                return;
            }

            RunningFadeController.GetRunPoseOffsets(player, out Vector3 runPosePosition, out Quaternion runPoseRotation);
            __instance.Weapon_Root_Anim.localPosition += runPosePosition;
            __instance.Weapon_Root_Anim.localRotation *= runPoseRotation;
        }
    }
}
