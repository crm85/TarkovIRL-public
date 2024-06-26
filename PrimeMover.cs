using System;
using Aki.Reflection.Patching;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace TarkovIRL
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class PrimeMover : BaseUnityPlugin
    {
        private const string modGUID = "TarkovIRL";
        private const string modName = "TarkovIRL";
        private const string modVersion = "0.31";
        private readonly Harmony harmony = new Harmony(modGUID);

        public static PrimeMover Instance;

        public AnimationCurve WeapSwayCurve;
        public AnimationCurve DeadZoneCurve;
        public AnimationCurve BreathCurve;
        public AnimationCurve PoseChangeCurve;

        public float DeltaTime = 0;
        public float Time = 0;


        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            Initialize();
        }

        void Initialize()
        {
            Utils.Logger = this.Logger;

            WeapSwayCurve = new AnimationCurve
            (
                new Keyframe(0f, -1f), new Keyframe(.1f, -1f), new Keyframe(.2f, 1f), new Keyframe(1f, 1f)
            );

            DeadZoneCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0f), new Keyframe(0.01f, 0.0820853f), new Keyframe(0.02f, 0.1603706f), new Keyframe(0.03f, 0.2348942f), new Keyframe(0.04f, 0.3056942f), new Keyframe(0.05f, 0.3728089f), new Keyframe(0.05999999f, 0.4362763f), new Keyframe(0.06999999f, 0.4961348f), new Keyframe(0.07999999f, 0.5524226f), new Keyframe(0.08999999f, 0.6051777f), new Keyframe(0.09999999f, 0.6544385f), new Keyframe(0.11f, 0.7002431f), new Keyframe(0.12f, 0.7426297f), new Keyframe(0.13f, 0.7816366f), new Keyframe(0.14f, 0.8173019f), new Keyframe(0.15f, 0.8496637f), new Keyframe(0.16f, 0.8787605f), new Keyframe(0.17f, 0.9046302f), new Keyframe(0.18f, 0.9273111f), new Keyframe(0.19f, 0.9468414f), new Keyframe(0.2f, 0.9632593f), new Keyframe(0.21f, 0.976603f), new Keyframe(0.22f, 0.9869107f), new Keyframe(0.23f, 0.9942206f), new Keyframe(0.24f, 0.998571f), new Keyframe(0.25f, 1f), new Keyframe(0.26f, 0.9976224f), new Keyframe(0.27f, 0.9906552f), new Keyframe(0.28f, 0.9792894f), new Keyframe(0.29f, 0.963716f), new Keyframe(0.3f, 0.9441259f), new Keyframe(0.31f, 0.9207103f), new Keyframe(0.32f, 0.8936599f), new Keyframe(0.33f, 0.8631661f), new Keyframe(0.3399999f, 0.8294196f), new Keyframe(0.3499999f, 0.7926116f), new Keyframe(0.3599999f, 0.752933f), new Keyframe(0.3699999f, 0.7105749f), new Keyframe(0.3799999f, 0.6657281f), new Keyframe(0.3899999f, 0.6185838f), new Keyframe(0.3999999f, 0.5693331f), new Keyframe(0.4099999f, 0.5181667f), new Keyframe(0.4199999f, 0.4652758f), new Keyframe(0.4299999f, 0.4108515f), new Keyframe(0.4399998f, 0.3550845f), new Keyframe(0.4499998f, 0.2981661f), new Keyframe(0.4599998f, 0.2402873f), new Keyframe(0.4699998f, 0.1816389f), new Keyframe(0.4799998f, 0.122412f), new Keyframe(0.4899998f, 0.06279773f), new Keyframe(0.4999998f, 0.002986908f), new Keyframe(0.5099998f, -0.05682945f), new Keyframe(0.5199998f, -0.1164602f), new Keyframe(0.5299998f, -0.1757144f), new Keyframe(0.5399998f, -0.2344009f), new Keyframe(0.5499998f, -0.292329f), new Keyframe(0.5599998f, -0.3493074f), new Keyframe(0.5699998f, -0.405145f), new Keyframe(0.5799997f, -0.4596515f), new Keyframe(0.5899997f, -0.512635f), new Keyframe(0.5999997f, -0.5639049f), new Keyframe(0.6099997f, -0.6132703f), new Keyframe(0.6199997f, -0.6605401f), new Keyframe(0.6299997f, -0.7055233f), new Keyframe(0.6399997f, -0.7480285f), new Keyframe(0.6499997f, -0.7878655f), new Keyframe(0.6599997f, -0.8248427f), new Keyframe(0.6699997f, -0.8587692f), new Keyframe(0.6799996f, -0.8894541f), new Keyframe(0.6899996f, -0.9167062f), new Keyframe(0.6999996f, -0.9403346f), new Keyframe(0.7099996f, -0.9601483f), new Keyframe(0.7199996f, -0.9759563f), new Keyframe(0.7299996f, -0.9875677f), new Keyframe(0.7399996f, -0.9947914f), new Keyframe(0.7499996f, -0.9974363f), new Keyframe(0.7599996f, -0.9954991f), new Keyframe(0.7699996f, -0.9892066f), new Keyframe(0.7799996f, -0.9787146f), new Keyframe(0.7899995f, -0.9641785f), new Keyframe(0.7999995f, -0.9457538f), new Keyframe(0.8099995f, -0.9235957f), new Keyframe(0.8199995f, -0.8978599f), new Keyframe(0.8299995f, -0.8687016f), new Keyframe(0.8399995f, -0.8362765f), new Keyframe(0.8499995f, -0.8007398f), new Keyframe(0.8599995f, -0.762247f), new Keyframe(0.8699995f, -0.7209537f), new Keyframe(0.8799995f, -0.6770151f), new Keyframe(0.8899994f, -0.6305867f), new Keyframe(0.8999994f, -0.5818241f), new Keyframe(0.9099994f, -0.5308825f), new Keyframe(0.9199994f, -0.4779174f), new Keyframe(0.9299994f, -0.4230843f), new Keyframe(0.9399994f, -0.3665386f), new Keyframe(0.9499994f, -0.3084358f), new Keyframe(0.9599994f, -0.2489312f), new Keyframe(0.9699994f, -0.1881803f), new Keyframe(0.9799994f, -0.1263387f), new Keyframe(0.9899994f, -0.0635615f), new Keyframe(0.9999993f, -4.351139E-06f)
            );

            BreathCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0f), new Keyframe(0.01f, 0.0820853f), new Keyframe(0.02f, 0.1603706f), new Keyframe(0.03f, 0.2348942f), new Keyframe(0.04f, 0.3056942f), new Keyframe(0.05f, 0.3728089f), new Keyframe(0.05999999f, 0.4362763f), new Keyframe(0.06999999f, 0.4961348f), new Keyframe(0.07999999f, 0.5524226f), new Keyframe(0.08999999f, 0.6051777f), new Keyframe(0.09999999f, 0.6544385f), new Keyframe(0.11f, 0.7002431f), new Keyframe(0.12f, 0.7426297f), new Keyframe(0.13f, 0.7816366f), new Keyframe(0.14f, 0.8173019f), new Keyframe(0.15f, 0.8496637f), new Keyframe(0.16f, 0.8787605f), new Keyframe(0.17f, 0.9046302f), new Keyframe(0.18f, 0.9273111f), new Keyframe(0.19f, 0.9468414f), new Keyframe(0.2f, 0.9632593f), new Keyframe(0.21f, 0.976603f), new Keyframe(0.22f, 0.9869107f), new Keyframe(0.23f, 0.9942206f), new Keyframe(0.24f, 0.998571f), new Keyframe(0.25f, 1f), new Keyframe(0.26f, 0.9976224f), new Keyframe(0.27f, 0.9906552f), new Keyframe(0.28f, 0.9792894f), new Keyframe(0.29f, 0.963716f), new Keyframe(0.3f, 0.9441259f), new Keyframe(0.31f, 0.9207103f), new Keyframe(0.32f, 0.8936599f), new Keyframe(0.33f, 0.8631661f), new Keyframe(0.3399999f, 0.8294196f), new Keyframe(0.3499999f, 0.7926116f), new Keyframe(0.3599999f, 0.752933f), new Keyframe(0.3699999f, 0.7105749f), new Keyframe(0.3799999f, 0.6657281f), new Keyframe(0.3899999f, 0.6185838f), new Keyframe(0.3999999f, 0.5693331f), new Keyframe(0.4099999f, 0.5181667f), new Keyframe(0.4199999f, 0.4652758f), new Keyframe(0.4299999f, 0.4108515f), new Keyframe(0.4399998f, 0.3550845f), new Keyframe(0.4499998f, 0.2981661f), new Keyframe(0.4599998f, 0.2402873f), new Keyframe(0.4699998f, 0.1816389f), new Keyframe(0.4799998f, 0.122412f), new Keyframe(0.4899998f, 0.06279773f), new Keyframe(0.4999998f, 0.002986908f), new Keyframe(0.5099998f, -0.05682945f), new Keyframe(0.5199998f, -0.1164602f), new Keyframe(0.5299998f, -0.1757144f), new Keyframe(0.5399998f, -0.2344009f), new Keyframe(0.5499998f, -0.292329f), new Keyframe(0.5599998f, -0.3493074f), new Keyframe(0.5699998f, -0.405145f), new Keyframe(0.5799997f, -0.4596515f), new Keyframe(0.5899997f, -0.512635f), new Keyframe(0.5999997f, -0.5639049f), new Keyframe(0.6099997f, -0.6132703f), new Keyframe(0.6199997f, -0.6605401f), new Keyframe(0.6299997f, -0.7055233f), new Keyframe(0.6399997f, -0.7480285f), new Keyframe(0.6499997f, -0.7878655f), new Keyframe(0.6599997f, -0.8248427f), new Keyframe(0.6699997f, -0.8587692f), new Keyframe(0.6799996f, -0.8894541f), new Keyframe(0.6899996f, -0.9167062f), new Keyframe(0.6999996f, -0.9403346f), new Keyframe(0.7099996f, -0.9601483f), new Keyframe(0.7199996f, -0.9759563f), new Keyframe(0.7299996f, -0.9875677f), new Keyframe(0.7399996f, -0.9947914f), new Keyframe(0.7499996f, -0.9974363f), new Keyframe(0.7599996f, -0.9954991f), new Keyframe(0.7699996f, -0.9892066f), new Keyframe(0.7799996f, -0.9787146f), new Keyframe(0.7899995f, -0.9641785f), new Keyframe(0.7999995f, -0.9457538f), new Keyframe(0.8099995f, -0.9235957f), new Keyframe(0.8199995f, -0.8978599f), new Keyframe(0.8299995f, -0.8687016f), new Keyframe(0.8399995f, -0.8362765f), new Keyframe(0.8499995f, -0.8007398f), new Keyframe(0.8599995f, -0.762247f), new Keyframe(0.8699995f, -0.7209537f), new Keyframe(0.8799995f, -0.6770151f), new Keyframe(0.8899994f, -0.6305867f), new Keyframe(0.8999994f, -0.5818241f), new Keyframe(0.9099994f, -0.5308825f), new Keyframe(0.9199994f, -0.4779174f), new Keyframe(0.9299994f, -0.4230843f), new Keyframe(0.9399994f, -0.3665386f), new Keyframe(0.9499994f, -0.3084358f), new Keyframe(0.9599994f, -0.2489312f), new Keyframe(0.9699994f, -0.1881803f), new Keyframe(0.9799994f, -0.1263387f), new Keyframe(0.9899994f, -0.0635615f), new Keyframe(0.9999993f, -4.351139E-06f)
            );

            PoseChangeCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0f), new Keyframe(0.01f, 0.007032989f), new Keyframe(0.02f, 0.02556881f), new Keyframe(0.03f, 0.05318547f), new Keyframe(0.04f, 0.08746101f), new Keyframe(0.05f, 0.1259734f), new Keyframe(0.05999999f, 0.1663008f), new Keyframe(0.06999999f, 0.2060211f), new Keyframe(0.07999999f, 0.2427123f), new Keyframe(0.08999999f, 0.2739525f), new Keyframe(0.09999999f, 0.2973197f), new Keyframe(0.11f, 0.310392f), new Keyframe(0.12f, 0.3106796f), new Keyframe(0.13f, 0.2966651f), new Keyframe(0.14f, 0.2694826f), new Keyframe(0.15f, 0.230537f), new Keyframe(0.16f, 0.1812333f), new Keyframe(0.17f, 0.1229763f), new Keyframe(0.18f, 0.05717105f), new Keyframe(0.19f, -0.0147776f), new Keyframe(0.2f, -0.09146473f), new Keyframe(0.21f, -0.1714854f), new Keyframe(0.22f, -0.2534347f), new Keyframe(0.23f, -0.3359077f), new Keyframe(0.24f, -0.4174995f), new Keyframe(0.25f, -0.4996343f), new Keyframe(0.26f, -0.593701f), new Keyframe(0.27f, -0.6929316f), new Keyframe(0.28f, -0.7877538f), new Keyframe(0.29f, -0.8685953f), new Keyframe(0.3f, -0.9258838f), new Keyframe(0.31f, -0.9537674f), new Keyframe(0.32f, -0.970719f), new Keyframe(0.33f, -0.981176f), new Keyframe(0.3399999f, -0.9856007f), new Keyframe(0.3499999f, -0.9844553f), new Keyframe(0.3599999f, -0.9782023f), new Keyframe(0.3699999f, -0.9673037f), new Keyframe(0.3799999f, -0.9522219f), new Keyframe(0.3899999f, -0.9334192f), new Keyframe(0.3999999f, -0.9113579f), new Keyframe(0.4099999f, -0.8865002f), new Keyframe(0.4199999f, -0.8593085f), new Keyframe(0.4299999f, -0.8302449f), new Keyframe(0.4399998f, -0.7992658f), new Keyframe(0.4499998f, -0.7661572f), new Keyframe(0.4599998f, -0.7311853f), new Keyframe(0.4699998f, -0.6946167f), new Keyframe(0.4799998f, -0.656718f), new Keyframe(0.4899998f, -0.6177557f), new Keyframe(0.4999998f, -0.5779963f), new Keyframe(0.5099998f, -0.5377064f), new Keyframe(0.5199998f, -0.4971525f), new Keyframe(0.5299998f, -0.4566014f), new Keyframe(0.5399998f, -0.4163193f), new Keyframe(0.5499998f, -0.3765729f), new Keyframe(0.5599998f, -0.3376288f), new Keyframe(0.5699998f, -0.2997535f), new Keyframe(0.5799997f, -0.2632135f), new Keyframe(0.5899997f, -0.2282754f), new Keyframe(0.5999997f, -0.1952056f), new Keyframe(0.6099997f, -0.1642709f), new Keyframe(0.6199997f, -0.1357378f), new Keyframe(0.6299997f, -0.1098726f), new Keyframe(0.6399997f, -0.08694214f), new Keyframe(0.6499997f, -0.06721276f), new Keyframe(0.6599997f, -0.05095112f), new Keyframe(0.6699997f, -0.03842378f), new Keyframe(0.6799996f, -0.02923922f), new Keyframe(0.6899996f, -0.02112969f), new Keyframe(0.6999996f, -0.01384568f), new Keyframe(0.7099996f, -0.007351071f), new Keyframe(0.7199996f, -0.00160972f), new Keyframe(0.7299996f, 0.003414486f), new Keyframe(0.7399996f, 0.007757682f), new Keyframe(0.7499996f, 0.011456f), new Keyframe(0.7599996f, 0.01454556f), new Keyframe(0.7699996f, 0.01706249f), new Keyframe(0.7799996f, 0.01904292f), new Keyframe(0.7899995f, 0.02052297f), new Keyframe(0.7999995f, 0.02153878f), new Keyframe(0.8099995f, 0.02212648f), new Keyframe(0.8199995f, 0.02232218f), new Keyframe(0.8299995f, 0.02216202f), new Keyframe(0.8399995f, 0.02168213f), new Keyframe(0.8499995f, 0.02091862f), new Keyframe(0.8599995f, 0.01990765f), new Keyframe(0.8699995f, 0.0186853f), new Keyframe(0.8799995f, 0.01728775f), new Keyframe(0.8899994f, 0.0157511f), new Keyframe(0.8999994f, 0.01411147f), new Keyframe(0.9099994f, 0.01240502f), new Keyframe(0.9199994f, 0.01066783f), new Keyframe(0.9299994f, 0.008936077f), new Keyframe(0.9399994f, 0.007245827f), new Keyframe(0.9499994f, 0.005633298f), new Keyframe(0.9599994f, 0.004134513f), new Keyframe(0.9699994f, 0.002785686f), new Keyframe(0.9799994f, 0.001622912f), new Keyframe(0.9899994f, 0.000682313f), new Keyframe(0.9999993f, 2.235174E-08f)
            );

            TryLoadPatch(new LerpCameraPatch_SwayAndModifiedHandPos());
            TryLoadPatch(new UpdateSwayFactorsPatch());
            TryLoadPatch(new SetHeadRotationPatch_ApplyDeadzone());
            TryLoadPatch(new LateUpdatePatch_UpdateWpnStats());
            TryLoadPatch(new OnShotPatch_UpdateWpnWeight());
            TryLoadPatch(new CalculateCameraPositionPatch());
        }

        void Update()
        {
            WeaponHandlingController.DeltaTime = DeltaTime;
            WeaponHandlingController.Time = Time;
            Utils.PumpUtilsUpdate(UnityEngine.Time.deltaTime);
            HandsMovController.UpdateLerp(DeltaTime);

            DeltaTime += (UnityEngine.Time.unscaledDeltaTime - DeltaTime);
            Time += UnityEngine.Time.unscaledDeltaTime;
        }

        void LateUpdate()
        {
            WeaponHandlingController.SwayUpdatedThisFrame = false;
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
    }
}
