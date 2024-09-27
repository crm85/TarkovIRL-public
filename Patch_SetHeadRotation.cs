using UnityEngine;
using EFT.Animations;
using SPT.Reflection.Patching;
using System.Reflection;
using EFT;
using HarmonyLib;

namespace TarkovIRL
{
    public class Patch_SetHeadRotation : ModulePatch
    {
        static FieldInfo _playerField;
        static FieldInfo _fcField;

        static Vector3 _dzLerp = Vector3.zero;
        static Vector3 _dzLerpTarget = Vector3.zero;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
            _fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("SetHeadRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ProceduralWeaponAnimation __instance, Vector3 headRot)
        {
            //float rate = PrimeMover.DevTestFloat3.Value;
            //_dzLerp.x = Mathf.Lerp(_dzLerp.x, _dzLerpTarget.x, PrimeMover.Instance.DeltaTime * rate);
            //_dzLerp.y = Mathf.Lerp(_dzLerp.y, _dzLerpTarget.y, PrimeMover.Instance.DeltaTime * rate);
            //_dzLerp.z = Mathf.Lerp(_dzLerp.z, _dzLerpTarget.z, PrimeMover.Instance.DeltaTime * rate);

            if (!PrimeMover.IsWeaponDeadzone.Value)
            {
                return true;
            }

            if ((UnityEngine.Object)(object)__instance == (UnityEngine.Object)null)
            {
                return true;
            }

            Player.FirearmController firearmController = (Player.FirearmController)_fcField.GetValue(__instance);
            if ((UnityEngine.Object)(object)firearmController == (UnityEngine.Object)null)
            {
                _dzLerpTarget = headRot;
                return true;

                // ^^
                // This needs to return true in order for Patch_Look to be able to play,
                // which it must do because of the conflict with FOV fix mod.
                // Before, I had this false and deployed the throw code below. In that case,
                // the return must be commented out because indeed there is no firearm to be found,
                // but rather something else(?) that inherets from ItemHandsController.

                // Ultimately the problem remains of snapping between the throw and the
                // re-presentation of the weapon after (and before!), but at least the full throw animation
                // is allowed to play before the snap back -- and if the player is moving, the rupture is
                // anyway attenuated and hard to notice.

                // I have this lerp in here but it doesn't really solve the problem, and makes
                // freelook very squishy so it's not a solution. This could use a more robust system
                // in the future but I don't know if everything can be solved and still remain consanate
                // with the FOV fix.
            }

            Player player = (Player)_playerField.GetValue(firearmController);
            if ((UnityEngine.Object)(object)player != (UnityEngine.Object)null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {

                
                    Vector3 headRotModified = DeadzoneController.GetHeadRotationWithDeadzone(player, PrimeMover.WeaponDeadzoneMulti.Value, headRot);
                    //_dzLerpTarget = headRotModified;
                    player.HeadRotation = headRotModified;
                    AccessTools.Field(typeof(ProceduralWeaponAnimation), "_headRotationVec").SetValue(__instance, headRotModified);
                
                return false;
            }
            return true;
        }
    }
}
