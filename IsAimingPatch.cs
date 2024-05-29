using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using TarkovIRL;
using UnityEngine;

namespace TarkovIRL
{
    public class IsAimingPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {


            return typeof(Player.FirearmController).GetMethod("get_IsAiming", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, bool __result)
        {
            float ergo = __instance.TotalErgonomics / 100f;
            WeaponHandlingController.TargetErgo = ergo;
        }
    }
}
