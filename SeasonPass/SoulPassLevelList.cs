using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SoulPassLevelList : MonoBehaviour
{
    public int passLevel;
    public UILabel levelLabel;
    public UISprite backImg;

    public void Set(int level, bool bOpen)
    {
        if (level == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        passLevel = level;
        levelLabel.text = string.Format("[b]{0}", level);
        SetState(bOpen);
    }

    public void SetState(bool bOpen)
    {
        backImg.spriteName = bOpen ? "board_challenge_on" : "board_challenge_off";
    }
}
