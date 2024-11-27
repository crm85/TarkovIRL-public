using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using RealismMod;
using SPT.Reflection.Patching;
using System.Reflection;
using static EFT.Player;
using static EFT.Player.FirearmController;
using static EFT.SkillManager;

namespace TarkovIRL
{
    internal class Patch_SetAnimatorAndProceduralValues : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("SetAnimatorAndProceduralValues", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Player.FirearmController __instance)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                player.QuickdrawWeaponFast = false;
            }
            return true;
        }
    }
}