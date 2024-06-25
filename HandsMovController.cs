using EFT;
using EFT.UI;
using System;
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
        static float poseProjectedPoseOffsetModifier = -0.01f;

        static float poseVerticalPoseOffsetTarget = 0;
        static float poseProjectedPoseOffsetTarget = 0;

        static float poseVerticalPoseOffsetLerp = 0;
        static float poseProjectedPoseOffsetLerp = 0;

        static float breathVerticalOffsetModifier = .02f;
        static float breathSpeedModifier = 1f;

        public static void UpdateLerp(float dt)
        {
            poseVerticalPoseOffsetLerp = Mathf.Lerp(poseVerticalPoseOffsetLerp, poseVerticalPoseOffsetTarget, dt * currentLerpRate);
            poseProjectedPoseOffsetLerp = Mathf.Lerp(poseProjectedPoseOffsetLerp, poseProjectedPoseOffsetTarget, dt * currentLerpRate);
        }
        public static Vector3 GetModifiedHandPosForBreath(Player player)
        {
            float stamNormalized = player.Physical.Stamina.Current / 104f;
            float stamModifier = 1f - stamNormalized;
            float sinTime = Mathf.Sin(Time.time * stamModifier * breathSpeedModifier) ;
            float breathOffset = breathVerticalOffsetModifier * sinTime * stamModifier;
            return new Vector3(0, breathOffset, 0);
        }

        public static Vector3 GetModifiedHandPosWithPose(EFT.Player player)
        {
            currentLerpRate = lerpRate;

            float poseLevel = player.PoseLevel;
            poseVerticalPoseOffsetTarget = (1f - poseLevel) * poseVerticalPoseOffsetModifier;
            poseProjectedPoseOffsetTarget = (1f - poseLevel) * poseProjectedPoseOffsetModifier;

            if (player.ProceduralWeaponAnimation.IsAiming)
            {
                poseVerticalPoseOffsetTarget = 0;
                poseProjectedPoseOffsetTarget = 0;

                currentLerpRate = lerpRate * 2f;
            }

            return new Vector3(0, poseVerticalPoseOffsetLerp, poseProjectedPoseOffsetLerp);
        }
    }
}
