using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ManagedDoom.Unity
{
    public class UnityContext
    {
        public Transform Root;
        public Renderer Renderer;
        public Texture2D Texture;
        public Material Material;
        public List<AudioClip> SoundClips;
        public List<AudioSource> SoundSources;
        public List<AudioClip> MusicClips;
        public List<AudioSource> MusicSources;
        public bool AllowInput;
    }
}
