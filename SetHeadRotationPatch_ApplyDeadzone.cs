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
        static FieldInfo playerField;
        static FieldInfo fcField;

        static readonly float _lerpRate = 10f;
        static readonly int _turnState1 = -31136456;
        static readonly int _turnState2 = 287005718;

        static float _deadZoneLerpTarget = 0;
        static bool _updateDZ = true;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("SetHeadRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Postfix(ProceduralWeaponAnimation __instance)
        {
            if (!_updateDZ)
            {
                Utils.Log(true, string.Format($"rot delta is {WeaponHandlingController.RotationDelta}"));
                _updateDZ = WeaponHandlingController.IsPlayerMovement || WeaponHandlingController.RotationDelta > 0.0002f;
            }

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

                headRotThisFrame.y *= 1.5f;

                bool flag1 = player.MovementContext.CurrentState.AnimatorStateHash == _turnState1;
                bool flag2 = player.MovementContext.CurrentState.AnimatorStateHash == _turnState2;
                bool flag3 = flag1 || flag2;


                float headDeltaRaw = player.MovementContext.DeltaRotation;
                float headDeltaTaperMulti = Mathf.Abs(headDeltaRaw / 45f);
                headDeltaTaperMulti = PrimeMover.Instance.DeadZoneCurve.Evaluate(headDeltaTaperMulti);
                float headDeltaAdjusted = WeaponHandlingController.ProcessHeadDelta(headDeltaRaw);

                float finalValue = headDeltaAdjusted * headDeltaTaperMulti;

                if (flag3)
                {
                    finalValue = 0;
                }

                float lerpRate = _lerpRate;

                if (__instance.IsAiming)
                {
                    _updateDZ = false;
                    finalValue = 0;
                    lerpRate = _lerpRate * (1f / (WeaponHandlingController.TargetErgo * WeaponHandlingController.TotalWeaponWeight * 2f));
                }

                if (!_updateDZ)
                {
                    finalValue = 0;
                }


                _deadZoneLerpTarget = Mathf.Lerp(_deadZoneLerpTarget, finalValue, Time.deltaTime * lerpRate);
                headRotThisFrame.y += _deadZoneLerpTarget;

                AccessTools.Field(typeof(ProceduralWeaponAnimation), "_headRotationVec").SetValue(__instance, headRotThisFrame);
            }
        }
    }
}