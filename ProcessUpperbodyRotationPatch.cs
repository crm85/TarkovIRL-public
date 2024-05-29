using Aki.Reflection.Patching;
using EFT.Animations;
using EFT;
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
    internal class ProcessUpperbodyRotationPatch : ModulePatch
    {
        private static FieldInfo mContextField;
        protected override MethodBase GetTargetMethod()
        {
            mContextField = AccessTools.Field(typeof(MovementState), "MovementContext");
            return typeof(MovementState).GetMethod("ProcessUpperbodyRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(MovementState __instance, float deltaTime)
        {
            MovementContext movementContext = (EFT.MovementContext)mContextField.GetValue(__instance);
            if (movementContext == null)
            {
                return;
            }
            float y = Mathf.DeltaAngle((float)Math.Sign(movementContext.HandsToBodyAngle) * movementContext.TrunkRotationLimit, movementContext.HandsToBodyAngle);
            y *= .75f;
            movementContext.ApplyRotation(Quaternion.Lerp(movementContext.TransformRotation, movementContext.TransformRotation * Quaternion.Euler(0f, y, 0f), 30f * deltaTime));
        }
    }
}