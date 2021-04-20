using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ServerCommon;


public class PopupSoulPassInfo : PopupBase
{
    [Serializable]
    public class ViewFinanceInfo
    {
        public int slotNo;
        public RewardType type;
        public int v1 = 0;
        public int v2 = 0;
        public RewardValueDesc finance { get { return new RewardValueDesc((int)type, v1, v2); } }

        public string expValue;
    }
    public List<ViewFinanceInfo> ViewInfoList;


    [Serializable]
    public class ViewFinanceSlot
    {
        public int slotNo;
        public GameObject slotObj;
        public GameObject iconPos;
        public UILabel label;
    }
    public List<ViewFinanceSlot> ViewSlotList;


    protected override void Init()
    {
        if (ViewInfoList == null)
            return;

        if (ViewSlotList == null)
            return;

        foreach (ViewFinanceSlot slot in ViewSlotList)
        {
            ViewFinanceInfo info = ViewInfoList.Find(o => o.slotNo == slot.slotNo);

            if (info == null)
            {
                slot.slotObj.SetActive(false);
                continue;
            }

            IconManageBase icon = ManagerCS.instance.MakeRewardIcon(slot.iconPos, info.finance);
            icon.SetPopup();

            slot.label.text = info.expValue;
        }
    }
}
