using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ServerCommon;
using ServerCommon.SoulPass;
using Newtonsoft.Json;


public class SoulPassRewardList : MonoBehaviour
{
    public int passLevel;
    public SoulPassRewardDesc rewardData;
    
    public GameObject iconPos;
    public UILabel countLabel;
    public GameObject maskObj;
    public GameObject lockObj;
    public GameObject effectObj;
    public TweenScale tween;
    
    private bool bEnableTakeReward;
    private bool bTakeGoldPass;
    


    public void Set(int passLv, SoulPassRewardDesc reward, bool bGoldPass, bool bOpen, bool bTake)
    {
        // 보상정보 없으면 빈슬롯임
        if (reward == null)
        {
            rewardData = new SoulPassRewardDesc();
            gameObject.SetActive(false);
            return;
        }

        passLevel = passLv;
        rewardData = reward;
        bTakeGoldPass = bGoldPass;
        
        iconPos.transform.DestroyChildren();
        IconManageBase icon = ManagerCS.instance.MakeRewardIcon(iconPos, rewardData.rwd.type, rewardData.rwd.v1, rewardData.rwd.v2, rewardData.rwd.v3);
        icon.SetActiveOnlyMainImage(true, 5);
        icon.SetPopup();
        icon.SetOnClickCallBack(OnClickTakeReward);
        icon.AddDragScrollview();

        int count = (rewardData.rwd.v2 == 0) ? rewardData.rwd.v1 : rewardData.rwd.v2;
        countLabel.text = (count <= 1) ? string.Empty : string.Format("[b]{0}", count);

        SetState(bOpen, bTake);
    }

    public void SetState(bool bOpen, bool bTake)
    {
        // 상태
        // 1. 잠금            -> LockObj
        // 2. 보상수령 가능   -> effectObj
        // 3. 보상수령 완료   -> maskObj
        bEnableTakeReward = bOpen && !bTake;

        maskObj.SetActive(bTake);
        lockObj.SetActive(!bOpen);
        effectObj.SetActive(bEnableTakeReward);
        tween.enabled = bEnableTakeReward && ManagerCS.instance.CheckSoulPassTakeRewardTarget(bTakeGoldPass, passLevel);
        transform.localScale = Vector3.one;
    }

    public void OnClickTakeReward()
    {
        // 보상 수령 불가
        if (bEnableTakeReward == false)
        {
            Debug.Log("보상 수령 불가");
            return;
        }

        // 보상 수령 실행
        ManagerCS.instance.SoulPassTakeRewardRequest(bTakeGoldPass, passLevel);
    }
}
