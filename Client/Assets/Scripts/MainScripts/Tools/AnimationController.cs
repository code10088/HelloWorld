using System;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animation ani;
    private int timerId;
    void Awake()
    {
        ani = GetComponent<Animation>();
    }
    public void Play(string name, float fadeLength = 0, Action finish = null)
    {
        if (timerId > 0) Driver.Instance.Remove(timerId);
        var clip = ani.GetClip(name);
        if (finish != null) timerId = Driver.Instance.StartTimer(clip.length, finish: finish);
        if (fadeLength > 0) ani.CrossFade(name, fadeLength);
        else ani.Play(name);
    }
    public void Stop(string name, Action finish = null)
    {
        if (timerId > 0) Driver.Instance.Remove(timerId);
        ani.Stop();
    }
}
