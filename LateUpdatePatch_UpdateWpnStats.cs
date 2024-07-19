using System.Collections.Generic;
using System.Reflection;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using TarkovIRL;
using UnityEngine;

public class LateUpdatePatch_UpdateWpnStats : ModulePatch
{
    static float _updateWeightTimer = 0;
    static readonly float _updateStatsTime = 1f;
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
        Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
        if (_updateWeightTimer > _updateStatsTime)
        {
            if (fc != null)
            {
                WeaponHandlingController.CurrentWeaponWeight = fc.Weapon.GetSingleItemTotalWeight();
                WeaponHandlingController.CurrentWeaponErgo = fc.TotalErgonomics / 100f;
            }
            else
            {
                WeaponHandlingController.CurrentWeaponWeight = 0;
                WeaponHandlingController.CurrentWeaponErgo = 1f;
            }
            _updateWeightTimer = 0;
        }

    }
}