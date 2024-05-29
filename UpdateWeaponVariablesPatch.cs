using Aki.Reflection.Patching;
using EFT;
using EFT.Animations;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TarkovIRL
{
    internal class UpdateWeaponVariablesPatch : ModulePatch
    {
        private static FieldInfo aimSpeedField;
        protected override MethodBase GetTargetMethod()
        {
            aimSpeedField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "SwayFalloff");
            return typeof(ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Postfix(ProceduralWeaponAnimation __instance)
        {
            float falloff = __instance.SwayFalloff;
            falloff *= .1f;
            AccessTools.Field(typeof(ProceduralWeaponAnimation), "SwayFalloff").SetValue(__instance, falloff);
        }
    }
}
