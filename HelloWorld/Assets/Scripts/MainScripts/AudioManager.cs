using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singletion<AudioManager>, SingletionInterface
{
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

    #region ±≥æ∞“Ù¿÷
    public void PlayMusic(string path, bool loop = true)
    {
        AssetManager.Instance.Load<AudioClip>(ref loadId, path, LoadMusicFinish);
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
        AssetManager.Instance.Unload(loadId);
    }
    #endregion

    #region “Ù–ß
    public int PlaySound(string path, bool loop = false)
    {
        AssetPool<SoundItem> pool;
        if (!audioPool.TryGetValue(path, out pool))
        {
            pool = new AssetPool<SoundItem>();
            pool.Init(path);
            audioPool.Add(path, pool);
        }
        var item = pool.Dequeue();
        item.Set(pool, loop);
        return item.ItemID;
    }
    public void StopAllSoud()
    {
        foreach (var item in audioPool)
        {
            var use = item.Value.Use;
            for (int i = 0; i < use.Count; i++)
            {
                item.Value.Enqueue(use[i]);
            }
        }
    }
    public void StopSound(int id)
    {
        foreach (var item in audioPool)
        {
            var use = item.Value.Use;
            for (int i = 0; i < use.Count; i++)
            {
                if (use[i].ItemID == id)
                {
                    item.Value.Enqueue(use[i]);
                    return;
                }
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
        private AssetPool<SoundItem> pool;
        private bool loop = false;
        private int timerId = -1;

        public void Set(AssetPool<SoundItem> pool, bool loop)
        {
            this.pool = pool;
            this.loop = loop;
            if (source) source.loop = loop;
        }
        protected override void Delay()
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
        public override void LoadFinish()
        {
            source = Instance.GetEmptyAudioSource();
            source.loop = loop;
            var clip = obj as AudioClip;
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
