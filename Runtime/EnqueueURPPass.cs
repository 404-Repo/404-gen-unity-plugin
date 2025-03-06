using UnityEngine;
#if GS_ENABLE_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace GaussianSplatting.Runtime
{
    [ExecuteInEditMode]
    public class EnqueueURPPass : MonoBehaviour
    {
#if GS_ENABLE_URP
        GaussianSplatURPFeature.GSRenderPass m_Pass;
        private void OnEnable()
        {
            m_Pass = new GaussianSplatURPFeature.GSRenderPass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
            };
            // Subscribe the OnBeginCamera method to the beginCameraRendering event.
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        }

        private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
        {
            //pre cull
            var system = GaussianSplatRenderSystem.instance;
            if (!system.GatherSplatsForCamera(cam))
                return;
            
            #if !UNITY_6000_0_OR_NEWER
            CommandBuffer cmb = system.InitialClearCmdBuffer(cam);
            m_Pass.m_Cmb = cmb;
            #endif
			
            // Use the EnqueuePass method to inject a custom render pass
			var scriptableRenderer = cam.GetUniversalAdditionalCameraData().scriptableRenderer;
			#if !UNITY_6000_0_OR_NEWER
            m_Pass.m_Renderer = scriptableRenderer;
            #endif
            
            scriptableRenderer.EnqueuePass(m_Pass);
        }
#endif
    }
}
