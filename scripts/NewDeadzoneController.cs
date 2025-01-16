using RealismMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    internal class NewDeadzoneController
    {
        static float _rotDeltaHistory = 0;
        static float _rotDeltaSmoothed = 0;
        static float _rotDeltaSmoothedInDeltaTime = 0;

        public static void Update(float fdt)
        {
            float rotDelta = PlayerMotionController.HorizontalRotationDelta * 100f;
            //UtilsTIRL.Log($"rot delta {rotDelta}");

            _rotDeltaHistory += rotDelta;
            _rotDeltaHistory -= _rotDeltaSmoothed;
            _rotDeltaSmoothed = _rotDeltaHistory * fdt * 9f;
        }

        public static Vector3 GetHeadRotWithDeadzone(Vector3 headRotInitial) 
        {
            float deadzoneMulti = PrimeMover.WeaponDeadzoneMulti.Value * WeaponController.GetWeaponMulti(false);
            if (WeaponController.IsStocked && PlayerMotionController.IsAiming)
            {
                deadzoneMulti *= PrimeMover.DeadzoneInADS.Value;
            }
            else if (StanceController.CurrentStance == EStance.ShortStock)
            {
                deadzoneMulti *= PrimeMover.DeadzoneInShortStock.Value;
            }
            else if (StanceController.CurrentStance == EStance.ActiveAiming)
            {
                deadzoneMulti *= PrimeMover.DeadzoneInActiveAim.Value;
            }
            else if (StanceController.CurrentStance == EStance.None)
            {
                deadzoneMulti *= PrimeMover.DeadzoneInVanilla.Value;
            }
            float efficiencyWeight = PrimeMover.DeadzoneWeightForEfficiency.Value ? EfficiencyController.EfficiencyModifierInverse : 1f;
            float highReadyMulti = StanceController.CurrentStance == EStance.HighReady ? 0.5f : 1f;
            //UtilsTIRL.Log($"efficiencyWeight on deadzone {efficiencyWeight}");
            _rotDeltaSmoothedInDeltaTime = Mathf.Lerp(_rotDeltaSmoothedInDeltaTime, _rotDeltaSmoothed * deadzoneMulti, PrimeMover.Instance.DeltaTime * PrimeMover.DeadzoneHeadFollowSpeedMulti.Value * efficiencyWeight * highReadyMulti);

            Vector3 headRotFinal = headRotInitial;
            headRotFinal.y += _rotDeltaSmoothedInDeltaTime;
            return headRotFinal;

        }
    }
}
