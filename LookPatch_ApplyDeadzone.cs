using UnityEngine;
using EFT.Animations;
using SPT.Reflection.Patching;
using System.Reflection;
using EFT;
using HarmonyLib;

namespace TarkovIRL
{
    public class LookPatch_ApplyDeadzone : ModulePatch
    {
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