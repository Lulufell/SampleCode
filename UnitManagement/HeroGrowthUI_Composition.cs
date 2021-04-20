using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ServerCommon;
using ServerCommon.DataTables;
using Newtonsoft.Json;


public partial class HeroGrowthUI
{

    private CompositionRule HeroCompositionRule;


    private void SetCompositionUI()
    {
        HeroCompositionRule = TableManagerCS.instance.HeroCompositionTable.GetCompositionRule(HeroBaseData.grade);

        MaxMaterialHeroCount = HeroCompositionRule.mherocnt;
        Color color;
        for (int i = 0; i < SlotList.Length; i++)
        { 
            color = SlotList[i].SlotImg.color;
            SlotList[i].SlotImg.color = GameUtils.GetColor(255, 255, 255, (byte)((i < MaxMaterialHeroCount) ? 200 : 50));
        }

        ExpBar.gameObject.SetActive(false);

        SetCompositionHeroList();
        SetCompositionSortingButton();
        SetCompositionButton();

        CalcCompositionResult();
    }

    private void SetCompositionHeroList()
    {
        SetCompositionHeroList(GameData.Heroes);
    }

    private void SetCompositionHeroList(List<HeroCompanion> list)
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

            // 합성 베이스 영웅이면 continue
            if (HeroList[i].manidx == HeroCompanionData.manidx)
                continue;

            // 특수 영웅이면 continue
            if (HeroList[i].c_base.bSpecial == HeroSpecialType.ExpObtainType || HeroList[i].c_base.bSpecial == HeroSpecialType.ImprintType)
                continue;
            
            // 강화 베이스 영웅과 등급이 다르면 continue
            if (HeroList[i].c_base.grade != HeroBaseData.grade)
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

        CheckMaterialHeroCount();

        HeroListScrollView.SetCompare(SortingClass.CompareGrowthList_Composition);

        ManagerCS.instance.SetFilteringButtonColor(ref FilteringButton);
    }

    private void SetCompositionSortingButton()
    {
        SortingButton.Init(SortCompositionHeroList);
        SortingButton.Sort();
    }

    private void SortCompositionHeroList(HeroSortingOption option, bool bJoinTeamOption, bool bCompressionMode, bool bResetScroll)
    {
        SortingClass.SetHeroSortingOption(option, bJoinTeamOption);
        HeroListScrollView.SetList(bCompressionMode);

        if (HeroIconList.Count == 0)
            return;

        HeroIconList.Sort(SortingClass.CompareGrowthList_Composition);
        HeroListScrollView.TargetOn(HeroIconList[0].transform, true);
    }

    public void CalcCompositionResult()
    {
        CostType = HeroCompositionRule.costtype;
        CostValue = (MaterialHeroIconList.Count >= 1) ? HeroCompositionRule.v1 : 0;

        CostTypeIcon.spriteName = ManagerCS.instance.FindSpriteIconName(CostType);
        CostValueLabel.text = CostValue.ToString("#,##0");

        SetRandomResultHeroInfo();
    }

    private void SetCompositionButton()
    {
        // ("heroInfo", 344) => [b]합성
        ButtonLabel.text = SSLocalization.Get("heroInfo", 129);
        EventDelegate.Set(ExecuteButton.onClick, OnClickCompositionButton);
    }

    private void OnClickCompositionButton()
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
            // ("heroInfo", 345) => 장비를 장착중인 영웅이 포함되어있습니다.\n합성하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 345);
        else
            // ("heroInfo", 344) => 합성하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 344);

        PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common);
        popup.Set(PopupCommon.Type.Payment, msg, ReqHeroComposition);
        popup.SetPayment(CostType, CostValue);
    }

    private void ReqHeroComposition()
    {
        if (!ManagerCS.instance.CheckEnoughCost(CostType, CostValue))
            return;

        CompositionHeroRequestParameter reqData = new CompositionHeroRequestParameter();
        reqData.baseheromanidx = HeroCompanionData.manidx;
        for (int i = 0; i < MaterialHeroIconList.Count; i++)
        {
            reqData.materialheromanidxlist.Add(MaterialHeroIconList[i].HeroCompanionData.manidx);
        }
        reqData.bSetTeam = false;
        reqData.settings = null;
        reqData.lastteamidx = ManagerCS.instance.UsingTeamSquadIdx;
        reqData.dfevtidx = ManagerCS.Getdfevtidx();
        reqData.bProgressDF = ManagerCS.GetbProgressDF();

        NetworkConnection.instance.NetCommand(PacketType.CompositionHeroRequestParameter, reqData,
            (string revData) =>
            {
                if (NetworkCS.CheckNetworkError())
                {
                    CompositionHeroResultParameter result = JsonConvert.DeserializeObject<CompositionHeroResultParameter>(revData);
                    ManagerCS.instance.SetReceiveFinaceDataToUI(result.finance);
                    ManagerCS.instance.SetReceiveMissionDataToUI(result.successMissionList);

                    StartCoroutine(StartCompositionEffect(
                        () =>
                        {
                            // 기존영웅 정보 저장
                            HeroCompanion baseHeroData = new HeroCompanion();
                            baseHeroData.CopyFrom(HeroCompanionData);

                            // 합성영웅 선택 및 재료로 사용된 영웅 제거
                            HeroCompanion resultHeroData = new HeroCompanion();
                            for (int i = 0; i < result.updateheroes.Count; i++)
                            {
                                ManagerCS.instance.uiController.RemoveHeros(result.updateheroes[i].manidx);

                                if (result.updateheroes[i].manidx == result.resultmanidx)
                                    resultHeroData.CopyFrom(result.updateheroes[i]);
                            }


                            // 합성한 영웅정보 갱신
                            ManagerCS.instance.uiController.GetHero(resultHeroData);
                            ManagerCS.instance.RefreshHeroBattlePower(resultHeroData.manidx);
                            
                            // UI 처리
                            ManagerCS.instance.targetHeroData = resultHeroData;
                            OnUpdateHeroInfo();

                            // 합성 결과 팝업
                            PopupManager.instance.MakePopup<PopupHeroCompositionResult>(ePopupType.HeroCompositionResult).SetCompositionResult(resultHeroData,
                                () =>
                                {
                                    ManagerCS.instance.uiController.State = UIState.HeroExpObtain;
                                    SetUI();
                                });

                        }));
                }
            });
    }

    private IEnumerator StartCompositionEffect(voidDelegate callback)
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

    internal override void OnUpdateHeroInfo()
    {
        SetDetailInfo();
        TargetHeroSlot.gameObject.SetActive(false);

        SetRandomResultHeroInfo(false);
    }
}
