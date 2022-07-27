using System.ComponentModel;
using System.IO;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if HDRP_ACCUM_API
using UnityEngine.Rendering.HighDefinition;
#endif
using UnityObject = UnityEngine.Object;


namespace UnityEditor.Recorder
{
    /// <summary>
    /// An ad-hoc collection of helpers for the Recorders.
    /// </summary>
    public static class UnityHelpers
    {
        /// <summary>
        /// Allows destroying Unity.Objects.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="allowDestroyingAssets"></param>
        public static void Destroy(UnityObject obj, bool allowDestroyingAssets = false)
        {
            if (obj == null)
                return;

            if (EditorApplication.isPlaying)
                UnityObject.Destroy(obj);
            else
                UnityObject.DestroyImmediate(obj, allowDestroyingAssets);
        }

        internal static bool IsPlaying()
        {
            return EditorApplication.isPlaying;
        }

        internal static GameObject CreateRecorderGameObject(string name)
        {
            var gameObject = new GameObject(name) { tag = "EditorOnly" };
            SetGameObjectVisibility(gameObject, RecorderOptions.ShowRecorderGameObject);
            return gameObject;
        }

        internal static void SetGameObjectsVisibility(bool value)
        {
            var rcb = BindingManager.FindRecorderBindings();
            foreach (var rc in rcb)
            {
                SetGameObjectVisibility(rc.gameObject, value);
            }

            var rcs = Object.FindObjectsOfType<RecorderComponent>();
            foreach (var rc in rcs)
            {
                SetGameObjectVisibility(rc.gameObject, value);
            }
        }

        static void SetGameObjectVisibility(GameObject obj, bool visible)
        {
            if (obj != null)
            {
                obj.hideFlags = visible ? HideFlags.None : HideFlags.HideInHierarchy;

                if (!Application.isPlaying)
                {
                    try
                    {
                        EditorSceneManager.MarkSceneDirty(obj.scene);
                        EditorApplication.RepaintHierarchyWindow();
                        EditorApplication.DirtyHierarchyWindowSorting();
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        internal static bool AreAllSceneDataLoaded()
        {
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.isLoaded == false)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// A label including the version of the package, for use in encoder metadata tags for instance.
        /// </summary>
        internal static string PackageDescription
        {
            get
            {
                return "Recorder " + PackageVersion;
            }
        }

        private static ListRequest LsPackages = Client.List();
        private static string PackageVersion
        {
            get
            {
                if (m_PackageVersion.Length == 0)
                {
                    // Read the package version
                    var packageInfo = PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.recorder");
                    m_PackageVersion = packageInfo.version;
                }
                return m_PackageVersion;
            }
        }
        private static string m_PackageVersion = "";

        /// <summary>
        /// Convert an RGBA32 texture to an RGB24 one.
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        internal static Texture2D RGBA32_to_RGB24(Texture2D tex)
        {
            if (tex.format != TextureFormat.RGBA32)
                throw new System.Exception($"Expecting RGBA32 format, received {tex.format}");

            Texture2D newTex = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
            newTex.SetPixels(tex.GetPixels());
            newTex.Apply();

            return newTex;
        }

        /// <summary>
        /// Load an asset from the current package's Editor/Assets folder.
        /// </summary>
        /// <param name="relativeFilePathWithExtension">The relative filename inside the Editor/Assets folder, without
        /// leading slash.</param>
        /// <param name="logError">Set this flag to true if you need to log errors when the Recorder cannot find the asset.</param>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <returns></returns>
        internal static T LoadLocalPackageAsset<T>(string relativeFilePathWithExtension, bool logError) where T : Object
        {
            T result = default(T);
            var fullPathInProject = $"Packages/com.unity.recorder/Editor/Assets/{relativeFilePathWithExtension}";

            if (File.Exists(fullPathInProject))
                result = AssetDatabase.LoadAssetAtPath(fullPathInProject, typeof(T)) as T;
            else if (logError)
                Debug.LogError($"Local asset file {fullPathInProject} not found.");
            return result;
        }

        /// <summary>
        /// Are we currently using the High Definition Render Pipeline.
        /// </summary>
        /// <returns>bool</returns>
        internal static bool UsingHDRP()
        {
            var pipelineAsset = GraphicsSettings.renderPipelineAsset;
            var usingHDRP = pipelineAsset != null && pipelineAsset.GetType().FullName.Contains("High");
            return usingHDRP;
        }

        /// <summary>
        /// Are we currently using the Universal Render Pipeline.
        /// </summary>
        /// <returns>bool</returns>
        internal static bool UsingURP()
        {
            var pipelineAsset = GraphicsSettings.renderPipelineAsset;
            var usingURP = pipelineAsset != null &&
                (pipelineAsset.GetType().FullName.Contains("Universal") ||
                    pipelineAsset.GetType().FullName.Contains("Lightweight"));
            return usingURP;
        }

        /// <summary>
        /// Are we currently using the Legacy Render Pipeline.
        /// </summary>
        /// <returns>bool</returns>
        internal static bool UsingLegacyRP()
        {
            var pipelineAsset = GraphicsSettings.renderPipelineAsset;
            return pipelineAsset == null;
        }

        /// <summary>
        /// Are we currently capturing SubFrames.
        /// </summary>
        /// <returns>bool</returns>
        internal static bool CaptureAccumulation(RecorderSettings settings)
        {
#if HDRP_ACCUM_API
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null && settings.IsAccumulationSupported())
            {
                IAccumulation accumulation = settings as IAccumulation;
                if (accumulation != null)
                {
                    AccumulationSettings aSettings = accumulation.GetAccumulationSettings();
                    if (aSettings != null)
                        return aSettings.CaptureAccumulation;
                }
            }
#endif
            return false;
        }

        /// <summary>
        /// Returns the color space of the specified graphics format.
        /// </summary>
        /// <param name="format">The graphics format to probe.</param>
        /// <returns></returns>
        internal static ImageRecorderSettings.ColorSpaceType GetColorSpaceType(GraphicsFormat format)
        {
            // All sRGB formats end with "_SRGB"?
            return format.ToString().EndsWith("_SRGB") ? ImageRecorderSettings.ColorSpaceType.sRGB_sRGB : ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB;
        }

        /// <summary>
        /// Returns the Recorder-specific color space matching the Unity color space.
        /// </summary>
        /// <param name="space">The Unity color space to probe</param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException">Throws an exception if the enum value is not as expected.</exception>
        internal static ImageRecorderSettings.ColorSpaceType GetColorSpaceType(ColorSpace space)
        {
            switch (space)
            {
                case ColorSpace.Gamma:
                    return ImageRecorderSettings.ColorSpaceType.sRGB_sRGB;
                case ColorSpace.Linear:
                    return ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB;
                default:
                    throw new InvalidEnumArgumentException($"Unexpected color space '{space}'");
            }
        }

        // Whether or not to perform a manual vertical flip, based on the user's intention as well as the characteristics
        // of the current graphics API (OpenGL is vflipped compared to Metal & D3D) and capture source.
        // wantFlippedTexture: If true, the user expects a vertically flipped texture.
        // calledFrom: The object that calls this method.
        internal static bool NeedToActuallyFlip(bool wantFlippedTexture, BaseRenderTextureInput calledFrom,
            bool encoderAlreadyFlips)
        {
            // We need to take several things into account: what the user expects, whether or not the rendering is made
            // on a GameView source, and whether or not the hardware is OpenGL.
            bool isGameView = calledFrom is GameViewInput; // game view is already flipped
            bool isCameraInputLegacyRP = calledFrom is CameraInput && UsingLegacyRP(); // legacy RP has vflipped camera input

            // OpenGL causes a flipped image except if:
            // * source is 360 camera
            // * source is RenderTextureInput
            // * source is RenderTextureSampler
            // * source is CameraInput in a URP project
            bool isFlippedBecauseOfOpenGL = !SystemInfo.graphicsUVStartsAtTop &&
                !(calledFrom is Camera360Input || calledFrom is RenderTextureInput
                    || calledFrom is RenderTextureSampler
                    || (calledFrom is CameraInput && UsingURP()));
            bool willBeFlipped = isGameView ^ encoderAlreadyFlips ^ isCameraInputLegacyRP ^ isFlippedBecauseOfOpenGL;

            return willBeFlipped != wantFlippedTexture;
        }
    }
}
