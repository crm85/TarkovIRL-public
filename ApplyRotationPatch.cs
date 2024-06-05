using Aki.Reflection.Patching;
using EFT.Animations;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TarkovIRL
{
    public class ApplyRotationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementContext).GetMethod("ApplyRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(MovementContext __instance)
        {
            bool flag1 = __instance.CurrentState.AnimatorStateHash == PlayerAnimator.TURN_LEFT_90_AIM_HASH;
            bool flag2 = __instance.CurrentState.AnimatorStateHash == PlayerAnimator.TURN_RIGHT_90_AIM_HASH;
            bool flag3 = flag1 || flag2;

            string isFlag = string.Format($"in the middle of turn? {flag3}");
            Utils.Log(true, isFlag);

            WeaponHandlingController.IsReposStance = flag3;
        }
    }
}
