using System.Collections.Generic;
using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using TarkovIRL;
using UnityEngine;

public class LateUpdatePatch_RetrieveFAC : ModulePatch
{
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
        if (fc != null)
        {
            WeaponHandlingController.TotalWeaponWeight = fc.Weapon.GetSingleItemTotalWeight();
        }
        else
        {
            WeaponHandlingController.TotalWeaponWeight = 0;
        }

    }
}