using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : Singletion<TimeManager>
{
    public int StartTimer(float time, Action action)
    {
        return 0;
    }
    public void StopTimer(int id)
    {

    }
    public int StartTimer1Second(float time, Action<float> action)
    {
        return 0;

    }
    public int StartTimer1Frame(float time, Action<int> action)
    {
        return 0;

    }
}
