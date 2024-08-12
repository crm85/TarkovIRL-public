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

                Vector3 headRotThisFrame = DeadzoneController.GetHeadRotationWithDeadzone(__instance);
                AccessTools.Field(typeof(ProceduralWeaponAnimation), "_headRotationVec").SetValue(__instance.ProceduralWeaponAnimation, headRotThisFrame);
            }
        }
    }
}