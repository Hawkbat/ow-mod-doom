using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ManagedDoom.Unity
{
    public sealed class UnityDoom : IDisposable
    {
        private UnityContext unityContext;

        private Config config;

        private GameContent content;

        private UnityVideo video;
        private UnitySound sound;
        private UnityMusic music;
        private UnityUserInput userInput;

        private Doom doom;

        private float updateTime;

        public bool AllowInput {
            get => unityContext.AllowInput;
            set => unityContext.AllowInput = value;
        }

        public bool Visible
        {
            get => unityContext.Renderer.enabled;
            set => unityContext.Renderer.enabled = value;
        }

        public int Volume {
            get => sound.Volume;
            set {
                sound.Volume = value;
                music.Volume = value;
            }
        }

        public UnityDoom(CommandLineArgs args, Transform parent)
        {
            try
            {
                config = new Config(ConfigUtilities.GetConfigPath());

                if (!config.IsRestoredFromFile)
                {
                    config.video_screenwidth = 320;
                    config.video_screenheight = 200;
                }

                config.video_screenwidth = Mathf.Clamp(config.video_screenwidth, 320, 3200);
                config.video_screenheight = Mathf.Clamp(config.video_screenheight, 200, 2000);

                var go = new GameObject("Doom");
                go.transform.SetParent(parent, false);
                go.layer = parent.gameObject.layer;

                unityContext = new UnityContext();
                unityContext.Root = go.transform;

                content = new GameContent(args);

                video = new UnityVideo(config, content, unityContext);

                if (!args.nosound.Present && !args.nosfx.Present)
                {
                    sound = new UnitySound(config, content.Wad, unityContext);
                }

                if (!args.nosound.Present && !args.nomusic.Present)
                {
                    var sfPath = Path.Combine(ConfigUtilities.GetExeDirectory(), config.audio_soundfont);
                    if (File.Exists(sfPath))
                    {
                        music = new UnityMusic(config, content.Wad, sfPath, unityContext);
                    }
                    else
                    {
                        Logger.Log("SoundFont '" + config.audio_soundfont + "' was not found!");
                    }
                }

                userInput = new UnityUserInput(config, !args.nomouse.Present, unityContext);

                doom = new Doom(args, config, content, video, sound, music, userInput);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public bool Run(float deltaTime)
        {
            var inputEvents = userInput.GenerateEvents();
            while (inputEvents.Count > 0) doom.PostEvent(inputEvents.Dequeue());

            float frameDuration = 1f / 35f;
            updateTime += deltaTime;
            if (updateTime > frameDuration)
            {
                updateTime %= frameDuration;

                if (doom.Update() == UpdateResult.Completed)
                {
                    return false;
                }
                video.Render(doom);
            }
            return true;
        }

        public void Dispose()
        {
            if (userInput != null)
            {
                userInput.Dispose();
                userInput = null;
            }

            if (music != null)
            {
                music.Dispose();
                music = null;
            }

            if (sound != null)
            {
                sound.Dispose();
                sound = null;
            }

            if (video != null)
            {
                video.Dispose();
                video = null;
            }

            if (content != null)
            {
                content.Dispose();
                content = null;
            }
        }

        public string QuitMessage => doom.QuitMessage;
    }
}