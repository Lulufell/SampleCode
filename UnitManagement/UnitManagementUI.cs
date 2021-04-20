using UnityEngine;
using System;
using System.Collections.Generic;
using ServerCommon;
using ServerCommon.DataTables;
using ServerCommon.Tutorials;
using Newtonsoft.Json;


public class UnitManagementUI : GameNotification
{
    // Data
    private HeroCompanion HeroCompanionData;
    private HeroBases HeroBaseData;
    
    private List<HeroCompanion> HeroList;       // 영웅 리스트
    private List<Icon_HeroList> HeroIconList;

    private Icon_HeroList TargetIcon;
    
    private bool isCurrentLock = false;

    private List<int> ActiveSetItemList = null;


    // UI - Left
    public UISprite SynastryIcon;
    public UILabel SynastryLabel;
    public UISprite HeroTypeIcon;
    public UILabel HeroTypeLabel;

    public UILabel Title;
    public UILabel Name;
    public UILabel Level;
    public GradeObject Grade;

    public UIProgressBar ExpGauge;
    public UILabel Exp;

    public UILabel BattlePower;
    public UILabel[] HeroStatus;
    public UILabel[] AddedStatus;

    public UIEventTrigger PrimaryTrigger;
    public UITexture PrimaryIcon;
    public UILabel PrimaryLevel;
    
    public UIEventTrigger SecondaryTrigger;
    public UITexture SecondaryIcon;
    public UILabel SecondaryLevel;

    public UIEventTrigger ThirdTrigger;
    public UITexture ThirdIcon;
    public GameObject ThirdDefaultObj;
    public UILabel ThirdLevel;

    public UIEventTrigger LeaderAbilityIcon;
    public UIEventTrigger SupporterAbilityIcon;
    public UIEventTrigger UniqueAbilityIcon;

    public GameObject ImprintSlotObject;
    public UISprite ImprintIcon;
    public UILabel ImprintLevel;
    public UILabel ImprintOption;
    public GameObject ImprintLockIcon;
    
    public TweenPosition DetailInfoTween;
    public GameObject DetailInfoButtonArrow;
    private bool bDetailInfoFlag = true;

    public GameObject BattlePowerObj;
    public GameObject MainStatusObj;
    public GameObject DetailInfoObj;
    public GameObject ImprintNoticeObj;


    // UI - Center
    public CharModelViewUI CharacterModelView;
    public GameObject[] EquipmentIconPosition;
    public GameObject[] EquipmentSlotEffect;
    public UIEventTrigger[] EquipmentDefaultIcon;

    public UIToggle LockToggle;
    public GameObject ExtractionBtnObj;

    [Serializable]
    public class UIButtonSet
    {
        public UIButton button;
        public GameObject lockIcon;
        public GameObject alarmIcon;

        public void SetActive(bool isActive)
        {
            button.gameObject.SetActive(isActive);
        }
        
        public bool isEnabled
        {
            get { return button.isEnabled; }
            set { button.isEnabled = value; }
        }

        public bool isLock
        {
            get { return (lockIcon != null) ? lockIcon.activeSelf : false; }
            set { if (lockIcon != null) lockIcon.SetActive(value); }
        }

        public bool isAlarm
        {
            set { if (alarmIcon != null) alarmIcon.SetActive(value); }
        }
    }
    public UIButtonSet ExpObtainButton;
    public UIButtonSet EvolutionButton;
    public UIButtonSet ImprintButton;
    public UIButtonSet ArousalButton;
    public UIButtonSet SkillEnchantButton;
    public UIButtonSet CompositionButton;
    

    // UI - Right
    public UnitListScrollView HeroListScrollView;
    public HeroSortingButton SortingButton;
    public UIButton FilteringButton;
    public UILabel HeroStorageCount;


    private void Awake()
    {
        HeroIconList = new List<Icon_HeroList>();

        // Tutorial
        if (ManagerCS.instance.IsActiveContents(eHeroContents.Equip_EnterLimit))
            TutorialManager.instance.StartTutorial(TutorialID.Equipment);
    }

    private void OnEnable()
    {
        ActiveSetItemEffect();
    }

    protected override void Init()
    {
        SetHeroList();
        SetSortingButton();
        SetHeroStorageCount();

        HeroCompanionData = (ManagerCS.instance.targetHeroData != null) ? ManagerCS.instance.targetHeroData : HeroIconList[0].HeroCompanionData;
        HeroBaseData = HeroCompanionData.c_base;

        SetDetailInfo();
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

        SetDetailInfo();
    }

    private void SetSortingButton()
    {
        if (TutorialManager.instance.CurrentTutorial == TutorialID.Arousal_Guide)
            ManagerCS.instance.eUnitListSortingOption = HeroSortingOption.Grade;

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

            if (icon == null)
            {
                HeroIconList.Sort(SortingClass.CompareHeroList);
                icon = HeroIconList[0];
            }
        }

        HeroListScrollView.TargetOn(icon.transform, true);

        HeroCompanionData = icon.HeroCompanionData;
        HeroBaseData = HeroCompanionData.c_base;

        SetDetailInfo();
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
    
    private void SetDetailInfo()
    {
        CheckActiveInfo();

        SetModelling();
        SetHeroInfo();
        SetStatus();
        SetSkill();
        SetImprint();
        SetEquipment();
        SetLock();
        SetButtonEnabled();
        
        // 선택 영웅정보 저장
        ManagerCS.instance.targetHeroData = HeroCompanionData;
    }

    private void CheckActiveInfo()
    {
        bool bActive = (HeroBaseData.bSpecial != HeroSpecialType.ImprintType);

        ExpGauge.gameObject.SetActive(bActive);
        BattlePowerObj.SetActive(bActive);
        MainStatusObj.SetActive(bActive);
        DetailInfoObj.SetActive(bActive);
        ExtractionBtnObj.SetActive(bActive);
        ImprintNoticeObj.SetActive(!bActive);
    }

    private void SetHeroInfo()
    {
        // Set HeroInfo
        Title.text = string.Format("[b]{0}[/b]", HeroBaseData.GetTitle());
        Name.text = string.Format("[b]{0}[/b]", HeroBaseData.GetName());
        Level.text = string.Format("[b]{0}[f5c649] / {1}[-][/b]", SSLocalization.Format("heroinfo", 1, HeroCompanionData.lv), HeroCompanionData.c_base.maxlevel);
        Grade.SetGrade(HeroBaseData.grade, HeroCompanionData.arousalStep);

        SynastryIcon.spriteName = ManagerCS.instance.GetSynastryIcon(HeroBaseData.synastry);
        SynastryLabel.color = GameUtils.GetSynastryColor(HeroBaseData.synastry);
        SynastryLabel.text = GameUtils.GetSynastryText(HeroBaseData.synastry);

        HeroTypeIcon.spriteName = ManagerCS.instance.GetHeroTypeIcon(HeroBaseData.herotype);
        HeroTypeLabel.text = GameUtils.GetHeroTypeText(HeroBaseData.herotype);

        
        if (HeroBaseData.bSpecial == HeroSpecialType.ImprintType)
            return;

        // ExpGauge Setting
        if (HeroCompanionData.lv >= HeroBaseData.maxlevel)
        {
            ExpGauge.value = 1f;
            Exp.text = SSLocalization.Get("heroInfo", 4);
        }
        else
        {
            int needExp = TableManagerCS.instance.HeroTable.GetNeedExp(HeroBaseData, HeroCompanionData.lv);

            ExpGauge.value = (float)HeroCompanionData.exp / needExp;
            Exp.text = string.Format("{0}/{1}", HeroCompanionData.exp, needExp);
        }
    }

    private void SetStatus()
    {
        if (HeroBaseData.bSpecial == HeroSpecialType.ImprintType)
            return;
        
        // Set Status
        BattlePower.text = string.Format("[b]{0}[/b]", HeroStatusManager.instance.Calc_BattlePower(HeroCompanionData).ToString("#,##0"));

        // <-- 기본 스탯 -->
        CombatUnitAbilities_Client _Status = HeroStatusManager.instance.Calc_HeroTotalStatus(HeroCompanionData);
        Color mainLabelOrginColor = GameUtils.GetColor(208, 208, 208);

        // 메인스탯
        HeroStatus[(int)HeroStatusInfo.Atk].text = _Status.GetStatusText(HeroStatusInfo.Atk);
        HeroStatus[(int)HeroStatusInfo.Atk].color = _Status.IsUniqueDmgEffect(HeroStatusInfo.Atk) == true ? AddedStatus[(int)HeroStatusInfo.Atk].color : mainLabelOrginColor;
        HeroStatus[(int)HeroStatusInfo.Def].text = _Status.GetStatusText(HeroStatusInfo.Def);
        HeroStatus[(int)HeroStatusInfo.Def].color = _Status.IsUniqueDmgEffect(HeroStatusInfo.Def) == true ? AddedStatus[(int)HeroStatusInfo.Def].color : mainLabelOrginColor;
        HeroStatus[(int)HeroStatusInfo.HP].text = _Status.GetStatusText(HeroStatusInfo.HP);
        HeroStatus[(int)HeroStatusInfo.HP].color = _Status.IsUniqueDmgEffect(HeroStatusInfo.HP) == true ? AddedStatus[(int)HeroStatusInfo.HP].color : mainLabelOrginColor;

        // 서브스탯
        HeroStatus[(int)HeroStatusInfo.AtkSpeed].text = _Status.GetStatusText(HeroStatusInfo.AtkSpeed);
        HeroStatus[(int)HeroStatusInfo.MoveSpeed].text = _Status.GetStatusText(HeroStatusInfo.MoveSpeed);
        HeroStatus[(int)HeroStatusInfo.CritRate].text = _Status.GetStatusText(HeroStatusInfo.CritRate);
        HeroStatus[(int)HeroStatusInfo.CritPower].text = _Status.GetStatusText(HeroStatusInfo.CritPower);
        HeroStatus[(int)HeroStatusInfo.HitRate].text = _Status.GetStatusText(HeroStatusInfo.HitRate);
        HeroStatus[(int)HeroStatusInfo.DodgeRate].text = _Status.GetStatusText(HeroStatusInfo.DodgeRate);
        HeroStatus[(int)HeroStatusInfo.EffectHit].text = _Status.GetStatusText(HeroStatusInfo.EffectHit);
        //HeroStatus[(int)HeroStatusInfo.EffectResist].text = _Status.GetStatusText(HeroStatusInfo.EffectResist);
        HeroStatus[(int)HeroStatusInfo.CCResist].text = _Status.GetStatusText(HeroStatusInfo.CCResist);
        HeroStatus[(int)HeroStatusInfo.DotResist].text = _Status.GetStatusText(HeroStatusInfo.DotResist);
        HeroStatus[(int)HeroStatusInfo.DeResist].text = _Status.GetStatusText(HeroStatusInfo.DeResist);


        // <-- 추가 스탯 -->
        CombatUnitAbilities_Client _AddedStatus = HeroStatusManager.instance.Calc_HeroAddedStatus(HeroCompanionData);
        string format = "(+{0})";

        // 추가 메인스탯
        AddedStatus[(int)HeroStatusInfo.Atk].text = (_AddedStatus.atkPW == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.Atk));
        AddedStatus[(int)HeroStatusInfo.Def].text = (_AddedStatus.defPW == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.Def));
        AddedStatus[(int)HeroStatusInfo.HP].text = (_AddedStatus.maxHp == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.HP));

        // 추가 서브스탯
        AddedStatus[(int)HeroStatusInfo.AtkSpeed].text = (_AddedStatus.atkSPD == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.AtkSpeed));
        AddedStatus[(int)HeroStatusInfo.MoveSpeed].text = (_AddedStatus.movSPD == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.MoveSpeed));
        AddedStatus[(int)HeroStatusInfo.CritRate].text = (_AddedStatus.criC == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.CritRate));
        AddedStatus[(int)HeroStatusInfo.CritPower].text = (_AddedStatus.maxCriD == 0 && _AddedStatus.criDmgInc == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.CritPower));
        AddedStatus[(int)HeroStatusInfo.HitRate].text = (_AddedStatus.HitChance == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.HitRate));
        AddedStatus[(int)HeroStatusInfo.DodgeRate].text = (_AddedStatus.dodgeC == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.DodgeRate));
        AddedStatus[(int)HeroStatusInfo.EffectHit].text = (_AddedStatus.EffectHit == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.EffectHit));
        //AddedStatus[(int)HeroStatusInfo.EffectResist].text = (_AddedStatus.EffectRes == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.EffectResist));
        AddedStatus[(int)HeroStatusInfo.CCResist].text = (_AddedStatus.ccRes == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.CCResist));
        AddedStatus[(int)HeroStatusInfo.DotResist].text = (_AddedStatus.dotRes == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.DotResist));
        AddedStatus[(int)HeroStatusInfo.DeResist].text = (_AddedStatus.deRes == 0) ? string.Empty : string.Format(format, _AddedStatus.GetStatusText(HeroStatusInfo.DeResist));
    }

    private void SetEquipment()
    {
        foreach (GameObject position in EquipmentIconPosition)
        {
            Icon_Equipment[] iconList = position.transform.GetComponentsInChildren<Icon_Equipment>();

            foreach (Icon_Equipment icon in iconList)
            {
                if (icon != null)
                    DestroyImmediate(icon.gameObject);
            }
        }

        EquipItemAbility equipAbility = null;

        // Weapon
        if (HeroCompanionData.wpnidx() != 0)
        {
            equipAbility = TableManagerCS.instance.EquipItemTable.FindAbility(HeroCompanionData.wpnidx());

            Icon_Equipment icon = ManagerCS.instance.MakeIcon<Icon_Equipment>(EquipmentIconPosition[(int)EquipUICategory.Weapon], ManagerCS.eIconType.Equipment);
            icon.transform.SetChildLayer(LayerMask.NameToLayer("3DUICamera_Layer"));
            icon.SetEquipmentIcon(equipAbility, HeroCompanionData.wpn);
            icon.SetPopup();
            icon.SetOnClickCallBack(
                () =>
                {
                    OnClickEquipmentIcon(icon, EquipDefineCategory.Equipment);
                });

            icon.SettingType(ManagerCS.eSettingType.B_Type);
        }

        // Armor
        if (HeroCompanionData.amridx() != 0)
        {
            equipAbility = TableManagerCS.instance.EquipItemTable.FindAbility(HeroCompanionData.amridx());

            Icon_Equipment icon = ManagerCS.instance.MakeIcon<Icon_Equipment>(EquipmentIconPosition[(int)EquipUICategory.Armor], ManagerCS.eIconType.Equipment);
            icon.transform.SetChildLayer(LayerMask.NameToLayer("3DUICamera_Layer"));
            icon.SetEquipmentIcon(equipAbility, HeroCompanionData.amr);
            icon.SetPopup();
            icon.SetOnClickCallBack(
                () =>
                {
                    OnClickEquipmentIcon(icon, EquipDefineCategory.Equipment);
                });

            icon.SettingType(ManagerCS.eSettingType.B_Type);
        }

        // Accessory
        if (HeroCompanionData.asr1idx() != 0)
        {
            equipAbility = TableManagerCS.instance.EquipItemTable.FindAbility(HeroCompanionData.asr1idx());

            Icon_Equipment icon = ManagerCS.instance.MakeIcon<Icon_Equipment>(EquipmentIconPosition[(int)EquipUICategory.Accessory], ManagerCS.eIconType.Equipment);
            icon.transform.SetChildLayer(LayerMask.NameToLayer("3DUICamera_Layer"));
            icon.SetEquipmentIcon(equipAbility, HeroCompanionData.asr1);
            icon.SetPopup();
            icon.SetOnClickCallBack(
                () =>
                {
                    OnClickEquipmentIcon(icon, EquipDefineCategory.Equipment);
                });

            icon.SettingType(ManagerCS.eSettingType.B_Type);
        }

        RuneAbility runeAbility = null;

        // RuneTop
        if (HeroCompanionData.runetopidx() != 0)
        {
            runeAbility = TableManagerCS.instance.RuneTable.GetRuneAbility(HeroCompanionData.runetop);

            Icon_Equipment icon = ManagerCS.instance.MakeIcon<Icon_Equipment>(EquipmentIconPosition[(int)EquipUICategory.RuneTop], ManagerCS.eIconType.Equipment);
            icon.transform.SetChildLayer(LayerMask.NameToLayer("3DUICamera_Layer"));
            icon.SetRuneIcon(HeroCompanionData.runetop, runeAbility);
            icon.SetPopup();
            icon.SetOnClickCallBack(
                () =>
                {
                    OnClickEquipmentIcon(icon, EquipDefineCategory.Rune);
                });

            icon.SettingType(ManagerCS.eSettingType.B_Type);
        }

        // RuneMid
        if (HeroCompanionData.runemididx() != 0)
        {
            runeAbility = TableManagerCS.instance.RuneTable.GetRuneAbility(HeroCompanionData.runemid);

            Icon_Equipment icon = ManagerCS.instance.MakeIcon<Icon_Equipment>(EquipmentIconPosition[(int)EquipUICategory.RuneMid], ManagerCS.eIconType.Equipment);
            icon.transform.SetChildLayer(LayerMask.NameToLayer("3DUICamera_Layer"));
            icon.SetRuneIcon(HeroCompanionData.runemid, runeAbility);
            icon.SetPopup();
            icon.SetOnClickCallBack(
                () =>
                {
                    OnClickEquipmentIcon(icon, EquipDefineCategory.Rune);
                });

            icon.SettingType(ManagerCS.eSettingType.B_Type);
        }

        // RuneBot
        if (HeroCompanionData.runebottomidx() != 0)
        {
            runeAbility = TableManagerCS.instance.RuneTable.GetRuneAbility(HeroCompanionData.runebottom);

            Icon_Equipment icon = ManagerCS.instance.MakeIcon<Icon_Equipment>(EquipmentIconPosition[(int)EquipUICategory.RuneBot], ManagerCS.eIconType.Equipment);
            icon.transform.SetChildLayer(LayerMask.NameToLayer("3DUICamera_Layer"));
            icon.SetRuneIcon(HeroCompanionData.runebottom, runeAbility);
            icon.SetPopup();
            icon.SetOnClickCallBack(
                () =>
                {
                    OnClickEquipmentIcon(icon, EquipDefineCategory.Rune);
                });

            icon.SettingType(ManagerCS.eSettingType.B_Type);
        }


        // Default Icon
        EquipmentDefaultIcon[(int)EquipUICategory.Weapon].gameObject.SetActive(HeroCompanionData.wpnidx() == 0);
        EventDelegate.Set(EquipmentDefaultIcon[(int)EquipUICategory.Weapon].onClick,
            () =>
            {
                OnClickDefaultIcon(EquipCategory.Weapon);
            });

        EquipmentDefaultIcon[(int)EquipUICategory.Armor].gameObject.SetActive(HeroCompanionData.amridx() == 0);
        EventDelegate.Set(EquipmentDefaultIcon[(int)EquipUICategory.Armor].onClick,
            () =>
            {
                OnClickDefaultIcon(EquipCategory.Armor);
            });

        EquipmentDefaultIcon[(int)EquipUICategory.Accessory].gameObject.SetActive(HeroCompanionData.asr1idx() == 0);
        EventDelegate.Set(EquipmentDefaultIcon[(int)EquipUICategory.Accessory].onClick,
            () =>
            {
                OnClickDefaultIcon(EquipCategory.Accessary);
            });

        EquipmentDefaultIcon[(int)EquipUICategory.RuneTop].gameObject.SetActive(HeroCompanionData.runetopidx() == 0);
        EventDelegate.Set(EquipmentDefaultIcon[(int)EquipUICategory.RuneTop].onClick,
            () =>
            {
                OnClickDefaultIcon(RuneSlot.Top);
            });

        EquipmentDefaultIcon[(int)EquipUICategory.RuneMid].gameObject.SetActive(HeroCompanionData.runemididx() == 0);
        EventDelegate.Set(EquipmentDefaultIcon[(int)EquipUICategory.RuneMid].onClick,
            () =>
            {
                OnClickDefaultIcon(RuneSlot.Middle);
            });

        EquipmentDefaultIcon[(int)EquipUICategory.RuneBot].gameObject.SetActive(HeroCompanionData.runebottomidx() == 0);
        EventDelegate.Set(EquipmentDefaultIcon[(int)EquipUICategory.RuneBot].onClick,
            () =>
            {
                OnClickDefaultIcon(RuneSlot.Bottom);
            });
    }

    private void OnClickEquipmentIcon(Icon_Equipment icon, EquipDefineCategory category)
    {
        if (category == EquipDefineCategory.Equipment)
        {
            PopupInventoryEquipmentItem popup = PopupManager.instance.MakePopup<PopupInventoryEquipmentItem>(ePopupType.InventoryEquipmentItem);
            popup.InitInventoryEquipment(icon, InventoryMainTab.WearEquipment, ManagerCS.instance.targetHeroData);
        }
        else if (category == EquipDefineCategory.Rune)
        {
            PopupInventoryEquipmentItem popup = PopupManager.instance.MakePopup<PopupInventoryEquipmentItem>(ePopupType.InventoryEquipmentItem);
            popup.InitInventoryRune(icon, InventoryMainTab.WearEquipment, ManagerCS.instance.targetHeroData);
        }
    }

    private void OnClickDefaultIcon(EquipCategory category)
    {
        if (HeroBaseData.bSpecial == HeroSpecialType.ImprintType)
        {
            PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice).Set(SSLocalization.Get("heroInfo", 319));
            return;
        }

        ManagerCS.instance.EquipmentWearInfo = new ManagerCS.EquipmentWearRequest(ManagerCS.instance.targetHeroData, category);
        ManagerCS.instance.uiController.ChangeUI(UIState.EquipmentWear, UIChangeType.SaveUI);
    }

    private void OnClickDefaultIcon(RuneSlot category)
    {
        if (HeroBaseData.bSpecial == HeroSpecialType.ImprintType)
        {
            PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice).Set(SSLocalization.Get("heroInfo", 319));
            return;
        }

        ManagerCS.instance.RuneWearInfo = new ManagerCS.RuneWearRequest(ManagerCS.instance.targetHeroData, category);
        ManagerCS.instance.uiController.ChangeUI(UIState.RuneWear, UIChangeType.SaveUI);
    }

    private void SetLock()
    {
        SetLock(GameData.LockedHeromanidxList.Find(row => row.value == HeroCompanionData.manidx) != null);
    }

    private void SetLock(bool isLock)
    {
        // Lock Setting
        isCurrentLock = isLock;
        LockToggle.value = isLock;
    }

    private void SetModelling()
    {
        // Character Model
        CharacterModelView.DestroyModel();
        CharacterModelView.MakeSimpleModel(HeroCompanionData.idx, HeroCompanionData.costidx, HeroCompanionData.cosmanidx);
        CharacterModelView.transform.SetChildLayer(LayerMask.NameToLayer("Character_Layer"));
    }

    private void SetSkill()
    {
        if (HeroBaseData.bSpecial == HeroSpecialType.ImprintType)
            return;

        // 임시
        if (HeroCompanionData.skillprimary == 0)
            return;


        // Primary
        int primary_idx = HeroCompanionData.skillprimary;
        int primary_lv = HeroCompanionData.skillprimarylv;

        PrimaryBasicInfo primaryInfo = TableManagerCS.instance.SkillPrimaryHeroTable.FindBasicInfo(primary_idx);

        Resmanager.LoadTextureToAsset(ref PrimaryIcon, ASSET_TYPE.ASSET_ICON_SKILL, primaryInfo.g_index);
        PrimaryLevel.gameObject.SetActive((primary_lv - 1) > 0);
        PrimaryLevel.text = ((primary_lv - 1) > 0) ? SSLocalization.Format("heroinfo", 1, primary_lv - 1) : string.Empty;

        EventDelegate.Set(PrimaryTrigger.onPress,
            () =>
            {
                PopupSkillInfo popup = PopupManager.instance.MakePopup<PopupSkillInfo>(ePopupType.SkillInfo);
                popup.SetPressPopup(PrimaryTrigger, new Vector2(220f, 0f));
                popup.Set(SKILL_KIND.PRIMARY, primary_idx, primary_lv, true);
            });
        

        // Secondary
        int secondary_idx = HeroCompanionData.skillsecondary;
        int secondary_lv = HeroCompanionData.skillsecondarylv;

        SecondaryBasicInfo secondaryInfo = TableManagerCS.instance.SkillSecondaryHeroTable.FindBasicInfo(secondary_idx);

        Resmanager.LoadTextureToAsset(ref SecondaryIcon, ASSET_TYPE.ASSET_ICON_SKILL, secondaryInfo.g_index);
        SecondaryLevel.gameObject.SetActive((secondary_lv - 1) > 0);
        SecondaryLevel.text = ((secondary_lv - 1) > 0) ? SSLocalization.Format("heroinfo", 1, secondary_lv - 1) : string.Empty;

        EventDelegate.Set(SecondaryTrigger.onPress,
            () =>
            {
                PopupSkillInfo popup = PopupManager.instance.MakePopup<PopupSkillInfo>(ePopupType.SkillInfo);
                popup.SetPressPopup(SecondaryTrigger, new Vector2(220f, 0f));
                popup.Set(SKILL_KIND.SECONDARY, secondary_idx, secondary_lv, true);
            });


        if (HeroBaseData.bSpecial == HeroSpecialType.NormalType)
        {
            ThirdTrigger.gameObject.SetActive(true);

            // Third
            int third_idx = HeroCompanionData.skillthird;
            int third_lv = HeroCompanionData.skillthirdlv;

            bool isHaveThird = third_idx > 0;

            ThirdDefaultObj.SetActive(!isHaveThird);
            ThirdIcon.gameObject.SetActive(isHaveThird);

            ThirdTrigger.onClick.Clear();
            ThirdTrigger.onPress.Clear();

            ThirdLevel.gameObject.SetActive((third_lv - 1) > 0);

            if (isHaveThird == true)
            {
                ThirdBasicInfo thirdInfo = TableManagerCS.instance.SkillThirdHeroTable.FindBasicInfo(third_idx);

                Resmanager.LoadTextureToAsset(ref ThirdIcon, ASSET_TYPE.ASSET_ICON_SKILL, thirdInfo.g_idx);
                ThirdLevel.text = ((third_lv - 1) > 0) ? SSLocalization.Format("heroinfo", 1, third_lv - 1) : string.Empty;

                EventDelegate.Set(ThirdTrigger.onPress,
                    () =>
                    {
                        PopupSkillInfo popup = PopupManager.instance.MakePopup<PopupSkillInfo>(ePopupType.SkillInfo);
                        popup.SetPressPopup(ThirdTrigger, new Vector2(220f, 0f));
                        popup.Set(SKILL_KIND.THIRD, third_idx, third_lv, true);
                    });

                EventDelegate.Set(ThirdTrigger.onClick,
                  () =>
                  {
                      PopupManager.instance.MakePopup<PopupArousalSkillInfo>(ePopupType.ArousalSkillInfo, false, true).Set(HeroCompanionData);
                  });
            }
            else
            {
                EventDelegate.Set(ThirdTrigger.onClick,
                () =>
                {
                    PopupManager.instance.MakePopup<PopupArousalSkillHelp>(ePopupType.ArousalSkillHelp).Set(HeroBaseData.skillthird);
                });
            }
        }
        else
        {
            ThirdTrigger.gameObject.SetActive(false);
        }



        // Leader Ability
        EventDelegate.Set(LeaderAbilityIcon.onPress,
            () =>
            {
                PopupSkillInfo popup = PopupManager.instance.MakePopup<PopupSkillInfo>(ePopupType.SkillInfo);
                popup.SetPressPopup(LeaderAbilityIcon, new Vector2(220f, 0f));

                string title = SSLocalization.Get("heroInfo", 253);
                string explain = TableManagerCS.instance.HeroTable.GetLeaderAbilityExplain(HeroBaseData.uidx);
                popup.Set(title, explain, "", 0);
            });

        // Supporter Ability
        EventDelegate.Set(SupporterAbilityIcon.onPress,
            () =>
            {
                PopupSkillInfo popup = PopupManager.instance.MakePopup<PopupSkillInfo>(ePopupType.SkillInfo);
                popup.SetPressPopup(SupporterAbilityIcon, new Vector2(220f, 0f));

                string title = SSLocalization.Get("heroInfo", 254);

                string synastryExplain = TableManagerCS.instance.HeroTable.GetSupporterSynastryAbilityExplain(HeroBaseData.uidx);
                string defaultExplain = TableManagerCS.instance.HeroTable.GetSupporterAbilityExplain(HeroBaseData.uidx);
                string explain = string.Format("{0}\n{1}", synastryExplain, defaultExplain);

                popup.Set(title, explain, "", 0);
            });

        // Unique Ability
        EventDelegate.Set(UniqueAbilityIcon.onPress,
            () =>
            {
                PopupSkillInfo popup = PopupManager.instance.MakePopup<PopupSkillInfo>(ePopupType.SkillInfo);
                popup.SetPressPopup(UniqueAbilityIcon, new Vector2(220f, 0f));

                string title = SSLocalization.Get("heroInfo", 255);
                string explain = TableManagerCS.instance.UniqueDmgEffectTable.GetUniqueAbilityExplain(HeroBaseData.uEffectGroup);
                popup.Set(title, explain, "", 0);
            });
    }

    private void SetImprint()
    {
        if (HeroBaseData.bSpecial == HeroSpecialType.ImprintType)
            return;

        if (HeroCompanionData.c_base.imprintGroup != 0)
        {
            ImprintSlotObject.SetActive(true);

            ImprintIcon.color = TableManagerCS.instance.HeroImprintTable.GetColor(HeroCompanionData.c_base.bornGrade, HeroCompanionData.imprintStep);

            ImprintLevel.text = TableManagerCS.instance.HeroImprintTable.GetLevel(HeroCompanionData.c_base.bornGrade, HeroCompanionData.imprintStep);
            ImprintLevel.color = TableManagerCS.instance.HeroImprintTable.GetColor(HeroCompanionData.c_base.bornGrade, HeroCompanionData.imprintStep);

            bool isLock = (HeroCompanionData.imprintStep == 0);
            ImprintLockIcon.SetActive(isLock);
            ImprintOption.gameObject.SetActive(true);

            string[] option = TableManagerCS.instance.HeroImprintTable.GetImprintOption(HeroCompanionData.c_base.imprintGroup, HeroCompanionData.imprintStep);
            ImprintOption.text = string.Format("[b]{0} {1}", option[0], isLock ? string.Format("+\n{0}", SSLocalization.Get("heroInfo", 307)) : option[1]);
            ImprintOption.color = TableManagerCS.instance.HeroImprintTable.GetColor(HeroCompanionData.c_base.bornGrade, HeroCompanionData.imprintStep);
        }
        else
        {
            ImprintSlotObject.SetActive(false);
        }
    }
    
    private void SetButtonEnabled()
    {
        if (HeroBaseData.bSpecial == HeroSpecialType.ImprintType)
        {
            ExpObtainButton.SetActive(true);
            EvolutionButton.SetActive(false);
            ImprintButton.SetActive(false);
            
            ExpObtainButton.isEnabled = false;
            ExpObtainButton.isLock = false;

            ArousalButton.isEnabled = false;
            ArousalButton.isLock = false;

            SkillEnchantButton.isEnabled = false;
            SkillEnchantButton.isLock = false;
            
            CompositionButton.isEnabled = false;
            CompositionButton.isLock = false;

            return;
        }

        bool isActiveContents = false;

        bool isMaxLv = (HeroCompanionData.lv == HeroBaseData.maxlevel);
        bool isMaxGrade = (HeroBaseData.grade == GameConstraints.HeroMaxGrade);
        bool isMaxImprint = (HeroCompanionData.imprintStep == TableManagerCS.instance.HeroImprintTable.GetMaxLv(HeroBaseData.bornGrade));

        if (isMaxLv && isMaxGrade && isMaxImprint)
        {
            ExpObtainButton.SetActive(true);
            ExpObtainButton.isEnabled = false;
            ExpObtainButton.isLock = false;

            EvolutionButton.SetActive(false);
            ImprintButton.SetActive(false);
        }
        else if (isMaxLv && isMaxGrade)
        {
            // 각인 버튼
            ImprintButton.SetActive(true);
            ImprintButton.isEnabled = true;
            ImprintButton.isLock = false;

            ExpObtainButton.SetActive(false);
            EvolutionButton.SetActive(false);
        }
        else if (!isMaxLv)
        {
            // 영웅강화(경험치상승) 버튼
            bool isExpObtainEnable =
                HeroBaseData.bSpecial != HeroSpecialType.ExpObtainType &&
                HeroCompanionData.lv < HeroBaseData.maxlevel;

            isActiveContents = ManagerCS.instance.IsActiveContents(eHeroContents.ExpObtain_EnterLimit);

            ExpObtainButton.SetActive(true);
            ExpObtainButton.isEnabled = isExpObtainEnable && isActiveContents;
            ExpObtainButton.isLock = !isActiveContents;

            EvolutionButton.SetActive(false);
            ImprintButton.SetActive(false);
        }
        else
        {
            // 영웅진화 버튼
            bool isEvolutionEnable =
                HeroBaseData.bSpecial != HeroSpecialType.ExpObtainType &&
                HeroBaseData.grade < GameConstraints.HeroMaxGrade &&
                HeroCompanionData.lv == HeroCompanionData.c_base.maxlevel &&
                TableManagerCS.instance.HeroEvolutionTable.GetEvolutionRule(HeroBaseData.uidx) != null;

            isActiveContents = ManagerCS.instance.IsActiveContents(eHeroContents.Evolution_EnterLimit);

            EvolutionButton.SetActive(true);
            EvolutionButton.isEnabled = isEvolutionEnable && isActiveContents;
            EvolutionButton.isLock = !isActiveContents;
            EvolutionButton.isAlarm = isEvolutionEnable;

            ExpObtainButton.SetActive(false);
            ImprintButton.SetActive(false);
        }

        // 특수영웅인지?
        bool isNormalType = HeroBaseData.bSpecial == HeroSpecialType.NormalType;

        // 영웅각성 버튼
        isActiveContents = ManagerCS.instance.IsActiveContents(eHeroContents.Arousal_EnterLimit);

        ArousalButton.SetActive(true);
        ArousalButton.isEnabled = isNormalType;
        ArousalButton.isLock = !isActiveContents;
        ArousalButton.isAlarm = HeroCompanionData.isEnableArousal;

        // 스킬강화 버튼
        isActiveContents = ManagerCS.instance.IsActiveContents(eHeroContents.SkillEnchant_EnterLimit);

        SkillEnchantButton.SetActive(true);
        SkillEnchantButton.isEnabled = isNormalType && isActiveContents;
        SkillEnchantButton.isLock = !isActiveContents;

        // 영웅합성 버튼
        isActiveContents = ManagerCS.instance.IsActiveContents(eHeroContents.Composition_EnterLimit);
        bool isCompositionEnableGrade = TableManagerCS.instance.HeroCompositionTable.CheckCompositionEnableGrade(HeroBaseData.bornGrade, HeroBaseData.grade);

        CompositionButton.SetActive(true);
        CompositionButton.isEnabled = isNormalType && isMaxLv && isCompositionEnableGrade;
        CompositionButton.isLock = !isActiveContents;
    }
    
    // 영웅강화 버튼
    public void OnClickHeroExpObtainButton()
    {
        ManagerCS.instance.uiController.ChangeUI(UIState.HeroExpObtain);
    }

    // 영웅진화 버튼
    public void OnClickHeroEvolutionButton()
    {
        ManagerCS.instance.uiController.ChangeUI(UIState.HeroEvolution);
    }

    // 영웅각성 버튼
    public void OnClickHeroArousalButton()
    {
        ManagerCS.instance.uiController.ChangeUI(UIState.HeroArousal);
    }

    // 스킬강화 버튼
    public void OnClickSkillEnchantButton()
    {
        ManagerCS.instance.uiController.ChangeUI(UIState.HeroSkillEnchant);
    }
    
    // 영웅합성 버튼
    public void OnClickHeroCompositionButton()
    {
        // 팀 참가, 잠금, 아르메스, 분쟁의대지 상태면 합성 불가능
        if (HeroCompanionData.c_jointeam != 0 || HeroCompanionData.c_lock || HeroCompanionData.isJoinArmes || HeroCompanionData.isOccupationDefenseUnit)
        {
            ManagerCS.instance.CheckDisableHero(HeroCompanionData);
            return;
        }
        
        ManagerCS.instance.uiController.ChangeUI(UIState.HeroComposition);
    }

    // 영혼추출 버튼
    public void OnClickHeroExtractionButton()
    {
        // 팀 참가, 잠금, 아르메스, 분쟁의대지 상태면 추출 불가능
        if (HeroCompanionData.c_jointeam != 0 || HeroCompanionData.c_lock || HeroCompanionData.isJoinArmes || HeroCompanionData.isOccupationDefenseUnit)
        {
            ManagerCS.instance.CheckDisableHero(HeroCompanionData);
            return;
        }
        

        // EnterExtractionUI -> Delegate
        voidDelegate EnterExtractionUI = () => { ManagerCS.instance.uiController.ChangeUI(UIState.HeroExtraction); };

        // 높은등급 확인
        if (HeroBaseData.grade >= GameConstraints.HeroHighGrade)
        {
            PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common);
            popup.Set(
                PopupCommon.Type.TwoBtn,
                SSLocalization.Get("heroInfo", 244),
                EnterExtractionUI);

            return;
        }

        // 장비착용 확인
        if (HeroCompanionData.isWearEquipment)
        {
            PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common);
            popup.Set(
                PopupCommon.Type.TwoBtn,
                SSLocalization.Get("heroInfo", 245),
                EnterExtractionUI);

            return;
        }

        EnterExtractionUI();
    }

    // 영웅잠금 버튼
    public void OnClickLockButton()
    {
        Send_HeroLockSwitchRequest();
    }
    
    // 영웅상세정보 버튼
    public void OnClickDetailInfoButton()
    {
        if (bDetailInfoFlag)
        {
            bDetailInfoFlag = false;
            DetailInfoTween.PlayForward();
            DetailInfoButtonArrow.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            bDetailInfoFlag = true;
            DetailInfoTween.PlayReverse();
            DetailInfoButtonArrow.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }
    }

    public void OnClickImprintInfoButton()
    {
        if (HeroBaseData.imprintGroup == 0)
            return;

        if (TableManagerCS.instance.HeroImprintTable.GetStartLv(HeroBaseData.bornGrade) == 0)
            return;

        PopupHeroImprintInfo popup = PopupManager.instance.MakePopup<PopupHeroImprintInfo>(ePopupType.HeroImprintInfo, false, true);
        popup.Set(HeroCompanionData);
    }
    
    // 영웅 잠금 프로토콜
    private void Send_HeroLockSwitchRequest()
    {
        Hero_LockSwitchRequestParameter data = new Hero_LockSwitchRequestParameter();
        data.heromanidx = HeroCompanionData.manidx;
        data.tlock = (short)(isCurrentLock ? 0 : 1);

        NetworkConnection.instance.NetCommand(PacketType.Hero_LockSwitchRequestParameter, data,
            (string _data)=>
            {
                if (NetworkCS.CheckNetworkError())
                {
                    Hero_LockSwitchResultParameter Hero_LockSwitchResult = JsonConvert.DeserializeObject<Hero_LockSwitchResultParameter>(_data);

                    Numeric64 LockedHeroIdx = new Numeric64(Hero_LockSwitchResult.heromanidx);

                    if (Hero_LockSwitchResult.tlock == 1)
                    {
                        GameData.LockedHeromanidxList.Add(LockedHeroIdx);
                        GameData.Heroes.Find(row => row.manidx == Hero_LockSwitchResult.heromanidx).c_lock = true;

                        SetLock(true);
                        TargetIcon.SetLock(true);
                    }
                    else
                    {
                        GameData.LockedHeromanidxList.Remove(
                            GameData.LockedHeromanidxList.Find(row => row.value == Hero_LockSwitchResult.heromanidx));
                        GameData.Heroes.Find(row => row.manidx == Hero_LockSwitchResult.heromanidx).c_lock = false;

                        SetLock(false);
                        TargetIcon.SetLock(false);
                    }

                    SetButtonEnabled();
                }
            });
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
        
    // 영웅 장비정보 갱신
    internal override void OnUpdateHeroEquipmentInfo(ManagerCS.EquipmentSetData setData)
    {
        Debug.Log(HeroCompanionData.c_battlepower);
        Debug.Log(HeroIconList.Find(o => o.HeroCompanionData.manidx == HeroCompanionData.manidx).HeroCompanionData.c_battlepower);

        Debug.Log("-------------------------");

        HeroCompanionData = GameData.Heroes.Find(o => o.manidx == HeroCompanionData.manidx);

        Debug.Log(HeroCompanionData.c_battlepower);
        Debug.Log(HeroIconList.Find(o => o.HeroCompanionData.manidx == HeroCompanionData.manidx).HeroCompanionData.c_battlepower);

        SetStatus();
        SetEquipment();

        switch (setData.category)
        {
            case EquipUICategory.Weapon:
                {
                    ActiveSetItemList = TableManagerCS.instance.SetItemTable.GetSetMatchActiveList(
                        HeroCompanionData.wpnidx(),
                        HeroCompanionData.GetEquipList());
                }
                break;

            case EquipUICategory.Armor:
                {
                    ActiveSetItemList = TableManagerCS.instance.SetItemTable.GetSetMatchActiveList(
                        HeroCompanionData.amridx(),
                        HeroCompanionData.GetEquipList());
                }
                break;

            case EquipUICategory.Accessory:
                {
                    ActiveSetItemList = TableManagerCS.instance.SetItemTable.GetSetMatchActiveList(
                        HeroCompanionData.asr1idx(),
                        HeroCompanionData.GetEquipList());
                }
                break;

            case EquipUICategory.RuneTop:
                {
                    ActiveSetItemList = TableManagerCS.instance.SetItemTable.GetSetMatchActiveList(
                        HeroCompanionData.runetopidx(),
                        HeroCompanionData.GetEquipList());
                }
                break;

            case EquipUICategory.RuneMid:
                {
                    ActiveSetItemList = TableManagerCS.instance.SetItemTable.GetSetMatchActiveList(
                        HeroCompanionData.runemididx(),
                        HeroCompanionData.GetEquipList());
                }
                break;

            case EquipUICategory.RuneBot:
                {
                    ActiveSetItemList = TableManagerCS.instance.SetItemTable.GetSetMatchActiveList(
                        HeroCompanionData.runebottomidx(),
                        HeroCompanionData.GetEquipList());
                }
                break;
        }
    }

    private void ActiveSetItemEffect()
    {
        for (int i = 0; i < EquipmentSlotEffect.Length; i++)
        {
            EquipmentSlotEffect[i].SetActive(false);
        }

        if (ActiveSetItemList == null || ActiveSetItemList.Count == 0)
            return;

        foreach (int idx in ActiveSetItemList)
        {
            if (HeroCompanionData.wpnidx() == idx)
                EquipmentSlotEffect[(int)EquipUICategory.Weapon].SetActive(true);

            if (HeroCompanionData.amridx() == idx)
                EquipmentSlotEffect[(int)EquipUICategory.Armor].SetActive(true);

            if (HeroCompanionData.asr1idx() == idx)
                EquipmentSlotEffect[(int)EquipUICategory.Accessory].SetActive(true);

            if (HeroCompanionData.runetopidx() == idx)
                EquipmentSlotEffect[(int)EquipUICategory.RuneTop].SetActive(true);

            if (HeroCompanionData.runemididx() == idx)
                EquipmentSlotEffect[(int)EquipUICategory.RuneMid].SetActive(true);

            if (HeroCompanionData.runebottomidx() == idx)
                EquipmentSlotEffect[(int)EquipUICategory.RuneBot].SetActive(true);
        }

        ManagerCS.instance.UpdateSetItemNotice(ActiveSetItemList);

        ActiveSetItemList = null;
    }

    // 영웅리스트 갱신
    internal override void OnUpdateHeroList()
    {
        SetHeroList();
        SetSortingButton();
        SetHeroStorageCount();
    }

    internal override void OnChangeSkillThird(HeroCompanion companion)
    {
        ManagerCS.instance.MissionNoticePopup();
        ManagerCS.instance.uiController.GetHero(companion);              // 영웅 정보 갱신
        ManagerCS.instance.RefreshHeroBattlePower(companion);     // 영웅 전투력 갱신

        OnUpdateHeroList();
    }
}
