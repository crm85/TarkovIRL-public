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
                deadzoneMulti *= 0.2f;
            }
            else if (StanceController.CurrentStance == EStance.ActiveAiming)
            {
                deadzoneMulti *= 0.5f;
            }
            _rotDeltaSmoothedInDeltaTime = Mathf.Lerp(_rotDeltaSmoothedInDeltaTime, _rotDeltaSmoothed * deadzoneMulti, PrimeMover.Instance.DeltaTime * 5f);

            Vector3 headRotFinal = headRotInitial;
            headRotFinal.y += _rotDeltaSmoothedInDeltaTime;
            return headRotFinal;

        }
    }
}
