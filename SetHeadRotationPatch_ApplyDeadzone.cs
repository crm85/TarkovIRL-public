using UnityEngine;
using EFT.Animations;
using Aki.Reflection.Patching;
using System.Reflection;
using EFT;
using System;
using EFT.UI;
using HarmonyLib;
using static MultiFlareLight;

namespace TarkovIRL
{
    public class SetHeadRotationPatch_ApplyDeadzone : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;
        private static float target = 0;
        private static float _lerpRate = 10f;

        static int _turnState1 = -31136456;
        static int _turnState2 = 287005718;

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


                bool flag1 = player.MovementContext.CurrentState.AnimatorStateHash == _turnState1;
                bool flag2 = player.MovementContext.CurrentState.AnimatorStateHash == _turnState2;
                bool flag3 = flag1 || flag2;


                float headDeltaRaw = player.MovementContext.DeltaRotation;
                float headDeltaTaperMulti = Mathf.Abs(headDeltaRaw / 45f);
                headDeltaTaperMulti = PrimeMover.Instance.deadZoneCurve.Evaluate(headDeltaTaperMulti);
                float headDeltaAdjusted = WeaponHandlingController.ProcessHeadDelta(headDeltaRaw);

                float finalValue = headDeltaAdjusted * headDeltaTaperMulti;

                if (flag3)
                {
                    finalValue = 0;
                }

                float lerpRate = _lerpRate;

                if (__instance.IsAiming)
                {
                    finalValue = 0;
                    lerpRate = _lerpRate * (1f / (WeaponHandlingController.TargetErgo * WeaponHandlingController.TotalWeaponWeight * 2f));
                }


                target = Mathf.Lerp(target, finalValue, Time.deltaTime * lerpRate);
                headRotThisFrame.y += target;

                AccessTools.Field(typeof(ProceduralWeaponAnimation), "_headRotationVec").SetValue(__instance, headRotThisFrame);
            }
        }
    }
}