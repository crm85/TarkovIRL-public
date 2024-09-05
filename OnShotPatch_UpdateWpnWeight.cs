using SPT.Reflection.Patching;
using EFT;
using System.Reflection;
using static EFT.Player;
using UnityEngine;

namespace TarkovIRL
{
    public class OnShotPatch_UpdateWpnWeight : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnMakingShot", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if ((UnityEngine.Object)(object)__instance != (UnityEngine.Object)null && __instance.IsYourPlayer)
            {
                FirearmController fc = __instance.HandsController as FirearmController;
                WeaponController.CurrentWeaponWeight = fc.Weapon.GetSingleItemTotalWeight();
                float shotWeight = WeaponController.CurrentWeaponWeight * (1f - WeaponController.CurrentWeaponErgoNorm);
                if (WeaponController.IsStocked && fc.IsAiming) AdsTimer.StartNewShot(fc.Weapon.AmmoCaliber);
            }
            else
            {
                return;
            }
        }
    }
}