using EFT;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TarkovIRL
{
    public class Patch_SetFootStep : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PlayerAnimator).GetMethod("SetFootStep", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(PlayerAnimator __instance, float val)
        {
            UtilsTIRL.Log(true, $"footstep with value {val}");
        }
    }
}
