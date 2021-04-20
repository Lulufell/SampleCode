using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using ServerCommon;
using ServerCommon.DataTables;
using ServerCommon.Tutorials;
using Newtonsoft.Json;

public class HeroArousalUI : GameNotification
{
    // Data
    private HeroCompanion HeroCompanionData;
    private HeroBases HeroBaseData;
    private int CurrentArousalGrade;

    private RewardType CostType;
    private int CostValue;
    private List<RewardValueDesc> CostMaterialList;

    private int targetGrade = 0;
    

    // UI - Left
    public UISprite SynastryIcon;
    public UILabel SynastryLabel;
    public UISprite HeroTypeIcon;
    public UILabel HeroTypeLabel;

    public UILabel Title;
    public UILabel Name;
    public GradeObject Grade;

    public UIGrid MaterialGroupListGrid;

    // UI - Center
    [Serializable]
    public class ArousalSlot
    {
        private int grade;
        public int arousalGrade { get { return grade; } }
        private SynastryType type;

        public enum State
        {
            Complete,   // 각성된 슬롯
            Disable,    // 각성되지 않은 슬롯
            Possible,   // 각성 가능한 슬롯
            Lock,       // 각성 불가능한 슬롯 (등급부족, 레벨부족)
        }
        private State slotState;

        public UISprite SlotImg;
        public UISprite SlotNumberImg;
        public GameObject PossibleObj;
        public GameObject LockObj;
        public GameObject SelectObj;
        public GameObject AlarmObj;
        public GameObject EffectObj;
        public UIEventTrigger CallBack;
        
        public delegate void SelectSlot(int grade, State state);

        public void SetSlot(int grade, SynastryType type)
        {
            this.grade = grade;
            this.type = type;
        }
        
        public void SetState(State state, bool bAlarm = false)
        {
            slotState = state;
            
            switch (state)
            {
                case State.Complete:
                    {
                        SlotImg.color = GameUtils.GetArousalSlotColor(type);
                        SlotNumberImg.color = GameUtils.GetArousalTextColor(type);

                        PossibleObj.SetActive(false);
                        LockObj.SetActive(false);
                        SelectObj.SetActive(false);
                    }
                    break;

                case State.Disable:
                    {
                        SlotImg.color = GameUtils.GetArousalSlotColor(SynastryType.None);
                        SlotNumberImg.color = GameUtils.GetArousalTextColor(SynastryType.None);

                        PossibleObj.SetActive(false);
                        LockObj.SetActive(false);
                        SelectObj.SetActive(false);
                    }
                    break;

                case State.Possible:
                    {
                        SlotImg.color = GameUtils.GetArousalSlotColor(SynastryType.None);
                        SlotNumberImg.color = GameUtils.GetArousalTextColor(SynastryType.None);

                        PossibleObj.SetActive(true);
                        LockObj.SetActive(false);
                        SelectObj.SetActive(false);
                    }
                    break;

                case State.Lock:
                    {
                        SlotImg.color = GameUtils.GetArousalSlotColor(SynastryType.None);
                        SlotNumberImg.color = GameUtils.GetArousalTextColor(SynastryType.None);

                        PossibleObj.SetActive(false);
                        LockObj.SetActive(true);
                        SelectObj.SetActive(false);
                    }
                    break;
            }

            AlarmObj.SetActive(bAlarm);
        }

        public void SetSelect(int selectGrade)
        {
            SelectObj.SetActive(grade == selectGrade);
        }

        public void SetSelect(bool isSelect)
        {
            SelectObj.SetActive(isSelect);
        }

        public void SetEffect(GameObject effect)
        {
            effect.transform.parent = EffectObj.transform;
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localScale = Vector3.one;

            EffectObj.SetActive(false);
        }

        public void SetCallBack(SelectSlot callback)
        {
            EventDelegate.Set(CallBack.onClick,
                () =>
                {
                    callback(grade, slotState);
                });
        }

        public void ExecuteCallBack()
        {
            EventDelegate.Execute(CallBack.onClick);
        }
    }
    public List<ArousalSlot> ArousalSlotList;


    // UI - Right
    public UISprite ArousalGradeText;
    public GradeObject ArousalGrade;

    public UISprite MainStatus_Icon;
    public UILabel MainStatus_Name;
    public UILabel MainStatus_Value;

    public GameObject SubStatus1;
    public UISprite SubStatus1_Icon;
    public UILabel SubStatus1_Name;
    public UILabel SubStatus1_Value;

    public GameObject SubStatus2;
    public UISprite SubStatus2_Icon;
    public UILabel SubStatus2_Name;
    public UILabel SubStatus2_Value;

    public UIGrid CostMaterialListGrid;
    public UIButton EssenceCompositionButton;

    public UIButton ArousalButton;
    public GameObject ButtonSet_Free;
    public UILabel ButtonFreeLabel;
    public GameObject ButtonSet_Cost;
    public UILabel ButtonCostLabel;
    public UISprite CostTypeIcon;
    public UILabel CostValueLabel;


    // UI - Effect & Animation
    public GameObject EffectTransform;
    private GameObject ArousalEffect_Synastry;
    private GameObject ArousaEffect_HeroType;
    private GameObject ArousalEffect_HeroType;
    public GameObject[] ArousalEffect_StarArr;
    public GameObject ArousalSkillFirstGetEffect;

    public GameObject UILockPanel;

    public GameObject StatusObj;

    // 중앙 스킬 슬롯 정보
    [Serializable]
    public class ArousalSkillSlotInfo
    {
        public GameObject Obj;
        public UITexture SkillIcon;
        public GameObject DefaultIcon;
        public UIEventTrigger SlotTrigger;
        public GameObject EffectPos;

        public void SetActive(bool active)
        {
            Obj.SetActive(active);
        }
    }
    public ArousalSkillSlotInfo ArousalSkillSlot;

    // 우측 스킬 설명들
    [Serializable]
    public class ArousalSkillExplanation
    {
        public GameObject Obj;
        public UILabel Name;
        public UILabel Explain;
        public UIButton InfoBtn;

        public void SetActive(bool active)
        {
            Obj.SetActive(active);
        }
    }

    public ArousalSkillExplanation ArousalSkillInfo;

    private ThirdBasicInfo ArousalSkillBasicInfo;
    private ThirdDetailInfo ArousalSkillDetailInfo;

    private void Awake()
    {
        TutorialManager.instance.StartTutorial(TutorialID.Arousal);
    }

    protected override void Init()
    {
        HeroCompanionData = ManagerCS.instance.targetHeroData;
        HeroBaseData = HeroCompanionData.c_base;
        CurrentArousalGrade = HeroCompanionData.arousalStep;

        SetArousalInfo();

        UILockPanel.SetActive(false);
    }

    private void SetArousalInfo()
    {
        SetHeroInfo();
        SetHaveMaterialGroup();
        SetArousalSlot();
        // 이펙트 생성뒤에
        LoadEffect();
        // 스킬슬롯 세팅
        SetArousalSkillSlot();

        targetGrade = (HeroCompanionData.arousalStep < 6) ? HeroCompanionData.arousalStep + 1 : 6;

        if (HeroCompanionData.arousalStep >= GameConstraints.MaxArousalStep)
        {
            SetArousalSkillInfo();

            if (HeroCompanionData.skillthird > 0)
            {
                // Change 튜토 체크
                if (TutorialManager.instance.CheckTutorial(TutorialID.ArousalSkill_Change) == false)
                    TutorialManager.instance.StartTutorial(TutorialID.ArousalSkill_Change);
            }
        }
        else
            ArousalSlotList.Find(row => row.arousalGrade == targetGrade).ExecuteCallBack();
    }

    private void SetHeroInfo()
    {
        // Set HeroInfo
        Title.text = string.Format("[b]{0}[/b]", HeroBaseData.GetTitle());
        Name.text = string.Format("[b]{0}[/b]", HeroBaseData.GetName());
        Grade.SetGrade(HeroBaseData.grade, HeroCompanionData.arousalStep);

        SynastryIcon.spriteName = ManagerCS.instance.GetSynastryIcon(HeroBaseData.synastry);
        SynastryLabel.color = GameUtils.GetSynastryColor(HeroBaseData.synastry);
        SynastryLabel.text = GameUtils.GetSynastryText(HeroBaseData.synastry);

        HeroTypeIcon.spriteName = ManagerCS.instance.GetHeroTypeIcon(HeroBaseData.herotype);
        HeroTypeLabel.text = GameUtils.GetHeroTypeText(HeroBaseData.herotype);
    }

    private void SetHaveMaterialGroup()
    {
        MaterialGroupListGrid.transform.DestroyChildren();

        List<int> materialGroup = TableManagerCS.instance.HeroArousalTable.GetMaterialGroupList(HeroBaseData.Arousalmaterial);

        for (int i = 0; i < materialGroup.Count; i++)
        {
            Icon_ETC icon = ManagerCS.instance.MakeIcon<Icon_ETC>(MaterialGroupListGrid.gameObject, ManagerCS.eIconType.ETC);

            ItemArticle article = GameData.CraftMaterialList.Find(row => row.idx == materialGroup[i]);
            if (article == null)
            {
                icon.SetRewardETCInfo(RewardType.CraftMaterial, materialGroup[i], 0, 0);
                icon.SetHaveCount(0);
            }
            else
            {
                icon.SetEtcItemIcon(article);
            }

            icon.SettingType(ManagerCS.eSettingType.Reward_Type);
            icon.SetUseOutCount(true);
            icon.SetHideBackImg(true);
            icon.SetPopup(RewardType.CraftMaterial, materialGroup[i]);
        }

        MaterialGroupListGrid.enabled = true;
        MaterialGroupListGrid.Reposition();
    }


    private void SetArousalSlot()
    {
        for (int i = 0, slotGrade = 1; i < ArousalSlotList.Count; ++i, ++slotGrade)
        {
            ArousalSlotList[i].SetSlot(slotGrade, HeroBaseData.synastry);
            ArousalSlotList[i].SetCallBack(SelectArousalSlot);

            // 각성된 슬롯
            if (slotGrade <= HeroCompanionData.arousalStep)
            {
                ArousalSlotList[i].SetState(ArousalSlot.State.Complete);
            }
            else if (HeroCompanionData.lv < TableManagerCS.instance.HeroArousalTable.GetLimitLevel(slotGrade))
            {
                ArousalSlotList[i].SetState(ArousalSlot.State.Lock);
            }
            else if (slotGrade == HeroCompanionData.arousalStep + 1)
            {
                // 재화 체크
                bool bEnough = true;

                List<RewardValueDesc> needMaterialList = TableManagerCS.instance.HeroArousalTable.GetCostMaterialList(HeroBaseData.Arousalmaterial, slotGrade);
                for (int j = 0; j < needMaterialList.Count; j++)
                {
                    ItemArticle article = GameData.CraftMaterialList.Find(row => row.idx == needMaterialList[j].v1);

                    if ((article == null) || (article.cnt < needMaterialList[j].v2))
                        bEnough = false;
                }

                ArousalSlotList[i].SetState(ArousalSlot.State.Possible, bEnough);
            }
            else
            {
                ArousalSlotList[i].SetState(ArousalSlot.State.Disable);
            }
        }
    }

    private void SelectArousalSlot(int grade, ArousalSlot.State state)
    {
        targetGrade = grade;

        // Slot SelectObj
        for (int i = 0; i < ArousalSlotList.Count; i++)
        {
            ArousalSlotList[i].SetSelect(grade);
        }

        SetArousalInfo(grade, state);
    }

    private void SetArousalInfo(int grade, ArousalSlot.State state)
    {
        StatusObj.SetActive(true);
        ArousalGradeText.gameObject.SetActive(true);
        ArousalSkillInfo.SetActive(false);

        ArousalGradeText.spriteName = string.Format("HeroWakeUpSlot{0}", grade);
        ArousalGradeText.color = GameUtils.GetArousalTextColor(HeroBaseData.synastry);
        ArousalGrade.SetGrade(grade, grade);


        List<string[]> optionList = TableManagerCS.instance.HeroArousalTable.GetArousalOption(HeroBaseData.Arousalstats, grade);
        List<string> iconResList = TableManagerCS.instance.HeroArousalTable.GetArousalOptionIconRes(HeroBaseData.Arousalstats, grade);

        MainStatus_Icon.spriteName = iconResList[0];
        MainStatus_Name.text = optionList[0][0];
        MainStatus_Value.text = optionList[0][1];

        SubStatus1.SetActive(optionList[1] != null);
        if (optionList[1] != null)
        {
            SubStatus1_Icon.spriteName = iconResList[1];
            SubStatus1_Name.text = optionList[1][0];
            SubStatus1_Value.text = optionList[1][1];
        }

        SubStatus2.SetActive(optionList[2] != null);
        if (optionList[2] != null)
        {
            SubStatus2_Icon.spriteName = iconResList[2];
            SubStatus2_Name.text = optionList[2][0];
            SubStatus2_Value.text = optionList[2][1];
        }


        CostMaterialListGrid.transform.DestroyChildren();

        CostMaterialList = TableManagerCS.instance.HeroArousalTable.GetCostMaterialList(HeroBaseData.Arousalmaterial, grade);

        bool bEnchantEnable = true;
        for (int i = 0; i < CostMaterialList.Count; i++)
        {
            Icon_ETC icon = ManagerCS.instance.MakeIcon<Icon_ETC>(CostMaterialListGrid.gameObject, ManagerCS.eIconType.ETC);
            icon.SetRewardETCInfo(CostMaterialList[i].type, CostMaterialList[i].v1, CostMaterialList[i].v2, CostMaterialList[i].v3);
            icon.SettingType(ManagerCS.eSettingType.Reward_Type);
            icon.SetUseOutCount(true);
            icon.SetHideBackImg(true);
            icon.SetPopup(CostMaterialList[i].type, CostMaterialList[i].v1);

            ItemArticle article = GameData.CraftMaterialList.Find(row => row.idx == CostMaterialList[i].v1);
            bool bEnough = (article != null) && (article.cnt >= CostMaterialList[i].v2);
            if (!bEnough)
            {
                icon.SetNotEnoughMsgObj(true);
                bEnchantEnable = false;
            }
        }

        CostMaterialListGrid.enabled = true;
        CostMaterialListGrid.Reposition();


        RewardValueDesc cost = TableManagerCS.instance.HeroArousalTable.GetCost(HeroBaseData.Arousalmaterial, grade);

        ButtonSet_Free.SetActive(cost.type == RewardType.None);
        ButtonSet_Cost.SetActive(cost.type != RewardType.None);
        // 각성
        ButtonFreeLabel.text = SSLocalization.Format("heroInfo", 128);
        ButtonCostLabel.text = SSLocalization.Format("heroInfo", 128);

        if (cost.type != RewardType.None)
        {
            CostType = cost.type;
            CostValue = cost.v1;

            CostTypeIcon.spriteName = ManagerCS.instance.FindSpriteIconName(CostType);
            CostValueLabel.text = CostValue.ToString("#,##0");
        }

        ArousalButton.isEnabled = (bEnchantEnable == true) && (state == ArousalSlot.State.Possible);

        EventDelegate.Set(ArousalButton.onClick, OnClickArousalButton);
    }

    private void OnClickArousalButton()
    {
        PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common);
        popup.Set(PopupCommon.Type.Payment, SSLocalization.Get("heroInfo", 239), ReqArousalHero);
        popup.SetPayment(CostType, CostValue);
    }

    private void ReqArousalHero()
    {
        ArousalHeroRequestParameter data = new ArousalHeroRequestParameter();
        data.heromanidx = HeroCompanionData.manidx;

        data.dfevtidx = ManagerCS.Getdfevtidx();
        data.bProgressDF = ManagerCS.GetbProgressDF();

        NetworkConnection.instance.NetCommand(PacketType.ArousalHeroRequestParameter, data,
            (string _data) =>
            {
                if (NetworkCS.CheckNetworkError())
                {
                    ArousalHeroResultParameter result = JsonConvert.DeserializeObject<ArousalHeroResultParameter>(_data);

                    ManagerCS.instance.SetReceiveFinaceDataToUI(result.finance);
                    ManagerCS.instance.SetReceiveMissionDataToUI(result.successMissionList);
                    ManagerCS.instance.SetReceiveGuildMissionDataToUI(result.successGuildMissionList);

                    if (!ManagerCS.instance.IsDiscountEventSync(DiscountCategory.HERO, PacketType.ArousalHeroRequestParameter, result.bSync, result.dfInfo, null))
                        return;

                    ManagerCS.instance.uiController.GetHero(result.hero_postinfo);              // 영웅 정보 갱신
                    ManagerCS.instance.uiController.GetCraftMaterial(result.sync_Materials);    // 재료 아이템 갱신
                    ManagerCS.instance.RefreshHeroBattlePower(result.hero_postinfo.manidx);     // 영웅 전투력 갱신

                    // 영웅 정보 갱신
                    HeroCompanionData = result.hero_postinfo;
                    
                    StartCoroutine(StartArousalEffect(
                        () =>
                        {
                            // 미션 알림 팝업
                            ManagerCS.instance.MissionNoticePopup();

                            // 슬롯 정보 갱신
                            SetHaveMaterialGroup();
                            SetArousalSlot();

                            // 다음 타겟슬롯 선택
                            targetGrade = (HeroCompanionData.arousalStep < 6) ? HeroCompanionData.arousalStep + 1 : 6;

                            // 결과 팝업
                            PopupHeroArousalResult popup = PopupManager.instance.MakePopup<PopupHeroArousalResult>(ePopupType.HeroArousalResult);

                            if (HeroCompanionData.arousalStep >= GameConstraints.MaxArousalStep)
                            {
                                // 최초 각성스킬 습득 가능 상태
                                popup.Set(result.hero_previnfo, result.hero_postinfo, delegate { StartCoroutine(FirstArousalSkillGetPossibleEffect()); });

								// 최대 각성 자랑하기
								ChattingConnection.instance.BroadCastGetHeroByMaxArousal(HeroCompanionData.idx, HeroCompanionData.arousalStep);
                            }
                            else
                            {
                                popup.Set(result.hero_previnfo, result.hero_postinfo, null);
                                ArousalSlotList.Find(row => row.arousalGrade == targetGrade).ExecuteCallBack();
                            }
                        }));
                }
            });
    }

    // 최초 각성 스킬 습득 가능한 상태 연출
    private IEnumerator FirstArousalSkillGetPossibleEffect()
    {
        // Escape && Lock
        SoundPlay.instance.Play(2025, SOUND_TYPE.EFFECT_TYPE, 0);

        ManagerCS.instance.IsTopUILock = true;
        UILockPanel.SetActive(true);
        
        // SkillEffect On
        ArousalEffect_HeroType.SetActive(false);
        ArousalSkillFirstGetEffect.SetActive(false);
        ArousalSkillFirstGetEffect.SetActive(true);

        SoundPlay.instance.SetSoundPlay(2026, SOUND_TYPE.EFFECT_TYPE, 1.5f);

        yield return new WaitForSeconds(2.0f);
        // Skill Slot Setting
        SetArousalSkillSlot();
        yield return new WaitForSeconds(1.0f);

        // SkillInfoSetting
        SetArousalSkillInfo();

        ManagerCS.instance.IsTopUILock = false;
        UILockPanel.SetActive(false);

        // SkillEffect Off
        yield return new WaitForSeconds(1.0f);
        ArousalSkillFirstGetEffect.SetActive(false);
    }
        
    private IEnumerator StartArousalEffect(voidDelegate callback)
    {
        // UI Lock
        ManagerCS.instance.IsTopUILock = true;
        UILockPanel.SetActive(true);

        // targetSlot
        int slotIdx = targetGrade - 1;
        
        // 각성 이펙트 On
        ArousalEffect_Synastry.SetActive(true);
        ArousaEffect_HeroType.SetActive(true);

        SoundPlay.instance.SetSoundPlay(1058, SOUND_TYPE.EFFECT_TYPE, 0);

        yield return new WaitForSeconds(2f);

        // 각성 이펙트 Off
        ArousalEffect_Synastry.SetActive(false);
        ArousaEffect_HeroType.SetActive(false);

        // 각성슬롯 이펙트 On
        ArousalSlotList[slotIdx].EffectObj.SetActive(true);

        SoundPlay.instance.SetSoundPlay(2160, SOUND_TYPE.EFFECT_TYPE, 0);

        // 별 이펙트 On
        ArousalEffect_StarArr[slotIdx].SetActive(true);

        // 별 정보 갱신
        Grade.SetGrade(HeroBaseData.grade, HeroCompanionData.arousalStep);
        yield return new WaitForSeconds(1f);

        // 각성슬롯 정보 갱신
        ArousalSlotList[slotIdx].SetState(ArousalSlot.State.Complete);

        yield return new WaitForSeconds(0.5f);

        // 슬롯 이펙트 Off
        ArousalSlotList[slotIdx].EffectObj.SetActive(false);
        
        yield return new WaitForSeconds(0.2f);

        // 별 이펙트 Off
        ArousalEffect_StarArr[slotIdx].SetActive(false);

        // UI Unlock
        ManagerCS.instance.IsTopUILock = false;
        UILockPanel.SetActive(false);

        // CallBack
        if (callback != null)
            callback();
    }


    private void LoadEffect()
    {
        NGUITools.AddChild(EffectTransform, GetEffectResource(EffectType.eff_synastry_loop));
        ArousalEffect_HeroType = NGUITools.AddChild(EffectTransform, GetEffectResource(EffectType.eff_herotype_loop));

        (ArousalEffect_Synastry = NGUITools.AddChild(EffectTransform, GetEffectResource(EffectType.eff_synastry_arousal))).SetActive(false);
        (ArousaEffect_HeroType = NGUITools.AddChild(EffectTransform, GetEffectResource(EffectType.eff_herotype_arousal))).SetActive(false);
        (ArousalSkillFirstGetEffect = NGUITools.AddChild(EffectTransform, GetEffectResource(EffectType.eff_arousalskill_slot))).SetActive(false);

        for (int i = 0; i < ArousalSlotList.Count; i++)
        {
            GameObject effect = NGUITools.AddChild(gameObject, GetEffectResource(EffectType.eff_synastry_slot));
            ArousalSlotList[i].SetEffect(effect);
        }

        for (int i = 0; i < ArousalEffect_StarArr.Length; i++)
        {
            ArousalEffect_StarArr[i].SetActive(false);
        }
    }
    
    private enum EffectType
    {
        eff_synastry_loop,
        eff_herotype_loop,
        eff_synastry_arousal,
        eff_herotype_arousal,
        eff_synastry_slot,
        eff_arousalskill_slot,
    }
    private string[] EffectTypeArr =
        {
            "e_{0}_magiccircle_loop_effect",
            "e_{0}_{1}_mark_loop_effect",
            "e_{0}_arousal_effest",
            "e_{0}_{1}_mark_arousal_effest",
            "e_{0}_slot_effect",
            "e_{0}_skill_slot_effect",
        };

    private string[] SynastryArr = { "", "fire", "water", "tree", "light", "dark" };
    private string[] HeroTypeArr = { "", "attack", "defense", "support", "balance" };
    
    private GameObject GetEffectResource(EffectType efftype)
    {
        string synastry = SynastryArr[(int)HeroBaseData.synastry];
        string herotype = HeroTypeArr[(int)HeroBaseData.herotype];
        string effectname = string.Format(EffectTypeArr[(int)efftype], synastry, herotype);

        switch (HeroBaseData.synastry)
        {
            case SynastryType.Fire:
                return Resmanager.AssetLoad(ASSET_TYPE.RAID_EFFECT, "Raid_Effect/UI/HeroArousal_effect/01_Fire/" + effectname) as GameObject;

            case SynastryType.Ice:
                return Resmanager.AssetLoad(ASSET_TYPE.RAID_EFFECT, "Raid_Effect/UI/HeroArousal_effect/02_Ice/" + effectname) as GameObject;

            case SynastryType.Tree:
                return Resmanager.AssetLoad(ASSET_TYPE.RAID_EFFECT, "Raid_Effect/UI/HeroArousal_effect/03_Tree/" + effectname) as GameObject;

            case SynastryType.Light:
                return Resmanager.AssetLoad(ASSET_TYPE.RAID_EFFECT, "Raid_Effect/UI/HeroArousal_effect/04_Light/" + effectname) as GameObject;

            case SynastryType.Dark:
                return Resmanager.AssetLoad(ASSET_TYPE.RAID_EFFECT, "Raid_Effect/UI/HeroArousal_effect/05_Dark/" + effectname) as GameObject;

            default:
                return null;
        }
    }
    
    public void OnClickArousalMaterialComposition()
    {
        PopupManager.instance.MakePopup<PopupEssenceComposition>(ePopupType.EssenceComposition, false, true).Set(HeroCompanionData.c_base.synastry);
    }
    
    private void RefreshMaterial()
    {
        SetHaveMaterialGroup();

        if (HeroCompanionData.arousalStep >= GameConstraints.MaxArousalStep)
            SetArousalSkillInfo();
        else
            ArousalSlotList.Find(row => row.arousalGrade == targetGrade).ExecuteCallBack();
    }

    public void Tutorial_RefreshMaterial()
    {
        RefreshMaterial();
    }

    internal override void OnUpdateHeroMaterial()
    {
        RefreshMaterial();
    }

    public void OnClickArousalSkillSlot()
    {
        SetArousalSkillInfo();
    }

    private void SetArousalSkillSlot()
    {
        ArousalSkillSlot.SetActive(false);

        if (HeroCompanionData.arousalStep < GameConstraints.MaxArousalStep)
            return;

        bool isHaveThird = HeroCompanionData.skillthird > 0;

        ArousalEffect_HeroType.SetActive(false);
        ArousalSkillSlot.SetActive(true);

        ArousalSkillSlot.DefaultIcon.SetActive(!isHaveThird);
        ArousalSkillSlot.SkillIcon.gameObject.SetActive(isHaveThird);

        if (isHaveThird == true)
        {
            ArousalSkillBasicInfo = TableManagerCS.instance.SkillThirdHeroTable.FindBasicInfo(HeroCompanionData.skillthird);
            ArousalSkillDetailInfo = TableManagerCS.instance.SkillThirdHeroTable.FindDetailInfo(HeroCompanionData.skillthird, HeroCompanionData.skillthirdlv);

            Resmanager.LoadTextureToAsset(ref ArousalSkillSlot.SkillIcon, ASSET_TYPE.ASSET_ICON_SKILL, ArousalSkillBasicInfo.g_idx);

            EventDelegate.Set(ArousalSkillSlot.SlotTrigger.onPress,
                () =>
                {
                    PopupSkillInfo popup = PopupManager.instance.MakePopup<PopupSkillInfo>(ePopupType.SkillInfo, false, true);
                    popup.SetPressPopup(ArousalSkillSlot.SlotTrigger, new Vector2(220f, 0f));
                    popup.Set(SKILL_KIND.THIRD, HeroCompanionData.skillthird, HeroCompanionData.skillthirdlv, true);
                });
        }

        EventDelegate.Set(ArousalSkillSlot.SlotTrigger.onClick , delegate { OnClickArousalSkillSlot(); });
    }

    private void SetArousalSkillInfo()
    {
        // Slot SelectObj
        for (int i = 0; i < ArousalSlotList.Count; i++)
            ArousalSlotList[i].SetSelect(false);

        StatusObj.SetActive(false);
        ArousalGradeText.gameObject.SetActive(false);
        ArousalSkillInfo.SetActive(true);

        bool isHaveThird = HeroCompanionData.skillthird > 0;

        if (isHaveThird == true)
        {
            ArousalSkillInfo.Name.gameObject.SetActive(true);
            ArousalSkillInfo.Name.text = ArousalSkillBasicInfo.SkillName;
            ArousalSkillInfo.Explain.text = TableManagerCS.instance.SkillThirdHeroTable.GetThirdExplain(ArousalSkillBasicInfo, ArousalSkillDetailInfo);

            // 스킬각성 재료 세팅
            CostMaterialListGrid.transform.DestroyChildren();

            RewardValueDesc BtnReward = null;
            TableManagerCS.instance.SkillThirdHeroTable.FindArousalSkillSpendCost(HeroCompanionData.c_base.skillthird, out CostMaterialList, out BtnReward);

            bool bGetPossible = true;
            for (int i = 0; i < CostMaterialList.Count; i++)
            {
                Icon_ETC icon = ManagerCS.instance.MakeIcon<Icon_ETC>(CostMaterialListGrid.gameObject, ManagerCS.eIconType.ETC);
                icon.SetRewardETCInfo(CostMaterialList[i].type, CostMaterialList[i].v1, CostMaterialList[i].v2, CostMaterialList[i].v3);
                icon.SettingType(ManagerCS.eSettingType.Reward_Type);
                icon.SetUseOutCount(true);
                icon.SetHideBackImg(true);
                icon.SetPopup(CostMaterialList[i].type, CostMaterialList[i].v1);

                ItemArticle article = GameData.CraftMaterialList.Find(row => row.idx == CostMaterialList[i].v1);
                bool bEnough = (article != null) && (article.cnt >= CostMaterialList[i].v2);
                if (!bEnough)
                {
                    icon.SetNotEnoughMsgObj(true);
                    bGetPossible = false;
                }
            }

            // 스킬각성 재화 버튼 세팅
            ButtonSet_Free.SetActive(BtnReward.type == RewardType.None);
            ButtonSet_Cost.SetActive(BtnReward.type != RewardType.None);
            // 스킬 습득
            ButtonFreeLabel.text = SSLocalization.Format("heroInfo", 338);
            ButtonCostLabel.text = SSLocalization.Format("heroInfo", 338);

            if (BtnReward.type != RewardType.None)
            {
                CostType = BtnReward.type;
                CostValue = BtnReward.v1;

                CostTypeIcon.spriteName = ManagerCS.instance.FindSpriteIconName(CostType);
                CostValueLabel.text = CostValue.ToString("#,##0");
            }

            ArousalButton.isEnabled = (bGetPossible == true);   
        }
        else
        {
            ArousalSkillInfo.Name.gameObject.SetActive(false);
            ArousalSkillInfo.Explain.text = SSLocalization.Format("heroInfo", 340);
            // 스킬 최초 습득 로직

            CostMaterialListGrid.transform.DestroyChildren();

            // 스킬각성 재화 무료 하드코딩
            ButtonSet_Free.SetActive(true);
            ButtonFreeLabel.text = SSLocalization.Format("heroInfo", 338);
            ButtonSet_Cost.SetActive(false);

            ArousalButton.isEnabled = true;

            // 오픈 튜토 확인
            if (TutorialManager.instance.CheckTutorial(TutorialID.ArousalSkill_Open) == false)
                TutorialManager.instance.StartTutorial(TutorialID.ArousalSkill_Open);

            // 오픈튜토중 어플종료시 습득튜토 발생
            if (TutorialManager.instance.CheckTutorial(TutorialID.ArousalSkill_Get) == false)
                TutorialManager.instance.StartTutorial(TutorialID.ArousalSkill_Get);
        }

        CostMaterialListGrid.enabled = true;
        CostMaterialListGrid.Reposition();

        EventDelegate.Set(ArousalButton.onClick, OnClickGetArousalSkill);
    }

    public void OnClickArousalSkillInfo()
    {
        PopupManager.instance.MakePopup<PopupArousalSkillInfo>(ePopupType.ArousalSkillInfo, false, true).Set(HeroCompanionData,
            delegate
            {
                // 각성던전 UI -> 각성 스킬정보를 닫을때 습득 튜토 확인
                if (TutorialManager.instance.CheckTutorial(TutorialID.ArousalSkill_Get) == false)
                    TutorialManager.instance.StartTutorial(TutorialID.ArousalSkill_Get);
            });
    }

    private void OnClickGetArousalSkill()
    {
        bool isHaveThird = HeroCompanionData.skillthird > 0;
        PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common);

        // 최초 습득인경우
        // 각성 스킬을 습득하시겠습니까?\n확률에 의해 1개의 각성 스킬을 습득하게 됩니다.
        if (isHaveThird == false)
            popup.Set(PopupCommon.Type.TwoBtn, SSLocalization.Get("popupCS", 368), ReqFirstArousalSkill);
        // 각성 스킬을 변경하시겠습니까 ?\n확률에 의해 1개의 각성 스킬을 습득하게 됩니다.
        else
        {
            popup.Set(PopupCommon.Type.Payment, SSLocalization.Get("popupCS", 369), ReqChangeArousalSkillStart);
            popup.SetPayment(CostType, CostValue);
        }

    }

    private void ReqFirstArousalSkill()
    {
        ArousalSkillHeroRequestParameter data = new ArousalSkillHeroRequestParameter();
        data.heromanidx = HeroCompanionData.manidx;

        data.dfevtidx = ManagerCS.Getdfevtidx();
        data.bProgressDF = ManagerCS.GetbProgressDF();

        NetworkConnection.instance.NetCommand(PacketType.ArousalSkillHeroRequestParameter, data,
            (string _data) =>
            {
                if (NetworkCS.CheckNetworkError())
                {
                    ArousalSkillHeroResultParameter result = JsonConvert.DeserializeObject<ArousalSkillHeroResultParameter>(_data);

                    ManagerCS.instance.SetReceiveFinaceDataToUI(result.finance);
                    ManagerCS.instance.SetReceiveMissionDataToUI(result.successMissionList);
                    ManagerCS.instance.uiController.GetSecondFinanceItem(result.sync_SecFinances);
                    ManagerCS.instance.uiController.GetCraftMaterial(result.sync_Materials);    // 재료 아이템 갱신
                    //ManagerCS.instance.SetReceiveGuildMissionDataToUI(result.successGuildMissionList);


                    if (!ManagerCS.instance.IsDiscountEventSync(DiscountCategory.HERO, PacketType.ArousalHeroRequestParameter, result.bSync, result.dfInfo, null))
                        return;

                    ManagerCS.instance.uiController.GetHero(result.hero_postinfo);              // 영웅 정보 갱신
                    ManagerCS.instance.RefreshHeroBattlePower(result.hero_postinfo.manidx);     // 영웅 전투력 갱신

                    // 영웅 정보 갱신
                    HeroCompanionData = result.hero_postinfo;

                    // 결과 팝업
                    PopupArousalSkillConfirm popup = PopupManager.instance.MakePopup<PopupArousalSkillConfirm>(ePopupType.ArousalSkillConfirm);
                    popup.Set(PopupArousalSkillConfirm.ThirdChooseState.Picked, HeroCompanionData, result.hero_postinfo.skillthird);
                }
            });
    }

    private void ReqChangeArousalSkillStart()
    {
        ArousalSkill_ImbueRequestParameter data = new ArousalSkill_ImbueRequestParameter();
        data.heromanidx = HeroCompanionData.manidx;

        NetworkConnection.instance.NetCommand(PacketType.ArousalSkill_ImbueRequestParameter, data,
            (string _data) =>
            {
                if (NetworkCS.CheckNetworkError())
                {
                    ArousalSkill_ImbueResultParameter result = JsonConvert.DeserializeObject<ArousalSkill_ImbueResultParameter>(_data);

                    ManagerCS.instance.SetReceiveFinaceDataToUI(result.finance);
                    ManagerCS.instance.SetReceiveMissionDataToUI(result.successMissionList);
                    ManagerCS.instance.uiController.GetSecondFinanceItem(result.sync_SecFinances);
                    ManagerCS.instance.uiController.GetCraftMaterial(result.sync_Materials);    // 재료 아이템 갱신

                    // 이상태에서 종료를해도 바뀐스킬로 적용이기때문에 미리 적용해둔다.
                    HeroCompanionData.skillthird = result.ArousalSkillID;
                    HeroCompanionData.skillthirdlv = 1;

                    // 선택 팝업
                    PopupArousalSkillConfirm popup = PopupManager.instance.MakePopup<PopupArousalSkillConfirm>(ePopupType.ArousalSkillConfirm);
                    popup.Set(PopupArousalSkillConfirm.ThirdChooseState.Should_Choose, HeroCompanionData, ArousalSkillBasicInfo, ArousalSkillDetailInfo, result.ArousalSkillID);
                }
            });
    }

    internal override void OnChangeSkillThird(HeroCompanion companion)
    {
        ManagerCS.instance.uiController.GetHero(companion);              // 영웅 정보 갱신
        ManagerCS.instance.RefreshHeroBattlePower(companion.manidx);     // 영웅 전투력 갱신

        // 영웅 정보 갱신
        HeroCompanionData = companion;

        // 미션 알림 팝업
        ManagerCS.instance.MissionNoticePopup();

        // 슬롯 정보 갱신
        SetHaveMaterialGroup();
        SetArousalSlot();
        SetArousalSkillSlot();
        SetArousalSkillInfo();
    }

}