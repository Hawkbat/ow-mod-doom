using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ManagedDoom.Video;

namespace ManagedDoom.Unity
{
    public sealed class UnityVideo : IVideo, IDisposable
    {
        private UnityContext unityContext;

        private Video.Renderer renderer;

        private int textureWidth;
        private int textureHeight;

        private byte[] textureData;

        private Color32[] colorData;
        private MeshRenderer meshRenderer;
        private Texture2D texture;
        private Material material;

        public UnityVideo(Config config, GameContent content, UnityContext unityContext)
        {
            try
            {
                Logger.Log("Initialize video: ");

                renderer = new Video.Renderer(config, content);

                config.video_gamescreensize = Mathf.Clamp(config.video_gamescreensize, 0, MaxWindowSize);
                config.video_gammacorrection = Mathf.Clamp(config.video_gammacorrection, 0, MaxGammaCorrectionLevel);

                if (config.video_highresolution)
                {
                    textureWidth = 512;
                    textureHeight = 1024;
                }
                else
                {
                    textureWidth = 256;
                    textureHeight = 512;
                }

                this.unityContext = unityContext;

                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.transform.SetParent(unityContext.Root, false);
                go.name = "Doom_Video";
                go.transform.localEulerAngles = Vector3.forward * -90f;
                go.transform.localScale = new Vector3(3f / 4f, 1f, 1f);
                go.layer = unityContext.Root.gameObject.layer;
                UnityEngine.Object.Destroy(go.GetComponent<Collider>());
                meshRenderer = go.GetComponent<MeshRenderer>();

                textureData = new byte[4 * renderer.Width * renderer.Height];
                colorData = new Color32[renderer.Width * renderer.Height];

                texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);

                material = new Material(Shader.Find("Unlit/Texture"));
                material.name = "Doom_Video";
                material.mainTexture = texture;
                material.mainTextureScale = new Vector2(renderer.Height / (float)textureWidth, renderer.Width / (float)textureHeight);

                meshRenderer.sharedMaterial = material;

                unityContext.Texture = texture;
                unityContext.Material = material;
                unityContext.Renderer = meshRenderer;

                Logger.Log("OK");
            }
            catch
            {
                Logger.Log("Failed");
                Dispose();
                throw;
            }
        }

        public void Render(Doom doom)
        {
            renderer.Render(doom, textureData);
            for (int i = 0; i < colorData.Length; i++)
            {
                colorData[i].r = textureData[i * 4 + 0];
                colorData[i].g = textureData[i * 4 + 1];
                colorData[i].b = textureData[i * 4 + 2];
                colorData[i].a = textureData[i * 4 + 3];
            }
            texture.SetPixels32(0, 0, renderer.Height, renderer.Width, colorData);
            texture.Apply();
        }

        public void InitializeWipe()
        {
            renderer.InitializeWipe();
        }

        public bool HasFocus()
        {
            return Application.isFocused;
        }

        public void Dispose()
        {
            Logger.Log("Shutdown renderer.");

            if (texture != null)
            {
                unityContext.Texture = null;
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }

            if (material != null)
            {
                unityContext.Material = null;
                UnityEngine.Object.Destroy(material);
                material = null;
            }

            if (meshRenderer != null)
            {
                unityContext.Renderer = null;
                UnityEngine.Object.Destroy(meshRenderer.gameObject);
                UnityEngine.Object.Destroy(meshRenderer);
            }
        }

        public int WipeBandCount => renderer.WipeBandCount;
        public int WipeHeight => renderer.WipeHeight;

        public int MaxWindowSize => renderer.MaxWindowSize;

        public int WindowSize
        {
            get => renderer.WindowSize;
            set => renderer.WindowSize = value;
        }

        public bool DisplayMessage
        {
            get => renderer.DisplayMessage;
            set => renderer.DisplayMessage = value;
        }

        public int MaxGammaCorrectionLevel => renderer.MaxGammaCorrectionLevel;

        public int GammaCorrectionLevel
        {
            get => renderer.GammaCorrectionLevel;
            set => renderer.GammaCorrectionLevel = value;
        }
    }
}