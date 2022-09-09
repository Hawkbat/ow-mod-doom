using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ManagedDoom.Audio;
using MeltySynth;

namespace ManagedDoom.Unity
{
    public sealed class UnityMusic : IMusic, IDisposable
    {
        private UnityContext unityContext;

        private Config config;
        private Wad wad;

        private MusStream stream;
        private Bgm current;

        public UnityMusic(Config config, Wad wad, string sfPath, UnityContext unityContext)
        {
            try
            {
                Logger.Log("Initialize music: ");

                this.config = config;
                this.wad = wad;
                this.unityContext = unityContext;

                unityContext.MusicClips = new List<AudioClip>();
                unityContext.MusicSources = new List<AudioSource>();

                stream = new MusStream(this, config, sfPath);
                current = Bgm.NONE;

                Logger.Log("OK");
            }
            catch
            {
                Logger.Log("Failed");
                Dispose();
                throw;
            }
        }

        public void StartMusic(Bgm bgm, bool loop)
        {
            if (bgm == current)
            {
                return;
            }

            var lump = "D_" + DoomInfo.BgmNames[(int)bgm].ToString().ToUpper();
            var data = wad.ReadLump(lump);
            var decoder = ReadData(data, loop);
            stream.SetDecoder(decoder);

            current = bgm;
        }

        private IDecoder ReadData(byte[] data, bool loop)
        {
            var isMus = true;
            for (var i = 0; i < MusDecoder.MusHeader.Length; i++)
            {
                if (data[i] != MusDecoder.MusHeader[i])
                {
                    isMus = false;
                }
            }

            if (isMus)
            {
                return new MusDecoder(data, loop);
            }

            var isMidi = true;
            for (var i = 0; i < MidiDecoder.MidiHeader.Length; i++)
            {
                if (data[i] != MidiDecoder.MidiHeader[i])
                {
                    isMidi = false;
                }
            }

            if (isMidi)
            {
                return new MidiDecoder(data, loop);
            }

            throw new Exception("Unknown format!");
        }

        public void Dispose()
        {
            Logger.Log("Shutdown music.");

            if (stream != null)
            {
                stream.Stop();
                stream.Dispose();
                stream = null;
            }

            unityContext.MusicClips = null;
            unityContext.MusicSources = null;
        }

        public int MaxVolume
        {
            get
            {
                return 15;
            }
        }

        public int Volume
        {
            get
            {
                return config.audio_musicvolume;
            }

            set
            {
                config.audio_musicvolume = value;
            }
        }

        private class MusStream : IDisposable
        {
            public SoundStatus Status
            {
                get {
                    if (leftAudioSource == null) return SoundStatus.Stopped;
                    if (leftAudioSource.isPlaying) return SoundStatus.Playing;
                    if (leftAudioSource.time > 0f) return SoundStatus.Paused;
                    return SoundStatus.Stopped;
                }
            }

            private UnityMusic parent;
            private Config config;

            private Synthesizer synthesizer;

            private IDecoder current;
            private IDecoder reserved;

            private AudioClip audioClip;
            private AudioSource leftAudioSource;
            private AudioSource rightAudioSource;

            public MusStream(UnityMusic parent, Config config, string sfPath)
            {
                this.parent = parent;
                this.config = config;

                config.audio_musicvolume = Mathf.Clamp(config.audio_musicvolume, 0, parent.MaxVolume);

                var settings = new SynthesizerSettings(MusDecoder.SampleRate);
                settings.BlockSize = MusDecoder.BlockLength;
                settings.EnableReverbAndChorus = config.audio_musiceffect;
                synthesizer = new Synthesizer(sfPath, settings);
            }

            public void SetDecoder(IDecoder decoder)
            {
                reserved = decoder;

                if (Status == SoundStatus.Stopped)
                {
                    Play();
                }
            }

            protected void OnGetData(float[] samples)
            {
                if (reserved != current)
                {
                    synthesizer.Reset();
                    current = reserved;
                }

                var a = 2.0F * config.audio_musicvolume / parent.MaxVolume;

                float[] left = new float[samples.Length / 2];
                float[] right = new float[samples.Length / 2];

                current.RenderWaveform(synthesizer, left, right);

                var pos = 0;

                for (var t = 0; t < left.Length; t++)
                {
                    var sampleLeft = Mathf.Clamp(a * left[t], -1f, 1f);
                    var sampleRight = Mathf.Clamp(a * right[t], -1f, 1f);
                    samples[pos++] = sampleLeft;
                    samples[pos++] = sampleRight;
                }
            }

            public void Play()
            {
                if (!audioClip)
                {
                    audioClip = AudioClip.Create("Doom_Music", MusDecoder.SampleRate, 2, MusDecoder.SampleRate, true, OnGetData);
                    parent.unityContext.MusicClips.Add(audioClip);
                }
                if (!leftAudioSource)
                {
                    leftAudioSource = new GameObject().AddComponent<AudioSource>();
                    leftAudioSource.gameObject.name = "Doom_Music_L";
                    leftAudioSource.gameObject.layer = parent.unityContext.Root.gameObject.layer;
                    leftAudioSource.transform.SetParent(parent.unityContext.Root, false);
                    leftAudioSource.transform.localPosition = Vector3.left;
                    leftAudioSource.clip = audioClip;
                    leftAudioSource.loop = true;
                    leftAudioSource.spatialBlend = 1f;
                    leftAudioSource.panStereo = -1f;
                    parent.unityContext.MusicSources.Add(leftAudioSource);
                }
                if (!rightAudioSource)
                {
                    rightAudioSource = new GameObject().AddComponent<AudioSource>();
                    rightAudioSource.gameObject.name = "Doom_Music_R";
                    rightAudioSource.gameObject.layer = parent.unityContext.Root.gameObject.layer;
                    rightAudioSource.transform.SetParent(parent.unityContext.Root, false);
                    rightAudioSource.transform.localPosition = Vector3.right;
                    rightAudioSource.clip = audioClip;
                    rightAudioSource.loop = true;
                    rightAudioSource.spatialBlend = 1f;
                    rightAudioSource.panStereo = 1f;
                    parent.unityContext.MusicSources.Add(rightAudioSource);
                }
                leftAudioSource.Stop();
                rightAudioSource.Stop();
                leftAudioSource.Play();
                rightAudioSource.Play();
            }

            public void Stop()
            {
                leftAudioSource.Stop();
                rightAudioSource.Stop();
            }

            public void Dispose()
            {
                parent.unityContext.MusicClips.Remove(audioClip);
                UnityEngine.Object.Destroy(audioClip);
                audioClip = null;
                parent.unityContext.MusicSources.Remove(leftAudioSource);
                UnityEngine.Object.Destroy(leftAudioSource.gameObject);
                UnityEngine.Object.Destroy(leftAudioSource);
                leftAudioSource = null;
                parent.unityContext.MusicSources.Remove(rightAudioSource);
                UnityEngine.Object.Destroy(rightAudioSource.gameObject);
                UnityEngine.Object.Destroy(rightAudioSource);
                rightAudioSource = null;
            }
        }



        private interface IDecoder
        {
            void RenderWaveform(Synthesizer synthesizer, Span<float> left, Span<float> right);
        }



        private class MusDecoder : IDecoder
        {
            public static readonly int SampleRate = 44100;
            public static readonly int BlockLength = SampleRate / 140;

            public static readonly byte[] MusHeader = new byte[]
            {
                (byte)'M',
                (byte)'U',
                (byte)'S',
                0x1A
            };

            private byte[] data;
            private bool loop;

            private int scoreLength;
            private int scoreStart;
            private int channelCount;
            private int channelCount2;
            private int instrumentCount;
            private int[] instruments;

            private MusEvent[] events;
            private int eventCount;

            private int[] lastVolume;
            private int p;
            private int delay;

            private int blockWrote;

            public MusDecoder(byte[] data, bool loop)
            {
                CheckHeader(data);

                this.data = data;
                this.loop = loop;

                scoreLength = BitConverter.ToUInt16(data, 4);
                scoreStart = BitConverter.ToUInt16(data, 6);
                channelCount = BitConverter.ToUInt16(data, 8);
                channelCount2 = BitConverter.ToUInt16(data, 10);
                instrumentCount = BitConverter.ToUInt16(data, 12);
                instruments = new int[instrumentCount];
                for (var i = 0; i < instruments.Length; i++)
                {
                    instruments[i] = BitConverter.ToUInt16(data, 16 + 2 * i);
                }

                events = new MusEvent[128];
                for (var i = 0; i < events.Length; i++)
                {
                    events[i] = new MusEvent();
                }
                eventCount = 0;

                lastVolume = new int[16];

                Reset();

                blockWrote = BlockLength;
            }

            private static void CheckHeader(byte[] data)
            {
                for (var p = 0; p < MusHeader.Length; p++)
                {
                    if (data[p] != MusHeader[p])
                    {
                        throw new Exception("Invalid format!");
                    }
                }
            }

            public void RenderWaveform(Synthesizer synthesizer, Span<float> left, Span<float> right)
            {
                var wrote = 0;
                while (wrote < left.Length)
                {
                    if (blockWrote == synthesizer.BlockSize)
                    {
                        ProcessMidiEvents(synthesizer);
                        blockWrote = 0;
                    }

                    var srcRem = synthesizer.BlockSize - blockWrote;
                    var dstRem = left.Length - wrote;
                    var rem = Math.Min(srcRem, dstRem);

                    synthesizer.Render(left.Slice(wrote, rem), right.Slice(wrote, rem));

                    blockWrote += rem;
                    wrote += rem;
                }
            }

            private void ProcessMidiEvents(Synthesizer synthesizer)
            {
                if (delay > 0)
                {
                    delay--;
                }

                if (delay == 0)
                {
                    delay = ReadSingleEventGroup();
                    SendEvents(synthesizer);

                    if (delay == -1)
                    {
                        synthesizer.NoteOffAll(false);

                        if (loop)
                        {
                            Reset();
                        }
                    }
                }
            }

            private void Reset()
            {
                for (var i = 0; i < lastVolume.Length; i++)
                {
                    lastVolume[i] = 0;
                }

                p = scoreStart;

                delay = 0;
            }

            private int ReadSingleEventGroup()
            {
                eventCount = 0;
                while (true)
                {
                    var result = ReadSingleEvent();
                    if (result == ReadResult.EndOfGroup)
                    {
                        break;
                    }
                    else if (result == ReadResult.EndOfFile)
                    {
                        return -1;
                    }
                }

                var time = 0;
                while (true)
                {
                    var value = data[p++];
                    time = time * 128 + (value & 127);
                    if ((value & 128) == 0)
                    {
                        break;
                    }
                }

                return time;
            }

            private ReadResult ReadSingleEvent()
            {
                var channelNumber = data[p] & 0xF;

                if (channelNumber == 15)
                {
                    channelNumber = 9;
                }
                else if (channelNumber >= 9)
                {
                    channelNumber++;
                }

                var eventType = (data[p] & 0x70) >> 4;
                var last = (data[p] >> 7) != 0;

                p++;

                var me = events[eventCount];
                eventCount++;

                switch (eventType)
                {
                    case 0: // RELEASE NOTE
                        me.Type = 0;
                        me.Channel = channelNumber;

                        var releaseNote = data[p++];

                        me.Data1 = releaseNote;
                        me.Data2 = 0;

                        break;

                    case 1: // PLAY NOTE
                        me.Type = 1;
                        me.Channel = channelNumber;

                        var playNote = data[p++];
                        var noteNumber = playNote & 127;
                        var noteVolume = (playNote & 128) != 0 ? data[p++] : -1;

                        me.Data1 = noteNumber;
                        if (noteVolume == -1)
                        {
                            me.Data2 = lastVolume[channelNumber];
                        }
                        else
                        {
                            me.Data2 = noteVolume;
                            lastVolume[channelNumber] = noteVolume;
                        }

                        break;

                    case 2: // PITCH WHEEL
                        me.Type = 2;
                        me.Channel = channelNumber;

                        var pitchWheel = data[p++];

                        var pw2 = (pitchWheel << 7) / 2;
                        var pw1 = pw2 & 127;
                        pw2 >>= 7;
                        me.Data1 = pw1;
                        me.Data2 = pw2;

                        break;

                    case 3: // SYSTEM EVENT
                        me.Type = 3;
                        me.Channel = channelNumber;

                        var systemEvent = data[p++];
                        me.Data1 = systemEvent;
                        me.Data2 = 0;

                        break;

                    case 4: // CONTROL CHANGE
                        me.Type = 4;
                        me.Channel = channelNumber;

                        var controllerNumber = data[p++];
                        var controllerValue = data[p++];

                        me.Data1 = controllerNumber;
                        me.Data2 = controllerValue;

                        break;

                    case 6: // END OF FILE
                        return ReadResult.EndOfFile;

                    default:
                        throw new Exception("Unknown event type!");
                }

                if (last)
                {
                    return ReadResult.EndOfGroup;
                }
                else
                {
                    return ReadResult.Ongoing;
                }
            }

            private void SendEvents(Synthesizer synthesizer)
            {
                for (var i = 0; i < eventCount; i++)
                {
                    var me = events[i];
                    switch (me.Type)
                    {
                        case 0: // RELEASE NOTE
                            synthesizer.NoteOff(me.Channel, me.Data1);
                            break;

                        case 1: // PLAY NOTE
                            synthesizer.NoteOn(me.Channel, me.Data1, me.Data2);
                            break;

                        case 2: // PITCH WHEEL
                            synthesizer.ProcessMidiMessage(me.Channel, 0xE0, me.Data1, me.Data2);
                            break;

                        case 3: // SYSTEM EVENT
                            switch (me.Data1)
                            {
                                case 11: // ALL NOTES OFF
                                    synthesizer.NoteOffAll(me.Channel, false);
                                    break;

                                case 14: // RESET ALL CONTROLS
                                    synthesizer.ResetAllControllers(me.Channel);
                                    break;
                            }
                            break;

                        case 4: // CONTROL CHANGE
                            switch (me.Data1)
                            {
                                case 0: // PROGRAM CHANGE
                                    synthesizer.ProcessMidiMessage(me.Channel, 0xC0, me.Data2, 0);
                                    break;

                                case 1: // BANK SELECTION
                                    synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x00, me.Data2);
                                    break;

                                case 2: // MODULATION
                                    synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x01, me.Data2);
                                    break;

                                case 3: // VOLUME
                                    synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x07, me.Data2);
                                    break;

                                case 4: // PAN
                                    synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x0A, me.Data2);
                                    break;

                                case 5: // EXPRESSION
                                    synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x0B, me.Data2);
                                    break;

                                case 6: // REVERB
                                    synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x5B, me.Data2);
                                    break;

                                case 7: // CHORUS
                                    synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x5D, me.Data2);
                                    break;

                                case 8: // PEDAL
                                    synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x40, me.Data2);
                                    break;
                            }
                            break;
                    }
                }
            }

            private class MusEvent
            {
                public int Type;
                public int Channel;
                public int Data1;
                public int Data2;
            }

            private enum ReadResult
            {
                Ongoing,
                EndOfGroup,
                EndOfFile
            }
        }



        private class MidiDecoder : IDecoder
        {
            public static readonly byte[] MidiHeader = new byte[]
            {
                (byte)'M',
                (byte)'T',
                (byte)'h',
                (byte)'d'
            };

            private MidiFile midi;
            private MidiFileSequencer sequencer;

            private bool loop;

            public MidiDecoder(byte[] data, bool loop)
            {
                midi = new MidiFile(new MemoryStream(data));
                this.loop = loop;
            }

            public void RenderWaveform(Synthesizer synthesizer, Span<float> left, Span<float> right)
            {
                if (sequencer == null)
                {
                    sequencer = new MidiFileSequencer(synthesizer);
                    sequencer.Play(midi, loop);
                }

                sequencer.Render(left, right);
            }
        }
    }
}