using System;
using System.Collections.Generic;

public class EffectManager : Singletion<EffectManager>
{

    public int Add(string path, float time)
    {
        return 0;
    }
    public void Remove(int id)
    {

    }


    private class EffectItem : LoadGameObjectItem
    {
        
    }
}