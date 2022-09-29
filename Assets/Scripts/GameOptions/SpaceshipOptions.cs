using GameOptionsUtility;
using GameplayIngredients;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class SpaceshipOptions : GameOption
{
    public class Preferences
    {
        public const string prefix = GameOptions.Preferences.prefix + "Spaceship.";
        public const string screenPercentage = prefix + "ScreenPercentage";
        public const string keyboardScheme = prefix + "FPSKeyboardScheme";
        public const string upsamplingMethod = prefix + "UpsamplingMethod";
    }

    public enum FPSKeyboardScheme
    {
        WASD = 0,
        IJKL = 1,
        ZQSD = 2
    }

    public enum UpsamplingMethod
    {
        CatmullRom = 0,
        CAS = 1,
        TAAU = 2,
        EASU_FSR = 3,
        DLSS = 4,
    }

    public FPSKeyboardScheme fpsKeyboardScheme
    {
        get => (FPSKeyboardScheme)PlayerPrefs.GetInt(Preferences.keyboardScheme, (int)FPSKeyboardScheme.WASD);
        set => PlayerPrefs.SetInt(Preferences.keyboardScheme, (int)value);
    }

    public UpsamplingMethod upsamplingMethod
    {
        get => (UpsamplingMethod)PlayerPrefs.GetInt(Preferences.upsamplingMethod, (int)UpsamplingMethod.EASU_FSR);
        set => PlayerPrefs.SetInt(Preferences.upsamplingMethod, (int)value);
    }

    public int screenPercentage
    {
        get 
        { 
            if(m_ScreenPercentage == -1) 
                m_ScreenPercentage = PlayerPrefs.GetInt(Preferences.screenPercentage, 100);

            return m_ScreenPercentage;
        }
        set 
        {
            m_ScreenPercentage = value;
            PlayerPrefs.SetInt(Preferences.screenPercentage, m_ScreenPercentage); 
        }
    }

    int m_ScreenPercentage = -1;
    bool init = false;
    public override void Apply()
    {
        if(!init)
        {
            DynamicResolutionHandler.SetDynamicResScaler(SetDynamicResolutionScale, DynamicResScalePolicyType.ReturnsPercentage);

            var gpuScore = GetGPU3DMark();
            UpdateRenderTargetResolution(gpuScore);
            UpdateQualityLevel(gpuScore);

            init = true;
        }

        UpdateUpscalingMethod();
        UpdateFPSControlScheme();

        Debug.LogFormat("Applying Graphics Options");

        var graphicsOption = GameOption.Get<GraphicOption>();
        if (graphicsOption != null)
        {
            graphicsOption.Apply();
        }

        Debug.LogFormat("GraphicsOption.targetFramerate = {0}", graphicsOption.targetFrameRate);
        Debug.LogFormat("Application.targetFrameRate = {0}", Application.targetFrameRate);

        Debug.LogFormat("GraphicsOption.vSync = {0}", graphicsOption.vSync);
        Debug.LogFormat("QualitySettings.vSyncCount = {0}", QualitySettings.vSyncCount);

    }

    public FPSKeys fpsKeys { get; private set; }
    public class FPSKeys
    {
        public readonly KeyCode forward;
        public readonly KeyCode back;
        public readonly KeyCode left;
        public readonly KeyCode right;
        public FPSKeys(KeyCode forward, KeyCode left, KeyCode back, KeyCode right)
        {
            this.forward = forward;
            this.back = back;
            this.left = left;
            this.right = right;
        }
    }

    void UpdateFPSControlScheme()
    {
        switch (fpsKeyboardScheme)
        {
            default:
            case FPSKeyboardScheme.WASD:
                fpsKeys = new FPSKeys(KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D);
                break;
            case FPSKeyboardScheme.IJKL:
                fpsKeys = new FPSKeys(KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L);
                break;
            case FPSKeyboardScheme.ZQSD:
                fpsKeys = new FPSKeys(KeyCode.Z, KeyCode.Q, KeyCode.S, KeyCode.D);
                break;
        }
    }

    void UpdateUpscalingMethod()
    {
        var vcm = Manager.Get<VirtualCameraManager>();
        var camera = vcm.GetComponent<Camera>();
        var hdCamera = vcm.GetComponent<HDAdditionalCameraData>();

        if(upsamplingMethod >= UpsamplingMethod.DLSS)
        {
            hdCamera.allowDeepLearningSuperSampling = true;
            hdCamera.deepLearningSuperSamplingUseCustomQualitySettings = true;
            hdCamera.deepLearningSuperSamplingQuality = 0;
            hdCamera.deepLearningSuperSamplingUseCustomAttributes = true;
            hdCamera.deepLearningSuperSamplingUseOptimalSettings = false;
            hdCamera.deepLearningSuperSamplingSharpening = 0.5f;
        }
        else
        {
            hdCamera.allowDeepLearningSuperSampling = false;

            switch (upsamplingMethod)
            {
                case UpsamplingMethod.CatmullRom:
                    DynamicResolutionHandler.SetUpscaleFilter(camera, DynamicResUpscaleFilter.CatmullRom);
                    break;
                case UpsamplingMethod.CAS:
                    DynamicResolutionHandler.SetUpscaleFilter(camera, DynamicResUpscaleFilter.ContrastAdaptiveSharpen);
                    break;
                case UpsamplingMethod.TAAU:
                    DynamicResolutionHandler.SetUpscaleFilter(camera, DynamicResUpscaleFilter.TAAU);
                    break;
                case UpsamplingMethod.EASU_FSR:
                    DynamicResolutionHandler.SetUpscaleFilter(camera, DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres);
                    break;
                default:
                    throw new System.NotImplementedException("Should not happen");
            }
        }
    }

    internal class AutoQuality
    {
        internal int level;
        internal int score;
    }

    void UpdateQualityLevel(int gpuScore)
    {
        var graphicsOption = GameOption.Get<GraphicOption>();
        if (graphicsOption == null)
        {
            return;
        }

        List<AutoQuality> autoQuality = new List<AutoQuality>();

        // It is important these are added in ascending score order
        autoQuality.Add(new AutoQuality { level = 0, score = 4000 });
        autoQuality.Add(new AutoQuality { level = 1, score = 10000 });
        autoQuality.Add(new AutoQuality { level = 2, score = 14000 });

        // Auto select the best known resolution for a given GPU score, if it exists.
        if (gpuScore > 0)
        {
            int scoredIndex = 0;
            int selQuality = 0;
            foreach (var quality in autoQuality)
            {
                selQuality = autoQuality[scoredIndex].level;
                if (quality.score < gpuScore)
                    scoredIndex++;
                else break;
            }
            
            Debug.LogFormat("Auto selected quality level: {1} ({0})", QualitySettings.names[selQuality], selQuality);

            graphicsOption.quality = selQuality;
        }
    }
    internal class AutoResolution
    {
        internal int width;
        internal int height;
        internal int score;

        internal int GetPixles()
        {
            return width * height;
        }
    }

    void UpdateRenderTargetResolution(int gpuScore)
    {
        var graphicsOption = GameOption.Get<GraphicOption>();
        if (graphicsOption == null)
        {
            return;
        }

        List<AutoResolution> autoRes = new List<AutoResolution>();

        // It is important these are added in ascending score order
        autoRes.Add(new AutoResolution { width = 1280, height = 720, score = 2000 });
        autoRes.Add(new AutoResolution { width = 1600, height = 900, score = 8000 });
        autoRes.Add(new AutoResolution { width = 1920, height = 1080, score = 14000 });
        autoRes.Add(new AutoResolution { width = 2560, height = 1440, score = 18000 });
        autoRes.Add(new AutoResolution { width = 3840, height = 2160, score = 24000 });

        // Auto select the best known resolution for a given GPU score, if it exists.
        if (gpuScore > 0)
        {
            int scoredIndex = 0;
            Resolution selRes = new Resolution();
            foreach (var scoredRed in autoRes)
            {
                selRes.width = autoRes[scoredIndex].width;
                selRes.height = autoRes[scoredIndex].height;
                if (scoredRed.score < gpuScore)
                {
                    scoredIndex++;
                }
                else break;
            }

            Debug.LogFormat("Auto selected resolution: {0}x{1}", selRes.width, selRes.height);

            // Pick the closest available
            var selPixels = selRes.width * selRes.height;
            foreach (var res in Screen.resolutions.OrderBy(o => o.width))
            {
                var resPixels = res.width * res.height;
                if (resPixels <= selPixels)
                {
                    graphicsOption.width = res.width;
                    graphicsOption.height = res.height;
                }
            }

            Debug.LogFormat("Best matched resolution: {0}x{1}", graphicsOption.width, graphicsOption.height);
        }
    }

    float SetDynamicResolutionScale()
    {
        return screenPercentage;
    }

    private int m_GPUScore = -1;
    public int GetGPU3DMark()
    {
        string gpuName = SystemInfo.graphicsDeviceName;
        if (m_GPUScore < 0)
        {
            if (gpuName.Contains("3DMARK-"))
            {
                int dashIndex = gpuName.IndexOf('-') + 1;
                int closeRoundIndex = gpuName.IndexOf(')', dashIndex);
                string scoreString = gpuName.Substring(dashIndex, closeRoundIndex - dashIndex);
                m_GPUScore = int.Parse(scoreString);
            }
            else
            {
                m_GPUScore = 0;
            }
        }

        Debug.LogFormat("GPU: {0} - {1}", gpuName, m_GPUScore);

        return m_GPUScore;
    }
}
