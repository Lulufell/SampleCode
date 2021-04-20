using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ServerCommon;
using Newtonsoft.Json;
using System;

public partial class HeroGrowthUI
{
    public UISlider ExpBar;
    public UILabel ExpLabel;
    public UILabel ObtainExpLabel;
    public UILabel IncreaseLevelLabel;
    public GameObject StatusUpEffect;
    
    private bool isMaxExp = false;

    public ExpGaugeAnimator ExpGaugeAnimator;

    private HeroExpObtainResultParameter HeroExpObtainResult = null;

    
    private void SetExpObtainUI()
    {
        MaxMaterialHeroCount = GameConstraints.MaxExpHeroRawCnt;
        Color color;
        for (int i = 0; i < SlotList.Length; i++)
        {
            color = SlotList[i].SlotImg.color;
            SlotList[i].SlotImg.color = GameUtils.GetColor(255, 255, 255, (byte)((i < MaxMaterialHeroCount) ? 200 : 50));
        }

        ExpBar.gameObject.SetActive(true);
        
        SetExpObtainHeroList();
        SetExpObtainSortingButton();
        SetExpObtainButton();

        CostType = RewardType.Gold;
        CostTypeIcon.spriteName = ManagerCS.instance.FindSpriteIconName(CostType);

        CalcExpObtainResult();
    }

    private void SetExpObtainHeroList()
    {
        SetExpObtainHeroList(GameData.Heroes);
    }

    private void SetExpObtainHeroList(List<HeroCompanion> list)
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
			// 20.05.08 Root.
			// 6성 만렙 캐릭터의 경우 각인이 가능한 영웅만 리스트에 표시
			bool isMaxLv = (HeroCompanionData.lv == HeroBaseData.maxlevel);
			bool isMaxGrade = (HeroBaseData.grade == GameConstraints.HeroMaxGrade);
			if (isMaxLv && isMaxGrade)
			{
				bool bEnableImprint = (HeroList[i].c_base.bSpecial == HeroSpecialType.ImprintType)
										|| (HeroList[i].c_base.imprintGroup != 0 && HeroList[i].c_base.limitGroup == HeroBaseData.limitGroup);
				if (!bEnableImprint)
					continue;
			}

			// 이미 리스트에 포함되어있는 영웅이면 continue - (필터추가로 인해 UI 내에서 리스트의 재생성이 일어날 수 있음)
			if (MaterialHeroIconList.Find(row => row.HeroCompanionData.manidx == HeroList[i].manidx) != null)
                continue;

            // 강화 베이스 영웅이면 continue
            if (HeroList[i].manidx == HeroCompanionData.manidx)
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

        HeroListScrollView.SetCompare(SortingClass.CompareGrowthList_ExpObtain);

        ManagerCS.instance.SetFilteringButtonColor(ref FilteringButton);
    }
    
    private void SetExpObtainSortingButton()
    {
        SortingButton.Init(SortExpObtainHeroList);
        SortingButton.Sort();
    }

    private void SortExpObtainHeroList(HeroSortingOption option, bool bJoinTeamOption, bool bCompressionMode, bool bResetScroll)
    {
        SortingClass.SetHeroSortingOption(option, bJoinTeamOption);
        HeroListScrollView.SetList(bCompressionMode);

        if (HeroIconList.Count == 0)
            return;

        HeroIconList.Sort(SortingClass.CompareGrowthList_ExpObtain);
        HeroListScrollView.TargetOn(HeroIconList[0].transform, true);
    }
    
    public void CalcExpObtainResult(bool isAnimation = false)
    {
        List<HeroCompanion> materialHeroList = new List<HeroCompanion>();

        if (!isAnimation)
        {
            for (int i = 0; i < MaterialHeroIconList.Count; i++)
            {
                materialHeroList.Add(MaterialHeroIconList[i].HeroCompanionData);
            }
        }

        int heroMaxLv = TableManagerCS.instance.HeroTable.GetHeroMaxLv(HeroCompanionData.idx);
        int obtainExp = TableManagerCS.instance.HeroTable.GetObtainExp(materialHeroList, HeroBaseData.synastry);

        // ("heroInfo", 64) => "경험치 +{0}"
        ObtainExpLabel.gameObject.SetActive(obtainExp > 0);
        ObtainExpLabel.text = (obtainExp > 0) ? SSLocalization.Format("heroInfo", 64, obtainExp) : string.Empty;

        HeroCompanion preHeroData = new HeroCompanion();
        preHeroData.CopyFrom(HeroCompanionData);

        HeroCompanion postHeroData = new HeroCompanion();
        postHeroData.CopyFrom(HeroCompanionData);
        postHeroData.c_base = HeroCompanionData.c_base;
        
        //int originalCostValue = HeroBaseData.expGold * MaterialHeroIconList.Count;
        //CostValue = ManagerCS.instance.GetDiscountPrice(originalCostValue);
        //ManagerCS.instance.SetDiscountEventLabel(CostValueLabel, CostValue, originalCostValue);

        CostValue = HeroBaseData.expGold * MaterialHeroIconList.Count;
        CostValueLabel.text = CostValue.ToString("#,##0");

        TableManagerCS.instance.HeroTable.GetIncreaseExp(ref postHeroData, obtainExp);
        
        if (postHeroData.lv < heroMaxLv)
        {
            isMaxExp = false;

            float resultNeedExp = TableManagerCS.instance.HeroTable.GetNeedExp(postHeroData.idx, postHeroData.lv);
            ExpLabel.text = string.Format("{0}/{1}", postHeroData.exp, resultNeedExp);
            ExpBar.value = postHeroData.exp / resultNeedExp;
        }
        else
        {
            isMaxExp = true;

            // ("heroInfo", 4) => "MAX"
            ExpLabel.text = SSLocalization.Format("heroInfo", 4);
            ExpBar.value = 1f;
        }

        if (!isAnimation)
        {
            IncreaseLevelLabel.text = string.Format("+{0}", (postHeroData.lv - preHeroData.lv));

            CalcImprintLevel(ref postHeroData);
        }

        SetResultHeroInfo(postHeroData);
    }

    private void SetExpObtainButton()
    {
        // ("heroInfo", 131) => [b]강화
        ButtonLabel.text = SSLocalization.Get("heroInfo", 131);
        EventDelegate.Set(ExecuteButton.onClick, OnClickExpObtainButton);
    }

    private void OnClickExpObtainButton()
    {
        bool isIncludeHighGrade = false;
        bool isIncludeEquipmentWear = false;
        for (int i = 0; i < MaterialHeroIconList.Count; i++)
        {
            if (MaterialHeroIconList[i].HeroBaseData.grade >= GameConstraints.HeroHighGrade)
            {
                isIncludeHighGrade = true;
            }

            if (MaterialHeroIconList[i].HeroCompanionData.isWearEquipment)
            {
                isIncludeEquipmentWear = true;
            }
        }

        string msg = string.Empty;

        if (isIncludeHighGrade && isIncludeEquipmentWear)
            // ("heroInfo", 282) => 높은 등급의 영웅과 장비를 장착한 영웅이 있습니다.\n강화를 진행 하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 231);
        else if (isIncludeHighGrade)
            // ("heroInfo", 233) => 높은등급의 영웅이 포함되어있습니다.\n강화하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 233);
        else if (isIncludeEquipmentWear)
            // ("heroInfo", 234) => 장비를 장착중인 영웅이 포함되어있습니다.\n강화하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 234);
        else
            // ("heroInfo", 232) => 강화하시겠습니까?
            msg = SSLocalization.Get("heroInfo", 232);


        PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common);
        popup.Set(PopupCommon.Type.Payment, msg, ReqHeroExpObtain);
        popup.SetPayment(CostType, CostValue);
    }

    
    private void ReqHeroExpObtain()
    {
        if (!ManagerCS.instance.CheckEnoughCost(CostType, CostValue))
            return;

        HeroExpObtainRequestParameter data = new HeroExpObtainRequestParameter();
        data.heromanidx = HeroCompanionData.manidx;
        for (int i = 0; i < MaterialHeroIconList.Count; i++)
        {
            data.consumedheromanidxlist.Add(MaterialHeroIconList[i].HeroCompanionData.manidx);
        }
        data.dfevtidx = ManagerCS.Getdfevtidx();
        data.bProgressDF = ManagerCS.GetbProgressDF();

        NetworkConnection.instance.NetCommand(PacketType.HeroExpObtainRequestParameter, data, RevHeroExpObtain);
    }

    private void RevHeroExpObtain(string data)
    {
        if (NetworkCS.CheckNetworkError())
        {
            HeroExpObtainResult = JsonConvert.DeserializeObject<HeroExpObtainResultParameter>(data);

            ManagerCS.instance.SetReceiveFinaceDataToUI(HeroExpObtainResult.finance);
            ManagerCS.instance.SetReceiveMissionDataToUI(HeroExpObtainResult.successMissionList);
            ManagerCS.instance.SetReceiveGuildMissionDataToUI(HeroExpObtainResult.successGuildMissionList);

            if (!ManagerCS.instance.IsDiscountEventSync(DiscountCategory.HERO, PacketType.EnchantingHeroRequestParameter, HeroExpObtainResult.bSync, HeroExpObtainResult.dfInfo, null))
                return;

            //HeroExpObtainResult.updateHeroInfo.c_base = TableManagerCS.instance.HeroTable.FindHeroBase(HeroExpObtainResult.updateHeroInfo.idx);

            StartCoroutine(StartExpObtainEffect(
                () =>
                {
                    // 기존영웅 정보 저장
                    HeroCompanion baseHeroData = new HeroCompanion();
                    baseHeroData.CopyFrom(HeroCompanionData);

                    // 강화영웅 선택 및 재료로 사용된 영웅 제거
                    HeroCompanion resultHeroData = new HeroCompanion();
                    resultHeroData.CopyFrom(HeroExpObtainResult.updateHeroInfo);

                    if (resultHeroData != null)
                        resultHeroData.c_joinslot = HeroCompanionData.c_joinslot;

                    // 강화한 영웅정보 갱신
                    ManagerCS.instance.uiController.GetHero(HeroExpObtainResult.updateHeroInfo);
                    ManagerCS.instance.RefreshHeroBattlePower(HeroExpObtainResult.updateHeroInfo.manidx);   // 영웅 전투력 갱신

                    // 재료로 사용된 영웅 제거
                    foreach (Numeric64 manidx in HeroExpObtainResult.erasedheromanidxlist)
                    {
                        ManagerCS.instance.uiController.RemoveHeros(manidx.value);
                    }
                    

                    // 결과 팝업 조건 체크
                    bool isTuto = (TutorialManager.instance.CheckTutorial(ServerCommon.Tutorials.TutorialID.Evolution) == false);
                    bool isMaxLv = (resultHeroData.lv == resultHeroData.c_base.maxlevel);
                    bool isMaxGrade = (resultHeroData.c_base.grade == GameConstraints.HeroMaxGrade);
                    bool isImprint = (resultHeroData.imprintStep > baseHeroData.imprintStep);


                    // 미션 알림 팝업 (최대레벨이 되어서 진화 UI로 변경되는 시점에 진화튜토리얼을 완료하지 않았으면 안띄워줌)
                    if (!(isMaxLv && isTuto))
                    {
                        ManagerCS.instance.MissionNoticePopup();

                        // 각인 결과 팝업
                        voidDelegate imprintDelegate =
                            () =>
                            {
								// 각인 결과 팝업
								if (isImprint)
								{
									if (baseHeroData.c_base.imprintGroup == 0)
										return;

									PopupHeroImprintResult imprintPopup = PopupManager.instance.MakePopup<PopupHeroImprintResult>(ePopupType.HeroImprintResult);
									imprintPopup.Set(HeroBaseData.imprintGroup, HeroBaseData.bornGrade, baseHeroData.imprintStep, resultHeroData.imprintStep);


									// 채팅 broadcast
									int maxImprintLv = TableManagerCS.instance.HeroImprintTable.GetMaxLv(resultHeroData.c_base.bornGrade);
									if (resultHeroData.imprintStep == maxImprintLv && resultHeroData.c_base.bornGrade > 3)
										ChattingConnection.instance.BroadCastGetHeroByMaxImprint(resultHeroData.idx, resultHeroData.imprintStep);

								}
                            };

                        // 강화 결과 팝업
                        if ((baseHeroData.lv < resultHeroData.lv) && (isMaxLv == true))
                        {
                            PopupCommon obtainPopup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common);

                            if (isMaxGrade)
                            {
                                obtainPopup.Set(PopupCommon.Type.OneBtn, SSLocalization.Get("heroInfo", 236), imprintDelegate);
                            }
                            else
                            {
                                obtainPopup.Set(PopupCommon.Type.OneBtn, SSLocalization.Get("heroInfo", 237), imprintDelegate);
                                ManagerCS.instance.uiController.State = UIState.HeroEvolution;
                            }
                        }
                        else
                        {
                            imprintDelegate();
                        }
                    }
                    else if (isMaxLv && !isMaxGrade)
                    {
                        ManagerCS.instance.uiController.State = UIState.HeroEvolution;
                    }
                    
                    // UI Refresh
                    ManagerCS.instance.targetHeroData = HeroExpObtainResult.updateHeroInfo;
                    SetUI();

                }));
        }
    }

    private IEnumerator StartExpObtainEffect(voidDelegate callback)
    {
        int step = 1;

        while (true)
        {
            yield return null;

            switch (step)
            {
                case 1:
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

                        step = (GrowthUIType == HeroGrowthUIType.ExpObtain) ? (step + 1) : 4;
                    }
                    break;

                case 2:
                    {
                        // Setting
                        CalcExpObtainResult(true);

                        ExpGaugeAnimator.SetData(HeroCompanionData, HeroExpObtainResult.updateHeroInfo,

                            // Level Up CallBack
                            () =>
                            {
                                if (StatusUpEffect.activeSelf)
                                    StatusUpEffect.SetActive(false);

                                StatusUpEffect.SetActive(true);
                            });

                        ++step;
                    }
                    break;

                case 3:
                    {
                        if (ExpGaugeAnimator.bAnimationComplete)
                        {
                            if (HeroExpObtainResult.updateHeroInfo.lv == HeroExpObtainResult.updateHeroInfo.c_base.maxlevel)
                            {
                                // ("heroInfo", 4) => "MAX"
                                ExpLabel.text = SSLocalization.Format("heroInfo", 4);
                                ExpBar.value = 1f;
                            }

                            ++step;
                        }
                    }
                    break;

                case 4:
                    {
                        // UI UnLock
                        ManagerCS.instance.IsTopUILock = false;
                        UILockPanel.SetActive(false);

                        // CallBack
                        if (callback != null)
                            callback();

                        // End Coroutine
                        yield break;
                    }
            }
        }
    }

    private void SetExpObtainTutorial()
    {
        Icon_HeroList tutoHero = HeroIconList.Find(row => row.HeroBaseData.uidx == GameConstraints.TutorialExpObtainHero);

        HeroListScrollView.TargetOn(tutoHero.transform);

        TutorialObject selectBtn = tutoHero.CallBack.gameObject.AddComponent<TutorialObject>();
        selectBtn.tag = "TUTORIAL";
        selectBtn.Key = "materialListIcon";
    }

    private void OnDisable()
    {
        StatusUpEffect.SetActive(false);
    }
}
