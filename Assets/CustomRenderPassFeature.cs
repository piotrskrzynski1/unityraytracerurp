using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
using System;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private Material raytraceMaterial;
        private Material averageMaterial;
        private RenderTexture currRender;
        private RenderTexture prevRender;

        public bool Accumulate = true;
        public bool first = true;
        public int numRenderedFrames = 1;

        private int prevScreenWidth = 0;
        private int prevScreenHeight = 0;

        public CustomRenderPass(Material material, Material material2)
        {
            this.raytraceMaterial = material;
            this.averageMaterial = material2;


        }


        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor cam = renderingData.cameraData.cameraTargetDescriptor;
            currRender = RenderTexture.GetTemporary(cam.width, cam.height, 0, RenderTextureFormat.ARGBFloat);
            if (prevRender == null)
            {
                prevRender = new RenderTexture(cam.width, cam.height, 0, RenderTextureFormat.ARGBFloat);
                prevRender.enableRandomWrite = true;
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (currRender == null || prevRender == null)
            {
                Debug.LogError("Render targets not properly allocated!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("Ray Tracing Pass");


            // Set frame count for ray tracing material and average material
            raytraceMaterial.SetInt("NumRenderedFrames", numRenderedFrames);
            averageMaterial.SetInt("NumRenderedFrames", numRenderedFrames);

            RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            if (first)
            {
                cmd.Blit(source, source, raytraceMaterial);
                numRenderedFrames++;
                cmd.Blit(source, prevRender);

                first = false;

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                return;
            }


            if (Accumulate)
            {
                cmd.Blit(null, currRender, raytraceMaterial);
                numRenderedFrames++;

                raytraceMaterial.SetInt("NumRenderedFrames", numRenderedFrames);

                averageMaterial.SetInt("NumRenderedFrames", numRenderedFrames);
                averageMaterial.SetTexture("currRender", currRender);
                averageMaterial.SetTexture("prevRender", prevRender);

                cmd.Blit(currRender, source, averageMaterial);
                Blit(cmd, source, prevRender);
            }
            else
            {
                cmd.Blit(currRender, source);
                numRenderedFrames++;
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            currRender.Release();
        }
    }


    CustomRenderPass m_ScriptablePass;

    // The materials for the ray tracing and averaging
    public Material raytraceMaterial;
    public Material averageMaterial;
    public bool Accumulate = true;

    public override void Create()
    {
        // Initialize the custom render pass
        m_ScriptablePass = new CustomRenderPass(raytraceMaterial, averageMaterial)
        {
            Accumulate = Accumulate,
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Enqueue the custom render pass
        renderer.EnqueuePass(m_ScriptablePass);
    }
}