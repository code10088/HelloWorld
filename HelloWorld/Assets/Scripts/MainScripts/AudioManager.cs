using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singletion<AudioManager>, SingletionInterface
{
    public static readonly string ResBgmPath = "Assets/ZRes/Audio/Bgm/";
    public static readonly string ResSoundPath = "Assets/ZRes/Audio/Sound/";

    private GameObject root;

    private int loadId = -1;
    private AudioSource music;
    private Dictionary<string, AssetPool<SoundItem>> audioPool = new();
    private Queue<AudioSource> sourceCache = new();

    public void Init()
    {
        root = new GameObject("Sound");
        music = root.AddComponent<AudioSource>();
    }

    #region ��������
    /// <summary>
    /// ��������
    /// </summary>
    /// <param name="path">�����Assets/ZRes/Audio/Bgm�����·��</param>
    public void PlayMusic(string path, bool loop = true)
    {
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

    #region ��Ч
    /// <summary>
    /// ��Ч
    /// </summary>
    /// <param name="path">�����Assets/ZRes/Audio/Sound�����·��</param>
    public int PlaySound(string path, bool loop = false)
    {
        AssetPool<SoundItem> pool;
        path = $"{ResSoundPath}{path}";
        if (!audioPool.TryGetValue(path, out pool))
        {
            pool = new AssetPool<SoundItem>();
            pool.Init(path);
            audioPool.Add(path, pool);
        }
        var item = pool.Dequeue<AudioClip>();
        item.Set(loop);
        return item.ItemID;
    }
    public void StopAllSoud()
    {
        foreach (var item in audioPool)
        {
            var use = item.Value.Use;
            for (int i = 0; i < use.Count; i++)
            {
                item.Value.Enqueue(use[i].ItemID);
            }
        }
    }
    public void StopSound(int id)
    {
        foreach (var item in audioPool)
        {
            if (item.Value.Use.Exists(a => a.ItemID == id))
            {
                item.Value.Enqueue(id);
                return;
            }
        }
    }
    #endregion

    private AudioSource GetEmptyAudioSource()
    {
        AudioSource src;
        if (sourceCache.Count > 0) src = sourceCache.Dequeue();
        else src = root.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        return src;
    }
    private void ReleaseAudioSource(AudioSource source)
    {
        if (source) sourceCache.Enqueue(source);
    }

    public class SoundItem : AssetPoolItem
    {
        private AudioSource source;
        private bool loop = false;
        private int timerId = -1;

        public void Set(bool loop)
        {
            this.loop = loop;
            if (source) source.loop = loop;
        }
        public override void Delay()
        {
            if (source)
            {
                source.Stop();
                Instance.ReleaseAudioSource(source);
            }
            if (timerId > 0)
            {
                TimeManager.Instance.StopTimer(timerId);
                timerId = -1;
            }
            base.Delay();
        }
        public override void Release()
        {
            if (source)
            {
                source.Stop();
                Instance.ReleaseAudioSource(source);
            }
            if (timerId > 0)
            {
                TimeManager.Instance.StopTimer(timerId);
                timerId = -1;
            }
            base.Release();
        }
        public override void Finish(Object asset)
        {
            source = Instance.GetEmptyAudioSource();
            source.loop = loop;
            var clip = asset as AudioClip;
            source.clip = clip;
            source.Play();
            if (!loop && timerId < 0) timerId = TimeManager.Instance.StartTimer(clip.length, finish: Stop);
        }
        private void Stop()
        {
            Instance.StopSound(ItemID);
        }
    }
}
