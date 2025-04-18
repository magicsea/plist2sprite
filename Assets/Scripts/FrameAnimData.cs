using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class FrameData
{
    public string res;
    public Sprite sprite;
    public float x;
    public float y;
}



[System.Serializable]
public class FrameAnimInfo
{
    public string AnimName;
    public int FrameRate;
    public string[] Events;
    public FrameData[] Frames;
    public bool Loop;
}

[System.Serializable]
public class FrameAnimRes
{
    public List<FrameAnimInfo> Infos;
}


public class FrameAnimData : MonoBehaviour
{
    public FrameAnimRes AnimRes;

    void Start()
    {
        
    }


    void Update()
    {
        
    }
}
