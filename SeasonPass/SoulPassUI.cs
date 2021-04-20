using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ServerCommon;
using ServerCommon.SoulPass;
using ServerCommon.CashProduct;

public class SoulPassUI : GameNotification
{
    private SoulPass_EnterResultParameter SoulPassData;
    private SoulPassLevelDesc currLvData;
    private SoulPassLevelDesc lastLvData;
    private float currTargetScrollLeftPos;
    private float currTargetScrollRightPos;
    private float lastTargetScrollPos;
    private float takeTargetScrollPos;
    private int takeRewardTargetLv;
    private float BaseScrollViewPosition;
    private bool bDrag;
    private bool bEnd = false;
    private const string fontBoldFormat = "[b]{0}";

    // Pass State
    public UILabel SeasonLabel;
    public UILabel NextPassLevelLabel;
    public UILabel PassPointLabel;
    public UIProgressBar PassPointGauge;
    public UILabel PassRemainTime;

    // Pass RewardList
    public UIScrollView PassListScrollVeiw;

    public SoulPassLevelList PassLevelObj;
    public SoulPassRewardList StartGoldPassObj;
    public SoulPassRewardList StartSilverPassObj;
    private List<SoulPassLevelList> PassLevelList;
    private List<SoulPassRewardList> GoldPassList;
    private List<SoulPassRewardList> SilverPassList;

    public UIGrid PassLevelListGrid;
    public UIGrid GoldPassListGrid;
    public UIGrid SilverPassListGrid;
    
    public UISprite PassLevelGaugeBar;
    public UISprite PassLevelGaugeBack;
    public UISprite OutViewGauge;

    // Pass Navigation
    public GameObject CurrLvLeftBtn;
    public UILabel CurrLvLeftLabel;

    public GameObject CurrLvRightBtn;
    public UILabel CurrLvRightLabel;

    public GameObject LastLvBtn;
    public UILabel LastLvLabel;

    public GameObject TakeLvBtn;
    public UILabel TakeLvLabel;

    // Pass LevelUp Cost
    public GameObject PassLevelUpButton;
    public UISprite PassLevelUpCostType;
    public UILabel PassLevelUpCostValue;

    // Pass Package
    public GameObject PassPackageButton;
    public UILabel PassPackageCostLabel;


    // Custom Data
    public int ListDistance = 220;
    

    
    protected override void Init()
    {
        SoulPassData = GameData.SoulPassEnterResult;

        // Data 세팅
        SetData();

        // UI 세팅
        SetState();
        SetRewardList();
        SetProgress();
        SetLevelUpCost();
        SetPackageInfo();

        OpenImminentSoulPassEndTimePopup(); // 종료임박 알림 팝업
    }

    private void Update()
    {
        if (bDrag)
            SetNavigationButtonActive();
    }

    private void SetData()
    {
        // 데이터 정렬 및 캐싱
        SoulPassData.levellist.OrderBy(o => o.level);
        currLvData = SoulPassData.levellist.Find(o => o.level == SoulPassData.state.level);
        lastLvData = SoulPassData.levellist[SoulPassData.levellist.Count - 1];
        takeRewardTargetLv = ManagerCS.instance.GetSoulPassTakeRewardTargetLevel();
    }

    private void SetState()
    {
        // 상태 세팅
        SeasonLabel.text = SSLocalization.Format("SoulPass", 7, SoulPassData.state.seasonno);

        int nextLv = currLvData.level + 1;
        if (nextLv <= lastLvData.level)
        {
            NextPassLevelLabel.text = GetBoldFont(nextLv.ToString());
            PassPointLabel.text = GetBoldFont(string.Format("{0}/{1}", SoulPassData.state.point, currLvData.nextpoint));
            PassPointGauge.value = (float)SoulPassData.state.point / currLvData.nextpoint;
        }
        else
        {
            NextPassLevelLabel.gameObject.SetActive(false);
            PassPointLabel.text = SSLocalization.Get("SoulPass", 6);
            PassPointGauge.value = 1;
        }

        // 바로가기 버튼 세팅
        CurrLvLeftLabel.text = CurrLvRightLabel.text = GetBoldFont(SoulPassData.state.level == 0 ? SSLocalization.Get("SoulPass", 4) : SoulPassData.state.level.ToString());
        LastLvLabel.text = GetBoldFont(lastLvData.level.ToString());
        TakeLvLabel.text = GetBoldFont(takeRewardTargetLv == 0 ? SSLocalization.Get("SoulPass", 4) : takeRewardTargetLv.ToString());

        // 레벨업 버튼 세팅
        PassLevelUpButton.SetActive(currLvData.level < lastLvData.level);

        // 남은시간 세팅
        OnUpdateGameContentsTimer();
    }

    private void SetRewardList()
    {
        // 보상 리스트 그리드 간격 세팅
        PassLevelListGrid.cellWidth = ListDistance;
        GoldPassListGrid.cellWidth = ListDistance;
        SilverPassListGrid.cellWidth = ListDistance;

        PassLevelList = new List<SoulPassLevelList>();
        GoldPassList = new List<SoulPassRewardList>();
        SilverPassList = new List<SoulPassRewardList>();

        // 보상 리스트 생성
        foreach (SoulPassLevelDesc lvData in SoulPassData.levellist)
        {
            // 1. 보상 수령 했는지 여부 => bOpen (*골드패스는 구매여부도 체크)
            // 2. 보상 수령 가능 여부 => bTake
            bool bOpen = false;
            bool bTake = false;
            SoulPassLevelInfo lvInfo;

            if (lvData.level == 0)
            {
                // Level
                bOpen = (lvData.level <= currLvData.level);
                PassLevelObj.Set(lvData.level, bOpen);
                PassLevelList.Add(PassLevelObj);
                
                // Cash Pass
                lvInfo = SoulPassData.state.passlist.Find(o => o.level == lvData.level);
                bOpen = SoulPassData.state.bCash && (lvData.level <= currLvData.level);
                bTake = (lvInfo != null) && lvInfo.bTaked;
                SoulPassRewardDesc passRewardData = SoulPassData.cashrlist.Find(o => o.ridx == lvData.passridx);
                StartGoldPassObj.Set(lvData.level, passRewardData, true, bOpen, bTake);
                GoldPassList.Add(StartGoldPassObj);

                // Base Pass
                lvInfo = SoulPassData.state.baselist.Find(o => o.level == lvData.level);
                bOpen = (lvData.level <= currLvData.level);
                bTake = (lvInfo != null) && lvInfo.bTaked;
                SoulPassRewardDesc baseRewardData = SoulPassData.baserlist.Find(o => o.ridx == lvData.baseridx);
                StartSilverPassObj.Set(lvData.level, baseRewardData, false, bOpen, bTake);
                SilverPassList.Add(StartSilverPassObj);
            }
            else
            {
                // Level
                bOpen = (lvData.level <= currLvData.level);
                SoulPassLevelList passLevelObj = NGUITools.AddChild(PassLevelListGrid.gameObject, PassLevelObj.gameObject).GetComponent<SoulPassLevelList>();
                passLevelObj.Set(lvData.level, bOpen);
                PassLevelList.Add(passLevelObj);

                // Cash Pass
                lvInfo = SoulPassData.state.passlist.Find(o => o.level == lvData.level);
                bOpen = SoulPassData.state.bCash && (lvData.level <= currLvData.level);
                bTake = (lvInfo != null) && lvInfo.bTaked;
                SoulPassRewardDesc passRewardData = SoulPassData.cashrlist.Find(o => o.ridx == lvData.passridx);
                SoulPassRewardList goldPassObj = NGUITools.AddChild(GoldPassListGrid.gameObject, StartGoldPassObj.gameObject).GetComponent<SoulPassRewardList>();
                goldPassObj.Set(lvData.level, passRewardData, true, bOpen, bTake);
                GoldPassList.Add(goldPassObj);

                // Base Pass
                lvInfo = SoulPassData.state.baselist.Find(o => o.level == lvData.level);
                bOpen = (lvData.level <= currLvData.level);
                bTake = (lvInfo != null) && lvInfo.bTaked;
                SoulPassRewardDesc baseRewardData = SoulPassData.baserlist.Find(o => o.ridx == lvData.baseridx);
                SoulPassRewardList silverPassObj = NGUITools.AddChild(SilverPassListGrid.gameObject, StartSilverPassObj.gameObject).GetComponent<SoulPassRewardList>();
                silverPassObj.Set(lvData.level, baseRewardData, false, bOpen, bTake);
                SilverPassList.Add(silverPassObj);
            }
        }
        
        PassLevelListGrid.Reposition();
        GoldPassListGrid.Reposition();
        SilverPassListGrid.Reposition();

        StartCoroutine((SetScrollViewPosition()));
    }
    
    IEnumerator SetScrollViewPosition()
    {
        yield return null;

        PassListScrollVeiw.ResetPosition();
        PassListScrollVeiw.onDragStarted = () => { bDrag = true; };
        PassListScrollVeiw.onStoppedMoving = () => { bDrag = false; };

        BaseScrollViewPosition = Mathf.Round(PassListScrollVeiw.transform.localPosition.x);
        
        SetNavigationPosition();

        if (currTargetScrollLeftPos >= lastTargetScrollPos)
            OnClickCurrentLevelLeftNavigation();
        else
            OnClickLastLevelNavigation();

        SetNavigationButtonActive();
    }

    private void SetNavigationPosition()
    {
        currTargetScrollLeftPos = Mathf.Round(BaseScrollViewPosition - (ListDistance * currLvData.level));
        currTargetScrollRightPos = Mathf.Round(BaseScrollViewPosition - (ListDistance * (currLvData.level + 2)) + PassListScrollVeiw.panel.GetViewSize().x);
        lastTargetScrollPos = Mathf.Round(BaseScrollViewPosition - (ListDistance * (lastLvData.level + 2)) + PassListScrollVeiw.panel.GetViewSize().x);
        takeTargetScrollPos = Mathf.Round(BaseScrollViewPosition - (ListDistance * takeRewardTargetLv));

        if (currTargetScrollRightPos < lastTargetScrollPos)
            currTargetScrollRightPos = lastTargetScrollPos;
    }

    private void SetNavigationButtonActive()
    {
        CurrLvLeftBtn.SetActive(PassListScrollVeiw.transform.localPosition.x < currTargetScrollLeftPos - ListDistance);
        CurrLvRightBtn.SetActive(PassListScrollVeiw.transform.localPosition.x > currTargetScrollRightPos + ListDistance);

        LastLvBtn.SetActive(PassListScrollVeiw.transform.localPosition.x <= currTargetScrollRightPos + ListDistance &&
                            PassListScrollVeiw.transform.localPosition.x > lastTargetScrollPos + ListDistance);

        TakeLvBtn.SetActive(takeRewardTargetLv >= 0 &&
                            PassListScrollVeiw.transform.localPosition.x >= currTargetScrollLeftPos - ListDistance &&
                            PassListScrollVeiw.transform.localPosition.x < takeTargetScrollPos - ListDistance);
    }

    private void SetProgress()
    {
        // 진행도 세팅
        int baseWidth = ListDistance / 2;
        PassLevelGaugeBack.SetDimensions(baseWidth + (ListDistance * (SoulPassData.levellist.Count - 1)) - (ListDistance / 2), PassLevelGaugeBack.height);

        if (currLvData.level == lastLvData.level)
        {
            PassLevelGaugeBar.SetDimensions(PassLevelGaugeBack.width, PassLevelGaugeBar.height);
        }
        else
        {
            int currIndex = SoulPassData.levellist.FindIndex(o => o.level == currLvData.level);
            PassLevelGaugeBar.SetDimensions(baseWidth + (ListDistance * currIndex), PassLevelGaugeBar.height);
        }

        // 레벨 데이터
        SoulPassLevelDesc lvData = SoulPassData.levellist.Find(o => o.level == currLvData.level);
        RefreshRewardList(lvData);
    }

    private void SetLevelUpCost()
    {
        RewardValueDesc cost = SoulPassData.levelupinfo.cost;
        PassLevelUpCostType.spriteName = ManagerCS.instance.FindSpriteIconName(cost.type);
        PassLevelUpCostValue.text = string.Format(fontBoldFormat, ((cost.v2 == 0) ? cost.v1 : cost.v2));
    }

    private void SetPackageInfo()
    {
        PassPackageButton.SetActive(!SoulPassData.state.bCash);
        PassPackageCostLabel.text = ManagerCS.instance.GetProductPrice(SoulPassData.packageinfo.basicmanidx);
    }

    private void RefreshRewardList()
    {
        SetData();
        SetState();
        SetNavigationPosition();
        SetNavigationButtonActive();
        
        foreach (SoulPassLevelDesc lvData in SoulPassData.levellist)
        {
            RefreshRewardList(lvData);
        }
    }

    private void RefreshRewardList(int level)
    {
        SetData();
        SetState();
        SetNavigationPosition();
        SetNavigationButtonActive();

        RefreshRewardList(SoulPassData.levellist.Find(o => o.level == level));
    }

    private void RefreshRewardList(SoulPassLevelDesc lvData)
    {
        if (lvData == null)
            return;
        
        // 1. 보상 수령 했는지 여부 => bOpen (*골드패스는 구매여부도 체크)
        // 2. 보상 수령 가능 여부 => bTake
        bool bOpen = false;
        bool bTake = false;
        SoulPassLevelInfo lvInfo;
        SoulPassRewardList passList;

        // Level
        bOpen = (lvData.level <= currLvData.level);
        PassLevelList.Find(o => o.passLevel == lvData.level).SetState(bOpen);

        // Gold Pass
        passList = GoldPassList.Find(o => o.passLevel == lvData.level);
        if (passList != null)
        {
            lvInfo = SoulPassData.state.passlist.Find(o => o.level == lvData.level);
            bOpen = SoulPassData.state.bCash && (lvData.level <= currLvData.level);
            bTake = (lvInfo != null) && lvInfo.bTaked;
            passList.SetState(bOpen, bTake);
        }
        
        // Silver Pass
        passList = SilverPassList.Find(o => o.passLevel == lvData.level);
        if (passList != null)
        {
            lvInfo = SoulPassData.state.baselist.Find(o => o.level == lvData.level);
            bOpen = (lvData.level <= currLvData.level);
            bTake = (lvInfo != null) && lvInfo.bTaked;
            passList.SetState(bOpen, bTake);
        }
    }

    private void OpenImminentSoulPassEndTimePopup()
    {
        if (ManagerCS.instance.isImminentSoulPassEndTime)
        {
            PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common, false, true);

            TimeSpan span = new TimeSpan(0, 0, GameData.SoulPassEnterResult.state.remaintimesec);
            if (span.Days > 0)
                popup.Set(PopupCommon.Type.OneBtn, SSLocalization.Format("popupCS", 428, span.Days));
            else
                popup.Set(PopupCommon.Type.OneBtn, SSLocalization.Get("popupCS", 429));

            ManagerCS.instance.SetOpenImminentSoulPassEndTimePopup();
        }
    }

    private string GetBoldFont(string text)
    {
        return string.Format(fontBoldFormat, text);
    }

    public void OnClickSoulPassInfo()
    {
        PopupManager.instance.MakePopup(ePopupType.SoulPassInfo, false, true);
    }

    public void OnClickPassLevelUpButton()
    {
        int costValue = (SoulPassData.levelupinfo.cost.v2 == 0) ? SoulPassData.levelupinfo.cost.v1 : SoulPassData.levelupinfo.cost.v2;

        PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common, false, true);
        popup.SetPayment(SoulPassData.levelupinfo.cost);
        popup.Set(PopupCommon.Type.Payment, SSLocalization.Format("SoulPass", 3, costValue),
            () =>
            {
                if (ManagerCS.instance.CheckEnoughCost(SoulPassData.levelupinfo.cost) == false)
                    return;

                ManagerCS.instance.SoulPassLevelUpRequest(
                    () =>
                    {
                        SetData();
                        SetState();
                        SetProgress();
                        SetNavigationPosition();

                        float xPos = Mathf.Round(PassListScrollVeiw.transform.localPosition.x - ListDistance);
                        if (xPos < lastTargetScrollPos)
                            xPos = lastTargetScrollPos;

                        Vector3 targetPos = new Vector3(xPos, PassListScrollVeiw.transform.localPosition.y, PassListScrollVeiw.transform.localPosition.z);
                        SpringPanel.Begin(PassListScrollVeiw.panel.cachedGameObject, targetPos, 8f).onFinished = () => { bDrag = false; };
                        bDrag = true;
                    });
            });
    }

    public void OnClickCurrentLevelLeftNavigation()
    {
        Vector3 targetPos = new Vector3(currTargetScrollLeftPos, PassListScrollVeiw.transform.localPosition.y, PassListScrollVeiw.transform.localPosition.z);
        SpringPanel.Begin(PassListScrollVeiw.panel.cachedGameObject, targetPos, 8f).onFinished = () => { bDrag = false; };
        bDrag = true;
    }

    public void OnClickCurrentLevelRightNavigation()
    {
        Vector3 targetPos = new Vector3(currTargetScrollRightPos, PassListScrollVeiw.transform.localPosition.y, PassListScrollVeiw.transform.localPosition.z);
        SpringPanel.Begin(PassListScrollVeiw.panel.cachedGameObject, targetPos, 8f).onFinished = () => { bDrag = false; };
        bDrag = true;
    }

    public void OnClickLastLevelNavigation()
    {
        Vector3 targetPos = new Vector3(lastTargetScrollPos, PassListScrollVeiw.transform.localPosition.y, PassListScrollVeiw.transform.localPosition.z);
        SpringPanel.Begin(PassListScrollVeiw.panel.cachedGameObject, targetPos, 8f).onFinished = () => { bDrag = false; };
        bDrag = true;
    }

    public void OnClickTakeLevelNavigation()
    {
        Vector3 targetPos = new Vector3(takeTargetScrollPos, PassListScrollVeiw.transform.localPosition.y, PassListScrollVeiw.transform.localPosition.z);
        SpringPanel.Begin(PassListScrollVeiw.panel.cachedGameObject, targetPos, 8f).onFinished = () => { bDrag = false; };
        bDrag = true;
    }
    
    public void OnClickBuyGoldPassButton()
    {
        PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common);
        popup.Set(PopupCommon.Type.TwoBtn, SSLocalization.Format("Shop", 40, SSLocalization.Get("newShop", SoulPassData.packageinfo.detailcsv)),
            () =>
            {
                ManagerCS.instance.PurchaseProduct(CashProductCategory.SoulPass, SoulPassData.packageinfo.basicmanidx);
            });
    }

    internal override void OnRefreshSoulPassReward()
    {
        RefreshRewardList();
    }

    internal override void OnUpdatePackageProduct(CashProductCategory category, int pidx)
    {
        if (category != CashProductCategory.SoulPass)
            return;

        SetPackageInfo();
        RefreshRewardList();
    }

    internal override void OnUpdateGameContentsTimer()
    {
        if (bEnd)
            return;

        TimeSpan span = new TimeSpan(0, 0, SoulPassData.state.remaintimesec);

        if (span.Days > 0)
            PassRemainTime.text = SSLocalization.Format("SoulPass", 1, span.Days, span.Hours, span.Minutes);    // 남은 시간 : {0}D {1:00.#}:{2:00.#}
        else
            PassRemainTime.text = SSLocalization.Format("SoulPass", 2, span.Hours, span.Minutes, span.Seconds); // 남은 시간 : {0:00.#}:{1:00.#}:{2:00.#}

        if (SoulPassData.state.remaintimesec <= 0)
        {
            PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common, true, false);
            popup.Set(PopupCommon.Type.OneBtn, SSLocalization.Get("SoulPass", 11), () => { ManagerCS.instance.StartNavigation(NavigationType.MainLobby); });

            bEnd = true;
        }
    }
}
