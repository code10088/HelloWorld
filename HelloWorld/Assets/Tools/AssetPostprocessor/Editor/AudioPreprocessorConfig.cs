using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetPreprocessor.Scripts.Editor
{
    [CreateAssetMenu(menuName="ScriptableObject/AssetPreprocessor/AudioPreprocessorConfig")]
    public class AudioPreprocessorConfig : BasePreprocessorConfig
    {
        [Header("音效设置")]
        public bool ForceToMono = true;
        public bool Normalize = true;
        [Tooltip("bgm使用该设置，子线程中加载，会导致播放延迟")]
        public bool LoadInBackground = true;
        public bool Ambisonic = false;

        [Header("平台")]
        public List<string> PlatformsRegexList = new List<string> { "Default", "PC", "Android", "iOS", "WebGL" };

        [Tooltip("常用sound使用DecompressOnLoad无压缩的释放到内存，不常用sound使用CompressedInMemory播放时消耗CPU解压处理，常用bgm使用DecompressOnLoad，不常用bgm使用Streaming")]
        public AudioClipLoadType LoadType = AudioClipLoadType.DecompressOnLoad;
        [Tooltip("如果设置false还需要调用audioClip.LoadAudioData()")]
        public bool PreloadAudioData = true;
        [Tooltip("常用sound使用PCM高质量，不常用sound使用Vorbis低质量，常用bgm使用PCM，不常用bgm使用Vorbis")]
        public AudioCompressionFormat CompressionFormat = AudioCompressionFormat.Vorbis;
        [Range(0, 1)]
        public float Quality = 0.7f;
        public AudioSampleRateSetting SampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        public uint SampleRate = 44100;
    }
}
