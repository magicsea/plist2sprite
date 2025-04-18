using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;

public class FrameAnimPlayer : MonoBehaviour
{
    public FrameAnimData animData;
    public SpriteRenderer render;
    public FrameAnimInfo anim;
    public bool isPlaying;
    public int counter;
    public int frameIndex;
    public float speed = 1; //播放速度，默认为1
    public string initAnimName;
    // Start is called before the first frame update
    void Start()
    {
        if (render == null)
        {
            render = GetComponent<SpriteRenderer>();
        }
        //initAnimName
        if (initAnimName != "")
        {
            Play(initAnimName);
        } 
        else if (animData!=null&& animData.AnimRes.Infos.Count>0)
        {
            Play(animData.AnimRes.Infos.First().AnimName);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (anim==null)
        {
            return;
        }
        if (isPlaying)
        {
            counter++;
            // 根据帧率和speed计算是否切换到下一帧
            if (counter >= anim.FrameRate / speed)  // 添加speed影响
            {
                counter = 0;
                frameIndex++;
                // 循环播放处理
                if (frameIndex >= anim.Frames.Length)
                {
                    frameIndex = 0;
                }
                // 更新当前显示的精灵帧
                render.sprite = anim.Frames[frameIndex].sprite;
            }
        }
    }

    void LoadRes(FrameAnimData animRes)
    {
        counter = 0;
        frameIndex = 0;
        animData = animRes;
        anim = null;
    }

    void Play(string animName)
    {
        anim = animData.AnimRes.Infos.Find(x => x.AnimName == animName);
        if (anim == null)
        {
            Debug.LogError("找不到动画：" + animName);
            return;
        }
        isPlaying = true;
    }

    void Pause()
    {
        isPlaying = false;

    }
}
