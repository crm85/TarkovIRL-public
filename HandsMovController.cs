using EFT;
using EFT.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    public static class HandsMovController
    {
        static float lerpRate = 4f;
        static float currentLerpRate = 0;

        static float poseVerticalPoseOffsetModifier = .03f;
        static float poseProjectedPoseOffsetModifier = -0.05f;

        static float poseVerticalPoseOffsetTarget = 0;
        static float poseProjectedPoseOffsetTarget = 0;

        static float poseVerticalPoseOffsetLerp = 0;
        static float poseProjectedPoseOffsetLerp = 0;

        static float breathVerticalOffsetModifier = 0.0075f;
        static float breathSpeedModifier = 0.55f;

        static float poseLevelLastFrame = 0;

        static float breathUpdateTimer = 0;
        static float armStamLoopTimerX = 0;
        static float armStamLoopTimerY = 0;

        static Vector3 poseShiftVector = Vector3.zero;
        static bool isChangingPose = false;
        static float changingPoseCycle = 0;
        static float vertDiff = 0;
        static float changePoseModifier = 0.01f;

        static readonly float ArmStamDTMulti = 0.25f;
        static readonly float ArmStamMultiFixed = 0.005f;



        public static void UpdateLerp(float dt)
        {
            poseVerticalPoseOffsetLerp = Mathf.Lerp(poseVerticalPoseOffsetLerp, poseVerticalPoseOffsetTarget, dt * currentLerpRate);
            poseProjectedPoseOffsetLerp = Mathf.Lerp(poseProjectedPoseOffsetLerp, poseProjectedPoseOffsetTarget, dt * currentLerpRate);
            ChangePoseUpdate(dt);
        }

        public static Vector3 GetModifiedHandPosForArmStam(Player player)
        {
            AnimationCurve armCurve = PrimeMover.Instance.ArmStamJitterCurve;
            float armStamNorm = player.Physical.HandsStamina.Current / 80f;
            float healthCommon = player.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
            float armHealthR = player.HealthController.GetBodyPartHealth(EBodyPart.RightArm).Normalized;
            float armHealthL = player.HealthController.GetBodyPartHealth(EBodyPart.LeftArm).Normalized;
            float strength = player.Skills.Strength.Current;
            float poseLevel = player.PoseLevel;

            float armStamMulti = 1f - armStamNorm;
            float healthMulti = 1f + ((1f - healthCommon) * .2f);
            float armHealthRMulti = 1f + ((1f - armHealthR) * .2f);
            float armHealthLMulti = 1f + ((1f - armHealthL) * .2f);
            float strengthMulti = 1f - (strength / 15000);
            float poseLevelMulti = 1f + poseLevel;

            armStamLoopTimerX += player.DeltaTime * ArmStamDTMulti * 0.37f;
            if (armStamLoopTimerX >= 1f)
            {
                armStamLoopTimerX -= 1f;
            }

            armStamLoopTimerY -= player.DeltaTime * ArmStamDTMulti;
            if (armStamLoopTimerY <= 0)
            {
                armStamLoopTimerY += 1f;
            }

            float finalMulti = armStamMulti * healthMulti * armHealthLMulti * armHealthRMulti * strengthMulti * poseLevelMulti;

            float armJitterModX = armCurve.Evaluate(armStamLoopTimerX) * finalMulti * PrimeMover.ArmStamJitter.Value * ArmStamMultiFixed;
            float armJitterModY = armCurve.Evaluate(armStamLoopTimerY) * finalMulti * PrimeMover.ArmStamJitter.Value * ArmStamMultiFixed;

            return new Vector3(armJitterModX, armJitterModY, 0);
        }

        public static Vector3 GetModifiedHandPosForBreath(Player player)
        {
            AnimationCurve breathCurve = PrimeMover.Instance.BreathCurve;
            float stamNormalized = player.Physical.Stamina.Current / 104f;
            float stamModifier = 1f - stamNormalized;
            float breathModifier = 1f + stamModifier;
            breathUpdateTimer += player.DeltaTime * breathModifier * breathSpeedModifier;
            if (breathUpdateTimer >= 1f)
            {
                breathUpdateTimer -= 1f;
            }
            float breathValue = breathCurve.Evaluate(breathUpdateTimer);
            float stamModClamped = Mathf.Clamp(stamModifier, 0.025f, 1f);
            float breathOffset = breathValue * breathVerticalOffsetModifier * stamModClamped * PrimeMover.BreathingEffectMulti.Value;
            return new Vector3(0, breathOffset, 0);
        }

        public static Vector3 GetModifiedHandPosWithPose(Player player)
        {
            float poseLevel = player.PoseLevel;
            poseVerticalPoseOffsetTarget = (1f - poseLevel) * poseVerticalPoseOffsetModifier;
            poseProjectedPoseOffsetTarget = (1f - poseLevel) * poseProjectedPoseOffsetModifier;

            if (player.ProceduralWeaponAnimation.IsAiming)
            {
                poseVerticalPoseOffsetTarget = 0;
                poseProjectedPoseOffsetTarget = 0;
            }

            return new Vector3(0, poseVerticalPoseOffsetLerp, poseProjectedPoseOffsetLerp);
        }

        public static Vector3 GetModifiedHandPosWithPoseChange(Player player)
        {
            Vector3 addedVert = poseShiftVector;
            float poseLevelThisFrame = player.PoseLevel;
            if (poseLevelThisFrame != poseLevelLastFrame)
            {
                vertDiff = poseLevelThisFrame - poseLevelLastFrame;
                if (!isChangingPose)
                {
                    isChangingPose = true;
                }
            }

            if (!player.ProceduralWeaponAnimation.IsAiming)
            {
                addedVert.y *= 3f;
            }
            
            poseLevelLastFrame = poseLevelThisFrame;
            return addedVert;
        }

        static void ChangePoseUpdate(float dt)
        {
            if (isChangingPose)
            {
                float vertDiffAbs = Mathf.Abs(vertDiff);
                float speedMod = 1f + (1f - vertDiffAbs);
                float directionSpeedMod = vertDiff < 0 ? 1.1f : 0.75f;
                changingPoseCycle += dt * speedMod * directionSpeedMod;
                if (changingPoseCycle >= 1f)
                {
                    isChangingPose = false;
                    changingPoseCycle = 0;
                    return;
                }
                float directionAttenuationMod = vertDiff < 0 ? 1f : 0.5f;
                float addedVert = PrimeMover.Instance.PoseChangeCurve.Evaluate(changingPoseCycle) * vertDiffAbs * changePoseModifier * directionAttenuationMod;
                poseShiftVector = new Vector3(0, addedVert, 0);
            }
            else
            {
                poseShiftVector = Vector3.zero;
            }
        }

        public static Vector3 GetModifiedHandPosForRotSpeed()
        {
            float addedZAxis = WeaponHandlingController.RotationDelta;
            return new Vector3(0, 0, -addedZAxis);
        }
    }
}
