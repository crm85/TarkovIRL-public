using System;
using SPT.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace TarkovIRL
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class PrimeMover : BaseUnityPlugin
    {
        const string modGUID = "TarkovIRL";
        const string modName = "TarkovIRL - WHM";
        const string modVersion = "0.4.6.1";

        public static PrimeMover Instance;

        public AnimationCurve BreathCurve;
        public AnimationCurve PoseChangeCurve;
        public AnimationCurve HandsShakeCurve;
        public AnimationCurve SmoothEdgesCurve;
        public AnimationCurve FootStepCurve;
        public AnimationCurve ErgoAttenuationCurve;

        public float DeltaTime = 0;
        public float Time = 0;

        // config items

        const string BASE_FEATURES_SECTION = "1 - Toggle base features";
        public static ConfigEntry<bool> IsWeaponDeadzone;
        public static ConfigEntry<bool> IsWeaponSway;
        public static ConfigEntry<bool> IsBreathingEffect;
        public static ConfigEntry<bool> IsPoseEffect;
        public static ConfigEntry<bool> IsPoseChangeEffect;
        public static ConfigEntry<bool> IsArmShakeEffect;
        public static ConfigEntry<bool> IsSmallMovementsEffect;
        public static ConfigEntry<bool> IsFootstepEffect;
        public static ConfigEntry<bool> IsParallaxEffect;

        const string ADJUST_VAR_SECTION = "2 - Adjust feature values";
        public static ConfigEntry<float> WeaponDeadzoneMulti;
        public static ConfigEntry<float> WeaponSwayMulti;
        public static ConfigEntry<float> BreathingEffectMulti;
        public static ConfigEntry<float> ArmShakeMulti;
        public static ConfigEntry<float> ParallaxMulti;
        public static ConfigEntry<float> AdsParallaxTimeMulti;
        public static ConfigEntry<float> ShotParallaxResetTimeMulti;
        public static ConfigEntry<float> EfficiencyLerpMulti;
        public static ConfigEntry<float> ShotParallaxWeaponWeightMulti;
        public static ConfigEntry<float> ParallaxSetSizeMulti;
        public static ConfigEntry<float> CameraUpdateMulti;
        public static ConfigEntry<float> FootstepLerpMulti;
        public static ConfigEntry<float> FootstepIntesnityMulti;
        public static ConfigEntry<float> ParallaxInAds;
        public static ConfigEntry<float> PistolSpecificParallax;
        public static ConfigEntry<float> ParallaxReturnToCenterMulti;
        public static ConfigEntry<float> DevTestFloat3;

        const string DEV_SECTION = "3 - Only for dev/testing";
        public static ConfigEntry<float> DevTestFloat4;
        public static ConfigEntry<float> DevTestFloat5;

        // config defaults
        readonly float AdsParallaxTimeMultiDefault = 30f;
        readonly float ArmShakeMultiDefault = 1f;
        readonly float BreathingEffectMultiDefault = 1f;
        readonly float CameraUpdateMultiDefault = 1f;
        readonly float EfficiencyLerpMultiDefault = 0.8f;
        readonly float FootstepLerpMultiDefault = 0.6f;
        readonly float FootstepIntesnityMultiDefault = 0.9f;
        readonly float ParallaxInAdsDefault = 0.2f;
        readonly float PistolSpecificParallaxDefault = 1f;
        readonly float ParallaxMultiDefault = 7.5f;
        readonly float ParallaxReturnToCenterMultiDefault = 10f;
        readonly float ParallaxSetSizeMultiDefault = 2f;
        readonly float ShotParallaxWeaponWeightMultiDefault = 5f;
        readonly float ShotParallaxResetTimeMultiDefault = 10f;
        readonly float WeaponDeadzoneMultiDefault = 2f;
        readonly float WeaponSwayMultiDefault = 0.5f;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            Initialize();
            LoadConfigValues();
        }

        void Initialize()
        {
            UtilsTIRL.Logger = this.Logger;

            BreathCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0.001708984f), new Keyframe(0.01f, 0.07340872f), new Keyframe(0.02f, 0.1447571f), new Keyframe(0.03f, 0.2159234f), new Keyframe(0.04f, 0.287077f), new Keyframe(0.05f, 0.3583873f), new Keyframe(0.05999999f, 0.4300235f), new Keyframe(0.06999999f, 0.5021549f), new Keyframe(0.07999999f, 0.5749511f), new Keyframe(0.08999999f, 0.6485812f), new Keyframe(0.09999999f, 0.7190837f), new Keyframe(0.11f, 0.7796323f), new Keyframe(0.12f, 0.8308698f), new Keyframe(0.13f, 0.8735839f), new Keyframe(0.14f, 0.9085627f), new Keyframe(0.15f, 0.9365937f), new Keyframe(0.16f, 0.958465f), new Keyframe(0.17f, 0.9749643f), new Keyframe(0.18f, 0.9868796f), new Keyframe(0.19f, 0.9949986f), new Keyframe(0.2f, 1.000109f), new Keyframe(0.21f, 1.003f), new Keyframe(0.22f, 1.004457f), new Keyframe(0.23f, 1.00527f), new Keyframe(0.24f, 1.006216f), new Keyframe(0.25f, 0.9934806f), new Keyframe(0.26f, 0.9566338f), new Keyframe(0.27f, 0.8993476f), new Keyframe(0.28f, 0.825294f), new Keyframe(0.29f, 0.7381451f), new Keyframe(0.3f, 0.6415731f), new Keyframe(0.31f, 0.5392498f), new Keyframe(0.32f, 0.4348473f), new Keyframe(0.33f, 0.3327447f), new Keyframe(0.3399999f, 0.2382682f), new Keyframe(0.3499999f, 0.1512108f), new Keyframe(0.3599999f, 0.07075208f), new Keyframe(0.3699999f, -0.003928483f), new Keyframe(0.3799999f, -0.07365131f), new Keyframe(0.3899999f, -0.1392368f), new Keyframe(0.3999999f, -0.2015056f), new Keyframe(0.4099999f, -0.2613338f), new Keyframe(0.4199999f, -0.3202845f), new Keyframe(0.4299999f, -0.378557f), new Keyframe(0.4399998f, -0.4358587f), new Keyframe(0.4499998f, -0.4918973f), new Keyframe(0.4599998f, -0.5463802f), new Keyframe(0.4699998f, -0.5990149f), new Keyframe(0.4799998f, -0.6495088f), new Keyframe(0.4899998f, -0.6975697f), new Keyframe(0.4999998f, -0.7429048f), new Keyframe(0.5099998f, -0.7852218f), new Keyframe(0.5199998f, -0.824228f), new Keyframe(0.5299998f, -0.8596311f), new Keyframe(0.5399998f, -0.8911383f), new Keyframe(0.5499998f, -0.9184575f), new Keyframe(0.5599998f, -0.941296f), new Keyframe(0.5699998f, -0.9593612f), new Keyframe(0.5799997f, -0.9723607f), new Keyframe(0.5899997f, -0.9800022f), new Keyframe(0.5999997f, -0.9830432f), new Keyframe(0.6099997f, -0.9852618f), new Keyframe(0.6199997f, -0.9870723f), new Keyframe(0.6299997f, -0.988526f), new Keyframe(0.6399997f, -0.9896744f), new Keyframe(0.6499997f, -0.9905687f), new Keyframe(0.6599997f, -0.9912603f), new Keyframe(0.6699997f, -0.9918006f), new Keyframe(0.6799996f, -0.992241f), new Keyframe(0.6899996f, -0.9926327f), new Keyframe(0.6999996f, -0.9930272f), new Keyframe(0.7099996f, -0.9934759f), new Keyframe(0.7199996f, -0.9940174f), new Keyframe(0.7299996f, -0.9938536f), new Keyframe(0.7399996f, -0.9923377f), new Keyframe(0.7499996f, -0.9892635f), new Keyframe(0.7599996f, -0.9844247f), new Keyframe(0.7699996f, -0.9776151f), new Keyframe(0.7799996f, -0.9686285f), new Keyframe(0.7899995f, -0.9572586f), new Keyframe(0.7999995f, -0.9433164f), new Keyframe(0.8099995f, -0.9273839f), new Keyframe(0.8199995f, -0.909653f), new Keyframe(0.8299995f, -0.8898755f), new Keyframe(0.8399995f, -0.8678032f), new Keyframe(0.8499995f, -0.8431879f), new Keyframe(0.8599995f, -0.8157812f), new Keyframe(0.8699995f, -0.7853352f), new Keyframe(0.8799995f, -0.7516014f), new Keyframe(0.8899994f, -0.7143317f), new Keyframe(0.8999994f, -0.673278f), new Keyframe(0.9099994f, -0.6281918f), new Keyframe(0.9199994f, -0.5788252f), new Keyframe(0.9299994f, -0.5249298f), new Keyframe(0.9399994f, -0.4662574f), new Keyframe(0.9499994f, -0.4025598f), new Keyframe(0.9599994f, -0.3335888f), new Keyframe(0.9699994f, -0.2590962f), new Keyframe(0.9799994f, -0.1788337f), new Keyframe(0.9899994f, -0.09255308f), new Keyframe(0.9999993f, -6.377697E-06f)
            );

            PoseChangeCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0f), new Keyframe(0.01f, 0.04753859f), new Keyframe(0.02f, 0.06824711f), new Keyframe(0.03f, 0.07190572f), new Keyframe(0.04f, 0.06472864f), new Keyframe(0.05f, 0.03672462f), new Keyframe(0.05999999f, -0.01143076f), new Keyframe(0.06999999f, -0.07712235f), new Keyframe(0.07999999f, -0.157735f), new Keyframe(0.08999999f, -0.2506537f), new Keyframe(0.09999999f, -0.3532633f), new Keyframe(0.11f, -0.4620121f), new Keyframe(0.12f, -0.5536414f), new Keyframe(0.13f, -0.6220356f), new Keyframe(0.14f, -0.6711998f), new Keyframe(0.15f, -0.7051392f), new Keyframe(0.16f, -0.7278589f), new Keyframe(0.17f, -0.7433642f), new Keyframe(0.18f, -0.7578313f), new Keyframe(0.19f, -0.7805023f), new Keyframe(0.2f, -0.8117735f), new Keyframe(0.21f, -0.8513885f), new Keyframe(0.22f, -0.8990905f), new Keyframe(0.23f, -0.9521281f), new Keyframe(0.24f, -0.9863883f), new Keyframe(0.25f, -0.998183f), new Keyframe(0.26f, -0.9904182f), new Keyframe(0.27f, -0.9660002f), new Keyframe(0.28f, -0.9278349f), new Keyframe(0.29f, -0.8788286f), new Keyframe(0.3f, -0.8218873f), new Keyframe(0.31f, -0.759917f), new Keyframe(0.32f, -0.695824f), new Keyframe(0.33f, -0.6325832f), new Keyframe(0.3399999f, -0.5721059f), new Keyframe(0.3499999f, -0.5145938f), new Keyframe(0.3599999f, -0.4601647f), new Keyframe(0.3699999f, -0.4089365f), new Keyframe(0.3799999f, -0.361027f), new Keyframe(0.3899999f, -0.316554f), new Keyframe(0.3999999f, -0.2756355f), new Keyframe(0.4099999f, -0.2383893f), new Keyframe(0.4199999f, -0.2049332f), new Keyframe(0.4299999f, -0.1753851f), new Keyframe(0.4399998f, -0.1498628f), new Keyframe(0.4499998f, -0.1284841f), new Keyframe(0.4599998f, -0.1113669f), new Keyframe(0.4699998f, -0.09862924f), new Keyframe(0.4799998f, -0.08953421f), new Keyframe(0.4899998f, -0.07422893f), new Keyframe(0.4999998f, -0.05229831f), new Keyframe(0.5099998f, -0.02683907f), new Keyframe(0.5199998f, -0.0009481311f), new Keyframe(0.5299998f, 0.02227778f), new Keyframe(0.5399998f, 0.03974189f), new Keyframe(0.5499998f, 0.04943786f), new Keyframe(0.5599998f, 0.05533161f), new Keyframe(0.5699998f, 0.05844303f), new Keyframe(0.5799997f, 0.05908555f), new Keyframe(0.5899997f, 0.05757258f), new Keyframe(0.5999997f, 0.05421757f), new Keyframe(0.6099997f, 0.04933393f), new Keyframe(0.6199997f, 0.0432351f), new Keyframe(0.6299997f, 0.03623449f), new Keyframe(0.6399997f, 0.02864554f), new Keyframe(0.6499997f, 0.02078166f), new Keyframe(0.6599997f, 0.01295629f), new Keyframe(0.6699997f, 0.00548286f), new Keyframe(0.6799996f, -0.001325212f), new Keyframe(0.6899996f, -0.007154491f), new Keyframe(0.6999996f, -0.01169154f), new Keyframe(0.7099996f, -0.01462297f), new Keyframe(0.7199996f, -0.01570082f), new Keyframe(0.7299996f, -0.01604912f), new Keyframe(0.7399996f, -0.01618946f), new Keyframe(0.7499996f, -0.01613806f), new Keyframe(0.7599996f, -0.01591115f), new Keyframe(0.7699996f, -0.01552495f), new Keyframe(0.7799996f, -0.01499569f), new Keyframe(0.7899995f, -0.01433959f), new Keyframe(0.7999995f, -0.01357289f), new Keyframe(0.8099995f, -0.01271181f), new Keyframe(0.8199995f, -0.01177257f), new Keyframe(0.8299995f, -0.01077141f), new Keyframe(0.8399995f, -0.009724541f), new Keyframe(0.8499995f, -0.008648196f), new Keyframe(0.8599995f, -0.007558605f), new Keyframe(0.8699995f, -0.006471992f), new Keyframe(0.8799995f, -0.005404581f), new Keyframe(0.8899994f, -0.0043726f), new Keyframe(0.8999994f, -0.003392277f), new Keyframe(0.9099994f, -0.002479835f), new Keyframe(0.9199994f, -0.001651503f), new Keyframe(0.9299994f, -0.0009235078f), new Keyframe(0.9399994f, -0.0003120769f), new Keyframe(0.9499994f, 0.0001665689f), new Keyframe(0.9599994f, 0.0004961994f), new Keyframe(0.9699994f, 0.0006605871f), new Keyframe(0.9799994f, 0.0006435066f), new Keyframe(0.9899994f, 0.0004287343f), new Keyframe(0.9999993f, 3.539026E-08f)
            );

            HandsShakeCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0f), new Keyframe(0.01f, 0.02002987f), new Keyframe(0.02f, 0.05177919f), new Keyframe(0.03f, 0.0861365f), new Keyframe(0.04f, 0.1139903f), new Keyframe(0.05f, 0.1262292f), new Keyframe(0.05999999f, 0.1387799f), new Keyframe(0.06999999f, 0.1714787f), new Keyframe(0.07999999f, 0.2052841f), new Keyframe(0.08999999f, 0.220898f), new Keyframe(0.09999999f, 0.1931826f), new Keyframe(0.11f, 0.08000854f), new Keyframe(0.12f, 0.07054432f), new Keyframe(0.13f, 0.07545625f), new Keyframe(0.14f, 0.06106036f), new Keyframe(0.15f, 0.03696794f), new Keyframe(0.16f, 0.01279034f), new Keyframe(0.17f, -0.001861159f), new Keyframe(0.18f, -0.00503464f), new Keyframe(0.19f, -0.02902272f), new Keyframe(0.2f, -0.06406359f), new Keyframe(0.21f, -0.09354599f), new Keyframe(0.22f, -0.1367515f), new Keyframe(0.23f, -0.1946985f), new Keyframe(0.24f, -0.2474255f), new Keyframe(0.25f, -0.2756253f), new Keyframe(0.26f, -0.2846023f), new Keyframe(0.27f, -0.2834283f), new Keyframe(0.28f, -0.2741062f), new Keyframe(0.29f, -0.2586388f), new Keyframe(0.3f, -0.2390289f), new Keyframe(0.31f, -0.2172793f), new Keyframe(0.32f, -0.1953928f), new Keyframe(0.33f, -0.1753722f), new Keyframe(0.3399999f, -0.1592204f), new Keyframe(0.3499999f, -0.1489402f), new Keyframe(0.3599999f, -0.1395335f), new Keyframe(0.3699999f, -0.09086414f), new Keyframe(0.3799999f, -0.01933302f), new Keyframe(0.3899999f, 0.04705437f), new Keyframe(0.3999999f, 0.08029242f), new Keyframe(0.4099999f, 0.1618353f), new Keyframe(0.4199999f, 0.2200256f), new Keyframe(0.4299999f, 0.2271181f), new Keyframe(0.4399998f, 0.2132713f), new Keyframe(0.4499998f, 0.1909959f), new Keyframe(0.4599998f, 0.1728026f), new Keyframe(0.4699998f, 0.169514f), new Keyframe(0.4799998f, 0.1561657f), new Keyframe(0.4899998f, 0.1291374f), new Keyframe(0.4999998f, 0.09996025f), new Keyframe(0.5099998f, 0.08016507f), new Keyframe(0.5199998f, 0.08081633f), new Keyframe(0.5299998f, 0.04337962f), new Keyframe(0.5399998f, -0.03990857f), new Keyframe(0.5499998f, -0.1130076f), new Keyframe(0.5599998f, -0.1297899f), new Keyframe(0.5699998f, -0.1205692f), new Keyframe(0.5799997f, -0.1226004f), new Keyframe(0.5899997f, -0.1690248f), new Keyframe(0.5999997f, -0.2687237f), new Keyframe(0.6099997f, -0.2949535f), new Keyframe(0.6199997f, -0.2905267f), new Keyframe(0.6299997f, -0.2679948f), new Keyframe(0.6399997f, -0.2130725f), new Keyframe(0.6499997f, -0.1072184f), new Keyframe(0.6599997f, -0.07028858f), new Keyframe(0.6699997f, -0.0884738f), new Keyframe(0.6799996f, -0.03864045f), new Keyframe(0.6899996f, 0.02149329f), new Keyframe(0.6999996f, 0.04437444f), new Keyframe(0.7099996f, 0.08881943f), new Keyframe(0.7199996f, 0.147704f), new Keyframe(0.7299996f, 0.1883931f), new Keyframe(0.7399996f, 0.1978056f), new Keyframe(0.7499996f, 0.1958648f), new Keyframe(0.7599996f, 0.1974101f), new Keyframe(0.7699996f, 0.1384161f), new Keyframe(0.7799996f, 0.04644331f), new Keyframe(0.7899995f, -0.01248662f), new Keyframe(0.7999995f, -0.03949538f), new Keyframe(0.8099995f, -0.1110162f), new Keyframe(0.8199995f, -0.182054f), new Keyframe(0.8299995f, -0.2041247f), new Keyframe(0.8399995f, -0.1287441f), new Keyframe(0.8499995f, -0.04300359f), new Keyframe(0.8599995f, -0.07900946f), new Keyframe(0.8699995f, -0.1746287f), new Keyframe(0.8799995f, -0.2664219f), new Keyframe(0.8899994f, -0.2970352f), new Keyframe(0.8999994f, -0.2974063f), new Keyframe(0.9099994f, -0.2874107f), new Keyframe(0.9199994f, -0.269584f), new Keyframe(0.9299994f, -0.2464621f), new Keyframe(0.9399994f, -0.2205806f), new Keyframe(0.9499994f, -0.1944753f), new Keyframe(0.9599994f, -0.170682f), new Keyframe(0.9699994f, -0.1515432f), new Keyframe(0.9799994f, -0.1052232f), new Keyframe(0.9899994f, -0.03925297f), new Keyframe(0.9999993f, -6.854534E-07f) 
            );

            SmoothEdgesCurve = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, 0.0001326637f), new Keyframe(0.02f, 0.0004161092f), new Keyframe(0.03f, 0.0008607136f), new Keyframe(0.04f, 0.001476854f), new Keyframe(0.05f, 0.002274909f), new Keyframe(0.05999999f, 0.003265254f), new Keyframe(0.06999999f, 0.004458267f), new Keyframe(0.07999999f, 0.005864326f), new Keyframe(0.08999999f, 0.007493807f), new Keyframe(0.09999999f, 0.00935709f), new Keyframe(0.11f, 0.01146455f), new Keyframe(0.12f, 0.01382656f), new Keyframe(0.13f, 0.01645351f), new Keyframe(0.14f, 0.01935577f), new Keyframe(0.15f, 0.02254371f), new Keyframe(0.16f, 0.02602772f), new Keyframe(0.17f, 0.02981817f), new Keyframe(0.18f, 0.03392544f), new Keyframe(0.19f, 0.03835991f), new Keyframe(0.2f, 0.04313195f), new Keyframe(0.21f, 0.04825195f), new Keyframe(0.22f, 0.05373026f), new Keyframe(0.23f, 0.05957729f), new Keyframe(0.24f, 0.0658034f), new Keyframe(0.25f, 0.07241896f), new Keyframe(0.26f, 0.07943435f), new Keyframe(0.27f, 0.08685997f), new Keyframe(0.28f, 0.09470617f), new Keyframe(0.29f, 0.1029833f), new Keyframe(0.3f, 0.1117019f), new Keyframe(0.31f, 0.1208721f), new Keyframe(0.32f, 0.1305044f), new Keyframe(0.33f, 0.1406093f), new Keyframe(0.3399999f, 0.1511969f), new Keyframe(0.3499999f, 0.1622778f), new Keyframe(0.3599999f, 0.1738624f), new Keyframe(0.3699999f, 0.1859608f), new Keyframe(0.3799999f, 0.1985837f), new Keyframe(0.3899999f, 0.2117413f), new Keyframe(0.3999999f, 0.225444f), new Keyframe(0.4099999f, 0.2406231f), new Keyframe(0.4199999f, 0.258181f), new Keyframe(0.4299999f, 0.2778718f), new Keyframe(0.4399998f, 0.2994483f), new Keyframe(0.4499998f, 0.3226634f), new Keyframe(0.4599998f, 0.34727f), new Keyframe(0.4699998f, 0.3730207f), new Keyframe(0.4799998f, 0.3996685f), new Keyframe(0.4899998f, 0.4269661f), new Keyframe(0.4999998f, 0.4546664f), new Keyframe(0.5099998f, 0.4825224f), new Keyframe(0.5199998f, 0.5102865f), new Keyframe(0.5299998f, 0.5377118f), new Keyframe(0.5399998f, 0.5645511f), new Keyframe(0.5499998f, 0.5905572f), new Keyframe(0.5599998f, 0.6154829f), new Keyframe(0.5699998f, 0.6390811f), new Keyframe(0.5799997f, 0.6611046f), new Keyframe(0.5899997f, 0.6813061f), new Keyframe(0.5999997f, 0.6994448f), new Keyframe(0.6099997f, 0.7163168f), new Keyframe(0.6199997f, 0.7326097f), new Keyframe(0.6299997f, 0.7483313f), new Keyframe(0.6399997f, 0.7634894f), new Keyframe(0.6499997f, 0.7780919f), new Keyframe(0.6599997f, 0.7921466f), new Keyframe(0.6699997f, 0.8056613f), new Keyframe(0.6799996f, 0.8186439f), new Keyframe(0.6899996f, 0.8311023f), new Keyframe(0.6999996f, 0.843044f), new Keyframe(0.7099996f, 0.8544773f), new Keyframe(0.7199996f, 0.8654097f), new Keyframe(0.7299996f, 0.8758491f), new Keyframe(0.7399996f, 0.8858035f), new Keyframe(0.7499996f, 0.8952805f), new Keyframe(0.7599996f, 0.9042881f), new Keyframe(0.7699996f, 0.9128339f), new Keyframe(0.7799996f, 0.9209261f), new Keyframe(0.7899995f, 0.9285723f), new Keyframe(0.7999995f, 0.9357804f), new Keyframe(0.8099995f, 0.9425582f), new Keyframe(0.8199995f, 0.9489135f), new Keyframe(0.8299995f, 0.9548541f), new Keyframe(0.8399995f, 0.9603881f), new Keyframe(0.8499995f, 0.965523f), new Keyframe(0.8599995f, 0.9702669f), new Keyframe(0.8699995f, 0.9746274f), new Keyframe(0.8799995f, 0.9786125f), new Keyframe(0.8899994f, 0.98223f), new Keyframe(0.8999994f, 0.9854877f), new Keyframe(0.9099994f, 0.9883934f), new Keyframe(0.9199994f, 0.9909551f), new Keyframe(0.9299994f, 0.9931804f), new Keyframe(0.9399994f, 0.9950773f), new Keyframe(0.9499994f, 0.9966536f), new Keyframe(0.9599994f, 0.9979171f), new Keyframe(0.9699994f, 0.9988757f), new Keyframe(0.9799994f, 0.9995371f), new Keyframe(0.9899994f, 0.9999093f), new Keyframe(0.9999993f, 1f)
            );

            FootStepCurve = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, 0.001186567f), new Keyframe(0.02f, 0.001699638f), new Keyframe(0.03f, 0.001349453f), new Keyframe(0.04f, -5.374909E-05f), new Keyframe(0.05f, -0.002699727f), new Keyframe(0.05999999f, -0.00677824f), new Keyframe(0.06999999f, -0.01247905f), new Keyframe(0.07999999f, -0.01999192f), new Keyframe(0.08999999f, -0.0295066f), new Keyframe(0.09999999f, -0.04121286f), new Keyframe(0.11f, -0.05530046f), new Keyframe(0.12f, -0.07195916f), new Keyframe(0.13f, -0.09137871f), new Keyframe(0.14f, -0.1137489f), new Keyframe(0.15f, -0.1392595f), new Keyframe(0.16f, -0.1681002f), new Keyframe(0.17f, -0.2004608f), new Keyframe(0.18f, -0.2365311f), new Keyframe(0.19f, -0.2765008f), new Keyframe(0.2f, -0.3205597f), new Keyframe(0.21f, -0.3688975f), new Keyframe(0.22f, -0.4217041f), new Keyframe(0.23f, -0.4791691f), new Keyframe(0.24f, -0.5415273f), new Keyframe(0.25f, -0.6106973f), new Keyframe(0.26f, -0.6841346f), new Keyframe(0.27f, -0.7570006f), new Keyframe(0.28f, -0.8244567f), new Keyframe(0.29f, -0.8816645f), new Keyframe(0.3f, -0.9237853f), new Keyframe(0.31f, -0.9459807f), new Keyframe(0.32f, -0.9457671f), new Keyframe(0.33f, -0.9420502f), new Keyframe(0.3399999f, -0.9398027f), new Keyframe(0.3499999f, -0.9387752f), new Keyframe(0.3599999f, -0.9387186f), new Keyframe(0.3699999f, -0.9393833f), new Keyframe(0.3799999f, -0.9405201f), new Keyframe(0.3899999f, -0.9418796f), new Keyframe(0.3999999f, -0.9432126f), new Keyframe(0.4099999f, -0.9442697f), new Keyframe(0.4199999f, -0.9448016f), new Keyframe(0.4299999f, -0.9445589f), new Keyframe(0.4399998f, -0.9432923f), new Keyframe(0.4499998f, -0.9407417f), new Keyframe(0.4599998f, -0.9348046f), new Keyframe(0.4699998f, -0.924293f), new Keyframe(0.4799998f, -0.9095647f), new Keyframe(0.4899998f, -0.8909773f), new Keyframe(0.4999998f, -0.8688885f), new Keyframe(0.5099998f, -0.8436559f), new Keyframe(0.5199998f, -0.8156375f), new Keyframe(0.5299998f, -0.7851906f), new Keyframe(0.5399998f, -0.7526731f), new Keyframe(0.5499998f, -0.7184428f), new Keyframe(0.5599998f, -0.6828572f), new Keyframe(0.5699998f, -0.6462739f), new Keyframe(0.5799997f, -0.6090508f), new Keyframe(0.5899997f, -0.5715455f), new Keyframe(0.5999997f, -0.5341157f), new Keyframe(0.6099997f, -0.4971191f), new Keyframe(0.6199997f, -0.4609134f), new Keyframe(0.6299997f, -0.4258562f), new Keyframe(0.6399997f, -0.3923054f), new Keyframe(0.6499997f, -0.3606184f), new Keyframe(0.6599997f, -0.3311531f), new Keyframe(0.6699997f, -0.304267f), new Keyframe(0.6799996f, -0.2803181f), new Keyframe(0.6899996f, -0.2596638f), new Keyframe(0.6999996f, -0.2422674f), new Keyframe(0.7099996f, -0.2258926f), new Keyframe(0.7199996f, -0.2101259f), new Keyframe(0.7299996f, -0.1949634f), new Keyframe(0.7399996f, -0.1804012f), new Keyframe(0.7499996f, -0.1664355f), new Keyframe(0.7599996f, -0.1530626f), new Keyframe(0.7699996f, -0.1402785f), new Keyframe(0.7799996f, -0.1280795f), new Keyframe(0.7899995f, -0.1164618f), new Keyframe(0.7999995f, -0.1054215f), new Keyframe(0.8099995f, -0.09495488f), new Keyframe(0.8199995f, -0.08505803f), new Keyframe(0.8299995f, -0.07572719f), new Keyframe(0.8399995f, -0.06695852f), new Keyframe(0.8499995f, -0.0587482f), new Keyframe(0.8599995f, -0.05109242f), new Keyframe(0.8699995f, -0.04398733f), new Keyframe(0.8799995f, -0.03742918f), new Keyframe(0.8899994f, -0.03141412f), new Keyframe(0.8999994f, -0.02593829f), new Keyframe(0.9099994f, -0.02099796f), new Keyframe(0.9199994f, -0.01658919f), new Keyframe(0.9299994f, -0.01270828f), new Keyframe(0.9399994f, -0.009351373f), new Keyframe(0.9499994f, -0.006514624f), new Keyframe(0.9599994f, -0.004194245f), new Keyframe(0.9699994f, -0.002386391f), new Keyframe(0.9799994f, -0.001087278f), new Keyframe(0.9899994f, -0.0002930462f), new Keyframe(0.9999993f, 8.940697E-08f)
            );

            ErgoAttenuationCurve = new AnimationCurve
            (
                new Keyframe(0f, 0.25f), new Keyframe(0.01f, 0.2506315f), new Keyframe(0.02f, 0.2517748f), new Keyframe(0.03f, 0.2534092f), new Keyframe(0.04f, 0.2555136f), new Keyframe(0.05f, 0.2580674f), new Keyframe(0.05999999f, 0.2610497f), new Keyframe(0.06999999f, 0.2644395f), new Keyframe(0.07999999f, 0.2682161f), new Keyframe(0.08999999f, 0.2723586f), new Keyframe(0.09999999f, 0.2768462f), new Keyframe(0.11f, 0.281658f), new Keyframe(0.12f, 0.2867732f), new Keyframe(0.13f, 0.2921709f), new Keyframe(0.14f, 0.2978303f), new Keyframe(0.15f, 0.3037306f), new Keyframe(0.16f, 0.3098509f), new Keyframe(0.17f, 0.3161703f), new Keyframe(0.18f, 0.322668f), new Keyframe(0.19f, 0.3293232f), new Keyframe(0.2f, 0.336115f), new Keyframe(0.21f, 0.3430226f), new Keyframe(0.22f, 0.3500251f), new Keyframe(0.23f, 0.3571016f), new Keyframe(0.24f, 0.3642314f), new Keyframe(0.25f, 0.3713936f), new Keyframe(0.26f, 0.3785673f), new Keyframe(0.27f, 0.3857317f), new Keyframe(0.28f, 0.392866f), new Keyframe(0.29f, 0.3999492f), new Keyframe(0.3f, 0.4069606f), new Keyframe(0.31f, 0.4138794f), new Keyframe(0.32f, 0.4206846f), new Keyframe(0.33f, 0.4273554f), new Keyframe(0.3399999f, 0.433871f), new Keyframe(0.3499999f, 0.4402105f), new Keyframe(0.3599999f, 0.4463531f), new Keyframe(0.3699999f, 0.452278f), new Keyframe(0.3799999f, 0.4579642f), new Keyframe(0.3899999f, 0.463391f), new Keyframe(0.3999999f, 0.4685375f), new Keyframe(0.4099999f, 0.4733828f), new Keyframe(0.4199999f, 0.4779062f), new Keyframe(0.4299999f, 0.4820867f), new Keyframe(0.4399998f, 0.4859036f), new Keyframe(0.4499998f, 0.4893359f), new Keyframe(0.4599998f, 0.4923628f), new Keyframe(0.4699998f, 0.4949635f), new Keyframe(0.4799998f, 0.4971172f), new Keyframe(0.4899998f, 0.4988029f), new Keyframe(0.4999998f, 0.4999999f), new Keyframe(0.5099998f, 0.5012925f), new Keyframe(0.5199998f, 0.5032603f), new Keyframe(0.5299998f, 0.5058756f), new Keyframe(0.5399998f, 0.509111f), new Keyframe(0.5499998f, 0.5129387f), new Keyframe(0.5599998f, 0.5173312f), new Keyframe(0.5699998f, 0.5222607f), new Keyframe(0.5799997f, 0.5276995f), new Keyframe(0.5899997f, 0.5336201f), new Keyframe(0.5999997f, 0.5399949f), new Keyframe(0.6099997f, 0.5467961f), new Keyframe(0.6199997f, 0.5539961f), new Keyframe(0.6299997f, 0.5615673f), new Keyframe(0.6399997f, 0.5694821f), new Keyframe(0.6499997f, 0.5777127f), new Keyframe(0.6599997f, 0.5862316f), new Keyframe(0.6699997f, 0.5950111f), new Keyframe(0.6799996f, 0.6040236f), new Keyframe(0.6899996f, 0.6132413f), new Keyframe(0.6999996f, 0.6226368f), new Keyframe(0.7099996f, 0.6321822f), new Keyframe(0.7199996f, 0.6418501f), new Keyframe(0.7299996f, 0.6516127f), new Keyframe(0.7399996f, 0.6614424f), new Keyframe(0.7499996f, 0.6713115f), new Keyframe(0.7599996f, 0.6811924f), new Keyframe(0.7699996f, 0.6910575f), new Keyframe(0.7799996f, 0.7008791f), new Keyframe(0.7899995f, 0.7106296f), new Keyframe(0.7999995f, 0.7202812f), new Keyframe(0.8099995f, 0.7298065f), new Keyframe(0.8199995f, 0.7391778f), new Keyframe(0.8299995f, 0.7483672f), new Keyframe(0.8399995f, 0.7573473f), new Keyframe(0.8499995f, 0.7660906f), new Keyframe(0.8599995f, 0.774569f), new Keyframe(0.8699995f, 0.7827553f), new Keyframe(0.8799995f, 0.7906216f), new Keyframe(0.8899994f, 0.7981403f), new Keyframe(0.8999994f, 0.8052838f), new Keyframe(0.9099994f, 0.8120244f), new Keyframe(0.9199994f, 0.8183345f), new Keyframe(0.9299994f, 0.8241866f), new Keyframe(0.9399994f, 0.8295527f), new Keyframe(0.9499994f, 0.8344054f), new Keyframe(0.9599994f, 0.8387171f), new Keyframe(0.9699994f, 0.84246f), new Keyframe(0.9799994f, 0.8456065f), new Keyframe(0.9899994f, 0.848129f), new Keyframe(0.9999993f, 0.8499999f)
            );

            TryLoadPatch(new Patch_LerpCamera_ForceUpdateSway());
            TryLoadPatch(new Patch_UpdateSwayFactors());
            TryLoadPatch(new Patch_LateUpdate_UpdateWpnStats());
            TryLoadPatch(new Patch_OnShot());
            TryLoadPatch(new Patch_CalculateCameraPosition_HandLayers());
            TryLoadPatch(new Patch_PlayStepSound());
            TryLoadPatch(new Patch_SetHeadRotation());
        }

        void LoadConfigValues()
        {
            // toggles
            IsWeaponDeadzone = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable weapon deadzone", "The weapon 'deadzone' effect is a separation of the player's camera from where the weapon is pointing. In vanilla these are perfectly aligned at all times; in this mod, these values become disaligned based on the size and ergo value of the weapon.");
            IsWeaponSway = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable weapon sway", "This mod changes how weapon sway works: your weapon generally sways ahead of your aimpoint (rather than behind like in vanilla), and the severity of the sway is influenced by the weapon's weight and ergo, and by your character's condition (health, stam, strength stat, gear weight).");
            IsBreathingEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable breathing effect", "Adds a visual oscillation to your character's weapon, the intensity of which depends on your current stamina.");
            IsPoseEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable stance-dependent weapon position", "When you crouch, your weapon position is pulled in closer to your character.");
            IsPoseChangeEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable stance transition effect", "When you change your crouch position, you see a dip in your sight picture, the speed and intensity of which is driven by how much you change your stance (e.g. incrimental change versus full change.");
            IsArmShakeEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable extra arms shaking", "Adds additional arm shake as arm stam decreases.");
            IsSmallMovementsEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable small visual effects", "Toggles small details: pulling the weapon in on rotation, lowering with unstocked weapon.");
            IsFootstepEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable footstep effect", "Player's weapon will bounce a bit more when taking steps, effect intesnity depends on several factors.");
            IsParallaxEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable parallax feature", "Extensive feature when causes the weapon to rotate in the player's hand and thus un-align the sights, when the player is rotating. The intensity of the effect depends on many factors: is in ADS?; player health and stam, weight and ergo of the weapon. This effect is wired into the sway effect.");

            // sliders
            WeaponDeadzoneMulti = ConstructFloatConfig(WeaponDeadzoneMultiDefault, ADJUST_VAR_SECTION, "Weapon deadzone multiplier", "", 0, 5f);
            WeaponSwayMulti = ConstructFloatConfig(WeaponSwayMultiDefault, ADJUST_VAR_SECTION, "Weapon sway multiplier", "", 0, 5f);
            BreathingEffectMulti = ConstructFloatConfig(BreathingEffectMultiDefault, ADJUST_VAR_SECTION, "Breathing effect multiplier", "", 0, 5f);
            ArmShakeMulti = ConstructFloatConfig(ArmShakeMultiDefault, ADJUST_VAR_SECTION, "Arm shake multiplier", "", 0, 5f);
            ShotParallaxResetTimeMulti = ConstructFloatConfig(ShotParallaxResetTimeMultiDefault, ADJUST_VAR_SECTION, "Shot-parallax cooldown multiplier", "Higher = d", 0, 20f);
            AdsParallaxTimeMulti = ConstructFloatConfig(AdsParallaxTimeMultiDefault, ADJUST_VAR_SECTION, "Ads-parallax cooldown multiplier", "", 0, 160f);
            ParallaxMulti = ConstructFloatConfig(ParallaxMultiDefault, ADJUST_VAR_SECTION,"Parallax multiplier", "", 0, 20f);
            ShotParallaxWeaponWeightMulti = ConstructFloatConfig(ShotParallaxWeaponWeightMultiDefault, ADJUST_VAR_SECTION, "Shot parallax weapon weight factor multiplier", "", 0, 10f);
            ParallaxSetSizeMulti = ConstructFloatConfig(ParallaxSetSizeMultiDefault, ADJUST_VAR_SECTION, "Parallax set size", "", 0, 20f);
            EfficiencyLerpMulti = ConstructFloatConfig(EfficiencyLerpMultiDefault, ADJUST_VAR_SECTION, "Efficiency lerp multiplier", "", 0, 10f);
            //CameraUpdateMulti = ConstructFloatConfig(CameraUpdateMultiDefault, ADJUST_VAR_SECTION, "Camera update rate multiplier", "Really a nothing-burger", 0, 10f);
            FootstepLerpMulti = ConstructFloatConfig(FootstepLerpMultiDefault, ADJUST_VAR_SECTION, "Footstep lerp speed multiplier", "How quickly the footstep animation plays", 0, 10f);
            FootstepIntesnityMulti = ConstructFloatConfig(FootstepIntesnityMultiDefault, ADJUST_VAR_SECTION, "Footstep intensity multiplier", "", 0, 10f);
            ParallaxInAds = ConstructFloatConfig(ParallaxInAdsDefault, ADJUST_VAR_SECTION, "Parallax effect in ADS", "The % of parallax effect that you see in ADS (with a stocked weapon)", 0, 1f);
            PistolSpecificParallax = ConstructFloatConfig(PistolSpecificParallaxDefault, ADJUST_VAR_SECTION, "Parallax effect value specifically for pistols", "", 0, 10f);
            ParallaxReturnToCenterMulti = ConstructFloatConfig(ParallaxReturnToCenterMultiDefault, ADJUST_VAR_SECTION, "Parallax return-to-center multiplier", "Higher = a faster return to center of the parallax", 0, 20f);


            // dev
            //DevTestFloat3 = ConstructFloatConfig(1f, DEV_SECTION, "Test value 3", "This is only for dev use, should not be connected to anything in production releases.", 1f, 30f);
            //DevTestFloat4 = ConstructFloatConfig(1f, DEV_SECTION, "Test value 4", "This is only for dev use, should not be connected to anything in production releases.", 1f, 10f);
            //DevTestFloat5 = ConstructFloatConfig(1f, DEV_SECTION, "Test value 5", "This is only for dev use, should not be connected to anything in production releases.", 0, 10f);
        }

        void Update()
        {
            DeltaTime += (UnityEngine.Time.unscaledDeltaTime - DeltaTime);
            Time += UnityEngine.Time.unscaledDeltaTime;

            UtilsTIRL.Update(DeltaTime);
            EfficiencyController.UpdateEfficiencyLerp(DeltaTime);
            FootstepController.UpdateStep(DeltaTime);
            SwayController.UpdateLerp(DeltaTime);
            //DeadzoneController.UpdateLerp(DeltaTime);
        }

        void LateUpdate()
        {
            SwayController.IsSwayUpdatedThisFrame = false;
        }

        void TryLoadPatch(ModulePatch patch)
        {
            try
            {
                ((ModulePatch)patch).Enable();
            }
            catch (Exception e)
            {
                string patchName = patch.ToString();
                UtilsTIRL.Log(true, "could not load " + patchName + " -- " + e);
                throw;
            }
        }

        ConfigEntry<float> ConstructFloatConfig(float defaultValue, string category, string descriptionShort, string descriptionFull, float min, float max)
        {
            ConfigEntry<float> result = ((BaseUnityPlugin)this).Config.Bind<float>(category, descriptionShort, defaultValue, new ConfigDescription(descriptionFull, (AcceptableValueBase)(object)new AcceptableValueRange<float>(min, max), Array.Empty<object>()));
            return result;
        }

        ConfigEntry<bool> ConstructBoolConfig(bool defaultValue, string category, string descriptionShort, string descriptionFull)
        {
            ConfigEntry<bool> result = ((BaseUnityPlugin)this).Config.Bind<bool>(category, descriptionShort, defaultValue, new ConfigDescription(descriptionFull, (AcceptableValueBase)null, Array.Empty<object>()));
            return result;
        }
    }
}