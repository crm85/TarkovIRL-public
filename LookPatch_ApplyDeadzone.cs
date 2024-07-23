using UnityEngine;
using EFT.Animations;
using SPT.Reflection.Patching;
using System.Reflection;
using EFT;
using System;
using EFT.UI;
using HarmonyLib;
using static MultiFlareLight;

namespace TarkovIRL
{
    public class LookPatch_ApplyDeadzone : ModulePatch
    {
        static FieldInfo _playerField;
        static FieldInfo _fcField;

        static readonly float _lerpRate = 10f;
        static readonly int _turnState1 = -31136456;
        static readonly int _turnState2 = 287005718;
        static readonly float _rotDeltaThresh = 0.0002f;

        static float _deadZoneLerpTarget = 0;
        static bool _updateDZ = true;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("Look", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Postfix(Player __instance)
        {
            if ((UnityEngine.Object)(object)__instance != (UnityEngine.Object)null && __instance.IsYourPlayer && __instance.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                if (!PrimeMover.IsWeaponDeadzone.Value)
                {
                    return;
                }

                if (!_updateDZ)
                {
                    _updateDZ = WeaponHandlingController.IsPlayerMovement || WeaponHandlingController.RotationDelta > _rotDeltaThresh;
                }

                Vector3 headRotThisFrame = __instance.HeadRotation;
                headRotThisFrame.y *= 1.5f;

                bool flag1 = __instance.MovementContext.CurrentState.AnimatorStateHash == _turnState1;
                bool flag2 = __instance.MovementContext.CurrentState.AnimatorStateHash == _turnState2;
                bool isChangeingStance = flag1 || flag2;

                float headDeltaRaw = __instance.MovementContext.DeltaRotation;
                float headDeltaTaperMulti = Mathf.Abs(headDeltaRaw / 45f);
                //headDeltaTaperMulti = PrimeMover.Instance.DeadZoneCurve.Evaluate(headDeltaTaperMulti);
                float headDeltaAdjusted = WeaponHandlingController.ProcessHeadDelta(headDeltaRaw);

                float finalValue = headDeltaAdjusted * headDeltaTaperMulti;
                float lerpRate = _lerpRate;

                if (isChangeingStance)
                {
                    finalValue = 0;
                    lerpRate *= 0.2f;
                }

                if (__instance.ProceduralWeaponAnimation.IsAiming)
                {
                    _updateDZ = false;
                    finalValue = 0;
                    lerpRate = _lerpRate * (1f / (WeaponHandlingController.CurrentWeaponErgo * WeaponHandlingController.CurrentWeaponWeight * 2f));
                }

                if (!_updateDZ)
                {
                    finalValue = 0;
                }

                _deadZoneLerpTarget = Mathf.Lerp(_deadZoneLerpTarget, finalValue, Time.deltaTime * lerpRate);
                headRotThisFrame.y += _deadZoneLerpTarget * PrimeMover.DeadzoneGlobalMultiplier.Value;

                AccessTools.Field(typeof(ProceduralWeaponAnimation), "_headRotationVec").SetValue(__instance.ProceduralWeaponAnimation, headRotThisFrame);
            }
        }
    }
}