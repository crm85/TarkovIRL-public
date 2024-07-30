using UnityEngine;
using SPT.Reflection.Patching;
using System.Reflection;
using HarmonyLib;
using EFT;
using EFT.Animations;

namespace TarkovIRL
{
    internal class UpdateSwayFactorsPatch : ModulePatch
    {

        private static FieldInfo playerField;
        private static FieldInfo fcField;
        private static readonly float _primarySwayValue = -0.5f;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("UpdateSwayFactors", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Postfix(ProceduralWeaponAnimation __instance)
        {

            if ((Object)(object)__instance == (Object)null)
            {
                return;
            }

            if (!PrimeMover.IsWeaponSway.Value)
            {
                return;
            }

            if (WeaponsHandlingController.IsSwayUpdatedThisFrame)
            {
                return;
            }

            Player.FirearmController firearmController = (Player.FirearmController)fcField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }

            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                WeaponsHandlingController.UpdateTransformHistory(player.Position);
                WeaponsHandlingController.UpdateRotationHistory(player.Rotation);

                float weaponWeight = WeaponsHandlingController.CurrentWeaponWeight;
                Vector3 newSwayFactors = __instance.MotionReact.SwayFactors;

                // vertical axis
                bool flag3 = firearmController.Weapon.WeapClass == "pistol";
                newSwayFactors.x *= -0.3f * weaponWeight;
                if (WeaponsHandlingController.VerticalTrend < 0 && !flag3)
                {
                    newSwayFactors.x *= -1f;
                }

                // 'z' axis (as in, projecting from the camera)
                newSwayFactors.y *= -.2f * weaponWeight;

                // horizontal axis ***
                float addedSway = _primarySwayValue * weaponWeight * WeaponsHandlingController.GetSpeedModifier(player) * WeaponsHandlingController.GetGeneralEfficiencyModifier(player);
                bool flag1 = firearmController.Weapon.GetFoldable() != null && firearmController.Weapon.Folded;
                bool flag2 = firearmController.Weapon.WeapClass == "pistol";
                WeaponsHandlingController.IsStocked = flag1 || flag2;

                if (__instance.IsAiming)
                {
                    addedSway *= -2f;
                    if (flag1)
                    {
                        addedSway *= -0.7f;
                    }
                    else if (flag2)
                    {
                        addedSway *= -3f;
                    }
                }
                else
                {
                    float rotDeltaEval = PrimeMover.Instance.WeapSwayCurve.Evaluate(WeaponsHandlingController.RotationDelta * 1000f);
                    addedSway *= rotDeltaEval;
                }
                addedSway *= PrimeMover.WeaponSwayGlobalMultiplier.Value;

                // push values
                newSwayFactors.z *= addedSway;
                __instance.MotionReact.SwayFactors = newSwayFactors;
                WeaponsHandlingController.IsSwayUpdatedThisFrame = true;
            }
            else
            {
                return;
            }
        }
    }
}