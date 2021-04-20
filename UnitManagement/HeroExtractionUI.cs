using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using ServerCommon;
using ServerCommon.Tutorials;
using Newtonsoft.Json;

public class HeroExtractionUI : GameNotification
{
    // Data
    private HeroCompanion HeroCompanionData;

    private List<HeroCompanion> HeroList;               // 영웅 리스트
    private List<Icon_HeroList> HeroIconList;           // 영웅 아이콘 리스트
    private List<Icon_HeroList> ExtractionHeroIconList; // 추출 영웅 리스트
    
    private enum RefreshType
    {
        Add,
        Remove,
        Clear,
    }


    // UI - Center
    [Serializable]
    public class ExtractionSlot
    {
        private Icon_HeroList icon;
        public HeroCompanion heroData { get { return (icon != null) ? icon.HeroCompanionData : null; } }

        public Transform slot;
        public GameObject slotEffect;
        public GameObject extractionEffect;
        public GameObject warningEffect;


        public void SetSlot(Icon_HeroList listicon, bool isAddSetting)
        {
            icon = listicon;
            icon.transform.parent = slot;

            if (isAddSetting)
            {
                // Add Setting
                icon.transform.localPosition = Vector3.zero;
                icon.transform.localScale = Vector3.one;

                TweenPosition tweenPos = slot.GetComponent<TweenPosition>();
                TweenAlpha tweenAlp = slot.GetComponent<TweenAlpha>();

                EventDelegate.Set(tweenPos.onFinished,
                    () =>
                    {
                        // Effect On
                        OnSlotEffect();

                        if (icon.HeroBaseData.grade >= GameConstraints.HeroHighGrade || icon.HeroCompanionData.isWearEquipment)
                            OnWarningEffect();
                        else
                            OffWarningEffect();
                    });

                tweenPos.ResetToBeginning();
                tweenPos.PlayForward();

                tweenAlp.ResetToBeginning();
                tweenAlp.PlayForward();
            }
            else if (icon.transform.localPosition != slot.localPosition)
            {
                // Refresh Setting
                TweenPosition tweenPos = icon.GetComponent<TweenPosition>();
                tweenPos.from = icon.transform.localPosition;
                tweenPos.to = slot.localPosition;
                tweenPos.duration = 0.1f;

                tweenPos.ResetToBeginning();
                tweenPos.PlayForward();

                // Effect Off
                OffSlotEffect();

                EventDelegate.Set(tweenPos.onFinished,
                    () =>
                    {
                        if (icon.HeroBaseData.grade >= GameConstraints.HeroHighGrade || icon.HeroCompanionData.isWearEquipment)
                            OnWarningEffect();
                        else
                            OffWarningEffect();
                    });
            }

            OffExtractionEffect();
        }

        public void ClearSlot()
        {
            icon = null;

            // Effect Off
            OffSlotEffect();
            OffWarningEffect();
        }

        public void OnExtractionEffect()
        {
            OffExtractionEffect();
            
            extractionEffect.SetActive(true);
        }

        public void OffExtractionEffect()
        {
            if (extractionEffect.activeSelf)
                extractionEffect.SetActive(false);
        }

        private void OnSlotEffect()
        {
            OffSlotEffect();
            
            slotEffect.SetActive(true);
        }

        private void OffSlotEffect()
        {
            if (slotEffect.activeSelf)
                slotEffect.SetActive(false);
        }

        private void OnWarningEffect()
        {
            warningEffect.SetActive(true);
        }

        private void OffWarningEffect()
        {
            warningEffect.SetActive(false);
        }
    }
    public List<ExtractionSlot> ExtractionSlotList;

    public UIGrid GetRewardListGrid;
    public UIButton ExtractionButton;

    // UI - Right
    public UnitListScrollView HeroListScrollView;
    public HeroSortingButton SortingButton;
    public UIButton FilteringButton;
    public UILabel HeroStorageCount;


    // UI - Effect & Animation
    public GameObject UILockPanel;



    private void Awake()
    {
        HeroIconList = new List<Icon_HeroList>();
        ExtractionHeroIconList = new List<Icon_HeroList>();

        TutorialManager.instance.StartTutorial(TutorialID.Extraction);
    }

    protected override void Init()
    {
        SetHeroList();
        SetSelectHero();
        SetSortingButton();
        SetHeroStorageCount();

        UILockPanel.SetActive(false);
    }

    private void SetHeroList()
    {
        SetHeroList(GameData.Heroes);
    }

    private void SetHeroList(List<HeroCompanion> list)
    {
        HeroList = list;

        for (int i = 0; i < HeroIconList.Count; i++)
        {
            DestroyImmediate(HeroIconList[i].gameObject);
        }
        HeroIconList.Clear();

        // Hero List Setting
        for (int i = 0; i < HeroList.Count; i++)
        {
            // 이미 리스트에 포함되어있는 영웅이면 continue - (필터추가로 인해 UI 내에서 리스트의 재생성이 일어날 수 있음)
            if (ExtractionHeroIconList.Find(row => row.HeroCompanionData.manidx == HeroList[i].manidx) != null)
                continue;

            if (HeroList[i].c_base.bSpecial == HeroSpecialType.ImprintType)
                continue;

            Icon_HeroList icon = ManagerCS.instance.MakeIcon<Icon_HeroList>(HeroListScrollView.grid, ManagerCS.eIconType.HeroList, true);
            icon.SetIcon(HeroList[i]);
            icon.SetCallBack(SelectHeroListIcon);

            // 팀 참가중이거나 잠금상태거나 아르메스탐험중이면 비활성화
            bool bDisable = HeroList[i].c_jointeam != 0 ||
                            HeroList[i].c_lock ||
                            HeroList[i].isJoinArmes ||
                            HeroList[i].isOccupationDefenseUnit;

            EventDelegate.Callback disableCallBack = ManagerCS.instance.GetCallbackCheckDisableHero(HeroList[i]);
            icon.SetDisable(bDisable, disableCallBack);

            HeroIconList.Add(icon);
        }

        HeroListScrollView.SetCompare(SortingClass.CompareHeroList);

        ExtractionButton.isEnabled = (ExtractionHeroIconList.Count > 0);

        ManagerCS.instance.SetFilteringButtonColor(ref FilteringButton);
    }

    private void SelectHeroListIcon(Icon_HeroList icon)
    {
        if (icon == null)
            return;
        
        if (ExtractionHeroIconList.Count >= GameConstraints.HeroExtractionLimit)
            return;
        
        AddExtractionListItem(icon);
    }
    
    private void AddExtractionListItem(Icon_HeroList icon)
    {
        if (icon == null)
            return;

        // 재료 슬롯 갱신
        RefreshExtractionList(RefreshType.Add, icon.HeroCompanionData);

        // 영웅 리스트 아이콘 삭제
        HeroIconList.Remove(icon);
        DestroyImmediate(icon.gameObject);

        // 영웅 리스트 갱신
        HeroListScrollView.Refresh();
    }

    private void RemoveExtractionListItem(Icon_HeroList icon)
    {
        if (icon == null)
            return;

        // 필터 체크 (필터에 속하지 않은 영웅이면 리스트에 추가하지 않는다)
        if (ManagerCS.instance.CheckFilteringHero(icon.HeroCompanionData))
        {
            // 영웅 리스트 아이콘 생성
            Icon_HeroList listIcon = ManagerCS.instance.MakeIcon<Icon_HeroList>(HeroListScrollView.grid, ManagerCS.eIconType.HeroList, true);
            listIcon.SetIcon(icon.HeroCompanionData);
            listIcon.SetCallBack(SelectHeroListIcon);

            HeroIconList.Add(listIcon);
        }

        // 영웅 리스트 갱신
        HeroListScrollView.Refresh();
        
        // 재료 슬롯 갱신
        RefreshExtractionList(RefreshType.Remove, icon.HeroCompanionData);
    }

    private void ClearExtractionList()
    {
        for (int i = ExtractionHeroIconList.Count - 1; i >= 0; i--)
        {
            // 영웅 리스트 아이콘 생성
            Icon_HeroList listIcon = ManagerCS.instance.MakeIcon<Icon_HeroList>(HeroListScrollView.grid, ManagerCS.eIconType.HeroList, true);
            listIcon.SetIcon(ExtractionHeroIconList[i].HeroCompanionData);
            listIcon.SetCallBack(SelectHeroListIcon);

            HeroIconList.Add(listIcon);
        }

        // 영웅 리스트 갱신
        HeroListScrollView.Refresh();
        
        RefreshExtractionList(RefreshType.Clear);
    }
    
    private void RefreshExtractionList(RefreshType type, HeroCompanion targetHero = null)
    {
        // Set ExtractionList !
        switch (type)
        {
            case RefreshType.Add:
                {
                    // 아이콘 생성
                    Icon_HeroList icon = ManagerCS.instance.MakeIcon<Icon_HeroList>(gameObject, ManagerCS.eIconType.HeroList);
                    icon.SetIcon(targetHero);
                    icon.SetEquipmentImg();
                    icon.SetCallBack(RemoveExtractionListItem);
                    icon.gameObject.AddComponent<TweenPosition>();

                    // 리스트에 등록
                    ExtractionHeroIconList.Add(icon);

                    // 슬롯 세팅
                    ExtractionSlotList[ExtractionHeroIconList.Count - 1].SetSlot(icon, true);
                }
                break;

            case RefreshType.Remove:
                {
                    // 아이콘 찾기
                    Icon_HeroList icon = ExtractionHeroIconList.Find(row => row.HeroCompanionData == targetHero);

                    // 리스트에서 삭제 및 아이콘 삭제
                    ExtractionHeroIconList.Remove(icon);
                    Destroy(icon.gameObject);
                    
                    // 슬롯 세팅
                    for (int i = 0; i < ExtractionSlotList.Count; i++)
                    {
                        if (ExtractionHeroIconList.Count > i)
                            ExtractionSlotList[i].SetSlot(ExtractionHeroIconList[i], false);
                        else
                            ExtractionSlotList[i].ClearSlot();
                    }
                }
                break;

            case RefreshType.Clear:
                {
                    // 아이콘 삭제
                    for (int i = ExtractionHeroIconList.Count - 1; i >= 0; i--)
                    {
                        Destroy(ExtractionHeroIconList[i].gameObject);
                    }

                    // 리스트 클리어
                    ExtractionHeroIconList.Clear();

                    // 슬롯 세팅
                    for (int i = 0; i < ExtractionSlotList.Count; i++)
                    {
                        ExtractionSlotList[i].ClearSlot();
                    }
                }
                break;
        }

        ExtractionButton.isEnabled = (ExtractionHeroIconList.Count > 0);

        // Get Reward !
        List<RewardValueDesc> rewardList = new List<RewardValueDesc>();

        for (int i = 0; i < ExtractionHeroIconList.Count; i++)
        {
            TableManagerCS.instance.HeroExtractionTable.CalcResultRewardGroup(ref rewardList, ExtractionHeroIconList[i].HeroCompanionData);
        }

        // Reward Sort !
        rewardList.Sort(
            (a, b) =>
            {
                if (a.type > b.type)
                    return 1;
                else if (a.type < b.type)
                    return -1;

                if (a.v1 > b.v1)
                    return 1;
                else if (a.v1 < b.v1)
                    return -1;

                return 0;
            });

        // Set Reward !
        GetRewardListGrid.transform.DestroyChildren();

        RewardValueDesc reward = null;
        for (int i = 0; i < rewardList.Count; i++)
        {
            reward = rewardList[i];

            Icon_ETC icon = ManagerCS.instance.MakeIcon<Icon_ETC>(GetRewardListGrid.gameObject, ManagerCS.eIconType.ETC);
            icon.SetRewardETCInfo(reward.type, reward.v1, reward.v2, reward.v3);
            icon.SettingType(ManagerCS.eSettingType.Reward_Type);
            icon.SetPopup();
        }

        GetRewardListGrid.enabled = true;
        GetRewardListGrid.Reposition();
    }

    public void OnClickClearSlotButton()
    {
        ClearExtractionList();
    }

    public void OnClickExtractionButton()
    {
        //ExtractionHeroIconList

        bool isIncludeHighGrade = false;
        bool isIncludeEquipmentWear = false;
        for (int i = 0; i < ExtractionHeroIconList.Count; i++)
        {
            if (ExtractionHeroIconList[i].HeroBaseData.grade >= GameConstraints.HeroHighGrade)
            {
                isIncludeHighGrade = true;
            }

            if (ExtractionHeroIconList[i].HeroCompanionData.isWearEquipment)
            {
                isIncludeEquipmentWear = true;
            }
        }

        string msg = string.Empty;

        if (isIncludeHighGrade && isIncludeEquipmentWear)
            // ("heroInfo", 283) => 높은 등급의 영웅과 장비를 장착한 영웅이 있습니다.\n영혼추출을 진행 하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 283);
        else if (isIncludeHighGrade)
            // ("heroInfo", 284) => 높은등급의 영웅이 포함되어있습니다.\n영혼추출을 진행 하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 284);
        else if (isIncludeEquipmentWear)
            // ("heroInfo", 285) => 장비를 장착중인 영웅이 포함되어있습니다.\n영혼추출을 진행 하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 285);
        else
            // ("heroInfo", 252) => 영혼추출을 진행 하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 252);
        

        PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common);
        popup.Set(PopupCommon.Type.TwoBtn, msg, ReqExtractionHero);
    }

    private void ReqExtractionHero()
    {
        ExtractionHeroRequestParameter reqData = new ExtractionHeroRequestParameter();

        foreach (Icon_HeroList icon in ExtractionHeroIconList)
        {
            reqData.heromanidxlist.Add(icon.HeroCompanionData.manidx);
        }
        
        reqData.dfevtidx = ManagerCS.Getdfevtidx();
        reqData.bProgressDF = ManagerCS.GetbProgressDF();
        
        NetworkConnection.instance.NetCommand(PacketType.ExtractionHeroRequestParameter, reqData,
            (string revData) =>
            {
                if (NetworkCS.CheckNetworkError())
                {
                    ExtractionHeroResultParameter result = JsonConvert.DeserializeObject<ExtractionHeroResultParameter>(revData);

                    ManagerCS.instance.SetReceiveFinaceDataToUI(result.finance);
                    ManagerCS.instance.SetReceiveMissionDataToUI(result.successMissionList);
                    ManagerCS.instance.SetReceiveGuildMissionDataToUI(result.successGuildMissionList);

                    // 추출된 영웅 제거
                    foreach (long manidx in result.erasedheromanidxlist)
                    {
                        ManagerCS.instance.uiController.RemoveHeros(manidx);
                    }

                    // 재료 아이템 갱신
                    ManagerCS.instance.uiController.GetSecondFinanceItem(result.sync_SecFinances);

                    if (ManagerCS.instance.uiController.UIFinance != null)
                        ManagerCS.instance.uiController.UIFinance.SetSecondFinanceData();

                    // 보상 데이터 정리...
                    List<RewardValueDesc> rewardList = new List<RewardValueDesc>();

                    for(int i=0; i< result.rlist.Count; i++)
                    {
                        RewardValueDesc reward = null;

                        if (result.rlist[i].type == RewardType.Gold)
                        {
                            reward = rewardList.Find(row => row.type == result.rlist[i].type);

                            if (reward == null)
                                rewardList.Add(result.rlist[i]);
                            else
                                reward.v1 += result.rlist[i].v1;
                        }

                        if (result.rlist[i].type == RewardType.FinanceItem)
                        {
                            reward = rewardList.Find(row => row.type == RewardType.FinanceItem && row.v1 == result.rlist[i].v1);

                            if (reward == null)
                                rewardList.Add(result.rlist[i]);
                            else
                                reward.v2 += result.rlist[i].v2;
                        }
                    }
                    
                    StartCoroutine(StartExtractionEffect(
                    () =>
                    {
                        // 미션 알림 팝업
                        ManagerCS.instance.MissionNoticePopup();

                        // 보상 팝업 영혼추출 성공
                        PopupManager.instance.MakePopup<PopupDetailReward>(ePopupType.DetailReward).Set(rewardList, SSLocalization.Format("heroInfo", 281));

                        // UI 갱신
                        SetHeroStorageCount();
                        RefreshExtractionList(RefreshType.Clear);

                        // 타겟히어로 추출했으면 타겟 제거
                        if (ManagerCS.instance.targetHeroData != null &&
                            result.erasedheromanidxlist.Contains(ManagerCS.instance.targetHeroData.manidx))
                        {
                            ManagerCS.instance.targetHeroData = null;
                        }

                    }));
                }
            });
    } 

    public IEnumerator StartExtractionEffect(voidDelegate callback)
    {
        // UI Lock
        ManagerCS.instance.IsTopUILock = true;
        UILockPanel.SetActive(true);
        
        // Extraction Slot Effect
        for (int i = 0; i < ExtractionSlotList.Count; i++)
        {
            if (ExtractionSlotList[i].heroData != null)
            {
                ExtractionSlotList[i].OnExtractionEffect();
            }
        }

        SoundPlay.instance.SetSoundPlay(2130, SOUND_TYPE.EFFECT_TYPE, 0);

        yield return new WaitForSeconds(0.5f);

        // 아이콘 삭제
        for (int i = ExtractionHeroIconList.Count - 1; i >= 0; i--)
        {
            Destroy(ExtractionHeroIconList[i].gameObject);
        }

        // 리스트 클리어
        ExtractionHeroIconList.Clear();

        // 슬롯 세팅
        for (int i = 0; i < ExtractionSlotList.Count; i++)
        {
            ExtractionSlotList[i].ClearSlot();
        }

        yield return new WaitForSeconds(1.5f);

        // UI UnLock
        ManagerCS.instance.IsTopUILock = false;
        UILockPanel.SetActive(false);

        // CallBack
        if (callback != null)
            callback();
    }

    private void SetSelectHero()
    {
        if (ManagerCS.instance.targetHeroData == null)
            return;

        // 판매선택한 영웅 자동으로 슬롯에 장착
        Icon_HeroList targetIcon = HeroIconList.Find(row => row.HeroCompanionData == ManagerCS.instance.targetHeroData);

        if (targetIcon != null)
            AddExtractionListItem(targetIcon);
    }

    private void SetSortingButton()
    {
        SortingButton.Init(SortHeroList);
        SortingButton.Sort();
    }

    private void SortHeroList(HeroSortingOption option, bool bJoinTeamOption, bool bCompressionMode, bool bResetScroll)
    {
        SortingClass.SetHeroSortingOption(option, bJoinTeamOption);
        HeroListScrollView.SetList(bCompressionMode);

        if (HeroIconList.Count == 0)
            return;

        HeroIconList.Sort(SortingClass.CompareHeroList);
        HeroListScrollView.TargetOn(HeroIconList[0].transform, true);
    }

    public void OnClickHeroFilteringButton()
    {
        PopupManager.instance.MakePopup<PopupHeroFiltering>(ePopupType.HeroFiltering).Initialize(GameData.Heroes, HeroFilteringCallBack);
    }

    private void HeroFilteringCallBack(List<HeroCompanion> list)
    {
        if (list == null)
            return;

        ManagerCS.instance.targetHeroData = null;

        SetHeroList(list);
        SetSortingButton();
    }

    private void SetHeroStorageCount()
    {
        HeroStorageCount.text = string.Format("[b]{0} / {1}[/b]", GameData.Heroes.Count, GameData.HeroMaxCapacity);
    }

    public void OnClickExpandHeroStorageButton()
    {
        ManagerCS.instance.ExpandHeroStoragePopup();
    }

    internal override void OnUpdateExpandHeroStorage()
    {
        SetHeroStorageCount();
    }

    // 영웅리스트 갱신
    internal override void OnUpdateHeroList()
    {
        SetHeroList();
        SetSortingButton();
        SetHeroStorageCount();
    }
}
