using UnityEngine;
using EFT.Animations;
using Aki.Reflection.Patching;
using System.Reflection;
using EFT;
using static EFT.Player;
using HarmonyLib;
using EFT.HealthSystem;

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
            if (!PrimeMover.IsWeaponSway.Value)
            {
                return;
            }

            if (WeaponHandlingController.SwayUpdatedThisFrame)
            {
                return;
            }
            if ((Object)(object)__instance == (Object)null)
            {
                return;
            }
            Player.FirearmController firearmController = (Player.FirearmController)fcField.GetValue(__instance);
            if ((Object)(object)firearmController == (Object)null)
            {
                return;
            }
            Player player = (Player)playerField.GetValue(firearmController);
            if ((Object)(object)player != (Object)null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                float healthCommon = player.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
                float armHealthR = player.HealthController.GetBodyPartHealth(EBodyPart.RightArm).Normalized;
                float armHealthL = player.HealthController.GetBodyPartHealth(EBodyPart.LeftArm).Normalized;
                float stamNormalized = player.Physical.Stamina.Current / 104f;
                float handStamNormalized = player.Physical.HandsStamina.Current / 70f;
                float strength = player.Skills.Strength.Current;
                float currentWeight = player.Physical.PreviousWeight;

                float healthMulti = 1f + ((1f - healthCommon) * .2f);
                float armHealthRMulti = 1f + ((1f - armHealthR) * .2f);
                float armHealthLMulti = 1f + ((1f - armHealthL) * .2f);
                float stamMulti = 1f + ((1f - stamNormalized) * .1f);
                float handStamMulti = 1f + ((1f - handStamNormalized) * .1f);
                float strengthMulti = 1f - (strength / 7500);
                float underweightReduction = Mathf.Clamp01(currentWeight / (strength * .034f));

                float speedMulti = player.Speed / .6f;
                if (!WeaponHandlingController.IsPlayerMovement)
                {
                    speedMulti = 0;
                }
                speedMulti = Mathf.Clamp(speedMulti, .25f, 1f);

                WeaponHandlingController.UpdateTransformHistory(player.Position);
                WeaponHandlingController.UpdateRotationHistory(player.Rotation);

                float weaponWeight = WeaponHandlingController.CurrentWeaponWeight;
                Vector3 newSwayFactors = __instance.MotionReact.SwayFactors;


                // vertical axis
                bool flag3 = firearmController.Weapon.WeapClass == "pistol";
                newSwayFactors.x *= -0.3f * weaponWeight;
                if (WeaponHandlingController.VerticalTrend < 0 && !flag3)
                {
                    newSwayFactors.x *= -1f;
                }

                // 'z' axis (as in, projecting from the camera)
                newSwayFactors.y *= -.2f * weaponWeight;

                // horizontal axis ***
                float addedSway = _primarySwayValue * weaponWeight * speedMulti * strengthMulti * underweightReduction * healthMulti * armHealthRMulti * armHealthLMulti * stamMulti * handStamMulti;

                if (__instance.IsAiming)
                {
                    bool flag1 = firearmController.Weapon.GetFoldable() != null && firearmController.Weapon.Folded;
                    bool flag2 = firearmController.Weapon.WeapClass == "pistol";
                    addedSway *= -2f;
                    if (flag1)
                    {
                        addedSway *= -1f;
                    }
                    else if (flag2)
                    {
                        addedSway *= -3f;
                    }
                }
                else
                {
                    float rotDeltaEval = PrimeMover.Instance.WeapSwayCurve.Evaluate(WeaponHandlingController.RotationDelta * 1000f);
                    addedSway *= rotDeltaEval;
                }
                addedSway *= PrimeMover.WeaponSwayGlobalMultiplier.Value;

                // push values
                newSwayFactors.z *= addedSway;
                __instance.MotionReact.SwayFactors = newSwayFactors;
                WeaponHandlingController.SwayUpdatedThisFrame = true;
            }
            else
            {
                return;
            }    
        }
    }
}