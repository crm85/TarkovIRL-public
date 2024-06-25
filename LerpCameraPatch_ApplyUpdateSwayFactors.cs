using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using HarmonyLib;
using TarkovIRL;
using UnityEngine;

namespace TarkovIRL
{
    public class LerpCameraPatch_ApplyUpdateSwayFactors : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("LerpCamera", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ProceduralWeaponAnimation __instance, float dt)
        {
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
                if (!player.IsInventoryOpened)
                {
                    __instance.UpdateSwayFactors();
                    float addedBreath = HandsMovController.GetAdjustedHandsPosition(player);
                    Vector3 addedPosePosition = HandsMovController.GetModifiedHandPosWithPose(player);
                    __instance.HandsContainer.WeaponRoot.position += addedPosePosition;
                }
            }
        }
    }
}