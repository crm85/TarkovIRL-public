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
        const string modVersion = "0.4.7";

        public static PrimeMover Instance;

        //
        // Curves
        //
        public AnimationCurve BreathCurve;
        public AnimationCurve PoseChangeCurve;
        public AnimationCurve HandsShakeCurve;
        public AnimationCurve SmoothEdgesCurve;
        public AnimationCurve FootStepCurve;
        public AnimationCurve ErgoAttenuationCurve;
        public AnimationCurve ThrowVisualCurveX;
        public AnimationCurve ThrowVisualCurveY;
        public AnimationCurve ThrowVisualUnderhandCurveX;
        public AnimationCurve ThrowVisualUnderhandCurveY;

        //
        // Dts
        //
        public float DeltaTime = 0;
        public float Time = 0;
        public float FixedDeltaTime = 0;

        //
        // config items
        //
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

        public static ConfigEntry<bool> IsLogging;
        public static ConfigEntry<bool> DebugSpam;

        const string ADJUST_VAR_SECTION = "2 - Adjust feature values";
        public static ConfigEntry<float> WeaponDeadzoneMulti;
        public static ConfigEntry<float> WeaponSwayMulti;
        public static ConfigEntry<float> BreathingEffectMulti;
        public static ConfigEntry<float> ArmShakeMulti;
        public static ConfigEntry<float> ArmShakeRateMulti;
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
        public static ConfigEntry<float> ThrowStrengthMulti;
        public static ConfigEntry<float> ThrowSpeedMulti;

        const string DEV_SECTION = "3 - Only for dev/testing";
        public static ConfigEntry<float> DevTestFloat3;
        public static ConfigEntry<float> DevTestFloat4;
        public static ConfigEntry<float> DevTestFloat5;

        // config defaults
        readonly float AdsParallaxTimeMultiDefault = 30f;
        readonly float ArmShakeMultiDefault = 2.5f;
        readonly float ArmShakeRateMultiDefault = 1f;
        readonly float BreathingEffectMultiDefault = 1f;
        readonly float CameraUpdateMultiDefault = 1f;
        readonly float EfficiencyLerpMultiDefault = 0.8f;
        readonly float FootstepLerpMultiDefault = 0.6f;
        readonly float FootstepIntesnityMultiDefault = 0.9f;
        readonly float ParallaxInAdsDefault = 0.2f;
        readonly float PistolSpecificParallaxDefault = 2f;
        readonly float ParallaxMultiDefault = 7.5f;
        readonly float ParallaxReturnToCenterMultiDefault = 10f;
        readonly float ParallaxSetSizeMultiDefault = 2f;
        readonly float ShotParallaxWeaponWeightMultiDefault = 5f;
        readonly float ShotParallaxResetTimeMultiDefault = 10f;
        readonly float WeaponDeadzoneMultiDefault = 2f;
        readonly float WeaponSwayMultiDefault = 0.5f;
        readonly float ThrowStrengthMultiDefault = 18f;
        readonly float ThrowSpeedMultiDefault = 2.3f;

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
                 new Keyframe(0f, 0.003456681f), new Keyframe(0.01f, 0.01467377f), new Keyframe(0.02f, 0.02527229f), new Keyframe(0.03f, 0.03407215f), new Keyframe(0.04f, 0.03989322f), new Keyframe(0.05f, 0.0415554f), new Keyframe(0.05999999f, 0.04440531f), new Keyframe(0.06999999f, 0.051765f), new Keyframe(0.07999999f, 0.05902842f), new Keyframe(0.08999999f, 0.0615889f), new Keyframe(0.09999999f, 0.05141038f), new Keyframe(0.11f, 0.01802858f), new Keyframe(0.12f, 0.0317804f), new Keyframe(0.13f, 0.04252484f), new Keyframe(0.14f, 0.03777219f), new Keyframe(0.15f, 0.02463009f), new Keyframe(0.16f, 0.01020622f), new Keyframe(0.17f, 0.001608178f), new Keyframe(0.18f, 0.001896187f), new Keyframe(0.19f, -0.007047208f), new Keyframe(0.2f, -0.02585391f), new Keyframe(0.21f, -0.03216255f), new Keyframe(0.22f, -0.03334584f), new Keyframe(0.23f, -0.05267931f), new Keyframe(0.24f, -0.04703839f), new Keyframe(0.25f, -0.04069633f), new Keyframe(0.26f, -0.04325364f), new Keyframe(0.27f, -0.04701382f), new Keyframe(0.28f, -0.04307806f), new Keyframe(0.29f, -0.03145951f), new Keyframe(0.3f, -0.03458425f), new Keyframe(0.31f, -0.05242091f), new Keyframe(0.32f, -0.03461784f), new Keyframe(0.33f, -0.03887944f), new Keyframe(0.3399999f, -0.04251146f), new Keyframe(0.3499999f, -0.03486739f), new Keyframe(0.3599999f, -0.02544173f), new Keyframe(0.3699999f, -0.01010045f), new Keyframe(0.3799999f, -0.02236057f), new Keyframe(0.3899999f, 0.001355158f), new Keyframe(0.3999999f, 0.01982367f), new Keyframe(0.4099999f, 0.04136363f), new Keyframe(0.4199999f, 0.028871f), new Keyframe(0.4299999f, 0.03566539f), new Keyframe(0.4399998f, 0.03741714f), new Keyframe(0.4499998f, 0.03172546f), new Keyframe(0.4599998f, 0.0288302f), new Keyframe(0.4699998f, 0.0284416f), new Keyframe(0.4799998f, 0.01997722f), new Keyframe(0.4899998f, 0.008944051f), new Keyframe(0.4999998f, 0.001161236f), new Keyframe(0.5099998f, 0.00244794f), new Keyframe(0.5199998f, 0.009561044f), new Keyframe(0.5299998f, 0.0011282f), new Keyframe(0.5399998f, -0.01759064f), new Keyframe(0.5499998f, -0.03935509f), new Keyframe(0.5599998f, -0.05692476f), new Keyframe(0.5699998f, -0.06214953f), new Keyframe(0.5799997f, -0.05987017f), new Keyframe(0.5899997f, -0.0629185f), new Keyframe(0.5999997f, -0.07895129f), new Keyframe(0.6099997f, -0.0832698f), new Keyframe(0.6199997f, -0.08200739f), new Keyframe(0.6299997f, -0.07845679f), new Keyframe(0.6399997f, -0.06902104f), new Keyframe(0.6499997f, -0.04983614f), new Keyframe(0.6599997f, -0.04493317f), new Keyframe(0.6699997f, -0.05027279f), new Keyframe(0.6799996f, -0.03982494f), new Keyframe(0.6899996f, -0.0257119f), new Keyframe(0.6999996f, -0.01667746f), new Keyframe(0.7099996f, 0.001633378f), new Keyframe(0.7199996f, 0.02325357f), new Keyframe(0.7299996f, 0.03712844f), new Keyframe(0.7399996f, 0.03921978f), new Keyframe(0.7499996f, 0.03961103f), new Keyframe(0.7599996f, 0.04532627f), new Keyframe(0.7699996f, 0.03232246f), new Keyframe(0.7799996f, 0.009538032f), new Keyframe(0.7899995f, -0.005662378f), new Keyframe(0.7999995f, -0.01157239f), new Keyframe(0.8099995f, -0.02694051f), new Keyframe(0.8199995f, -0.04455786f), new Keyframe(0.8299995f, -0.05643301f), new Keyframe(0.8399995f, -0.05457452f), new Keyframe(0.8499995f, -0.04635786f), new Keyframe(0.8599995f, -0.04785218f), new Keyframe(0.8699995f, -0.05541039f), new Keyframe(0.8799995f, -0.06524507f), new Keyframe(0.8899994f, -0.07518715f), new Keyframe(0.8999994f, -0.08971603f), new Keyframe(0.9099994f, -0.07793824f), new Keyframe(0.9199994f, -0.06209349f), new Keyframe(0.9299994f, -0.05362688f), new Keyframe(0.9399994f, -0.06287797f), new Keyframe(0.9499994f, -0.06029455f), new Keyframe(0.9599994f, -0.03613466f), new Keyframe(0.9699994f, -0.01750777f), new Keyframe(0.9799994f, -0.00874934f), new Keyframe(0.9899994f, -0.003794672f), new Keyframe(0.9999993f, 0.003456004f)
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

            ThrowVisualCurveX = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, 0.002978058f), new Keyframe(0.02f, 0.009249885f), new Keyframe(0.03f, 0.01808028f), new Keyframe(0.04f, 0.02873405f), new Keyframe(0.05f, 0.04047598f), new Keyframe(0.05999999f, 0.05257087f), new Keyframe(0.06999999f, 0.06428354f), new Keyframe(0.07999999f, 0.07487878f), new Keyframe(0.08999999f, 0.08362138f), new Keyframe(0.09999999f, 0.08977617f), new Keyframe(0.11f, 0.07975212f), new Keyframe(0.12f, 0.04375243f), new Keyframe(0.13f, -0.009889014f), new Keyframe(0.14f, -0.07281638f), new Keyframe(0.15f, -0.1366737f), new Keyframe(0.16f, -0.1931049f), new Keyframe(0.17f, -0.2337542f), new Keyframe(0.18f, -0.2526112f), new Keyframe(0.19f, -0.2665491f), new Keyframe(0.2f, -0.2806153f), new Keyframe(0.21f, -0.294713f), new Keyframe(0.22f, -0.3087457f), new Keyframe(0.23f, -0.3226167f), new Keyframe(0.24f, -0.3362293f), new Keyframe(0.25f, -0.3494868f), new Keyframe(0.26f, -0.3622926f), new Keyframe(0.27f, -0.37455f), new Keyframe(0.28f, -0.3861625f), new Keyframe(0.29f, -0.3970333f), new Keyframe(0.3f, -0.4070658f), new Keyframe(0.31f, -0.4159627f), new Keyframe(0.32f, -0.4211259f), new Keyframe(0.33f, -0.422064f), new Keyframe(0.3399999f, -0.4191369f), new Keyframe(0.3499999f, -0.4127041f), new Keyframe(0.3599999f, -0.4031253f), new Keyframe(0.3699999f, -0.3907602f), new Keyframe(0.3799999f, -0.3759684f), new Keyframe(0.3899999f, -0.3591096f), new Keyframe(0.3999999f, -0.3405434f), new Keyframe(0.4099999f, -0.3206295f), new Keyframe(0.4199999f, -0.2997276f), new Keyframe(0.4299999f, -0.2781972f), new Keyframe(0.4399998f, -0.256398f), new Keyframe(0.4499998f, -0.2346898f), new Keyframe(0.4599998f, -0.2134321f), new Keyframe(0.4699998f, -0.1929846f), new Keyframe(0.4799998f, -0.1737069f), new Keyframe(0.4899998f, -0.1559589f), new Keyframe(0.4999998f, -0.1400999f), new Keyframe(0.5099998f, -0.1264897f), new Keyframe(0.5199998f, -0.1150528f), new Keyframe(0.5299998f, -0.1044564f), new Keyframe(0.5399998f, -0.09455517f), new Keyframe(0.5499998f, -0.08532649f), new Keyframe(0.5599998f, -0.07674783f), new Keyframe(0.5699998f, -0.06879658f), new Keyframe(0.5799997f, -0.06145016f), new Keyframe(0.5899997f, -0.05468599f), new Keyframe(0.5999997f, -0.04848151f), new Keyframe(0.6099997f, -0.04281412f), new Keyframe(0.6199997f, -0.03766123f), new Keyframe(0.6299997f, -0.03300028f), new Keyframe(0.6399997f, -0.02880868f), new Keyframe(0.6499997f, -0.02506384f), new Keyframe(0.6599997f, -0.0217432f), new Keyframe(0.6699997f, -0.01882415f), new Keyframe(0.6799996f, -0.01628414f), new Keyframe(0.6899996f, -0.01410055f), new Keyframe(0.6999996f, -0.01225084f), new Keyframe(0.7099996f, -0.01071239f), new Keyframe(0.7199996f, -0.009462655f), new Keyframe(0.7299996f, -0.008479021f), new Keyframe(0.7399996f, -0.007738933f), new Keyframe(0.7499996f, -0.007219799f), new Keyframe(0.7599996f, -0.006899022f), new Keyframe(0.7699996f, -0.006754041f), new Keyframe(0.7799996f, -0.006762281f), new Keyframe(0.7899995f, -0.006901138f), new Keyframe(0.7999995f, -0.007148027f), new Keyframe(0.8099995f, -0.007480398f), new Keyframe(0.8199995f, -0.007875651f), new Keyframe(0.8299995f, -0.008311182f), new Keyframe(0.8399995f, -0.008764438f), new Keyframe(0.8499995f, -0.009212844f), new Keyframe(0.8599995f, -0.009633817f), new Keyframe(0.8699995f, -0.01000475f), new Keyframe(0.8799995f, -0.01030309f), new Keyframe(0.8899994f, -0.01050624f), new Keyframe(0.8999994f, -0.01059162f), new Keyframe(0.9099994f, -0.01053666f), new Keyframe(0.9199994f, -0.01031873f), new Keyframe(0.9299994f, -0.009915337f), new Keyframe(0.9399994f, -0.009303771f), new Keyframe(0.9499994f, -0.008461602f), new Keyframe(0.9599994f, -0.007366158f), new Keyframe(0.9699994f, -0.005994886f), new Keyframe(0.9799994f, -0.004325181f), new Keyframe(0.9899994f, -0.002334483f), new Keyframe(0.9999993f, -1.937151E-07f)
            );

            ThrowVisualCurveY = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, -0.002133129f), new Keyframe(0.02f, -0.0007746345f), new Keyframe(0.03f, 0.003373357f), new Keyframe(0.04f, 0.009608719f), new Keyframe(0.05f, 0.01722933f), new Keyframe(0.05999999f, 0.02553305f), new Keyframe(0.06999999f, 0.03381777f), new Keyframe(0.07999999f, 0.04138135f), new Keyframe(0.08999999f, 0.04752167f), new Keyframe(0.09999999f, 0.05153698f), new Keyframe(0.11f, 0.0535779f), new Keyframe(0.12f, 0.05426566f), new Keyframe(0.13f, 0.05369683f), new Keyframe(0.14f, 0.0519679f), new Keyframe(0.15f, 0.04917544f), new Keyframe(0.16f, 0.04541595f), new Keyframe(0.17f, 0.04078599f), new Keyframe(0.18f, 0.03538207f), new Keyframe(0.19f, 0.02930073f), new Keyframe(0.2f, 0.0226385f), new Keyframe(0.21f, 0.01549193f), new Keyframe(0.22f, 0.007957522f), new Keyframe(0.23f, 0.0001318268f), new Keyframe(0.24f, -0.007888626f), new Keyframe(0.25f, -0.0160073f), new Keyframe(0.26f, -0.02412766f), new Keyframe(0.27f, -0.03215319f), new Keyframe(0.28f, -0.03998734f), new Keyframe(0.29f, -0.04753359f), new Keyframe(0.3f, -0.05469544f), new Keyframe(0.31f, -0.06137628f), new Keyframe(0.32f, -0.06747967f), new Keyframe(0.33f, -0.07290903f), new Keyframe(0.3399999f, -0.07756777f), new Keyframe(0.3499999f, -0.08135948f), new Keyframe(0.3599999f, -0.08418755f), new Keyframe(0.3699999f, -0.08595549f), new Keyframe(0.3799999f, -0.08663395f), new Keyframe(0.3899999f, -0.08696654f), new Keyframe(0.3999999f, -0.08718631f), new Keyframe(0.4099999f, -0.08729605f), new Keyframe(0.4199999f, -0.08729858f), new Keyframe(0.4299999f, -0.0871967f), new Keyframe(0.4399998f, -0.08699321f), new Keyframe(0.4499998f, -0.08669093f), new Keyframe(0.4599998f, -0.08629265f), new Keyframe(0.4699998f, -0.08580118f), new Keyframe(0.4799998f, -0.08521932f), new Keyframe(0.4899998f, -0.0845499f), new Keyframe(0.4999998f, -0.08379569f), new Keyframe(0.5099998f, -0.08295952f), new Keyframe(0.5199998f, -0.08204418f), new Keyframe(0.5299998f, -0.0810525f), new Keyframe(0.5399998f, -0.07998727f), new Keyframe(0.5499998f, -0.07885128f), new Keyframe(0.5599998f, -0.07764735f), new Keyframe(0.5699998f, -0.07637829f), new Keyframe(0.5799997f, -0.0750469f), new Keyframe(0.5899997f, -0.07365599f), new Keyframe(0.5999997f, -0.07220837f), new Keyframe(0.6099997f, -0.07070682f), new Keyframe(0.6199997f, -0.06915417f), new Keyframe(0.6299997f, -0.06755322f), new Keyframe(0.6399997f, -0.06590676f), new Keyframe(0.6499997f, -0.06421763f), new Keyframe(0.6599997f, -0.0624886f), new Keyframe(0.6699997f, -0.06072249f), new Keyframe(0.6799996f, -0.05892211f), new Keyframe(0.6899996f, -0.05709026f), new Keyframe(0.6999996f, -0.05522974f), new Keyframe(0.7099996f, -0.05334336f), new Keyframe(0.7199996f, -0.05143393f), new Keyframe(0.7299996f, -0.04950425f), new Keyframe(0.7399996f, -0.04755713f), new Keyframe(0.7499996f, -0.04559538f), new Keyframe(0.7599996f, -0.04362179f), new Keyframe(0.7699996f, -0.04163917f), new Keyframe(0.7799996f, -0.03965033f), new Keyframe(0.7899995f, -0.03765807f), new Keyframe(0.7999995f, -0.03566522f), new Keyframe(0.8099995f, -0.03367455f), new Keyframe(0.8199995f, -0.03168888f), new Keyframe(0.8299995f, -0.02971101f), new Keyframe(0.8399995f, -0.02774376f), new Keyframe(0.8499995f, -0.02578992f), new Keyframe(0.8599995f, -0.02385231f), new Keyframe(0.8699995f, -0.02193373f), new Keyframe(0.8799995f, -0.02003698f), new Keyframe(0.8899994f, -0.01816487f), new Keyframe(0.8999994f, -0.01632018f), new Keyframe(0.9099994f, -0.01450577f), new Keyframe(0.9199994f, -0.01272441f), new Keyframe(0.9299994f, -0.01097888f), new Keyframe(0.9399994f, -0.009272054f), new Keyframe(0.9499994f, -0.007606678f), new Keyframe(0.9599994f, -0.005985588f), new Keyframe(0.9699994f, -0.004411571f), new Keyframe(0.9799994f, -0.002887443f), new Keyframe(0.9899994f, -0.001416005f), new Keyframe(0.9999993f, -7.450581E-08f)
            );

            ThrowVisualUnderhandCurveX = new AnimationCurve
            (
               new Keyframe(0f, 0f), new Keyframe(0.01f, 0.001810697f), new Keyframe(0.02f, 0.002448291f), new Keyframe(0.03f, 0.001997397f), new Keyframe(0.04f, 0.0005426306f), new Keyframe(0.05f, -0.001831393f), new Keyframe(0.05999999f, -0.005040056f), new Keyframe(0.06999999f, -0.008998747f), new Keyframe(0.07999999f, -0.01362285f), new Keyframe(0.08999999f, -0.01882775f), new Keyframe(0.09999999f, -0.02453342f), new Keyframe(0.11f, -0.03063369f), new Keyframe(0.12f, -0.03508093f), new Keyframe(0.13f, -0.03511371f), new Keyframe(0.14f, -0.03031639f), new Keyframe(0.15f, -0.02415645f), new Keyframe(0.16f, -0.01682964f), new Keyframe(0.17f, -0.008419836f), new Keyframe(0.18f, 0.0009890869f), new Keyframe(0.19f, 0.01131325f), new Keyframe(0.2f, 0.02246878f), new Keyframe(0.21f, 0.0343718f), new Keyframe(0.22f, 0.04693845f), new Keyframe(0.23f, 0.06008482f), new Keyframe(0.24f, 0.07372707f), new Keyframe(0.25f, 0.08778128f), new Keyframe(0.26f, 0.1021636f), new Keyframe(0.27f, 0.1167902f), new Keyframe(0.28f, 0.1315771f), new Keyframe(0.29f, 0.1464405f), new Keyframe(0.3f, 0.1612965f), new Keyframe(0.31f, 0.1760613f), new Keyframe(0.32f, 0.1906509f), new Keyframe(0.33f, 0.2049816f), new Keyframe(0.3399999f, 0.2189693f), new Keyframe(0.3499999f, 0.231978f), new Keyframe(0.3599999f, 0.2430236f), new Keyframe(0.3699999f, 0.2521778f), new Keyframe(0.3799999f, 0.2595338f), new Keyframe(0.3899999f, 0.2651844f), new Keyframe(0.3999999f, 0.2692229f), new Keyframe(0.4099999f, 0.2717423f), new Keyframe(0.4199999f, 0.2728357f), new Keyframe(0.4299999f, 0.2725961f), new Keyframe(0.4399998f, 0.2711166f), new Keyframe(0.4499998f, 0.2684903f), new Keyframe(0.4599998f, 0.2648103f), new Keyframe(0.4699998f, 0.2601696f), new Keyframe(0.4799998f, 0.2546613f), new Keyframe(0.4899998f, 0.2483786f), new Keyframe(0.4999998f, 0.2414143f), new Keyframe(0.5099998f, 0.2338617f), new Keyframe(0.5199998f, 0.2258138f), new Keyframe(0.5299998f, 0.2173637f), new Keyframe(0.5399998f, 0.2086044f), new Keyframe(0.5499998f, 0.199629f), new Keyframe(0.5599998f, 0.1905307f), new Keyframe(0.5699998f, 0.1814024f), new Keyframe(0.5799997f, 0.1723373f), new Keyframe(0.5899997f, 0.1634283f), new Keyframe(0.5999997f, 0.1547686f), new Keyframe(0.6099997f, 0.1464514f), new Keyframe(0.6199997f, 0.1385695f), new Keyframe(0.6299997f, 0.1312161f), new Keyframe(0.6399997f, 0.1244844f), new Keyframe(0.6499997f, 0.1182909f), new Keyframe(0.6599997f, 0.1122041f), new Keyframe(0.6699997f, 0.1062043f), new Keyframe(0.6799996f, 0.100298f), new Keyframe(0.6899996f, 0.09449176f), new Keyframe(0.6999996f, 0.0887922f), new Keyframe(0.7099996f, 0.08320588f), new Keyframe(0.7199996f, 0.07773937f), new Keyframe(0.7299996f, 0.07239924f), new Keyframe(0.7399996f, 0.06719206f), new Keyframe(0.7499996f, 0.06212441f), new Keyframe(0.7599996f, 0.05720284f), new Keyframe(0.7699996f, 0.05243395f), new Keyframe(0.7799996f, 0.04782429f), new Keyframe(0.7899995f, 0.04338043f), new Keyframe(0.7999995f, 0.03910896f), new Keyframe(0.8099995f, 0.03501646f), new Keyframe(0.8199995f, 0.03110947f), new Keyframe(0.8299995f, 0.02739457f), new Keyframe(0.8399995f, 0.02387834f), new Keyframe(0.8499995f, 0.02056735f), new Keyframe(0.8599995f, 0.01746818f), new Keyframe(0.8699995f, 0.01458739f), new Keyframe(0.8799995f, 0.01193154f), new Keyframe(0.8899994f, 0.009507224f), new Keyframe(0.8999994f, 0.007321008f), new Keyframe(0.9099994f, 0.005379461f), new Keyframe(0.9199994f, 0.003689155f), new Keyframe(0.9299994f, 0.002256647f), new Keyframe(0.9399994f, 0.001088545f), new Keyframe(0.9499994f, 0.0001913831f), new Keyframe(0.9599994f, -0.000428237f), new Keyframe(0.9699994f, -0.0007637739f), new Keyframe(0.9799994f, -0.0008086339f), new Keyframe(0.9899994f, -0.0005562529f), new Keyframe(0.9999993f, -6.705523E-08f)
            );

            ThrowVisualUnderhandCurveY = new AnimationCurve
            (
               new Keyframe(0f, 0f), new Keyframe(0.01f, 2.776347E-05f), new Keyframe(0.02f, 0.0001206479f), new Keyframe(0.03f, 0.0002929796f), new Keyframe(0.04f, 0.0005590851f), new Keyframe(0.05f, 0.0009332909f), new Keyframe(0.05999999f, 0.001429923f), new Keyframe(0.06999999f, 0.002063308f), new Keyframe(0.07999999f, 0.002847773f), new Keyframe(0.08999999f, 0.003797644f), new Keyframe(0.09999999f, 0.004927247f), new Keyframe(0.11f, 0.006250909f), new Keyframe(0.12f, 0.007782956f), new Keyframe(0.13f, 0.009537715f), new Keyframe(0.14f, 0.01153968f), new Keyframe(0.15f, 0.01394965f), new Keyframe(0.16f, 0.01679166f), new Keyframe(0.17f, 0.02003028f), new Keyframe(0.18f, 0.02363011f), new Keyframe(0.19f, 0.02755572f), new Keyframe(0.2f, 0.03177169f), new Keyframe(0.21f, 0.0362426f), new Keyframe(0.22f, 0.04093304f), new Keyframe(0.23f, 0.04580759f), new Keyframe(0.24f, 0.05083083f), new Keyframe(0.25f, 0.05596733f), new Keyframe(0.26f, 0.06118168f), new Keyframe(0.27f, 0.06643847f), new Keyframe(0.28f, 0.07167383f), new Keyframe(0.29f, 0.07649778f), new Keyframe(0.3f, 0.08080909f), new Keyframe(0.31f, 0.08462422f), new Keyframe(0.32f, 0.08795962f), new Keyframe(0.33f, 0.09083176f), new Keyframe(0.3399999f, 0.09325711f), new Keyframe(0.3499999f, 0.09525213f), new Keyframe(0.3599999f, 0.09683327f), new Keyframe(0.3699999f, 0.09801699f), new Keyframe(0.3799999f, 0.09881977f), new Keyframe(0.3899999f, 0.09925805f), new Keyframe(0.3999999f, 0.09934831f), new Keyframe(0.4099999f, 0.099107f), new Keyframe(0.4199999f, 0.09855059f), new Keyframe(0.4299999f, 0.09769553f), new Keyframe(0.4399998f, 0.09655828f), new Keyframe(0.4499998f, 0.09515533f), new Keyframe(0.4599998f, 0.0935031f), new Keyframe(0.4699998f, 0.09161809f), new Keyframe(0.4799998f, 0.08951674f), new Keyframe(0.4899998f, 0.0872155f), new Keyframe(0.4999998f, 0.08473087f), new Keyframe(0.5099998f, 0.08207927f), new Keyframe(0.5199998f, 0.07927719f), new Keyframe(0.5299998f, 0.07634109f), new Keyframe(0.5399998f, 0.07328741f), new Keyframe(0.5499998f, 0.07013264f), new Keyframe(0.5599998f, 0.06689321f), new Keyframe(0.5699998f, 0.0635856f), new Keyframe(0.5799997f, 0.06022627f), new Keyframe(0.5899997f, 0.0568317f), new Keyframe(0.5999997f, 0.05341832f), new Keyframe(0.6099997f, 0.05000261f), new Keyframe(0.6199997f, 0.046601f), new Keyframe(0.6299997f, 0.04323001f), new Keyframe(0.6399997f, 0.03990604f), new Keyframe(0.6499997f, 0.03664561f), new Keyframe(0.6599997f, 0.03346514f), new Keyframe(0.6699997f, 0.0303811f), new Keyframe(0.6799996f, 0.02740996f), new Keyframe(0.6899996f, 0.02457382f), new Keyframe(0.6999996f, 0.0219612f), new Keyframe(0.7099996f, 0.01958843f), new Keyframe(0.7199996f, 0.01744326f), new Keyframe(0.7299996f, 0.01551343f), new Keyframe(0.7399996f, 0.01378667f), new Keyframe(0.7499996f, 0.01225072f), new Keyframe(0.7599996f, 0.01089332f), new Keyframe(0.7699996f, 0.009702204f), new Keyframe(0.7799996f, 0.008665115f), new Keyframe(0.7899995f, 0.007769793f), new Keyframe(0.7999995f, 0.007003974f), new Keyframe(0.8099995f, 0.006355396f), new Keyframe(0.8199995f, 0.005811796f), new Keyframe(0.8299995f, 0.005360913f), new Keyframe(0.8399995f, 0.004990485f), new Keyframe(0.8499995f, 0.004688254f), new Keyframe(0.8599995f, 0.004441952f), new Keyframe(0.8699995f, 0.004239323f), new Keyframe(0.8799995f, 0.004068097f), new Keyframe(0.8899994f, 0.003916027f), new Keyframe(0.8999994f, 0.003770838f), new Keyframe(0.9099994f, 0.003620273f), new Keyframe(0.9199994f, 0.003452064f), new Keyframe(0.9299994f, 0.003253961f), new Keyframe(0.9399994f, 0.003013697f), new Keyframe(0.9499994f, 0.002719009f), new Keyframe(0.9599994f, 0.002357632f), new Keyframe(0.9699994f, 0.001917314f), new Keyframe(0.9799994f, 0.001385782f), new Keyframe(0.9899994f, 0.0007507838f), new Keyframe(0.9999993f, 5.029142E-08f)
            );


            TryLoadPatch(new Patch_LerpCamera_ForceUpdateSway());
            TryLoadPatch(new Patch_UpdateSwayFactors());
            TryLoadPatch(new Patch_LateUpdate_UpdateWpnStats());
            TryLoadPatch(new Patch_OnShot());
            TryLoadPatch(new Patch_CalculateCameraPosition_HandLayers());
            TryLoadPatch(new Patch_PlayStepSound());
            TryLoadPatch(new Patch_SetHeadRotation());
            TryLoadPatch(new Patch_ThrowGrenade());
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
            ArmShakeRateMulti = ConstructFloatConfig(ArmShakeRateMultiDefault, ADJUST_VAR_SECTION, "Arm shake effect speed multiplier", "", 0, 5f);
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
            ParallaxReturnToCenterMulti = ConstructFloatConfig(ParallaxReturnToCenterMultiDefault, ADJUST_VAR_SECTION, "Parallax return-to-center multiplier", "Higher = a faster return to center of the parallax", 0, 20f);
            ThrowStrengthMulti = ConstructFloatConfig(ThrowStrengthMultiDefault, ADJUST_VAR_SECTION, "Throw visual effect multi", "", 0, 100f);
            ThrowSpeedMulti = ConstructFloatConfig(ThrowSpeedMultiDefault, ADJUST_VAR_SECTION, "Throw effect speed", "", 0, 10f);


            // dev
            IsLogging = ConstructBoolConfig(false, DEV_SECTION, "Enable debug logging", "");
            DebugSpam = ConstructBoolConfig(false, DEV_SECTION, "Enable debug spam", "");
            //DevTestFloat3 = ConstructFloatConfig(1f, DEV_SECTION, "Test value 3", "This is only for dev use, should not be connected to anything in production releases.", 1f, 30f);
            //DevTestFloat4 = ConstructFloatConfig(1f, DEV_SECTION, "Test value 4", "This is only for dev use, should not be connected to anything in production releases.", 1f, 10f);
            //DevTestFloat5 = ConstructFloatConfig(1f, DEV_SECTION, "Test value 5", "This is only for dev use, should not be connected to anything in production releases.", 0, 10f);
        }

        //
        // drive lerps
        //
        void DriveLerps()
        {
            UtilsTIRL.Update(DeltaTime);
            EfficiencyController.UpdateEfficiencyLerp(DeltaTime);
            FootstepController.UpdateStep(DeltaTime);
            SwayController.UpdateLerp(DeltaTime);
            ThrowController.UpdateLerp(DeltaTime);
        }

        void Update()
        {
            DeltaTime += (UnityEngine.Time.unscaledDeltaTime - DeltaTime);
            Time += UnityEngine.Time.unscaledDeltaTime;

            DriveLerps();
        }

        void FixedUpdate()
        {
            FixedDeltaTime += (UnityEngine.Time.unscaledDeltaTime - FixedDeltaTime);
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