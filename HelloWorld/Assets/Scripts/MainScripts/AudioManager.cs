using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singletion<AudioManager>, SingletionInterface
{
    public static readonly string ResBgmPath = "Assets/ZRes/Audio/Bgm/";
    public static readonly string ResSoundPath = "Assets/ZRes/Audio/Sound/";

    private GameObject root;
    private bool musicMute = false;
    private bool soundMute = false;

    private int loadId = -1;
    private AudioSource music;
    private Dictionary<int, SoundItem> audioItems = new();
    private Queue<SoundItem> itemCache = new();

    public void Init()
    {
        root = new GameObject("Sound");
        music = root.AddComponent<AudioSource>();
    }
    public void SetMusicMute(bool state)
    {
        musicMute = state;
        if (musicMute) StopMusic();
    }
    public void SetSoundMute(bool state)
    {
        soundMute = state;
        if (soundMute) StopAllSoud();
    }

    #region 背景音乐
    /// <summary>
    /// 背景音乐
    /// </summary>
    /// <param name="path">相对于Assets/ZRes/Audio/Bgm的相对路径</param>
    public void PlayMusic(string path, bool loop = true)
    {
        if (musicMute) return;
        AssetManager.Instance.Load<AudioClip>(ref loadId, $"{ResBgmPath}{path}.mp3", LoadMusicFinish);
        music.loop = loop;
    }
    private void LoadMusicFinish(int id, Object asset)
    {
        music.clip = asset as AudioClip;
        music.Play();
    }
    public void StopMusic()
    {
        music.Stop();
        AssetManager.Instance.Unload(ref loadId);
    }
    #endregion

    #region 音效
    /// <summary>
    /// 音效
    /// </summary>
    /// <param name="path">相对于Assets/ZRes/Audio/Sound的相对路径</param>
    public int PlaySound(string path, bool loop = false)
    {
        if (soundMute) return -1;
        var item = itemCache.Count > 0 ? itemCache.Dequeue() : new SoundItem();
        var itemId = item.Play($"{ResSoundPath}{path}", loop);
        audioItems.Add(itemId, item);
        return itemId;
    }
    public void StopAllSoud()
    {
        foreach (var item in audioItems) item.Value.Stop();
    }
    public void StopSound(int id)
    {
        if (audioItems.TryGetValue(id, out var item)) item.Stop();
    }
    #endregion

    public class SoundItem
    {
        private int itemId = -1;
        private AudioSource src;
        private bool loop = false;
        private int timerId = -1;

        public SoundItem()
        {
            src = Instance.root.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
        }
        public int Play(string path, bool loop)
        {
            this.loop = loop;
            src.loop = loop;
            AssetManager.Instance.Load<AudioClip>(ref itemId, path, LoadFinish);
            return itemId;
        }
        private void LoadFinish(int loadId, Object asset)
        {
            var clip = asset as AudioClip;
            src.clip = clip;
            src.Play();
            if (!loop && timerId < 0) timerId = TimeManager.Instance.StartTimer(clip.length, finish: Stop);
        }
        public void Stop()
        {
            Instance.audioItems.Remove(itemId);
            AssetManager.Instance.Unload(ref itemId, CacheTime.Short);
            src.Stop();
            src.clip = null;
            if (timerId > 0) TimeManager.Instance.StopTimer(timerId);
            timerId = -1;
            Instance.itemCache.Enqueue(this);
        }
    }
}
