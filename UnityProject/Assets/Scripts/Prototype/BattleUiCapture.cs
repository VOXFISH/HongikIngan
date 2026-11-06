using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace HongikIngan
{
    // Opt-in smoke capture. Hidden Windows player windows can have a black swap-chain buffer.
    // Render the actual scene canvas to a texture, then restore every camera/canvas setting.
    public static class BattleUiCapture
    {
        public static void Capture(Canvas canvas, string path)
        {
            var camera = Camera.main;
            if (camera == null) camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null || canvas == null) throw new InvalidOperationException("Capture camera/canvas missing.");
            var mode = canvas.renderMode; var worldCamera = canvas.worldCamera; float distance = canvas.planeDistance;
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D pixels = null;
            try
            {
                target.Create(); camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                // Rebuild every batch after changing render mode; cached overlay batches can be absent from a camera capture.
                foreach (var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>()) graphic.SetAllDirty();
                Canvas.ForceUpdateCanvases();
                if (RenderPipelineManager.currentPipeline != null)
                {
                    var request = new RenderPipeline.StandardRequest { destination = target };
                    if (!RenderPipeline.SupportsRenderRequest(camera, request)) throw new InvalidOperationException("Render request unsupported.");
                    RenderPipeline.SubmitRenderRequest(camera, request);
                }
                else camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply();
                var colors = pixels.GetPixels32(); bool varied = false;
                for (int i=0;i<colors.Length;i+=113)
                    if (!colors[i].Equals(colors[0])) { varied=true; break; }
                if (!varied) throw new InvalidOperationException("Scene capture was blank.");
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                canvas.renderMode = mode; canvas.worldCamera = worldCamera; canvas.planeDistance = distance;
                Canvas.ForceUpdateCanvases();
                target.Release(); UnityEngine.Object.Destroy(target);
                if (pixels != null) UnityEngine.Object.Destroy(pixels);
            }
        }
    }
}
