using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using TarkovIRL;
using UnityEngine;

public class Patch_LateUpdate_UpdateWpnStats : ModulePatch
{
    // readyonlys
    static readonly float _UpdateStatsTime = 1f;
    
    // vars
    static float _updateWeightTimer = 0;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.Public);
    }

    [PatchPostfix]
	private static void PatchPostfix(Player __instance)
    {
        _updateWeightTimer += Time.deltaTime;

        if (!__instance.IsYourPlayer)
        {
            return;
        }

        // get weapon name hash
        //int weaponHash = __instance.HandsAnimator.Animator.
        //WeaponController.CurrentWeaponHash(weaponHash);

        // get firearm controller
        Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
        if (fc == null) return;

        //UtilsTIRL.Log($"{fc.Weapon.Name.GetHashCode()}");
        WeaponController.CurrentWeaponHash(fc.Weapon.Name.GetHashCode());

        // get weapon anim state
        AnimStateController.SetCurrentWeaponAnimState(__instance.HandsAnimator.Animator.GetCurrentAnimatorStateInfo(1).nameHash);

        // update augmented reload controller
        AugmentedReloadController.Update(__instance.HandsAnimator);

        // refresh weapon info
        if (_updateWeightTimer > _UpdateStatsTime)
        {
            WeaponController.UpdateWpnStats(fc);
            _updateWeightTimer = 0;
        }
    }
}