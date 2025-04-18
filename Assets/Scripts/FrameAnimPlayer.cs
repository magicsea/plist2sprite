using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameAnimPlayer : MonoBehaviour
{
    public SpriteRenderer render;
    public FrameAnimData anim;
    public bool isPlaying;
    public int counter;
    public int frameIndex;
    // Start is called before the first frame update
    void Start()
    {
        if (render == null)
        {
            render = GetComponent<SpriteRenderer>();
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (anim==null||anim.Info==null)
        {
            return;
        }
        if (isPlaying)
        {
            counter++;
            // 根据帧率计算是否切换到下一帧
            if (counter >= anim.Info.FrameRate)
            {
                counter = 0;
                frameIndex++;
                // 循环播放处理
                if (frameIndex >= anim.Info.Frames.Length)
                {
                    frameIndex = 0;
                }
                // 更新当前显示的精灵帧
                render.sprite = anim.Info.Frames[frameIndex].sprite;
                // 根据offset设置偏移
                if (anim.Info.Frames[frameIndex].sprite != null)
                {
                    Vector3 offset = new Vector3(
                        anim.Info.Frames[frameIndex].x,
                        anim.Info.Frames[frameIndex].y,
                        0
                    );
                    render.transform.localPosition = offset;
                }
            }
        }
    }

    void LoadRes(FrameAnimData animRes)
    {
        counter = 0;
        frameIndex = 0;
        anim = animRes;
    }

    void Play()
    {
        isPlaying = true;
    }

    void Pause()
    {
        isPlaying = false;

    }
}
