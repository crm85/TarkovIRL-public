using SPT.Reflection.Patching;
using EFT;
using EFT.Animations;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    internal class Patch_CalculateCameraPosition_HandLayers : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("CalculateCameraPosition", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ProceduralWeaponAnimation __instance)
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
            if ((UnityEngine.Object)(object)player != (UnityEngine.Object)null && player.IsYourPlayer)
            {
                WeaponController.IsUsingMounted = IsMountedOrStationaryWeapon(__instance, player, firearmController);

                if (AnimStateController.IsBlindfire)
                {
                    RunningFadeController.Reset();
                    ParallaxAdsController.Reset();
                    DirectionalSwayController.Reset();
                    NewSwayController.Reset();
                    FootstepController.Reset();
                    return;
                }

                if (WeaponController.IsUsingMounted)
                {
                    RunningFadeController.Reset();
                    ParallaxController.Reset(player);
                    ParallaxAdsController.Reset();
                    DirectionalSwayController.Reset();
                    NewSwayController.Reset();
                    FootstepController.Reset();
                    return;
                }

                EfficiencyController.UpdateEfficiency(player);

                Vector3 breathOffset = HandBreathController.GetModifiedHandPosForBreath(player);
                Vector3 armStamOffset = HandShakeController.GetHandsShakePosition(player);
                Vector3 poseOffset = HandPoseController.GetModifiedHandPosWithPose(player);
                Quaternion poseOffsetRot = HandPoseController.GetModifiedHandRotWithPoseChange();
                Vector3 changePoseOffset = HandPoseController.GetModifiedHandPosWithPoseChange(player);
                Vector3 movementZOffsets = HandMovWithRotController.GetModifiedHandPosZMovement(player);
                Vector3 unstockedOffset = HandMovWithRotController.GetModifiedHandPosForLoweredWeapon(player);
                Vector3 footstepOffset = FootstepController.GetModifiedHandPosFootstep;
                Vector3 parallaxPosition = __instance.HandsContainer.WeaponRoot.localPosition;
                Quaternion parallaxRotation = __instance.HandsContainer.WeaponRoot.localRotation;
                ParallaxController.GetModifiedHandPosRotParallax(player, ref parallaxPosition, ref parallaxRotation);
                Vector3 addedSwayPosition = NewSwayController.GetNewSwayPosition();
                Quaternion addedSwayRotation = NewSwayController.GetNewSwayRotation();
                Vector3 sideToSidePosition = FootstepController.GetSideToSidePosition();
                Quaternion sideToSideRotation = FootstepController.GetSideToSideRotation();

                bool isBreathPos = PrimeMover.IsBreathingEffect.Value;
                bool isPosePos = PrimeMover.IsPoseEffect.Value;
                bool isPoseChangePos = PrimeMover.IsPoseChangeEffect.Value;
                bool isArmShake = PrimeMover.IsArmShakeEffect.Value;
                bool isSmallEffects = PrimeMover.IsSmallMovementsEffect.Value;
                bool isFootstep = PrimeMover.IsFootstepEffect.Value;
                bool isParallax = PrimeMover.IsParallaxEffect.Value;
                bool isNewSway = PrimeMover.IsWeaponSway.Value;

                Vector3 addedPosition = Vector3.zero;
                Quaternion addedRotation = Quaternion.identity;

                if (isBreathPos) addedPosition += breathOffset;
                if (isPosePos)
                {
                    addedPosition += poseOffset;
                }
                if (isPoseChangePos)
                {
                    addedPosition += changePoseOffset;
                    addedRotation *= poseOffsetRot;
                }
                if (isArmShake) addedPosition += armStamOffset;
                if (isSmallEffects)
                {
                    addedPosition += movementZOffsets;
                    addedPosition += unstockedOffset;
                }
                if (isParallax)
                {
                    addedPosition += parallaxPosition;
                    addedRotation *= parallaxRotation;

                }
                if (isFootstep)
                {
                    addedPosition += footstepOffset;
                    addedPosition += sideToSidePosition;
                    addedRotation *= sideToSideRotation;
                }
                if (isNewSway)
                {
                    addedPosition += addedSwayPosition;
                    addedRotation *= addedSwayRotation;
                }

                DirectionalSwayController.GetDirectionalSway(out Vector3 directionalSwayPos, out Quaternion directionalSwayRot);
                addedPosition += directionalSwayPos;
                addedRotation *= directionalSwayRot;

                // weapon selection transitions
                WeaponSelectionController.GetWeaponSelectionTransforms(out Vector3 WeaponTransitionPos, out Quaternion WeaponTransitionRot);
                addedPosition += WeaponTransitionPos;
                addedRotation *= WeaponTransitionRot;

                __instance.HandsContainer.WeaponRoot.localPosition += addedPosition;
                __instance.HandsContainer.WeaponRoot.localRotation *= addedRotation;

            }
        }

        static bool IsMountedOrStationaryWeapon(ProceduralWeaponAnimation proceduralWeaponAnimation, Player player, Player.FirearmController firearmController)
        {
            if (proceduralWeaponAnimation.IsMountedState)
            {
                return true;
            }

            if (player.MovementContext == null)
            {
                return false;
            }

            if (player.MovementContext.CurrentState.Name == EPlayerState.Stationary || player.MovementContext.IsInMountedState || player.MovementContext.StationaryWeapon != null)
            {
                return true;
            }

            if (firearmController?.Weapon != null && firearmController.Weapon.IsStationaryWeapon)
            {
                return true;
            }

            return false;
        }
    }
}
