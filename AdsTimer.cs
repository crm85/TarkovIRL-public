using EFT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    public static class AdsTimer
    {
        static readonly float _MinWeight = 0.2f;

        static float _parallaxWeight = 1f;

        static bool _intoAds = false;
        static float _weaponMulti = 1f;
        static float _adsLerpWeight = 1f;

        static bool _shotSwitch = false;
        static float _shotLerpWeight = 1f;

        public static void UpdateLerps()
        {
            LerpAds();
            LerpShot();
            DefineParallaxWeight();
        }

        public static void StartNewAds(bool intoAds, float wpnMulti)
        {
            _intoAds = intoAds;
            _weaponMulti = wpnMulti;
        }

        static void LerpAds()
        {
            float targetWeight = _intoAds ? _MinWeight : 1f;
            _adsLerpWeight = Mathf.Lerp(_adsLerpWeight, targetWeight, PrimeMover.Instance.DeltaTime * _weaponMulti * PrimeMover.AdsParallaxTaperMulti.Value);
        }

        static void LerpShot()
        {
            _shotLerpWeight = Mathf.Lerp(_shotLerpWeight, 0, PrimeMover.Instance.DeltaTime * _weaponMulti * PrimeMover.ShotParallaxTaperMulti.Value);
            if (_shotLerpWeight < _MinWeight)
            {
                _shotSwitch = false;
            }
        }

        static public void StartNewShot(string caliber)
        {
            TarkovIRL.UtilsTIRL.Log(true, $"caliber name is {caliber}");
            _shotLerpWeight = 1f;
            _shotSwitch = true;
        }

        static void DefineParallaxWeight()
        {
            _parallaxWeight = _shotSwitch ? _shotLerpWeight : _adsLerpWeight;
        }

        //
        // sole output of class
        //
        public static float ParallaxWeight { get { return _parallaxWeight; } }
    }
}
