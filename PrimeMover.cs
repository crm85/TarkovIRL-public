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
        const string modName = "TarkovIRL";
        const string modVersion = "0.4";
        readonly Harmony harmony = new Harmony(modGUID);

        public static PrimeMover Instance;

        public AnimationCurve WeapSwayCurve;
        public AnimationCurve DeadZoneCurve;
        public AnimationCurve BreathCurve;
        public AnimationCurve PoseChangeCurve;
        public AnimationCurve ArmStamJitterCurve;

        public float DeltaTime = 0;
        public float Time = 0;

        // config items

        const string BASE_FEATURES_SECTION = "Define base features";
        public static ConfigEntry<bool> IsWeaponDeadzone;
        public static ConfigEntry<bool> IsWeaponSway;
        public static ConfigEntry<bool> IsBreathingEffect;
        public static ConfigEntry<bool> IsPoseEffect;
        public static ConfigEntry<bool> IsPoseChangeEffect;
        public static ConfigEntry<bool> IsArmJitterEffect;

        const string ADJUST_VAR_SECTION = "Adjust feature values";
        public static ConfigEntry<float> DeadzoneGlobalMultiplier;
        public static ConfigEntry<float> WeaponSwayGlobalMultiplier;
        public static ConfigEntry<float> BreathingEffectMulti;
        public static ConfigEntry<float> RotHistoryPoolClamp;
        public static ConfigEntry<float> ArmStamJitter;

        const string DEV_SECTION = "Only for dev/testing";
        public static ConfigEntry<float> DevTestFloat;
        public static ConfigEntry<float> DevTestFloat1;
        public static ConfigEntry<float> DevTestFloat2;
        public static ConfigEntry<float> DevTestFloat3;
        public static ConfigEntry<float> DevTestFloat4;

        // config defaults
        float _deadzoneGlobalMultiplierDefault = 2f;
        float _weaponSwayGlobalMultiplierDefault = 0.5f;
        float _breathingEffectMultiDefault = 1f;
        float _rotHistoryPoolClampDefault = 0.015f;
        float _armJitterDefault = 1f;

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
            Utils.Logger = this.Logger;

            WeapSwayCurve = new AnimationCurve
            (
                new Keyframe(0f, -1.002515f), new Keyframe(0.01f, -1.001876f), new Keyframe(0.02f, -1.001636f), new Keyframe(0.03f, -1.001684f), new Keyframe(0.04f, -1.001909f), new Keyframe(0.05f, -1.0022f), new Keyframe(0.05999999f, -1.002444f), new Keyframe(0.06999999f, -1.002532f), new Keyframe(0.07999999f, -1.002352f), new Keyframe(0.08999999f, -1.001792f), new Keyframe(0.09999999f, -1.000742f), new Keyframe(0.11f, -0.9990897f), new Keyframe(0.12f, -0.9967243f), new Keyframe(0.13f, -0.9946804f), new Keyframe(0.14f, -0.9648644f), new Keyframe(0.15f, -0.07617289f), new Keyframe(0.16f, 0.8704994f), new Keyframe(0.17f, 0.9847867f), new Keyframe(0.18f, 0.9947838f), new Keyframe(0.19f, 0.9952846f), new Keyframe(0.2f, 0.9957775f), new Keyframe(0.21f, 0.9962486f), new Keyframe(0.22f, 0.9966984f), new Keyframe(0.23f, 0.9971274f), new Keyframe(0.24f, 0.9975358f), new Keyframe(0.25f, 0.9979241f), new Keyframe(0.26f, 0.9982928f), new Keyframe(0.27f, 0.9986423f), new Keyframe(0.28f, 0.9989729f), new Keyframe(0.29f, 0.999285f), new Keyframe(0.3f, 0.9995792f), new Keyframe(0.31f, 0.9998558f), new Keyframe(0.32f, 1.000115f), new Keyframe(0.33f, 1.000358f), new Keyframe(0.3399999f, 1.000584f), new Keyframe(0.3499999f, 1.000794f), new Keyframe(0.3599999f, 1.000989f), new Keyframe(0.3699999f, 1.001169f), new Keyframe(0.3799999f, 1.001333f), new Keyframe(0.3899999f, 1.001484f), new Keyframe(0.3999999f, 1.001621f), new Keyframe(0.4099999f, 1.001744f), new Keyframe(0.4199999f, 1.001854f), new Keyframe(0.4299999f, 1.001952f), new Keyframe(0.4399998f, 1.002037f), new Keyframe(0.4499998f, 1.00211f), new Keyframe(0.4599998f, 1.002172f), new Keyframe(0.4699998f, 1.002223f), new Keyframe(0.4799998f, 1.002264f), new Keyframe(0.4899998f, 1.002294f), new Keyframe(0.4999998f, 1.002315f), new Keyframe(0.5099998f, 1.002326f), new Keyframe(0.5199998f, 1.002328f), new Keyframe(0.5299998f, 1.002321f), new Keyframe(0.5399998f, 1.002307f), new Keyframe(0.5499998f, 1.002285f), new Keyframe(0.5599998f, 1.002255f), new Keyframe(0.5699998f, 1.002219f), new Keyframe(0.5799997f, 1.002176f), new Keyframe(0.5899997f, 1.002127f), new Keyframe(0.5999997f, 1.002073f), new Keyframe(0.6099997f, 1.002013f), new Keyframe(0.6199997f, 1.001949f), new Keyframe(0.6299997f, 1.00188f), new Keyframe(0.6399997f, 1.001807f), new Keyframe(0.6499997f, 1.00173f), new Keyframe(0.6599997f, 1.00165f), new Keyframe(0.6699997f, 1.001568f), new Keyframe(0.6799996f, 1.001483f), new Keyframe(0.6899996f, 1.001397f), new Keyframe(0.6999996f, 1.001308f), new Keyframe(0.7099996f, 1.001219f), new Keyframe(0.7199996f, 1.001129f), new Keyframe(0.7299996f, 1.001039f), new Keyframe(0.7399996f, 1.000948f), new Keyframe(0.7499996f, 1.000859f), new Keyframe(0.7599996f, 1.00077f), new Keyframe(0.7699996f, 1.000683f), new Keyframe(0.7799996f, 1.000597f), new Keyframe(0.7899995f, 1.000514f), new Keyframe(0.7999995f, 1.000433f), new Keyframe(0.8099995f, 1.000355f), new Keyframe(0.8199995f, 1.000281f), new Keyframe(0.8299995f, 1.00021f), new Keyframe(0.8399995f, 1.000144f), new Keyframe(0.8499995f, 1.000083f), new Keyframe(0.8599995f, 1.000026f), new Keyframe(0.8699995f, 0.9999753f), new Keyframe(0.8799995f, 0.9999302f), new Keyframe(0.8899994f, 0.9998915f), new Keyframe(0.8999994f, 0.9998594f), new Keyframe(0.9099994f, 0.9998345f), new Keyframe(0.9199994f, 0.9998172f), new Keyframe(0.9299994f, 0.9998078f), new Keyframe(0.9399994f, 0.9998069f), new Keyframe(0.9499994f, 0.9998147f), new Keyframe(0.9599994f, 0.9998317f), new Keyframe(0.9699994f, 0.9998583f), new Keyframe(0.9799994f, 0.999895f), new Keyframe(0.9899994f, 0.9999421f), new Keyframe(0.9999993f, 1f)
            );

            DeadZoneCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0f), new Keyframe(0.01f, 0.0820853f), new Keyframe(0.02f, 0.1603706f), new Keyframe(0.03f, 0.2348942f), new Keyframe(0.04f, 0.3056942f), new Keyframe(0.05f, 0.3728089f), new Keyframe(0.05999999f, 0.4362763f), new Keyframe(0.06999999f, 0.4961348f), new Keyframe(0.07999999f, 0.5524226f), new Keyframe(0.08999999f, 0.6051777f), new Keyframe(0.09999999f, 0.6544385f), new Keyframe(0.11f, 0.7002431f), new Keyframe(0.12f, 0.7426297f), new Keyframe(0.13f, 0.7816366f), new Keyframe(0.14f, 0.8173019f), new Keyframe(0.15f, 0.8496637f), new Keyframe(0.16f, 0.8787605f), new Keyframe(0.17f, 0.9046302f), new Keyframe(0.18f, 0.9273111f), new Keyframe(0.19f, 0.9468414f), new Keyframe(0.2f, 0.9632593f), new Keyframe(0.21f, 0.976603f), new Keyframe(0.22f, 0.9869107f), new Keyframe(0.23f, 0.9942206f), new Keyframe(0.24f, 0.998571f), new Keyframe(0.25f, 1f), new Keyframe(0.26f, 0.9976224f), new Keyframe(0.27f, 0.9906552f), new Keyframe(0.28f, 0.9792894f), new Keyframe(0.29f, 0.963716f), new Keyframe(0.3f, 0.9441259f), new Keyframe(0.31f, 0.9207103f), new Keyframe(0.32f, 0.8936599f), new Keyframe(0.33f, 0.8631661f), new Keyframe(0.3399999f, 0.8294196f), new Keyframe(0.3499999f, 0.7926116f), new Keyframe(0.3599999f, 0.752933f), new Keyframe(0.3699999f, 0.7105749f), new Keyframe(0.3799999f, 0.6657281f), new Keyframe(0.3899999f, 0.6185838f), new Keyframe(0.3999999f, 0.5693331f), new Keyframe(0.4099999f, 0.5181667f), new Keyframe(0.4199999f, 0.4652758f), new Keyframe(0.4299999f, 0.4108515f), new Keyframe(0.4399998f, 0.3550845f), new Keyframe(0.4499998f, 0.2981661f), new Keyframe(0.4599998f, 0.2402873f), new Keyframe(0.4699998f, 0.1816389f), new Keyframe(0.4799998f, 0.122412f), new Keyframe(0.4899998f, 0.06279773f), new Keyframe(0.4999998f, 0.002986908f), new Keyframe(0.5099998f, -0.05682945f), new Keyframe(0.5199998f, -0.1164602f), new Keyframe(0.5299998f, -0.1757144f), new Keyframe(0.5399998f, -0.2344009f), new Keyframe(0.5499998f, -0.292329f), new Keyframe(0.5599998f, -0.3493074f), new Keyframe(0.5699998f, -0.405145f), new Keyframe(0.5799997f, -0.4596515f), new Keyframe(0.5899997f, -0.512635f), new Keyframe(0.5999997f, -0.5639049f), new Keyframe(0.6099997f, -0.6132703f), new Keyframe(0.6199997f, -0.6605401f), new Keyframe(0.6299997f, -0.7055233f), new Keyframe(0.6399997f, -0.7480285f), new Keyframe(0.6499997f, -0.7878655f), new Keyframe(0.6599997f, -0.8248427f), new Keyframe(0.6699997f, -0.8587692f), new Keyframe(0.6799996f, -0.8894541f), new Keyframe(0.6899996f, -0.9167062f), new Keyframe(0.6999996f, -0.9403346f), new Keyframe(0.7099996f, -0.9601483f), new Keyframe(0.7199996f, -0.9759563f), new Keyframe(0.7299996f, -0.9875677f), new Keyframe(0.7399996f, -0.9947914f), new Keyframe(0.7499996f, -0.9974363f), new Keyframe(0.7599996f, -0.9954991f), new Keyframe(0.7699996f, -0.9892066f), new Keyframe(0.7799996f, -0.9787146f), new Keyframe(0.7899995f, -0.9641785f), new Keyframe(0.7999995f, -0.9457538f), new Keyframe(0.8099995f, -0.9235957f), new Keyframe(0.8199995f, -0.8978599f), new Keyframe(0.8299995f, -0.8687016f), new Keyframe(0.8399995f, -0.8362765f), new Keyframe(0.8499995f, -0.8007398f), new Keyframe(0.8599995f, -0.762247f), new Keyframe(0.8699995f, -0.7209537f), new Keyframe(0.8799995f, -0.6770151f), new Keyframe(0.8899994f, -0.6305867f), new Keyframe(0.8999994f, -0.5818241f), new Keyframe(0.9099994f, -0.5308825f), new Keyframe(0.9199994f, -0.4779174f), new Keyframe(0.9299994f, -0.4230843f), new Keyframe(0.9399994f, -0.3665386f), new Keyframe(0.9499994f, -0.3084358f), new Keyframe(0.9599994f, -0.2489312f), new Keyframe(0.9699994f, -0.1881803f), new Keyframe(0.9799994f, -0.1263387f), new Keyframe(0.9899994f, -0.0635615f), new Keyframe(0.9999993f, -4.351139E-06f)
            );

            BreathCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0.001708984f), new Keyframe(0.01f, 0.07340872f), new Keyframe(0.02f, 0.1447571f), new Keyframe(0.03f, 0.2159234f), new Keyframe(0.04f, 0.287077f), new Keyframe(0.05f, 0.3583873f), new Keyframe(0.05999999f, 0.4300235f), new Keyframe(0.06999999f, 0.5021549f), new Keyframe(0.07999999f, 0.5749511f), new Keyframe(0.08999999f, 0.6485812f), new Keyframe(0.09999999f, 0.7190837f), new Keyframe(0.11f, 0.7796323f), new Keyframe(0.12f, 0.8308698f), new Keyframe(0.13f, 0.8735839f), new Keyframe(0.14f, 0.9085627f), new Keyframe(0.15f, 0.9365937f), new Keyframe(0.16f, 0.958465f), new Keyframe(0.17f, 0.9749643f), new Keyframe(0.18f, 0.9868796f), new Keyframe(0.19f, 0.9949986f), new Keyframe(0.2f, 1.000109f), new Keyframe(0.21f, 1.003f), new Keyframe(0.22f, 1.004457f), new Keyframe(0.23f, 1.00527f), new Keyframe(0.24f, 1.006216f), new Keyframe(0.25f, 0.9934806f), new Keyframe(0.26f, 0.9566338f), new Keyframe(0.27f, 0.8993476f), new Keyframe(0.28f, 0.825294f), new Keyframe(0.29f, 0.7381451f), new Keyframe(0.3f, 0.6415731f), new Keyframe(0.31f, 0.5392498f), new Keyframe(0.32f, 0.4348473f), new Keyframe(0.33f, 0.3327447f), new Keyframe(0.3399999f, 0.2382682f), new Keyframe(0.3499999f, 0.1512108f), new Keyframe(0.3599999f, 0.07075208f), new Keyframe(0.3699999f, -0.003928483f), new Keyframe(0.3799999f, -0.07365131f), new Keyframe(0.3899999f, -0.1392368f), new Keyframe(0.3999999f, -0.2015056f), new Keyframe(0.4099999f, -0.2613338f), new Keyframe(0.4199999f, -0.3202845f), new Keyframe(0.4299999f, -0.378557f), new Keyframe(0.4399998f, -0.4358587f), new Keyframe(0.4499998f, -0.4918973f), new Keyframe(0.4599998f, -0.5463802f), new Keyframe(0.4699998f, -0.5990149f), new Keyframe(0.4799998f, -0.6495088f), new Keyframe(0.4899998f, -0.6975697f), new Keyframe(0.4999998f, -0.7429048f), new Keyframe(0.5099998f, -0.7852218f), new Keyframe(0.5199998f, -0.824228f), new Keyframe(0.5299998f, -0.8596311f), new Keyframe(0.5399998f, -0.8911383f), new Keyframe(0.5499998f, -0.9184575f), new Keyframe(0.5599998f, -0.941296f), new Keyframe(0.5699998f, -0.9593612f), new Keyframe(0.5799997f, -0.9723607f), new Keyframe(0.5899997f, -0.9800022f), new Keyframe(0.5999997f, -0.9830432f), new Keyframe(0.6099997f, -0.9852618f), new Keyframe(0.6199997f, -0.9870723f), new Keyframe(0.6299997f, -0.988526f), new Keyframe(0.6399997f, -0.9896744f), new Keyframe(0.6499997f, -0.9905687f), new Keyframe(0.6599997f, -0.9912603f), new Keyframe(0.6699997f, -0.9918006f), new Keyframe(0.6799996f, -0.992241f), new Keyframe(0.6899996f, -0.9926327f), new Keyframe(0.6999996f, -0.9930272f), new Keyframe(0.7099996f, -0.9934759f), new Keyframe(0.7199996f, -0.9940174f), new Keyframe(0.7299996f, -0.9938536f), new Keyframe(0.7399996f, -0.9923377f), new Keyframe(0.7499996f, -0.9892635f), new Keyframe(0.7599996f, -0.9844247f), new Keyframe(0.7699996f, -0.9776151f), new Keyframe(0.7799996f, -0.9686285f), new Keyframe(0.7899995f, -0.9572586f), new Keyframe(0.7999995f, -0.9433164f), new Keyframe(0.8099995f, -0.9273839f), new Keyframe(0.8199995f, -0.909653f), new Keyframe(0.8299995f, -0.8898755f), new Keyframe(0.8399995f, -0.8678032f), new Keyframe(0.8499995f, -0.8431879f), new Keyframe(0.8599995f, -0.8157812f), new Keyframe(0.8699995f, -0.7853352f), new Keyframe(0.8799995f, -0.7516014f), new Keyframe(0.8899994f, -0.7143317f), new Keyframe(0.8999994f, -0.673278f), new Keyframe(0.9099994f, -0.6281918f), new Keyframe(0.9199994f, -0.5788252f), new Keyframe(0.9299994f, -0.5249298f), new Keyframe(0.9399994f, -0.4662574f), new Keyframe(0.9499994f, -0.4025598f), new Keyframe(0.9599994f, -0.3335888f), new Keyframe(0.9699994f, -0.2590962f), new Keyframe(0.9799994f, -0.1788337f), new Keyframe(0.9899994f, -0.09255308f), new Keyframe(0.9999993f, -6.377697E-06f)
            );

            PoseChangeCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0f), new Keyframe(0.01f, 0.04753859f), new Keyframe(0.02f, 0.06824711f), new Keyframe(0.03f, 0.07190572f), new Keyframe(0.04f, 0.06472864f), new Keyframe(0.05f, 0.03672462f), new Keyframe(0.05999999f, -0.01143076f), new Keyframe(0.06999999f, -0.07712235f), new Keyframe(0.07999999f, -0.157735f), new Keyframe(0.08999999f, -0.2506537f), new Keyframe(0.09999999f, -0.3532633f), new Keyframe(0.11f, -0.4620121f), new Keyframe(0.12f, -0.5536414f), new Keyframe(0.13f, -0.6220356f), new Keyframe(0.14f, -0.6711998f), new Keyframe(0.15f, -0.7051392f), new Keyframe(0.16f, -0.7278589f), new Keyframe(0.17f, -0.7433642f), new Keyframe(0.18f, -0.7578313f), new Keyframe(0.19f, -0.7805023f), new Keyframe(0.2f, -0.8117735f), new Keyframe(0.21f, -0.8513885f), new Keyframe(0.22f, -0.8990905f), new Keyframe(0.23f, -0.9521281f), new Keyframe(0.24f, -0.9863883f), new Keyframe(0.25f, -0.998183f), new Keyframe(0.26f, -0.9904182f), new Keyframe(0.27f, -0.9660002f), new Keyframe(0.28f, -0.9278349f), new Keyframe(0.29f, -0.8788286f), new Keyframe(0.3f, -0.8218873f), new Keyframe(0.31f, -0.759917f), new Keyframe(0.32f, -0.695824f), new Keyframe(0.33f, -0.6325832f), new Keyframe(0.3399999f, -0.5721059f), new Keyframe(0.3499999f, -0.5145938f), new Keyframe(0.3599999f, -0.4601647f), new Keyframe(0.3699999f, -0.4089365f), new Keyframe(0.3799999f, -0.361027f), new Keyframe(0.3899999f, -0.316554f), new Keyframe(0.3999999f, -0.2756355f), new Keyframe(0.4099999f, -0.2383893f), new Keyframe(0.4199999f, -0.2049332f), new Keyframe(0.4299999f, -0.1753851f), new Keyframe(0.4399998f, -0.1498628f), new Keyframe(0.4499998f, -0.1284841f), new Keyframe(0.4599998f, -0.1113669f), new Keyframe(0.4699998f, -0.09862924f), new Keyframe(0.4799998f, -0.08953421f), new Keyframe(0.4899998f, -0.07422893f), new Keyframe(0.4999998f, -0.05229831f), new Keyframe(0.5099998f, -0.02683907f), new Keyframe(0.5199998f, -0.0009481311f), new Keyframe(0.5299998f, 0.02227778f), new Keyframe(0.5399998f, 0.03974189f), new Keyframe(0.5499998f, 0.04943786f), new Keyframe(0.5599998f, 0.05533161f), new Keyframe(0.5699998f, 0.05844303f), new Keyframe(0.5799997f, 0.05908555f), new Keyframe(0.5899997f, 0.05757258f), new Keyframe(0.5999997f, 0.05421757f), new Keyframe(0.6099997f, 0.04933393f), new Keyframe(0.6199997f, 0.0432351f), new Keyframe(0.6299997f, 0.03623449f), new Keyframe(0.6399997f, 0.02864554f), new Keyframe(0.6499997f, 0.02078166f), new Keyframe(0.6599997f, 0.01295629f), new Keyframe(0.6699997f, 0.00548286f), new Keyframe(0.6799996f, -0.001325212f), new Keyframe(0.6899996f, -0.007154491f), new Keyframe(0.6999996f, -0.01169154f), new Keyframe(0.7099996f, -0.01462297f), new Keyframe(0.7199996f, -0.01570082f), new Keyframe(0.7299996f, -0.01604912f), new Keyframe(0.7399996f, -0.01618946f), new Keyframe(0.7499996f, -0.01613806f), new Keyframe(0.7599996f, -0.01591115f), new Keyframe(0.7699996f, -0.01552495f), new Keyframe(0.7799996f, -0.01499569f), new Keyframe(0.7899995f, -0.01433959f), new Keyframe(0.7999995f, -0.01357289f), new Keyframe(0.8099995f, -0.01271181f), new Keyframe(0.8199995f, -0.01177257f), new Keyframe(0.8299995f, -0.01077141f), new Keyframe(0.8399995f, -0.009724541f), new Keyframe(0.8499995f, -0.008648196f), new Keyframe(0.8599995f, -0.007558605f), new Keyframe(0.8699995f, -0.006471992f), new Keyframe(0.8799995f, -0.005404581f), new Keyframe(0.8899994f, -0.0043726f), new Keyframe(0.8999994f, -0.003392277f), new Keyframe(0.9099994f, -0.002479835f), new Keyframe(0.9199994f, -0.001651503f), new Keyframe(0.9299994f, -0.0009235078f), new Keyframe(0.9399994f, -0.0003120769f), new Keyframe(0.9499994f, 0.0001665689f), new Keyframe(0.9599994f, 0.0004961994f), new Keyframe(0.9699994f, 0.0006605871f), new Keyframe(0.9799994f, 0.0006435066f), new Keyframe(0.9899994f, 0.0004287343f), new Keyframe(0.9999993f, 3.539026E-08f)
            );

            ArmStamJitterCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0f), new Keyframe(0.01f, 0.02002987f), new Keyframe(0.02f, 0.05177919f), new Keyframe(0.03f, 0.0861365f), new Keyframe(0.04f, 0.1139903f), new Keyframe(0.05f, 0.1262292f), new Keyframe(0.05999999f, 0.1387799f), new Keyframe(0.06999999f, 0.1714787f), new Keyframe(0.07999999f, 0.2052841f), new Keyframe(0.08999999f, 0.220898f), new Keyframe(0.09999999f, 0.1931826f), new Keyframe(0.11f, 0.08000854f), new Keyframe(0.12f, 0.07054432f), new Keyframe(0.13f, 0.07545625f), new Keyframe(0.14f, 0.06106036f), new Keyframe(0.15f, 0.03696794f), new Keyframe(0.16f, 0.01279034f), new Keyframe(0.17f, -0.001861159f), new Keyframe(0.18f, -0.00503464f), new Keyframe(0.19f, -0.02902272f), new Keyframe(0.2f, -0.06406359f), new Keyframe(0.21f, -0.09354599f), new Keyframe(0.22f, -0.1367515f), new Keyframe(0.23f, -0.1946985f), new Keyframe(0.24f, -0.2474255f), new Keyframe(0.25f, -0.2756253f), new Keyframe(0.26f, -0.2846023f), new Keyframe(0.27f, -0.2834283f), new Keyframe(0.28f, -0.2741062f), new Keyframe(0.29f, -0.2586388f), new Keyframe(0.3f, -0.2390289f), new Keyframe(0.31f, -0.2172793f), new Keyframe(0.32f, -0.1953928f), new Keyframe(0.33f, -0.1753722f), new Keyframe(0.3399999f, -0.1592204f), new Keyframe(0.3499999f, -0.1489402f), new Keyframe(0.3599999f, -0.1395335f), new Keyframe(0.3699999f, -0.09086414f), new Keyframe(0.3799999f, -0.01933302f), new Keyframe(0.3899999f, 0.04705437f), new Keyframe(0.3999999f, 0.08029242f), new Keyframe(0.4099999f, 0.1618353f), new Keyframe(0.4199999f, 0.2200256f), new Keyframe(0.4299999f, 0.2271181f), new Keyframe(0.4399998f, 0.2132713f), new Keyframe(0.4499998f, 0.1909959f), new Keyframe(0.4599998f, 0.1728026f), new Keyframe(0.4699998f, 0.169514f), new Keyframe(0.4799998f, 0.1561657f), new Keyframe(0.4899998f, 0.1291374f), new Keyframe(0.4999998f, 0.09996025f), new Keyframe(0.5099998f, 0.08016507f), new Keyframe(0.5199998f, 0.08081633f), new Keyframe(0.5299998f, 0.04337962f), new Keyframe(0.5399998f, -0.03990857f), new Keyframe(0.5499998f, -0.1130076f), new Keyframe(0.5599998f, -0.1297899f), new Keyframe(0.5699998f, -0.1205692f), new Keyframe(0.5799997f, -0.1226004f), new Keyframe(0.5899997f, -0.1690248f), new Keyframe(0.5999997f, -0.2687237f), new Keyframe(0.6099997f, -0.2949535f), new Keyframe(0.6199997f, -0.2905267f), new Keyframe(0.6299997f, -0.2679948f), new Keyframe(0.6399997f, -0.2130725f), new Keyframe(0.6499997f, -0.1072184f), new Keyframe(0.6599997f, -0.07028858f), new Keyframe(0.6699997f, -0.0884738f), new Keyframe(0.6799996f, -0.03864045f), new Keyframe(0.6899996f, 0.02149329f), new Keyframe(0.6999996f, 0.04437444f), new Keyframe(0.7099996f, 0.08881943f), new Keyframe(0.7199996f, 0.147704f), new Keyframe(0.7299996f, 0.1883931f), new Keyframe(0.7399996f, 0.1978056f), new Keyframe(0.7499996f, 0.1958648f), new Keyframe(0.7599996f, 0.1974101f), new Keyframe(0.7699996f, 0.1384161f), new Keyframe(0.7799996f, 0.04644331f), new Keyframe(0.7899995f, -0.01248662f), new Keyframe(0.7999995f, -0.03949538f), new Keyframe(0.8099995f, -0.1110162f), new Keyframe(0.8199995f, -0.182054f), new Keyframe(0.8299995f, -0.2041247f), new Keyframe(0.8399995f, -0.1287441f), new Keyframe(0.8499995f, -0.04300359f), new Keyframe(0.8599995f, -0.07900946f), new Keyframe(0.8699995f, -0.1746287f), new Keyframe(0.8799995f, -0.2664219f), new Keyframe(0.8899994f, -0.2970352f), new Keyframe(0.8999994f, -0.2974063f), new Keyframe(0.9099994f, -0.2874107f), new Keyframe(0.9199994f, -0.269584f), new Keyframe(0.9299994f, -0.2464621f), new Keyframe(0.9399994f, -0.2205806f), new Keyframe(0.9499994f, -0.1944753f), new Keyframe(0.9599994f, -0.170682f), new Keyframe(0.9699994f, -0.1515432f), new Keyframe(0.9799994f, -0.1052232f), new Keyframe(0.9899994f, -0.03925297f), new Keyframe(0.9999993f, -6.854534E-07f) 
            );

            TryLoadPatch(new LerpCameraPatch_UpdateSway());
            TryLoadPatch(new UpdateSwayFactorsPatch());
            TryLoadPatch(new LookPatch_ApplyDeadzone());
            TryLoadPatch(new LateUpdatePatch_UpdateWpnStats());
            TryLoadPatch(new OnShotPatch_UpdateWpnWeight());
            TryLoadPatch(new CalculateCameraPositionPatch_HandsPositionLayers());
        }

        void LoadConfigValues()
        {
            // section 1
            IsWeaponDeadzone = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable weapon deadzone", "The weapon 'deadzone' effect is a separation of the player's camera from where the weapon is pointing. In vanilla these are perfectly aligned at all times; in this mod, these values become disaligned based on the size and ergo value of the weapon.");
            IsWeaponSway = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable weapon sway", "This mod changes how weapon sway works: your weapon generally sways ahead of your aimpoint (rather than behind like in vanilla), and the severity of the sway is defined by many factors. See mod documentation for fuller explanation.");
            IsBreathingEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable breathing effect", "Adds a visual oscillation to your character's weapon, the intensity of which depends on your current stamina.");
            IsPoseEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable stance-dependent weapon position", "When you crouch, your weapon position is pulled in closer to your character.");
            IsPoseChangeEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable stance transition effect", "When you change your crouch position, you see a dip in your sight picture, the speed and intensity of which is driven by how much you change your stance (e.g. incrimental change versus full change.");
            IsArmJitterEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable extra arms shaking", "Adds additional arm shake as arm stam decreases");


            // section 2
            DeadzoneGlobalMultiplier = ConstructFloatConfig(_deadzoneGlobalMultiplierDefault, ADJUST_VAR_SECTION, "Weapon deadzone intensity", "Set deadzone intensity.", 0, 5f);
            WeaponSwayGlobalMultiplier = ConstructFloatConfig(_weaponSwayGlobalMultiplierDefault, ADJUST_VAR_SECTION, "Weapon sway intensity", "Set weapon sway intensity.", 0, 5f);
            BreathingEffectMulti = ConstructFloatConfig(_breathingEffectMultiDefault, ADJUST_VAR_SECTION, "Breathing effect intensity", "Set breathing effect intensity.", 0, 5f);
            RotHistoryPoolClamp = ConstructFloatConfig(_rotHistoryPoolClampDefault, ADJUST_VAR_SECTION, "Rotation History Clamp Size", "Hard to explain this one, I wouldn't touch it if I were you.", 0, 0.1f);
            ArmStamJitter = ConstructFloatConfig(_armJitterDefault, ADJUST_VAR_SECTION, "Arm shake intensity", "Set extra arm shake intensity.", 0, 5f);

            // section 5
            DevTestFloat = ConstructFloatConfig(1f, DEV_SECTION, "Test value", "This is only for dev use, should not be connected to anything in production releases.", -10, 1000);
            DevTestFloat1 = ConstructFloatConfig(0.5f, DEV_SECTION, "Test value 1", "This is only for dev use, should not be connected to anything in production releases.", -10, 100);
            DevTestFloat2 = ConstructFloatConfig(100f, DEV_SECTION, "Test value 2", "This is only for dev use, should not be connected to anything in production releases.", -10, 100);
            DevTestFloat3 = ConstructFloatConfig(1f, DEV_SECTION, "Test value 3", "This is only for dev use, should not be connected to anything in production releases.", -10, 100);
            DevTestFloat4 = ConstructFloatConfig(1f, DEV_SECTION, "Test value 4", "This is only for dev use, should not be connected to anything in production releases.", -10, 100);
        }

        void Update()
        {
            WeaponsHandlingController.DeltaTime = DeltaTime;
            WeaponsHandlingController.Time = Time;
            Utils.PumpUtilsUpdate(UnityEngine.Time.deltaTime);
            HandsMovController.UpdateLerp(DeltaTime);

            DeltaTime += (UnityEngine.Time.unscaledDeltaTime - DeltaTime);
            Time += UnityEngine.Time.unscaledDeltaTime;
        }

        void LateUpdate()
        {
            WeaponsHandlingController.IsSwayUpdatedThisFrame = false;
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
                Utils.Log(true, "could not load " + patchName + " -- " + e);
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