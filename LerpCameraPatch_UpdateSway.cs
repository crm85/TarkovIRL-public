﻿using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Animations;
using HarmonyLib;
using UnityEngine;

namespace TarkovIRL
{
    public class LerpCameraPatch_UpdateSway : ModulePatch
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
            if ((Object)(object)player != (Object)null && player.IsYourPlayer)
            {

                if (!player.IsInventoryOpened)
                {
                    __instance.UpdateSwayFactors();
                }
            }
        }
    }
}