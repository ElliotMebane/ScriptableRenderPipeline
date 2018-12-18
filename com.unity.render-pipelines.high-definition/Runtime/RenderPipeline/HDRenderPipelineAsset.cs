using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // The HDRenderPipeline assumes linear lighting. Doesn't work with gamma.
    public partial class HDRenderPipelineAsset : RenderPipelineAsset
    {


        HDRenderPipelineAsset()
        {
        }

        protected override UnityEngine.Rendering.RenderPipeline CreatePipeline()
        {
            return new HDRenderPipeline(this);
        }

        [SerializeField]
        RenderPipelineResources m_RenderPipelineResources;

        public RenderPipelineResources renderPipelineResources
        {
            get { return m_RenderPipelineResources; }
            set { m_RenderPipelineResources = value; }
        }

#if UNITY_EDITOR
        HDRenderPipelineEditorResources m_RenderPipelineEditorResources;


        public HDRenderPipelineEditorResources renderPipelineEditorResources
        {
            get
            {
                //there is no clean way to load editor resources without having it serialized
                // - impossible to load them at deserialization
                // - constructor only called at asset creation
                // - cannot rely on OnEnable
                //thus fallback with lazy init for them
                if (m_RenderPipelineEditorResources == null || m_RenderPipelineEditorResources.Equals(null))
                    m_RenderPipelineEditorResources = UnityEditor.AssetDatabase.LoadAssetAtPath<HDRenderPipelineEditorResources>(HDUtils.GetHDRenderPipelinePath() + "Editor/RenderPipelineResources/HDRenderPipelineEditorResources.asset");
                return m_RenderPipelineEditorResources;
            }
            set { m_RenderPipelineEditorResources = value; }
        }
#endif

        // To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
        // we create a runtime copy (m_ActiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)
        [SerializeField]
        FrameSettings m_RenderingPathDefaultCameraFrameSettings = FrameSettings.defaultCamera;

        [SerializeField]
        FrameSettings m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings = FrameSettings.defaultCustomOrBakeReflectionProbe;

        [SerializeField]
        FrameSettings m_RenderingPathDefaultRealtimeReflectionFrameSettings = FrameSettings.defaultRealtimeReflectionProbe;

        public ReflectionSystemParameters reflectionSystemParameters
        {
            get
            {
                return new ReflectionSystemParameters
                {
                    maxPlanarReflectionProbePerCamera = renderPipelineSettings.lightLoopSettings.planarReflectionProbeCacheSize,
                    maxActivePlanarReflectionProbe = 512,
                    planarReflectionProbeSize = (int)renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize,
                    maxActiveReflectionProbe = 512,
                    reflectionProbeSize = (int)renderPipelineSettings.lightLoopSettings.reflectionCubemapSize
                };
            }
        }

        // Store the various RenderPipelineSettings for each platform (for now only one)
        public RenderPipelineSettings renderPipelineSettings = new RenderPipelineSettings();

        // Return the current use RenderPipelineSettings (i.e for the current platform)
        public RenderPipelineSettings GetRenderPipelineSettings()
        {
            return renderPipelineSettings;
        }

        public bool allowShaderVariantStripping = true;
        public bool enableSRPBatcher = false;
        public bool enableVariantStrippingLog = false;

        [SerializeField]
        public DiffusionProfileSettings diffusionProfileSettings;


        // HDRP use GetRenderingLayerMaskNames to create its light linking system
        // Mean here we define our name for light linking.
        [System.NonSerialized]
        string[] m_RenderingLayerNames = null;
        string[] renderingLayerNames
        {
            get
            {
                if (m_RenderingLayerNames == null)
                {
                    m_RenderingLayerNames = new string[32];

                    // By design we can't touch this one, but we can rename it
                    m_RenderingLayerNames[0] = "Light Layer default";

                    // We only support up to 7 layer + default.
                    for (int i = 1; i < 8; ++i)
                    {
                        m_RenderingLayerNames[i] = string.Format("Light Layer {0}", i);
                    }

                    // Unused
                    for (int i = 8; i < m_RenderingLayerNames.Length; ++i)
                    {
                        m_RenderingLayerNames[i] = string.Format("Unused {0}", i);
                    }
                }

                return m_RenderingLayerNames;
            }
        }

        public override string[] renderingLayerMaskNames
        {
            get
            {
                return renderingLayerNames;
            }
        }

        public override Shader defaultShader
        {
            get
            {
                return m_RenderPipelineResources.shaders.defaultPS;
            }
        }

#if UNITY_EDITOR
        public override Material defaultMaterial
        {
            get
            {
                return renderPipelineEditorResources == null ? null : renderPipelineEditorResources.materials.defaultDiffuseMat;
            }
        }

        // call to GetAutodeskInteractiveShaderXXX are only from within editor
        public override Shader autodeskInteractiveShader
        {
            get
            {
                return renderPipelineEditorResources == null ? null : renderPipelineEditorResources.shaderGraphs.autodeskInteractive;
            }
        }

        public override Shader autodeskInteractiveTransparentShader
        {
            get
            {
                return renderPipelineEditorResources == null ? null : renderPipelineEditorResources.shaderGraphs.autodeskInteractiveTransparent;
            }
        }

        public override Shader autodeskInteractiveMaskedShader
        {
            get
            {
                return renderPipelineEditorResources == null ? null : renderPipelineEditorResources.shaderGraphs.autodeskInteractiveMasked;
            }
        }

        // Note: This function is HD specific
        public Material GetDefaultDecalMaterial()
        {
            return renderPipelineEditorResources == null ? null : renderPipelineEditorResources.materials.defaultDecalMat;
        }

        // Note: This function is HD specific
        public Material GetDefaultMirrorMaterial()
        {
            return renderPipelineEditorResources == null ? null : renderPipelineEditorResources.materials.defaultMirrorMat;
        }

        public override Material defaultTerrainMaterial
        {
            get
            {
                return renderPipelineEditorResources == null ? null : renderPipelineEditorResources.materials.defaultTerrainMat;
            }
        }

        // Array structure that allow us to manipulate the set of defines that the HD render pipeline needs
        List<string> defineArray = new List<string>();

        bool UpdateDefineList(bool flagValue, string defineMacroValue)
        {
            bool macroExists = defineArray.Contains(defineMacroValue);
            if (flagValue)
            {
                if (!macroExists)
                {
                    defineArray.Add(defineMacroValue);
                    return true;
                }
            }
            else
            {
                if (macroExists)
                {
                    defineArray.Remove(defineMacroValue);
                    return true;
                }
            }
            return false;
        }

        // This function allows us to raise or remove some preprocessing defines based on the render pipeline settings
        public void EvaluateSettings()
        {
#if REALTIME_RAYTRACING_SUPPORT
            // Grab the current set of defines and split them
            string currentDefineList = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Standalone);
            defineArray.Clear();
            defineArray.AddRange(currentDefineList.Split(';'));

            // Update all the individual defines
            bool needUpdate = false;
            needUpdate |= UpdateDefineList(renderPipelineSettings.supportRayTracing, "ENABLE_RAYTRACING");

            // Only set if it changed
            if(needUpdate)
            {
                UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Standalone, string.Join(";", defineArray.ToArray()));
            }
#endif
        }
#endif
    }
}
