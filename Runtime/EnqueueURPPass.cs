﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GaussianSplatting.Runtime
{
    [ExecuteInEditMode]
    public class EnqueueURPPass : MonoBehaviour
    {
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

            CommandBuffer cmb = system.InitialClearCmdBuffer(cam);
            m_Pass.m_Cmb = cmb;
            
            // Use the EnqueuePass method to inject a custom render pass
            var scriptableRenderer = cam.GetUniversalAdditionalCameraData().scriptableRenderer;
            m_Pass.m_Renderer = scriptableRenderer;
            scriptableRenderer.EnqueuePass(m_Pass);
        }
        
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            m_Pass?.Dispose();
        }
    }
}