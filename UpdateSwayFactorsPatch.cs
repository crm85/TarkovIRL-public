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
        private static readonly float _PrimarySwayValue = -0.5f;

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
                float weaponWeight = WeaponsHandlingController.CurrentWeaponWeight;
                bool isFolded = firearmController.Weapon.GetFoldable() != null && firearmController.Weapon.Folded;
                bool isPistol = firearmController.Weapon.WeapClass == "pistol";

                Vector3 newSwayFactors = __instance.MotionReact.SwayFactors;

                // vertical axis
                newSwayFactors.x *= -0.3f * weaponWeight;
                if (PlayerMotionController.VerticalTrend < 0 && !isPistol)
                {
                    newSwayFactors.x *= -1f;
                }

                // z axis
                newSwayFactors.y *= -.2f * weaponWeight;

                // horizontal axis ***
                float speedMulti = PlayerMotionController.IsPlayerMovement ? PlayerMotionController.GetNormalSpeed(player) : 0.25f;
                float addedSway = _PrimarySwayValue * weaponWeight * speedMulti * WeaponsHandlingController.GetSwayModifier(player);

                WeaponsHandlingController.IsStocked = isFolded || isPistol;

                if (__instance.IsAiming)
                {
                    addedSway *= -2f;
                    if (isFolded)
                    {
                        addedSway *= -0.7f;
                    }
                    else if (isPistol)
                    {
                        addedSway *= -3f;
                    }
                }
                else
                {
                    float rotDeltaEval = PrimeMover.Instance.WeapSwayCurve.Evaluate(PlayerMotionController.RotationDelta * 1000f);
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