using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using ServerCommon;
using ServerCommon.DataTables;
using ServerCommon.Tutorials;
using Newtonsoft.Json;

public class HeroSkillEnchantUI : GameNotification
{
    // Data
    private HeroCompanion HeroCompanionData;
    private HeroBases HeroBaseData;

    private List<HeroCompanion> HeroList;       // 영웅 리스트
    private List<Icon_HeroList> HeroIconList;

    private Icon_HeroList TargetIcon;

    private SKILL_KIND SelectType;
    private int Idx;
    private int Lv;

    private RewardType CostType;
    private int CostValue;
    private List<RewardValueDesc> CostMaterialList;

    private GameObject TooltipListItem;


    // UI - Left
    public UILabel NameLabel;
    public UILabel ExplainLabel;
    public UILabel CooltimeLabel;

    [Serializable]
    public class EnchantAbilitySlot
    {
        private SKILL_KIND SkillType;
        private int Index;
        private int Lv;
        public int EnchantLv { get { return Lv; } }


        public UILabel LevelLable;
        public UILabel AbilityLabel;
        public GameObject SlotEffect;
        public UIEventTrigger CallBack;

        public delegate void OnPressCallBack(int idx, int lv);


        public void Set(SKILL_KIND type, int idx, int lv)
        {
            SkillType = type;
            Index = idx;
            Lv = lv;

            LevelLable.text = string.Format("[b]+{0}[/b]", Lv - 1);
            AbilityLabel.text = GetAbility();
            SlotEffect.SetActive(false);
        }
        
        private string GetAbility()
        {
            string EffectText = string.Empty;

            switch (SkillType)
            {
                case SKILL_KIND.PRIMARY:
                    {
                        PrimaryDetailInfo curInfo = TableManagerCS.instance.SkillPrimaryHeroTable.FindDetailInfo(Index, Lv);
                        PrimaryDetailInfo beforeInfo = TableManagerCS.instance.SkillPrimaryHeroTable.FindDetailInfo(Index, Lv - 1);

                        float power = curInfo.fPower - beforeInfo.fPower;
                        float time = curInfo.fTime - beforeInfo.fTime;
                        float cooltime = curInfo.fCoolTime - beforeInfo.fCoolTime;

                        EffectText = SSLocalization.Format("SkillEnchant", curInfo.enchantCSV, power.ToString("#,##0.##"), time.ToString("#,##0.##"), cooltime.ToString("#,##0.##"));
                    }
                    break;

                case SKILL_KIND.SECONDARY:
                    {
                        SecondaryDetailInfo curInfo = TableManagerCS.instance.SkillSecondaryHeroTable.FindDetailInfo(Index, Lv);
                        SecondaryDetailInfo beforeInfo = TableManagerCS.instance.SkillSecondaryHeroTable.FindDetailInfo(Index, Lv - 1);

                        float chance = curInfo.fChance - beforeInfo.fChance;
                        float time = curInfo.fTime - beforeInfo.fTime;
                        float value1 = curInfo.fValue1 - beforeInfo.fValue1;
                        float value2 = curInfo.fValue2 - beforeInfo.fValue2;
                        float value3 = curInfo.fValue3 - beforeInfo.fValue3;

                        EffectText = SSLocalization.Format("SkillEnchant", curInfo.enchantCSV, chance.ToString("#,##0.##"), time.ToString("#,##0.##"), value1.ToString("#,##0.##"), value2.ToString("#,##0.##"), value3.ToString("#,##0.##"));
                    }
                    break;

                case SKILL_KIND.THIRD:
                    {
                        ThirdDetailInfo curInfo = TableManagerCS.instance.SkillThirdHeroTable.FindDetailInfo(Index, Lv);
                        ThirdDetailInfo beforeInfo = TableManagerCS.instance.SkillThirdHeroTable.FindDetailInfo(Index, Lv - 1);

                        float skillChance = curInfo.SkillChance - beforeInfo.SkillChance;
                        float skillTime = curInfo.SkillTime - beforeInfo.SkillTime;
                        float optionIdx = 0;
                        float skillPower = curInfo.fSkillPower - beforeInfo.fSkillPower;
                        float optionIdx2 = 0;
                        float skillPower2 = curInfo.fSkillPower2 - beforeInfo.fSkillPower2;
                        float reductionIdx = 0;
                        float reductionrRte = curInfo.fReductionrRte - beforeInfo.fReductionrRte;
                        float standardHp = curInfo.fStandardHp - beforeInfo.fStandardHp;
                        float standardTime = curInfo.StandardTime - beforeInfo.StandardTime;
                        float RateMax = curInfo.fRateMax - beforeInfo.fRateMax;

                        EffectText = SSLocalization.Format("SkillEnchant", curInfo.enchantCSV,
                                                                           skillChance.ToString("#,##0.##"),
                                                                           skillTime.ToString("#,##0.##"),
                                                                           optionIdx.ToString("#,##0.##"),
                                                                           skillPower.ToString("#,##0.##"),
                                                                           optionIdx2.ToString("#,##0.##"),
                                                                           skillPower2.ToString("#,##0.##"),
                                                                           reductionIdx.ToString("#,##0.##"),
                                                                           reductionrRte.ToString("#,##0.##"),
                                                                           standardHp.ToString("#,##0.##"),
                                                                           standardTime.ToString("#,##0.##"),
                                                                           RateMax.ToString("#,##0.##"));
                    }
                    break;
            }
            
            return EffectText;
        }

        public void SetActiveAbilitySlot(bool isActive)
        {
            if (isActive)
            {
                LevelLable.color = GameUtils.GetColor("749CB9");
                AbilityLabel.color = GameUtils.GetColor("FFFFFF");
            }
            else
            {
                LevelLable.color = GameUtils.GetColor("6E6E6E");
                AbilityLabel.color = GameUtils.GetColor("6E6E6E");
            }
        }

        public void SetEvent(OnPressCallBack press, EventDelegate.Callback release)
        {
            EventDelegate.Set(CallBack.onPress, () => { press(Index, Lv); });
            EventDelegate.Set(CallBack.onRelease, release);
        }

        public void ActiveSlotEffect()
        {
            if (SlotEffect.activeSelf)
                SlotEffect.SetActive(false);

            SlotEffect.SetActive(true);

            SetActiveAbilitySlot(true);
        }

        public void InActiveSlotEffect()
        {
            if (SlotEffect.activeSelf)
                SlotEffect.SetActive(false);

            SetActiveAbilitySlot(false);
        }
    }
    public List<EnchantAbilitySlot> EnchantAbilityList;

    public GameObject Tooltip;
    public UIGrid TooltipGrid;
    public UISprite TooltipBG;


    // UI - Center
    public GameObject TargetHeroIconSlot;
    public GameObject TargetHeroIconEffect;

    public UIGrid SkillIconGrid;

    // Primary
    public UIEventTrigger PrimaryTrigger;
    public UITexture PrimaryIcon;
    public UILabel PrimaryLevel;
    public GameObject PrimarySelectObj;
    public GameObject PrimaryEnchantEffect;

    // Secondary
    public UIEventTrigger SecondaryTrigger;
    public UITexture SecondaryIcon;
    public UILabel SecondaryLevel;
    public GameObject SecondarySelectObj;
    public GameObject SecondaryEnchantEffect;

    // Third
    public UIEventTrigger ThirdTrigger;
    public UITexture ThirdIcon;
    public UILabel ThirdLevel;
    public GameObject ThirdSelectObj;
    public GameObject ThirdEnchantEffect;

    public UIGrid CostMaterialListGrid;
    public UISprite CostTypeIcon;
    public UILabel CostValueLabel;
    public UIButton EnchantButton;

    public GameObject MaxLevelLabel;


    // UI - Right
    public UnitListScrollView HeroListScrollView;
    public UILabel HeroStorageCount;
    public HeroSortingButton SortingButton;
    public UIButton FilteringButton;

    // UI - Effect & Animation
    public GameObject UILockPanel;



    private void Awake()
    {
        HeroIconList = new List<Icon_HeroList>();
        TooltipListItem = Resmanager.Load("UI/02_UnitManagement/SkillEnchantTooltipList") as GameObject;

        TutorialManager.instance.StartTutorial(TutorialID.SkillEnchant);
    }

    protected override void Init()
    {
        SetHeroList();
        SetSortingButton();
        SetHeroStorageCount();

        HeroCompanionData = (ManagerCS.instance.targetHeroData != null) ? ManagerCS.instance.targetHeroData : HeroIconList[0].HeroCompanionData;
        HeroBaseData = HeroCompanionData.c_base;

        SetHeroInfo();

        TargetHeroIconEffect.SetActive(false);
        Tooltip.SetActive(false);
        UILockPanel.SetActive(false);
    }

    private void SetHeroList()
    {
        List<HeroCompanion> list = ManagerCS.instance.GetFilteringHero(GameData.Heroes);

        bool isContain = true;

        if (ManagerCS.instance.targetHeroData != null)
            isContain = list.Find(row => row.manidx == ManagerCS.instance.targetHeroData.manidx) != null;
        
        if (list.Count == 0 || !isContain)
        {
            ManagerCS.instance.Clear_hfo_main();
            list = ManagerCS.instance.GetFilteringHero(GameData.Heroes);
        }

        SetHeroList(list);
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
            // 특수 영웅이면 continue
            if (HeroList[i].c_base.bSpecial != HeroSpecialType.NormalType)
                continue;

            Icon_HeroList icon = ManagerCS.instance.MakeIcon<Icon_HeroList>(HeroListScrollView.grid, ManagerCS.eIconType.HeroList, true);
            icon.SetIcon(HeroList[i]);
            icon.SetCallBack(SelectHeroIcon);

            HeroIconList.Add(icon);
        }

        HeroListScrollView.SetCompare(SortingClass.CompareHeroList);
        HeroListScrollView.onStoppedMoving = OnSelectHero;

        ManagerCS.instance.SetFilteringButtonColor(ref FilteringButton);
    }

    private void SelectHeroIcon(Icon_HeroList target)
    {
        HeroListScrollView.TargetOn(target.transform);
    }

    private void OnSelectHero()
    {
        TargetIcon = HeroListScrollView.GetTargetData<Icon_HeroList>();

        if (TargetIcon == null)
            return;

        if (TargetIcon.HeroCompanionData == HeroCompanionData)
            return;

        HeroCompanionData = TargetIcon.HeroCompanionData;
        HeroBaseData = HeroCompanionData.c_base;

        SetHeroInfo();
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

        SetTargetHero(bResetScroll);
    }

    private void SetTargetHero(bool bResetScroll)
    {
        if (HeroIconList.Count == 0)
            return;
        
        Icon_HeroList icon = null;
        if (bResetScroll || ManagerCS.instance.targetHeroData == null)
        {
            HeroIconList.Sort(SortingClass.CompareHeroList);
            icon = HeroIconList[0];
        }
        else
        {
            icon = HeroIconList.Find(row => row.HeroCompanionData.manidx == ManagerCS.instance.targetHeroData.manidx);
        }

        HeroListScrollView.TargetOn(icon.transform, true);

        HeroCompanionData = icon.HeroCompanionData;
        HeroBaseData = HeroCompanionData.c_base;

        SetHeroInfo();
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

    private void SetHeroInfo(bool bInitSelect = false)
    {
        SetHeroIcon();
        SetSkill();

        if (bInitSelect)
        {
            if (HeroCompanionData.skillprimarylv < GameConstraints.SkillPrimaryEnchantMaxLv)
                SetPrimarySkillEnchant();
            else if (HeroCompanionData.skillsecondarylv < GameConstraints.SkillSecondaryEnchantMaxLv)
                SetSecondarySkillEnchant();
            else if (HeroCompanionData.skillthirdlv > 0 && HeroCompanionData.skillthirdlv < GameConstraints.SkillThirdEnchantMaxLv)
                SetThirdSkillEnchant();
            else
                SetPrimarySkillEnchant();
        }
        else
        {
            switch (SelectType)
            {
                case SKILL_KIND.PRIMARY:
                    SetPrimarySkillEnchant();
                    break;

                case SKILL_KIND.SECONDARY:
                    SetSecondarySkillEnchant();
                    break;

                case SKILL_KIND.THIRD:

                    if (HeroCompanionData.skillthird > 0)
                        SetThirdSkillEnchant();
                    else
                    {
                        if (HeroCompanionData.skillprimarylv < GameConstraints.SkillPrimaryEnchantMaxLv)
                            SetPrimarySkillEnchant();
                        else if (HeroCompanionData.skillsecondarylv < GameConstraints.SkillSecondaryEnchantMaxLv)
                            SetSecondarySkillEnchant();
                        else
                            SetPrimarySkillEnchant();
                    }
                    break;
            }
        }

        // 선택 영웅정보 저장
        ManagerCS.instance.targetHeroData = HeroCompanionData;
    }

    private void SetHeroIcon()
    {
        TargetHeroIconSlot.transform.DestroyChildren();

        Icon_Hero icon = ManagerCS.instance.MakeIcon<Icon_Hero>(TargetHeroIconSlot, ManagerCS.eIconType.Hero);
        icon.SetHeroIcon(HeroCompanionData);
        icon.SettingType(ManagerCS.eSettingType.A_Type);
    }

    private void SetSkill()
    {
        // Primary
        int primary_idx = HeroCompanionData.skillprimary;
        int primary_lv = HeroCompanionData.skillprimarylv;

        PrimaryBasicInfo primaryInfo = TableManagerCS.instance.SkillPrimaryHeroTable.FindBasicInfo(primary_idx);

        Resmanager.LoadTextureToAsset(ref PrimaryIcon, ASSET_TYPE.ASSET_ICON_SKILL, primaryInfo.g_index);
        PrimaryLevel.gameObject.SetActive((primary_lv - 1) > 0);
        PrimaryLevel.text = ((primary_lv - 1) > 0) ? SSLocalization.Format("heroinfo", 1, primary_lv - 1) : string.Empty;

        EventDelegate.Set(PrimaryTrigger.onClick, SetPrimarySkillEnchant);
        EventDelegate.Set(PrimaryTrigger.onPress,
            () =>
            {
                PopupSkillInfo popup = PopupManager.instance.MakePopup<PopupSkillInfo>(ePopupType.SkillInfo);
                popup.SetPressPopup(PrimaryTrigger, new Vector2(220f, 0f));
                popup.Set(SKILL_KIND.PRIMARY, primary_idx, primary_lv);
            });

        PrimaryEnchantEffect.SetActive(false);
        

        // Secondary
        int secondary_idx = HeroCompanionData.skillsecondary;
        int secondary_lv = HeroCompanionData.skillsecondarylv;

        SecondaryBasicInfo secondaryInfo = TableManagerCS.instance.SkillSecondaryHeroTable.FindBasicInfo(secondary_idx);

        Resmanager.LoadTextureToAsset(ref SecondaryIcon, ASSET_TYPE.ASSET_ICON_SKILL, secondaryInfo.g_index);
        SecondaryLevel.gameObject.SetActive((secondary_lv - 1) > 0);
        SecondaryLevel.text = ((secondary_lv - 1) > 0) ? SSLocalization.Format("heroinfo", 1, secondary_lv - 1) : string.Empty;

        EventDelegate.Set(SecondaryTrigger.onClick, SetSecondarySkillEnchant);
        EventDelegate.Set(SecondaryTrigger.onPress,
            () =>
            {
                PopupSkillInfo popup = PopupManager.instance.MakePopup<PopupSkillInfo>(ePopupType.SkillInfo);
                popup.SetPressPopup(SecondaryTrigger, new Vector2(220f, 0f));
                popup.Set(SKILL_KIND.SECONDARY, secondary_idx, secondary_lv);
            });

        SecondaryEnchantEffect.SetActive(false);

        // Third
        int third_idx = HeroCompanionData.skillthird;
        int third_lv = HeroCompanionData.skillthirdlv;

        bool isHaveThird = third_idx > 0;

        ThirdTrigger.gameObject.SetActive(isHaveThird);

        if (isHaveThird == true)
        {
            ThirdBasicInfo thirdInfo = TableManagerCS.instance.SkillThirdHeroTable.FindBasicInfo(third_idx);

            Resmanager.LoadTextureToAsset(ref ThirdIcon, ASSET_TYPE.ASSET_ICON_SKILL, thirdInfo.g_idx);
            ThirdLevel.gameObject.SetActive((third_lv - 1) > 0);
            ThirdLevel.text = ((third_lv - 1) > 0) ? SSLocalization.Format("heroinfo", 1, third_lv - 1) : string.Empty;

            EventDelegate.Set(ThirdTrigger.onClick, SetThirdSkillEnchant);
            EventDelegate.Set(ThirdTrigger.onPress,
                () =>
                {
                    PopupSkillInfo popup = PopupManager.instance.MakePopup<PopupSkillInfo>(ePopupType.SkillInfo);
                    popup.SetPressPopup(ThirdTrigger, new Vector2(220f, 0f));
                    popup.Set(SKILL_KIND.THIRD, third_idx, third_lv, true);
                });
        }

        ThirdEnchantEffect.SetActive(false);

        SkillIconGrid.Reposition();
    }

    private void SetPrimarySkillEnchant()
    {
        SelectType = SKILL_KIND.PRIMARY;

        Idx = HeroCompanionData.skillprimary;
        Lv = HeroCompanionData.skillprimarylv;
        
        NameLabel.text = TableManagerCS.instance.SkillPrimaryHeroTable.GetPrimaryName(Idx);
        ExplainLabel.text = TableManagerCS.instance.SkillPrimaryHeroTable.GetPrimaryExplain(Idx, Lv);
        CooltimeLabel.gameObject.SetActive(true);
        CooltimeLabel.text = TableManagerCS.instance.SkillPrimaryHeroTable.GetPrimaryCoolTime(Idx, Lv);


        for (int i = 0, enchantstep = 2; i < EnchantAbilityList.Count; ++i, ++enchantstep)
        {
            EnchantAbilityList[i].Set(SKILL_KIND.PRIMARY, Idx, enchantstep);
            EnchantAbilityList[i].SetActiveAbilitySlot(enchantstep <= Lv);
            EnchantAbilityList[i].SetEvent(OnPressAbilitySlot, OnReleaseAbilitySlot);
        }

        PrimarySelectObj.SetActive(true);
        SecondarySelectObj.SetActive(false);
        ThirdSelectObj.SetActive(false);

        CalcEnchantCost();
    }

    private void SetSecondarySkillEnchant()
    {
        SelectType = SKILL_KIND.SECONDARY;

        Idx = HeroCompanionData.skillsecondary;
        Lv = HeroCompanionData.skillsecondarylv;
        
        NameLabel.text = TableManagerCS.instance.SkillSecondaryHeroTable.GetSecondaryName(Idx);
        ExplainLabel.text = TableManagerCS.instance.SkillSecondaryHeroTable.GetSecondaryExplain(Idx, Lv);
        CooltimeLabel.gameObject.SetActive(false);

        for (int i = 0, enchantstep = 2; i < EnchantAbilityList.Count; ++i, ++enchantstep)
        {
            EnchantAbilityList[i].Set(SKILL_KIND.SECONDARY, Idx, enchantstep);
            EnchantAbilityList[i].SetActiveAbilitySlot(enchantstep <= Lv);
            EnchantAbilityList[i].SetEvent(OnPressAbilitySlot, OnReleaseAbilitySlot);
        }

        PrimarySelectObj.SetActive(false);
        SecondarySelectObj.SetActive(true);
        ThirdSelectObj.SetActive(false);

        CalcEnchantCost();
    }

    private void CalcEnchantCost()
    {
        switch (SelectType)
        {
            case SKILL_KIND.PRIMARY:
                {
                    bool isMaxLv = (Lv >= GameConstraints.SkillPrimaryEnchantMaxLv);

                    CostMaterialListGrid.gameObject.SetActive(!isMaxLv);
                    EnchantButton.gameObject.SetActive(!isMaxLv);
                    MaxLevelLabel.SetActive(isMaxLv);

                    if (!isMaxLv)
                    {
                        PrimaryDetailInfo info = TableManagerCS.instance.SkillPrimaryHeroTable.FindDetailInfo(Idx, Lv + 1);
                        CostType = info.type;
                        CostValue = info.v1;
                        CostMaterialList = TableManagerCS.instance.SkillPrimaryHeroTable.GetCostMaterialList(Idx, Lv + 1);

                        SetEnchantCost();
                    }
                }
                break;

            case SKILL_KIND.SECONDARY:
                {
                    bool isMaxLv = (Lv >= GameConstraints.SkillSecondaryEnchantMaxLv);

                    CostMaterialListGrid.gameObject.SetActive(!isMaxLv);
                    EnchantButton.gameObject.SetActive(!isMaxLv);
                    MaxLevelLabel.SetActive(isMaxLv);

                    if (!isMaxLv)
                    {
                        SecondaryDetailInfo info = TableManagerCS.instance.SkillSecondaryHeroTable.FindDetailInfo(Idx, Lv + 1);
                        CostType = info.type;
                        CostValue = info.v1;
                        CostMaterialList = TableManagerCS.instance.SkillSecondaryHeroTable.GetCostMaterialList(Idx, Lv + 1);

                        SetEnchantCost();
                    }
                }
                break;

            case SKILL_KIND.THIRD:
                {
                    bool isMaxLv = (Lv >= GameConstraints.SkillThirdEnchantMaxLv);

                    CostMaterialListGrid.gameObject.SetActive(!isMaxLv);
                    EnchantButton.gameObject.SetActive(!isMaxLv);
                    MaxLevelLabel.SetActive(isMaxLv);

                    if (!isMaxLv)
                    {
                        
                        ThirdDetailInfo info = TableManagerCS.instance.SkillThirdHeroTable.FindDetailInfo(Idx, Lv + 1);

                        ThirdEnchantSpendCostInfo Cost = TableManagerCS.instance.SkillThirdHeroTable.GetEnchantSpendCost(HeroBaseData.limitGroup, Lv + 1);

                        CostType = Cost.costType;
                        CostValue = Cost.v1;

                        CostMaterialList = TableManagerCS.instance.SkillThirdHeroTable.GetEnchantSpendMaterialList(Cost.mGroup);

                        SetEnchantCost();
                    }
                }
                break;
        }
    }

    private void SetEnchantCost()
    {
        CostMaterialListGrid.transform.DestroyChildren();

        bool bEnchantEnable = true;
        for (int i = 0; i < CostMaterialList.Count; i++)
        {
            Icon_ETC icon = ManagerCS.instance.MakeIcon<Icon_ETC>(CostMaterialListGrid.gameObject, ManagerCS.eIconType.ETC);
            RewardValueDesc item = CostMaterialList[i];
            icon.SetRewardETCInfo(item.type, item.v1, item.v2, item.v3);
            icon.SettingType(ManagerCS.eSettingType.Reward_Type);
            icon.SetPopup();

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

        CostTypeIcon.spriteName = ManagerCS.instance.FindSpriteIconName(CostType);
        CostValueLabel.text = CostValue.ToString("#,##0");
        EnchantButton.isEnabled = bEnchantEnable;
    }

    public void OnClickSkillEnchantButton()
    {
        PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common);
        popup.Set(PopupCommon.Type.Payment, SSLocalization.Get("heroInfo", 247), ReqEnchantingSkill);
        popup.SetPayment(CostType, CostValue);
    }

    private void ReqEnchantingSkill()
    {
        EnchantingSkillRequestParameter data = new EnchantingSkillRequestParameter();

        data.heromanidx = HeroCompanionData.manidx;
        data.skillid = Idx;
        data.skillslot = (short)(SelectType + 1);

        data.dfevtidx = ManagerCS.Getdfevtidx();
        data.bProgressDF = ManagerCS.GetbProgressDF();

        NetworkConnection.instance.NetCommand(PacketType.EnchantingSkillRequestParameter, data,
            (string _data) =>
            {
                if (NetworkCS.CheckNetworkError())
                {
                    EnchantingSkillResultParameter result = JsonConvert.DeserializeObject<EnchantingSkillResultParameter>(_data);

                    ManagerCS.instance.SetReceiveFinaceDataToUI(result.finance);
                    ManagerCS.instance.SetReceiveMissionDataToUI(result.successMissionList);
                    //ManagerCS.instance.SetReceiveGuildMissionDataToUI(result.successGuildMissionList);

                    if (!ManagerCS.instance.IsDiscountEventSync(DiscountCategory.HERO, PacketType.EnchantingSkillRequestParameter, result.bSync, result.dfInfo, null))
                        return;
                    
                    ManagerCS.instance.uiController.GetHero(result.hero_postinfo);              // 영웅 정보 갱신
                    ManagerCS.instance.uiController.GetCraftMaterial(result.sync_Materials);    // 재료 아이템 갱신

                    // 영웅 정보 갱신
                    HeroCompanionData = result.hero_postinfo;

                    StartCoroutine(StartSkillEnchantEffect(
                        () =>
                        {
                            // 미션 알림 팝업
                            ManagerCS.instance.MissionNoticePopup();
                            
                            TargetIcon.SetIcon(result.hero_postinfo);
                            TargetIcon.OnTarget(true);

                            SetHeroInfo(false);

                        }));
                }
            });
    }


    private IEnumerator StartSkillEnchantEffect(voidDelegate callback)
    {
        SoundPlay.instance.SetSoundPlay(2131, SOUND_TYPE.EFFECT_TYPE, 1);
        SoundPlay.instance.SetSoundPlay(2132, SOUND_TYPE.EFFECT_TYPE, 0);

        // UI Lock
        ManagerCS.instance.IsTopUILock = true;
        UILockPanel.SetActive(true);

        if (TargetHeroIconEffect.activeSelf)
            TargetHeroIconEffect.SetActive(false);

        TargetHeroIconEffect.SetActive(true);

        yield return new WaitForSeconds(1f);

        switch (SelectType)
        {
            case SKILL_KIND.PRIMARY:
                {
                    if (PrimaryEnchantEffect.activeSelf)
                        PrimaryEnchantEffect.SetActive(false);

                    PrimaryEnchantEffect.SetActive(true);

                    EnchantAbilitySlot targetSlot = EnchantAbilityList.Find(row => row.EnchantLv == HeroCompanionData.skillprimarylv);
                    if (targetSlot != null)
                        targetSlot.ActiveSlotEffect();
                }
                break;

            case SKILL_KIND.SECONDARY:
                {
                    if (SecondaryEnchantEffect.activeSelf)
                        SecondaryEnchantEffect.SetActive(false);

                    SecondaryEnchantEffect.SetActive(true);

                    EnchantAbilitySlot targetSlot = EnchantAbilityList.Find(row => row.EnchantLv == HeroCompanionData.skillsecondarylv);
                    if (targetSlot != null)
                        targetSlot.ActiveSlotEffect();
                }
                break;

            case SKILL_KIND.THIRD:
                {
                    if (ThirdEnchantEffect.activeSelf)
                        ThirdEnchantEffect.SetActive(false);

                    ThirdEnchantEffect.SetActive(true);

                    EnchantAbilitySlot targetSlot = EnchantAbilityList.Find(row => row.EnchantLv == HeroCompanionData.skillthirdlv);
                    if (targetSlot != null)
                        targetSlot.ActiveSlotEffect();
                }
                break;
        }
        
        yield return new WaitForSeconds(2f);

        // UI Unlock
        ManagerCS.instance.IsTopUILock = false;
        UILockPanel.SetActive(false);

        // CallBack
        if (callback != null)
            callback();
    }

    private void OnPressAbilitySlot(int idx, int lv)
    {
        if (UIEventTrigger.current.transform == null)
            return;

        TooltipGrid.transform.DestroyChildren();

        List<RewardValueDesc> materialList = new List<RewardValueDesc>();
        switch (SelectType)
        {
            case SKILL_KIND.PRIMARY:
                materialList = TableManagerCS.instance.SkillPrimaryHeroTable.GetCostMaterialList(idx, lv);
                break;

            case SKILL_KIND.SECONDARY:
                materialList = TableManagerCS.instance.SkillSecondaryHeroTable.GetCostMaterialList(idx, lv);
                break;
        }
        
        for (int i = 0; i < materialList.Count; i++)
        {
            GameObject obj = NGUITools.AddChild(TooltipGrid.gameObject, TooltipListItem);
            SkillEnchantTooltipList item = obj.GetComponent<SkillEnchantTooltipList>();
            item.SetCostMaterial(materialList[i]);
        }

        TooltipGrid.enabled = true;
        TooltipGrid.Reposition();

        TooltipBG.SetDimensions(TooltipBG.width, 80 + (int)(TooltipGrid.cellHeight * materialList.Count));

        Tooltip.SetActive(true);

        Tooltip.transform.position = UIEventTrigger.current.transform.position;
        Tooltip.transform.localPosition = Tooltip.transform.localPosition + new Vector3(100f, 50f, 0f);
        
        float tooltipPos = TooltipBG.worldCorners[0].y;
        float bottomPos = transform.GetComponentInChildren<Camera>().ScreenToWorldPoint(new Vector3(0, 30f, 0)).y;

        if (tooltipPos < bottomPos)
            Tooltip.transform.position = Tooltip.transform.position + new Vector3(0f, (bottomPos - tooltipPos), 0f);
    }

    private void OnPressThirdAbilitySlot(int idx, int lv)
    {
        if (UIEventTrigger.current.transform == null)
            return;

        TooltipGrid.transform.DestroyChildren();

        ThirdEnchantSpendCostInfo Cost = TableManagerCS.instance.SkillThirdHeroTable.GetEnchantSpendCost(HeroBaseData.limitGroup, lv);

        List<RewardValueDesc> materialList = TableManagerCS.instance.SkillThirdHeroTable.GetEnchantSpendMaterialList(Cost.mGroup);

        for (int i = 0; i < materialList.Count; i++)
        {
            GameObject obj = NGUITools.AddChild(TooltipGrid.gameObject, TooltipListItem);
            SkillEnchantTooltipList item = obj.GetComponent<SkillEnchantTooltipList>();
            item.SetCostMaterial(materialList[i]);
        }

        TooltipGrid.enabled = true;
        TooltipGrid.Reposition();

        TooltipBG.SetDimensions(TooltipBG.width, 80 + (int)(TooltipGrid.cellHeight * materialList.Count));

        Tooltip.SetActive(true);

        Tooltip.transform.position = UIEventTrigger.current.transform.position;
        Tooltip.transform.localPosition = Tooltip.transform.localPosition + new Vector3(100f, 50f, 0f);

        float tooltipPos = TooltipBG.worldCorners[0].y;
        float bottomPos = transform.GetComponentInChildren<Camera>().ScreenToWorldPoint(new Vector3(0, 30f, 0)).y;

        if (tooltipPos < bottomPos)
            Tooltip.transform.position = Tooltip.transform.position + new Vector3(0f, (bottomPos - tooltipPos), 0f);
    }

    private void OnReleaseAbilitySlot()
    {
        Tooltip.SetActive(false);
    }

    private void SetHeroStorageCount()
    {
        HeroStorageCount.text = string.Format("[b]{0} / {1}[/b]", GameData.Heroes.Count, GameData.HeroMaxCapacity);
    }

    public void OnClickExpandHeroStorageButton()
    {
        ManagerCS.instance.ExpandHeroStoragePopup();
    }

    #region Third
    private void SetThirdSkillEnchant()
    {
        SelectType = SKILL_KIND.THIRD;

        Idx = HeroCompanionData.skillthird;
        Lv = HeroCompanionData.skillthirdlv;

        NameLabel.text = TableManagerCS.instance.SkillThirdHeroTable.GetThirdName(Idx);
        ExplainLabel.text = TableManagerCS.instance.SkillThirdHeroTable.GetThirdExplain(Idx, Lv);
        CooltimeLabel.gameObject.SetActive(false);

        for (int i = 0, enchantstep = 2; i < EnchantAbilityList.Count; ++i, ++enchantstep)
        {
            EnchantAbilityList[i].Set(SKILL_KIND.THIRD, Idx, enchantstep);
            EnchantAbilityList[i].SetActiveAbilitySlot(enchantstep <= Lv);
            EnchantAbilityList[i].SetEvent(OnPressThirdAbilitySlot, OnReleaseAbilitySlot);
        }

        PrimarySelectObj.SetActive(false);
        SecondarySelectObj.SetActive(false);
        ThirdSelectObj.SetActive(true);

        CalcEnchantCost();
    }
    #endregion

    internal override void OnUpdateExpandHeroStorage()
    {
        SetHeroStorageCount();
    }

    internal override void OnUpdateHeroMaterial()
    {
        RefreshMaterial();
    }

    private void RefreshMaterial()
    {
        SetEnchantCost();
    }

    // 영웅리스트 갱신
    internal override void OnUpdateHeroList()
    {
        SetHeroList();
        SetSortingButton();
        SetHeroStorageCount();
    }

    internal override void UIRefresh()
    {
        TargetHeroIconEffect.SetActive(false);

        for (int i = 0; i < EnchantAbilityList.Count; i++)
            EnchantAbilityList[i].SlotEffect.SetActive(false);
    }
}
