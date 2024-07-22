using SPT.Reflection.Patching;
using EFT;
using System.Reflection;
using static EFT.Player;

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
            if ((UnityEngine.Object) (object)__instance != (UnityEngine.Object)null || __instance.IsYourPlayer)
            {
                return;
            }

            FirearmController fc = __instance.HandsController as FirearmController;
            WeaponHandlingController.CurrentWeaponWeight = fc.Weapon.GetSingleItemTotalWeight();
        }
    }
}
