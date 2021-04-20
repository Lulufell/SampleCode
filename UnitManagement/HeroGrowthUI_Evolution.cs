using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ServerCommon;
using ServerCommon.DataTables;
using ServerCommon.Tutorials;
using Newtonsoft.Json;


public partial class HeroGrowthUI
{
    private enum eMaterialType
    {
        None,               // 타입없음
        ExpObtain,          // 강화 타입
        Evolution,          // 진화 타입
        Imprint,            // 각인 타입
        ImprintEvolution,   // 각인진화 타입
    }
    private eMaterialType evolutionSlotType;
    private EvolutionRule HeroEvolutionRule;

    private void SetEvolutionUI()
    {
        HeroEvolutionRule = TableManagerCS.instance.HeroEvolutionTable.GetEvolutionRule(HeroBaseData.uidx);

        MaxMaterialHeroCount = HeroBaseData.grade;
        Color color;
        for (int i = 0; i < SlotList.Length; i++)
        {
            color = SlotList[i].SlotImg.color;
            SlotList[i].SlotImg.color = GameUtils.GetColor(255, 255, 255, (byte)((i < MaxMaterialHeroCount) ? 200 : 50));
        }

        ExpBar.gameObject.SetActive(false);
        
        SetEvolutionHeroList();
        SetEvolutionSortingButton();
        SetEvolutionButton();

        CalcEvolutionResult();

        TutorialManager.instance.StartTutorial(TutorialID.Evolution);
    }

    private void SetEvolutionHeroList()
    {
        SetEvolutionHeroList(GameData.Heroes);
    }
    
    private void SetEvolutionHeroList(List<HeroCompanion> list)
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
            if (MaterialHeroIconList.Find(row => row.HeroCompanionData.manidx == HeroList[i].manidx) != null)
                continue;

            // 진화 베이스 영웅이면 continue
            if (HeroList[i].manidx == HeroCompanionData.manidx)
                continue;
            
            // 강화전용 영웅이면
            if (HeroList[i].c_base.bSpecial == HeroSpecialType.ExpObtainType)
                continue;

            // 각인전용 영웅이고 타겟영웅이 최대각인이면
            bool isImprintHero = (HeroList[i].c_base.bSpecial == HeroSpecialType.ImprintType);
            bool isMaxImprint = TableManagerCS.instance.HeroImprintTable.isMaxLv(HeroCompanionData);
            
            if (isImprintHero)
            {
                if (isMaxImprint)
                    continue;

                if (HeroBaseData.bornGrade < GameConstraints.HeroImprint_ExclusiveUseHeroTargetBornGrade)
                    continue;
            }
            
            // 진화 베이스 영웅과 등급이 다르면 continue
            // 등급이 달라도 각인 가능하면 리스트에 추가
            if ((HeroList[i].c_base.grade != HeroBaseData.grade) &&
                (HeroList[i].c_base.imprintGroup == 0 || HeroList[i].c_base.limitGroup != HeroBaseData.limitGroup) &&
                (isImprintHero == false))
            {
                continue;
            }
            
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

            // 각인 가능 영웅인지?
            if (isImprintHero)
                icon.SetEnableImprint();
            else if (HeroBaseData.imprintGroup != 0 && HeroBaseData.limitGroup == HeroList[i].c_base.limitGroup)
                icon.SetEnableImprint();
            
            HeroIconList.Add(icon);
        }

        CheckMaterialHeroCount();

        HeroListScrollView.SetCompare(SortingClass.CompareGrowthList_Evolution);

        ManagerCS.instance.SetFilteringButtonColor(ref FilteringButton);
    }

    private void SetEvolutionSortingButton()
    {
        SortingButton.Init(SortEvolutionHeroList);
        SortingButton.Sort();
    }

    private void SortEvolutionHeroList(HeroSortingOption option, bool bJoinTeamOption, bool bCompressionMode, bool bResetScroll)
    {
        SortingClass.SetHeroSortingOption(option, bJoinTeamOption);
        HeroListScrollView.SetList(bCompressionMode);

        if (HeroIconList.Count == 0)
            return;

        HeroIconList.Sort(SortingClass.CompareGrowthList_Evolution);
        HeroListScrollView.TargetOn(HeroIconList[0].transform, true);
    }

    public void CalcEvolutionResult()
    {
        CostType = HeroEvolutionRule.costtype;
        CostValue = (MaterialHeroIconList.Count >= 1) ? HeroEvolutionRule.v1 : 0;

        CostTypeIcon.spriteName = ManagerCS.instance.FindSpriteIconName(CostType);
        CostValueLabel.text = CostValue.ToString("#,##0");
        
        int resultidx = HeroEvolutionRule.result;

        HeroCompanion resultHero = new HeroCompanion();
        resultHero.CopyFrom(HeroCompanionData);
        resultHero.idx = resultidx;
        resultHero.c_base = TableManagerCS.instance.HeroTable.FindHeroBase(resultidx);

        CalcImprintLevel(ref resultHero);

        SetResultHeroInfo(resultHero);
    }
    
    private void SetEvolutionButton()
    {
        // ("heroInfo", 130) => [b]진화
        ButtonLabel.text = SSLocalization.Get("heroInfo", 130);
        EventDelegate.Set(ExecuteButton.onClick, OnClickEvolutionButton);
    }

    private void OnClickEvolutionButton()
    {
        bool isIncludeEquipmentWear = false;
        for (int i = 0; i < MaterialHeroIconList.Count; i++)
        {
            if (MaterialHeroIconList[i].HeroCompanionData.isWearEquipment)
            {
                isIncludeEquipmentWear = true;
                break;
            }
        }

        string msg = string.Empty;

        if (isIncludeEquipmentWear)
            // ("heroInfo", 282) => 장비를 장착중인 영웅이 포함되어있습니다.\n진화하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 282);
        else
            // ("heroInfo", 235) => 진화하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 235);

        PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common);
        popup.Set(PopupCommon.Type.Payment, msg, ReqHeroEvolution);
        popup.SetPayment(CostType, CostValue);
    }
    
    private void ReqHeroEvolution()
    {
        if (!ManagerCS.instance.CheckEnoughCost(CostType, CostValue))
            return;

        EvolutionHeroRequestParameter data = new EvolutionHeroRequestParameter();
        data.baseheromanidx = HeroCompanionData.manidx;
        for (int i = 0; i < MaterialHeroIconList.Count; i++)
        {
            data.materialheromanidxlist.Add(MaterialHeroIconList[i].HeroCompanionData.manidx);
        }
        data.bSetTeam = false;
        data.settings = null;
        data.lastteamidx = ManagerCS.instance.UsingTeamSquadIdx;
        data.dfevtidx = ManagerCS.Getdfevtidx();
        data.bProgressDF = ManagerCS.GetbProgressDF();

        NetworkConnection.instance.NetCommand(PacketType.EvolutionHeroRequestParameter, data, RevHeroEvolution);
    }

    private void RevHeroEvolution(string data)
    {
        if (NetworkCS.CheckNetworkError())
        {
            EvolutionHeroResultParameter result = JsonConvert.DeserializeObject<EvolutionHeroResultParameter>(data);

            ManagerCS.instance.SetReceiveFinaceDataToUI(result.finance);
            ManagerCS.instance.SetReceiveMissionDataToUI(result.successMissionList);
            ManagerCS.instance.SetReceiveGuildMissionDataToUI(result.successGuildMissionList);

            if (!ManagerCS.instance.IsDiscountEventSync(DiscountCategory.HERO, PacketType.EvolutionHeroRequestParameter, result.bSync, result.dfInfo, null))
                return;

            StartCoroutine(StartEvolutionEffect(
                () =>
                {
                    // 기존영웅 정보 저장
                    HeroCompanion baseHeroData = new HeroCompanion();
                    baseHeroData.CopyFrom(HeroCompanionData);

                    // 진화영웅 선택 및 재료로 사용된 영웅 제거
                    HeroCompanion resultHeroData = new HeroCompanion();
                    for (int i = 0; i < result.updateheroes.Count; i++)
                    {
                        ManagerCS.instance.uiController.RemoveHeros(result.updateheroes[i].manidx);

                        if (result.updateheroes[i].manidx == result.resultmanidx)
                            resultHeroData.CopyFrom(result.updateheroes[i]);
                    }

                    if (resultHeroData != null)
                    {
                        resultHeroData.c_jointeam = HeroCompanionData.c_jointeam;
                        resultHeroData.c_joinslot = HeroCompanionData.c_joinslot;
                    }

                    // 진화한 영웅정보 갱신
                    ManagerCS.instance.uiController.GetHero(resultHeroData);
                    ManagerCS.instance.RefreshHeroBattlePower(resultHeroData.manidx);   // 영웅 전투력 갱신

                    // 방송하기
                    ChattingConnection.instance.BroadCastGetEvolutionHero(resultHeroData.idx);

                    // 미션 알림 팝업
                    ManagerCS.instance.MissionNoticePopup();

                    // 각인 결과 팝업
                    voidDelegate imprintDelegate =
                        () =>
                        {
                            // 각인 결과 팝업
                            if (resultHeroData.imprintStep > baseHeroData.imprintStep)
                            {
                                if (baseHeroData.c_base.imprintGroup == 0)
                                    return;

                                PopupHeroImprintResult imprintPopup = PopupManager.instance.MakePopup<PopupHeroImprintResult>(ePopupType.HeroImprintResult);
                                imprintPopup.Set(baseHeroData.c_base.imprintGroup, baseHeroData.c_base.bornGrade, baseHeroData.imprintStep, resultHeroData.imprintStep);
                            }
                        };

                    // 진화 결과 팝업
                    PopupHeroEvolutionResult evolutionPopup = PopupManager.instance.MakePopup<PopupHeroEvolutionResult>(ePopupType.HeroEvolutionResult);
                    evolutionPopup.Set(baseHeroData, resultHeroData, imprintDelegate);

                    // UI 갱신
                    ManagerCS.instance.targetHeroData = resultHeroData;
                    ManagerCS.instance.uiController.State = UIState.HeroExpObtain;
                    SetUI();

                }));
        }
    }

    private IEnumerator StartEvolutionEffect(voidDelegate callback)
    {
        // UI Lock
        ManagerCS.instance.IsTopUILock = true;
        UILockPanel.SetActive(true);

        for (int i = 0; i < MaterialHeroIconList.Count; i++)
        {
            if (SlotList[i].ConsumeEffect.activeSelf)
                SlotList[i].ConsumeEffect.SetActive(false);

            SlotList[i].ConsumeEffect.SetActive(true);

            if (i == 0)
            {
                SoundPlay.instance.SetSoundPlay(2006, SOUND_TYPE.EFFECT_TYPE);
                SoundPlay.instance.SetSoundPlay(2146, SOUND_TYPE.EFFECT_TYPE);
            }

            yield return new WaitForSeconds(0.2f);
            SoundPlay.instance.SetSoundPlay(1042, SOUND_TYPE.EFFECT_TYPE, 0.22f);

            MaterialHeroIconList[i].gameObject.SetActive(false);
            SlotList[i].WarningEffect.SetActive(false);
        }

        yield return new WaitForSeconds(0.5f);

        // UI Unlock
        ManagerCS.instance.IsTopUILock = false;
        UILockPanel.SetActive(false);

        // CallBack
        if (callback != null)
            callback();
    }
}
