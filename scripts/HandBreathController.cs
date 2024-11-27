using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    internal class HandBreathController
    {
        // readonlys
        static readonly float _BreathSpeedModifier = 0.55f;
        static readonly float _BreathVerticalOffsetModifier = 0.0075f;

        // vars
        static float _breathUpdateTimer = 0;

        public static Vector3 GetModifiedHandPosForBreath(Player player)
        {
            AnimationCurve breathCurve = PrimeMover.Instance.BreathCurve;
            float stamNormalized = player.Physical.Stamina.Current / 104f;
            float stamModifier = 1f - stamNormalized;
            float breathModifier = 1f + stamModifier;
            bool isAugmentedBreath = _breathUpdateTimer < 0.55f && _breathUpdateTimer > 0.26f && PlayerMotionController.IsHoldingBreath;
            isAugmentedBreath &= PlayerMotionController.IsAiming;
            isAugmentedBreath &= player.Physical.Stamina.NormalValue > 0.01f;
            PlayerMotionController.IsAugmentedBreath = isAugmentedBreath;
            float holdBreathMulti = isAugmentedBreath ? 0.25f : 1f;
            _breathUpdateTimer += player.DeltaTime * breathModifier * _BreathSpeedModifier * holdBreathMulti;
            if (_breathUpdateTimer >= 1f)
            {
                _breathUpdateTimer -= 1f;
            }

            float breathValue = breathCurve.Evaluate(_breathUpdateTimer);
            float stamModClamped = Mathf.Clamp(stamModifier, 0.025f, 1f);
            float breathOffset = breathValue * _BreathVerticalOffsetModifier * stamModClamped * PrimeMover.BreathingEffectMulti.Value;
            return new Vector3(0, breathOffset, 0);
        }
    }
}
