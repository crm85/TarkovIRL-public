using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using TarkovIRL;
using UnityEngine;

public class Patch_LateUpdate_UpdateWpnStats : ModulePatch
{
    static int _weaponHashLastFrame = 0;
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.Public);
    }

    [PatchPostfix]
	private static void PatchPostfix(Player __instance)
    {
        if (!__instance.IsYourPlayer)
        {
            return;
        }
        Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
        if (fc == null) return;

        AnimStateController.SetCurrentWeaponAnimState(__instance.HandsAnimator.Animator.GetCurrentAnimatorStateInfo(1).nameHash);

        if (fc.FirearmsAnimator != null)
        {
            AugmentedReloadController.Update(fc.FirearmsAnimator);
        }

        int weaponHash = fc.Weapon.Name.GetHashCode();
        if (weaponHash != _weaponHashLastFrame)
        {
            WeaponController.UpdateWpnStats(fc);
            WeaponController.SetCurrentWeaponHash(weaponHash);
        }
        _weaponHashLastFrame = weaponHash;
    }
}