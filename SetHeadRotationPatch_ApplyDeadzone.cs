using UnityEngine;
using EFT.Animations;
using Aki.Reflection.Patching;
using System.Reflection;
using EFT;
using System;
using EFT.UI;
using HarmonyLib;

namespace TarkovIRL
{
    public class SetHeadRotationPatch_ApplyDeadzone : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;
        private static float target = 0;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("SetHeadRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Postfix(ProceduralWeaponAnimation __instance)
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
            if ((UnityEngine.Object)(object)player != (UnityEngine.Object)null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {

                Vector3 headRotThisFrame = player.HeadRotation;

                headRotThisFrame.x *= 1.5f;
                headRotThisFrame.y *= 1.5f;
                headRotThisFrame.z *= 1.5f;
                // ^ just adds +50% head freelook rotation

                float headDeltaRaw = player.MovementContext.DeltaRotation;
                float headDeltaAdjusted = WeaponHandlingController.ProcessHeadDelta(headDeltaRaw);
                float weightMulti = WeaponHandlingController.TotalWeaponWeight * 0.1f;
                float finalValue = headDeltaAdjusted * weightMulti;

                float lerpRate = 10f;

                if (__instance.IsAiming)
                {
                    finalValue = 0;
                    lerpRate = WeaponHandlingController.TargetErgo * WeaponHandlingController.TotalWeaponWeight * 2f;
                }

                target = Mathf.Lerp(target, finalValue, Time.deltaTime * lerpRate);
                headRotThisFrame.y += target;

                AccessTools.Field(typeof(ProceduralWeaponAnimation), "_headRotationVec").SetValue(__instance, headRotThisFrame);
            }
        }
    }
}