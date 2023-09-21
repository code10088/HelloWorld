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
                use[i].Stop();
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
                    use[i].Stop();
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
        private bool loop = false;
        private bool stop = false;
        private int timerId = -1;

        public void Set(bool loop)
        {
            this.loop = loop;
            if (source) source.loop = loop;
        }
        public void Stop()
        {
            TimeManager.Instance.StopTimer(timerId);
            timerId = -1;
            stop = true;
            Instance.ReleaseAudioSource(source);
            Release();
        }
        public override void LoadFinish(Object asset)
        {
            if (stop || asset == null) return;
            source = Instance.GetEmptyAudioSource();
            source.loop = loop;
            var clip = asset as AudioClip;
            source.clip = clip;
            source.Play();
            if (!loop && timerId < 0) timerId = TimeManager.Instance.StartTimer(clip.length, finish: Stop);
        }
    }
}
