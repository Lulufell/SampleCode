using UnityEngine;
using System;
using System.Collections.Generic;
using ServerCommon;
using ServerCommon.DataTables;
using ServerCommon.Tutorials;


public partial class HeroGrowthUI : GameNotification
{
    public enum HeroGrowthUIType
    {
        ExpObtain,
        Evolution,
        Composition,
    }
    private HeroGrowthUIType GrowthUIType;

    // Data
    private HeroCompanion HeroCompanionData;
    private HeroBases HeroBaseData;

    private List<HeroCompanion> HeroList;           // 영웅 리스트
    private List<Icon_HeroList> HeroIconList;       // 영웅 아이콘 리스트
    private List<Icon_Hero> MaterialHeroIconList;   // 재료 영웅 리스트

    private int MaxMaterialHeroCount;

    private bool isMaxGrowth = false;
    private bool isMaxImprint = false;

    private RewardType CostType;
    private int CostValue;


    // UI - Left
    public UISprite SynastryIcon;
    public UILabel SynastryLabel;
    public UISprite HeroTypeIcon;
    public UILabel HeroTypeLabel;

    public UILabel Title;
    public UILabel Name;

    public GameObject ImprintSlotObject;


    // 기본 정보
    public UILabel Base_Level;
    public UILabel Base_MaxLevel;
    public GradeObject Base_Grade;
    public UILabel Base_BattlePower;
    public UILabel[] Base_Status;
    public UISprite Base_ImprintIcon;
    public UILabel Base_ImprintLevel;
    public GameObject Base_ImprintLockIcon;


    // 강화 후 정보
    public UILabel Result_Level;
    public UILabel Result_MaxLevel;
    public GradeObject Result_Grade;
    public UILabel Result_BattlePower;
    public UILabel[] Result_Status;
    public UISprite Result_ImprintIcon;
    public UILabel Result_ImprintLevel;


    // UI - Center
    public GameObject TargetHeroSlot;

    [Serializable]
    public class SlotObject
    {
        public GameObject SlotPos;
        public UISprite SlotImg;
        public GameObject WarningEffect;
        public GameObject ImprintEffect;
        public GameObject ConsumeEffect;
    }
    public SlotObject[] SlotList;

    public GameObject ClearButton;
    public UIButton ExecuteButton;
    public UILabel ButtonLabel;
    public UISprite CostTypeIcon;
    public UILabel CostValueLabel;


    // UI - Right
    public UnitListScrollView HeroListScrollView;
    public HeroSortingButton SortingButton;
    public UIButton FilteringButton;
    public UILabel HeroStorageCount;


    // UI - Effect & Animation
    public GameObject UILockPanel;
    public GameObject EmptyObj;


    private void Awake()
    {
        HeroIconList = new List<Icon_HeroList>();
        MaterialHeroIconList = new List<Icon_Hero>();
    }

    protected override void Init()
    {
        StatusUpEffect.SetActive(false);
        UILockPanel.SetActive(false);

        SetUI();

        if (TutorialManager.instance.CurrentTutorial == TutorialID.TeamSetting)
            SetExpObtainTutorial();
    }

    internal override void UIRefresh()
    {
        ManagerCS.instance.HeroUI_TopButtonDisable();
    }

    private void SetUI()
    {
        SetDetailInfo();
        SetHeroStorageCount();
        ClearMaterialList();

        switch (ManagerCS.instance.uiController.State)
        {
            case UIState.HeroExpObtain:
                {
                    GrowthUIType = HeroGrowthUIType.ExpObtain;
                    SetExpObtainUI();
                }
                break;

            case UIState.HeroEvolution:
                {
                    GrowthUIType = HeroGrowthUIType.Evolution;
                    SetEvolutionUI();
                }
                break;

            case UIState.HeroComposition:
                {
                    GrowthUIType = HeroGrowthUIType.Composition;
                    SetCompositionUI();
                }
                break;
        }

        ManagerCS.instance.uiController.UIFinance.SetTopUI(ManagerCS.instance.uiController.State);

        // 최대등급 최대레벨일 때 UI 세팅
        bool isMaxLv = (HeroCompanionData.lv == HeroBaseData.maxlevel);
        bool isMaxGrade = (HeroBaseData.grade == GameConstraints.HeroMaxGrade);
        bool isMaxImprint = (HeroCompanionData.imprintStep == TableManagerCS.instance.HeroImprintTable.GetMaxLv(HeroBaseData.bornGrade));

        if (isMaxLv && isMaxGrade && isMaxImprint)
        {
            isMaxGrowth = true;

            for (int i = 0; i < MaterialHeroIconList.Count; i++)
            {
                SlotList[i].SlotPos.SetActive(false);
            }

            ClearButton.SetActive(false);
            ExecuteButton.gameObject.SetActive(false);

            ExpBar.gameObject.SetActive(false);
        }

        UIRefresh();
    }

    private void SetDetailInfo()
    {
        HeroCompanionData = ManagerCS.instance.targetHeroData;
        HeroBaseData = HeroCompanionData.c_base;

        SetHeroIcon();
        SetHeroInfo();
    }

    private void SetHeroIcon()
    {
        if (TargetHeroSlot.activeSelf == false)
            TargetHeroSlot.gameObject.SetActive(true);

        TargetHeroSlot.transform.DestroyChildren();

        Icon_Hero icon = ManagerCS.instance.MakeIcon<Icon_Hero>(TargetHeroSlot, ManagerCS.eIconType.Hero);
        icon.SetHeroIcon(HeroCompanionData);
        icon.SettingType(ManagerCS.eSettingType.A_Type);
    }

    private void SetHeroInfo()
    {
        // Set HeroInfo
        Title.text = string.Format("[b]{0}[/b]", HeroBaseData.GetTitle());
        Name.text = string.Format("[b]{0}[/b]", HeroBaseData.GetName());

        // Name => ResizeFreely + Anchor 로 인해 리사이즈 후, 앵커가 움직이면서 사이즈가 다시 변경되어 이슈발생
        // 수동으로 Resize
        Name.MarkAsChanged();

        SynastryIcon.spriteName = ManagerCS.instance.GetSynastryIcon(HeroBaseData.synastry);
        SynastryLabel.color = GameUtils.GetSynastryColor(HeroBaseData.synastry);
        SynastryLabel.text = GameUtils.GetSynastryText(HeroBaseData.synastry);

        HeroTypeIcon.spriteName = ManagerCS.instance.GetHeroTypeIcon(HeroBaseData.herotype);
        HeroTypeLabel.text = GameUtils.GetHeroTypeText(HeroBaseData.herotype);

        Base_Level.text = string.Format("[b]{0}[/b]", SSLocalization.Format("heroinfo", 1, HeroCompanionData.lv));
        Base_MaxLevel.text = string.Format("[b]{0}[/b]", SSLocalization.Format("heroinfo", 1, HeroBaseData.maxlevel));
        Base_Grade.SetGrade(HeroBaseData.grade, HeroCompanionData.arousalStep);

        // Set Status
        Base_BattlePower.text = string.Format("[b]{0}[/b]", HeroStatusManager.instance.Calc_BattlePower(HeroCompanionData).ToString("#,##0"));

        // <-- 기본 스탯 -->
        CombatUnitAbilities_Client preStatus = HeroStatusManager.instance.Calc_HeroTotalStatus(HeroCompanionData);

        // 메인스탯
        Base_Status[(int)HeroStatusInfo.Atk].text = preStatus.GetStatusText(HeroStatusInfo.Atk);
        Base_Status[(int)HeroStatusInfo.Def].text = preStatus.GetStatusText(HeroStatusInfo.Def);
        Base_Status[(int)HeroStatusInfo.HP].text = preStatus.GetStatusText(HeroStatusInfo.HP);

        // 서브스탯
        Base_Status[(int)HeroStatusInfo.AtkSpeed].text = preStatus.GetStatusText(HeroStatusInfo.AtkSpeed);
        Base_Status[(int)HeroStatusInfo.MoveSpeed].text = preStatus.GetStatusText(HeroStatusInfo.MoveSpeed);
        Base_Status[(int)HeroStatusInfo.CritRate].text = preStatus.GetStatusText(HeroStatusInfo.CritRate);
        Base_Status[(int)HeroStatusInfo.CritPower].text = preStatus.GetStatusText(HeroStatusInfo.CritPower);
        Base_Status[(int)HeroStatusInfo.HitRate].text = preStatus.GetStatusText(HeroStatusInfo.HitRate);
        Base_Status[(int)HeroStatusInfo.DodgeRate].text = preStatus.GetStatusText(HeroStatusInfo.DodgeRate);
        Base_Status[(int)HeroStatusInfo.EffectHit].text = preStatus.GetStatusText(HeroStatusInfo.EffectHit);
        Base_Status[(int)HeroStatusInfo.CCResist].text = preStatus.GetStatusText(HeroStatusInfo.CCResist);
        Base_Status[(int)HeroStatusInfo.DotResist].text = preStatus.GetStatusText(HeroStatusInfo.DotResist);
        Base_Status[(int)HeroStatusInfo.DeResist].text = preStatus.GetStatusText(HeroStatusInfo.DeResist);

        // 각인 정보
        if (HeroCompanionData.c_base.imprintGroup != 0)
        {
            ImprintSlotObject.SetActive(true);

            Base_ImprintIcon.color = TableManagerCS.instance.HeroImprintTable.GetColor(HeroBaseData.bornGrade, HeroCompanionData.imprintStep);
            Base_ImprintLevel.color = TableManagerCS.instance.HeroImprintTable.GetColor(HeroBaseData.bornGrade, HeroCompanionData.imprintStep);
            Base_ImprintLevel.text = TableManagerCS.instance.HeroImprintTable.GetLevel(HeroBaseData.bornGrade, HeroCompanionData.imprintStep);
            Base_ImprintLockIcon.SetActive(HeroCompanionData.imprintStep == 0);
        }
        else
        {
            ImprintSlotObject.SetActive(false);
        }
    }

    private void SetResultHeroInfo(HeroCompanion resultHeroData)
    {
        // <-- 결과 스탯 -->
        bool bIncrease = false;

        // 레벨, 최대레벨, 등급
        bIncrease = (resultHeroData.lv > HeroCompanionData.lv);
        Result_Level.gameObject.SetActive(bIncrease);
        Result_Level.text = (bIncrease) ? string.Format("[b]{0}[/b]", SSLocalization.Format("heroinfo", 1, resultHeroData.lv)) : string.Empty;

        bIncrease = (resultHeroData.c_base.maxlevel > HeroCompanionData.c_base.maxlevel);
        Result_MaxLevel.gameObject.SetActive(bIncrease);
        Result_MaxLevel.text = (bIncrease) ? string.Format("[b]{0}[/b]", SSLocalization.Format("heroinfo", 1, resultHeroData.c_base.maxlevel)) : string.Empty;

        bIncrease = (resultHeroData.c_base.grade > HeroCompanionData.c_base.grade);
        Result_Grade.gameObject.SetActive(bIncrease);
        Result_Grade.SetGrade(resultHeroData.c_base.grade, resultHeroData.arousalStep);


        // 전투력
        int baseBattlePower = HeroStatusManager.instance.Calc_BattlePower(HeroCompanionData);
        int resultBattlePower = HeroStatusManager.instance.Calc_BattlePower(resultHeroData);

        bIncrease = (resultBattlePower > baseBattlePower);
        Result_BattlePower.gameObject.SetActive(bIncrease);
        Result_BattlePower.text = (bIncrease) ? string.Format("[b]{0}[/b]", resultBattlePower.ToString("#,##0")) : string.Empty;


        CombatUnitAbilities_Client preStatus = HeroStatusManager.instance.Calc_HeroTotalStatus(HeroCompanionData);
        CombatUnitAbilities_Client postStatus = HeroStatusManager.instance.Calc_HeroTotalStatus(resultHeroData);

        // 메인스탯
        bIncrease = (postStatus.atkPW > preStatus.atkPW);
        Result_Status[(int)HeroStatusInfo.Atk].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.Atk].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.Atk) : string.Empty;

        bIncrease = (postStatus.defPW > preStatus.defPW);
        Result_Status[(int)HeroStatusInfo.Def].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.Def].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.Def) : string.Empty;

        bIncrease = (postStatus.maxHp > preStatus.maxHp);
        Result_Status[(int)HeroStatusInfo.HP].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.HP].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.HP) : string.Empty;


        // 서브스탯
        bIncrease = (postStatus.atkSPD > preStatus.atkSPD);
        Result_Status[(int)HeroStatusInfo.AtkSpeed].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.AtkSpeed].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.AtkSpeed) : string.Empty;

        bIncrease = (postStatus.movSPD > preStatus.movSPD);
        Result_Status[(int)HeroStatusInfo.MoveSpeed].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.MoveSpeed].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.MoveSpeed) : string.Empty;

        bIncrease = (postStatus.criC > preStatus.criC);
        Result_Status[(int)HeroStatusInfo.CritRate].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.CritRate].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.CritRate) : string.Empty;

        bIncrease = (postStatus.maxCriD > preStatus.maxCriD);
        Result_Status[(int)HeroStatusInfo.CritPower].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.CritPower].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.CritPower) : string.Empty;

        bIncrease = (postStatus.HitChance > preStatus.HitChance);
        Result_Status[(int)HeroStatusInfo.HitRate].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.HitRate].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.HitRate) : string.Empty;

        bIncrease = (postStatus.dodgeC > preStatus.dodgeC);
        Result_Status[(int)HeroStatusInfo.DodgeRate].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.DodgeRate].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.DodgeRate) : string.Empty;

        bIncrease = (postStatus.EffectHit > preStatus.EffectHit);
        Result_Status[(int)HeroStatusInfo.EffectHit].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.EffectHit].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.EffectHit) : string.Empty;

        bIncrease = (postStatus.ccRes > preStatus.ccRes);
        Result_Status[(int)HeroStatusInfo.CCResist].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.CCResist].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.CCResist) : string.Empty;

        bIncrease = (postStatus.dotRes > preStatus.dotRes);
        Result_Status[(int)HeroStatusInfo.DotResist].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.DotResist].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.DotResist) : string.Empty;

        bIncrease = (postStatus.deRes > preStatus.deRes);
        Result_Status[(int)HeroStatusInfo.DeResist].gameObject.SetActive(bIncrease);
        Result_Status[(int)HeroStatusInfo.DeResist].text = (bIncrease) ? postStatus.GetStatusText(HeroStatusInfo.DeResist) : string.Empty;

        // 각인 정보
        if (HeroCompanionData.c_base.imprintGroup != 0)
        {
            bIncrease = (resultHeroData.imprintStep > HeroCompanionData.imprintStep);
            Result_ImprintIcon.gameObject.SetActive(bIncrease);
            Result_ImprintIcon.color = TableManagerCS.instance.HeroImprintTable.GetColor(resultHeroData.c_base.bornGrade, resultHeroData.imprintStep);
            Result_ImprintLevel.color = TableManagerCS.instance.HeroImprintTable.GetColor(resultHeroData.c_base.bornGrade, resultHeroData.imprintStep);
            Result_ImprintLevel.text = TableManagerCS.instance.HeroImprintTable.GetLevel(resultHeroData.c_base.bornGrade, resultHeroData.imprintStep);
        }
    }

    private void SetRandomResultHeroInfo(bool bActiveResultInfo = true)
    {
        // <-- 결과 스탯 -->

        // 최대레벨, 등급
        Result_MaxLevel.gameObject.SetActive(bActiveResultInfo);
        Result_MaxLevel.text = string.Format("[b]{0}[/b]", SSLocalization.Format("heroinfo", 1, HeroCompanionData.c_base.maxlevel + 10));

        Result_Grade.gameObject.SetActive(bActiveResultInfo);
        Result_Grade.SetGrade(HeroCompanionData.c_base.grade + 1, 0);

        const string resultValue = "?";

        // 전투력        
        Result_BattlePower.gameObject.SetActive(bActiveResultInfo);
        Result_BattlePower.text = string.Format("[b]{0}[/b]", resultValue);


        // 메인스탯
        Result_Status[(int)HeroStatusInfo.Atk].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.Atk].text = resultValue;

        Result_Status[(int)HeroStatusInfo.Def].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.Def].text = resultValue;

        Result_Status[(int)HeroStatusInfo.HP].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.HP].text = resultValue;


        // 서브스탯
        Result_Status[(int)HeroStatusInfo.AtkSpeed].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.AtkSpeed].text = resultValue;

        Result_Status[(int)HeroStatusInfo.MoveSpeed].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.MoveSpeed].text = resultValue;

        Result_Status[(int)HeroStatusInfo.CritRate].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.CritRate].text = resultValue;

        Result_Status[(int)HeroStatusInfo.CritPower].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.CritPower].text = resultValue;

        Result_Status[(int)HeroStatusInfo.HitRate].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.HitRate].text = resultValue;

        Result_Status[(int)HeroStatusInfo.DodgeRate].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.DodgeRate].text = resultValue;

        Result_Status[(int)HeroStatusInfo.EffectHit].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.EffectHit].text = resultValue;

        Result_Status[(int)HeroStatusInfo.CCResist].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.CCResist].text = resultValue;

        Result_Status[(int)HeroStatusInfo.DotResist].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.DotResist].text = resultValue;

        Result_Status[(int)HeroStatusInfo.DeResist].gameObject.SetActive(bActiveResultInfo);
        Result_Status[(int)HeroStatusInfo.DeResist].text = resultValue;
    }

    private void SelectHeroListIcon(Icon_HeroList icon)
    {
        if (icon == null)
            return;

        // 재료 등록 개수 초과
        if (MaterialHeroIconList.Count >= MaxMaterialHeroCount)
            return;

        // 재료 체크
        if (CheckMaterial(icon) == false)
            return;

        AddMaterialListItem(icon);
    }

    private void SelectMaterialListIcon(Icon_Hero icon)
    {
        if (icon == null)
            return;

        RemoveMaterialListItem(icon);
    }

    private bool CheckMaterial(Icon_HeroList icon)
    {
        switch (GrowthUIType)
        {
            case HeroGrowthUIType.ExpObtain:
                return CheckMaterial_ExpObtain(icon);
            case HeroGrowthUIType.Evolution:
                return CheckMaterial_Evolution(icon);
            case HeroGrowthUIType.Composition:
                return CheckMaterial_Composition(icon);

            // Error
            default:
                return false;
        }
    }

    private bool CheckMaterial_ExpObtain(Icon_HeroList icon)
    {
        int maxImprint = 0;

        // 각인전용영웅이면 최대각인등급이 없음
        if (icon.HeroBaseData.bSpecial == HeroSpecialType.ImprintType)
            maxImprint = -1;
        else
            maxImprint = TableManagerCS.instance.HeroImprintTable.GetMaxLv(icon.HeroBaseData.bornGrade);

        // 재료타입 판별
        eMaterialType materialType = (icon.isEnableImprint) ? eMaterialType.Imprint : eMaterialType.ExpObtain;

        // 재료타입 별 처리
        switch (materialType)
        {
            // 1. 강화재료
            case eMaterialType.ExpObtain:
                {
                    // 최대경험치 상태
                    if (isMaxExp)
                    {
                        PopupNotice popup = PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice, false, true);
                        // ("heroInfo", 309) -> 최대 레벨입니다.
                        popup.Set(SSLocalization.Get("heroInfo", 309));

                        return false;
                    }
                }
                break;

            // 2. 각인재료
            case eMaterialType.Imprint:
                {
                    // 최대각인 상태
                    if (isMaxImprint)
                    {
                        // 1-1. 최대각인 상태이기 때문에 일반강화 처리
                        if (isMaxExp)
                        {
                            PopupNotice popup = PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice, false, true);
                            // ("heroInfo", 309) -> 최대 레벨입니다.
                            popup.Set(SSLocalization.Get("heroInfo", 309));

                            return false;
                        }
                        else
                        {
                            PopupNotice popup = PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice, false, true);
                            // ("heroInfo", 313) -> 이미 각인 등급이 최대이므로 동일한 영웅을 재료로 추가하여도 더 이상 각인이 진행되지 않습니다.
                            popup.Set(SSLocalization.Get("heroInfo", 313));
                        }
                    }
                    else
                    {
                        // 1-1. 최대각인영웅을 재료로 등록
                        if (icon.HeroCompanionData.imprintStep == maxImprint)
                        {
                            PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common, false, true);
                            // ("heroInfo", 310) -> 각인 등급이 SSS인 영웅을 재료로 사용 할 경우 '업적명' 업적이 체크되지 않습니다.
                            popup.Set(PopupCommon.Type.OneBtn, SSLocalization.Get("heroInfo", 310));
                        }
                    }
                }
                break;
        }

        return true;
    }

    private bool CheckMaterial_Evolution(Icon_HeroList icon)
    {
        int maxImprint = 0;

        // 각인전용영웅이면 최대각인등급이 없음
        if (icon.HeroBaseData.bSpecial == HeroSpecialType.ImprintType)
            maxImprint = -1;
        else
            maxImprint = TableManagerCS.instance.HeroImprintTable.GetMaxLv(icon.HeroBaseData.bornGrade);


        int targetGrade = HeroBaseData.grade;

        // 재료타입 판별
        eMaterialType materialType = eMaterialType.None;

        // 1. 진화재료 -> 등급(==) 각인(!=)
        if (icon.HeroBaseData.grade == targetGrade && icon.isEnableImprint == false)
        {
            materialType = eMaterialType.Evolution;
        }
        // 2. 각인재료 -> 등급(!=) 각인(==)
        else if (icon.isEnableImprint && icon.HeroBaseData.grade != targetGrade)
        {
            materialType = eMaterialType.Imprint;
        }
        // 3. 각인진화재료 -> 등급(==) 각인(==)
        else if (icon.HeroBaseData.grade == targetGrade && icon.isEnableImprint)
        {
            materialType = eMaterialType.ImprintEvolution;
        }

        if (MaterialHeroIconList.Count == 0)
            evolutionSlotType = eMaterialType.None;

        // 재료타입 별 처리
        switch (materialType)
        {
            // 1. 진화재료
            case eMaterialType.Evolution:
                {
                    // 1-1. 각인모드면 진화재료 사용 불가능
                    if (evolutionSlotType == eMaterialType.Imprint)
                    {
                        PopupNotice popup = PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice, false, true);
                        // ("heroInfo", 311) -> 각인영웅이 재료로 포함되어있어 진화영웅을 넣을 수 없습니다.
                        popup.Set(SSLocalization.Get("heroInfo", 311));

                        return false;
                    }
                }
                break;

            // 2. 각인재료
            case eMaterialType.Imprint:
                {
                    // 1-1. 진화모드면 각인재료 사용 불가능
                    if (evolutionSlotType == eMaterialType.Evolution)
                    {
                        PopupNotice popup = PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice, false, true);
                        // ("heroInfo", 312) -> 진화영웅이 재료로 포함되어있어 각인영웅을 넣을 수 없습니다.
                        popup.Set(SSLocalization.Get("heroInfo", 312));

                        return false;
                    }

                    // 1-2. 최대각인 상태
                    if (isMaxImprint)
                    {
                        PopupNotice popup = PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice, false, true);
                        // ("heroInfo", 313) -> 이미 각인 등급이 최대이므로 동일한 영웅을 재료로 추가하여도 더 이상 각인이 진행되지 않습니다.
                        popup.Set(SSLocalization.Get("heroInfo", 313));

                        // * 각인모드에서는 최대각인 상태에서 더 재료를 등록할 필요가 없다 *
                        return false;
                    }
                    // 1-3. 최대각인영웅을 재료로 등록
                    else if (icon.HeroCompanionData.imprintStep == maxImprint)
                    {
                        PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common, false, true);
                        // ("heroInfo", 314) -> 각인 등급이 SSS인 영웅을 재료로 사용 할 경우 '업적명' 업적이 체크되지 않습니다.
                        popup.Set(PopupCommon.Type.OneBtn, SSLocalization.Get("heroInfo", 314));
                    }
                }
                break;

            // 3. 각인진화재료
            case eMaterialType.ImprintEvolution:
                {
                    // 1-1. 최대각인 상태
                    if (isMaxImprint)
                    {
                        PopupNotice popup = PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice, false, true);
                        // ("heroInfo", 313) -> 이미 각인 등급이 최대이므로 동일한 영웅을 재료로 추가하여도 더 이상 각인이 진행되지 않습니다.
                        popup.Set(SSLocalization.Get("heroInfo", 313));

                        // * 각인모드에서는 최대각인 상태에서 더 재료를 등록할 필요가 없다 *
                        if (evolutionSlotType == eMaterialType.Imprint)
                            return false;
                    }
                    // 1-2. 최대각인영웅을 재료로 등록
                    else if (icon.HeroCompanionData.imprintStep == maxImprint)
                    {
                        PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common, false, true);
                        // ("heroInfo", 314) -> 각인 등급이 SSS인 영웅을 재료로 사용 할 경우 '업적명' 업적이 체크되지 않습니다.
                        popup.Set(PopupCommon.Type.OneBtn, SSLocalization.Get("heroInfo", 314));
                    }
                }
                break;
        }

        return true;
    }

    private bool CheckMaterial_Composition(Icon_HeroList icon)
    {
        // 체크조건이 없지만, 혹시 필요하면 여기에 작성
        return true;
    }

    private void AddMaterialListItem(Icon_HeroList listIcon)
    {
        if (listIcon == null)
            return;

        // 재료 아이콘 생성
        Icon_Hero icon = ManagerCS.instance.MakeIcon<Icon_Hero>(gameObject, ManagerCS.eIconType.Hero);
        icon.SetHeroIcon(listIcon.HeroCompanionData);
        icon.SettingType(ManagerCS.eSettingType.A_Type);
        icon.SetEquipmentIcon(listIcon.HeroCompanionData.isWearEquipment);
        icon.SetImprintInfo();
        icon.SetCallBack(SelectMaterialListIcon);

        if (GrowthUIType != HeroGrowthUIType.Composition)
        {
            bool bEnableImprint =
            (icon.HeroCompanionData.c_base.bSpecial == HeroSpecialType.ImprintType) ||
            (icon.HeroCompanionData.c_base.imprintGroup != 0 && icon.HeroCompanionData.c_base.limitGroup == HeroBaseData.limitGroup);

            icon.bEnableImprint = bEnableImprint;
        }

        MaterialHeroIconList.Add(icon);

        // 재료 슬롯 갱신
        RefreshMaterialList();

        // 영웅 리스트 아이콘 삭제
        HeroIconList.Remove(listIcon);
        DestroyImmediate(listIcon.gameObject);

        CheckMaterialHeroCount();
        // 영웅 리스트 갱신
        HeroListScrollView.Refresh();
    }

    private void RemoveMaterialListItem(Icon_Hero icon)
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

            if (GrowthUIType != HeroGrowthUIType.Composition)
            {
                // 각인 가능 영웅인지?
                bool bEnableImprint =
                    (icon.HeroCompanionData.c_base.bSpecial == HeroSpecialType.ImprintType) ||
                    (icon.HeroCompanionData.c_base.imprintGroup != 0 && icon.HeroCompanionData.c_base.limitGroup == HeroBaseData.limitGroup);

                if (bEnableImprint)
                    listIcon.SetEnableImprint();
            }

            HeroIconList.Add(listIcon);
        }

        // 영웅 리스트 갱신
        HeroListScrollView.Refresh();

        // 재료 아이콘 삭제
        MaterialHeroIconList.Remove(icon);
        Destroy(icon.gameObject);

        CheckMaterialHeroCount();
        // 재료 슬롯 갱신
        RefreshMaterialList();
    }

    private void ClearMaterialList()
    {
        for (int i = MaterialHeroIconList.Count - 1; i >= 0; i--)
        {
            RemoveMaterialListItem(MaterialHeroIconList[i]);
        }

        MaterialHeroIconList.Clear();

        for (int i = 0; i < SlotList.Length; i++)
        {
            SlotList[i].ConsumeEffect.SetActive(false);
        }

        RefreshMaterialList();
    }

    private void RefreshMaterialList()
    {
        List<HeroCompanion> materialList = new List<HeroCompanion>();

        for (int i = 0; i < SlotList.Length; i++)
        {
            if (i < MaterialHeroIconList.Count)
            {
                MaterialHeroIconList[i].transform.parent = SlotList[i].SlotPos.transform;
                MaterialHeroIconList[i].transform.localPosition = Vector3.zero;
                MaterialHeroIconList[i].transform.localScale = Vector3.one;

                if (MaterialHeroIconList[i].bEnableImprint && !isMaxImprintLevel(materialList))
                {
                    SlotList[i].WarningEffect.SetActive(false);
                    SlotList[i].ImprintEffect.SetActive(true);
                }
                else
                {
                    bool bWarning = MaterialHeroIconList[i].HeroCompanionData.isWearEquipment ||
                                MaterialHeroIconList[i].HeroBaseData.grade >= GameConstraints.HeroHighGrade;

                    SlotList[i].WarningEffect.SetActive(bWarning);
                    SlotList[i].ImprintEffect.SetActive(false);
                }

                materialList.Add(MaterialHeroIconList[i].HeroCompanionData);
            }
            else
            {
                SlotList[i].WarningEffect.SetActive(false);
                SlotList[i].ImprintEffect.SetActive(false);
            }
        }

        switch (GrowthUIType)
        {
            case HeroGrowthUIType.ExpObtain:
                {
                    CalcExpObtainResult();
                    ExecuteButton.isEnabled = (MaterialHeroIconList.Count > 0);
                }
                break;

            case HeroGrowthUIType.Evolution:
                {
                    CalcEvolutionResult();
                    CalcEvolutionSlotType();

                    if (evolutionSlotType == eMaterialType.None || evolutionSlotType == eMaterialType.Evolution)
                    {
                        SetEvolutionButton();
                        ExecuteButton.isEnabled = (MaterialHeroIconList.Count >= MaxMaterialHeroCount);
                    }
                    else
                    {
                        SetExpObtainButton();
                        ExecuteButton.isEnabled = (MaterialHeroIconList.Count > 0);

                        CostValue = HeroBaseData.expGold * MaterialHeroIconList.Count;
                        CostValueLabel.text = CostValue.ToString("#,##0");
                    }
                }
                break;

            case HeroGrowthUIType.Composition:
                {
                    CalcCompositionResult();
                    ExecuteButton.isEnabled = (MaterialHeroIconList.Count >= MaxMaterialHeroCount);
                }
                break;
        }
    }

    private void CalcImprintLevel(ref HeroCompanion resultData)
    {
        int maxImprintLv = TableManagerCS.instance.HeroImprintTable.GetMaxLv(resultData.c_base.bornGrade);

        if (resultData.imprintStep >= maxImprintLv)
        {
            isMaxImprint = true;
            return;
        }

        // 재료영웅 체크
        for (int cnt = 0; cnt < MaterialHeroIconList.Count; cnt++)
        {
            // 각인전용영웅이면 체크 패스
            if (MaterialHeroIconList[cnt].HeroBaseData.bSpecial != HeroSpecialType.ImprintType)
            {
                if (MaterialHeroIconList[cnt].HeroBaseData.imprintGroup == 0)
                    continue;

                if (MaterialHeroIconList[cnt].HeroBaseData.limitGroup != resultData.c_base.limitGroup)
                    continue;
            }

            // 같은 영웅이면
            // 재료영웅 + 각인레벨만큼 증가

            // Base + ImprintLv
            int increaseLv = 1 + MaterialHeroIconList[cnt].HeroCompanionData.imprintStep;

            for (int i = 0; i < increaseLv; i++)
            {
                resultData.imprintStep++;

                if (resultData.imprintStep >= maxImprintLv)
                {
                    isMaxImprint = true;
                    return;
                }
            }
        }

        isMaxImprint = (resultData.imprintStep >= maxImprintLv);
    }

    private bool isMaxImprintLevel(List<HeroCompanion> materialList)
    {
        HeroCompanion baseData = new HeroCompanion();
        baseData.CopyFrom(HeroCompanionData);

        int maxImprintLv = TableManagerCS.instance.HeroImprintTable.GetMaxLv(baseData.c_base.bornGrade);

        if (baseData.imprintStep >= maxImprintLv)
            return true;

        // 재료영웅 체크
        for (int cnt = 0; cnt < materialList.Count; cnt++)
        {
            if (materialList[cnt].c_base.imprintGroup == 0)
                continue;

            if (materialList[cnt].c_base.limitGroup != baseData.c_base.limitGroup)
                continue;

            // 같은 영웅이면
            // 재료영웅 + 각인레벨만큼 증가

            // Base + ImprintLv
            int increaseLv = 1 + materialList[cnt].imprintStep;

            for (int i = 0; i < increaseLv; i++)
            {
                baseData.imprintStep++;

                if (baseData.imprintStep >= maxImprintLv)
                    return true;
            }
        }

        return (baseData.imprintStep >= maxImprintLv);
    }

    private void CalcEvolutionSlotType()
    {
        evolutionSlotType = eMaterialType.None;

        int targetGrade = HeroBaseData.grade;
        Icon_Hero icon = null;

        for (int cnt = 0; cnt < MaterialHeroIconList.Count; cnt++)
        {
            icon = MaterialHeroIconList[cnt];

            // 1. 진화재료 -> 등급(==) 각인(!=)
            if (icon.HeroBaseData.grade == targetGrade && icon.bEnableImprint == false)
            {
                evolutionSlotType = eMaterialType.Evolution;
            }
            // 2. 각인재료 -> 등급(!=) 각인(==)
            else if (icon.bEnableImprint && icon.HeroBaseData.grade != targetGrade)
            {
                evolutionSlotType = eMaterialType.Imprint;
            }
            // 3. 각인진화재료 -> 등급(==) 각인(==)
            else if (icon.HeroBaseData.grade == targetGrade && icon.bEnableImprint)
            {
                List<HeroCompanion> materialList = new List<HeroCompanion>();
                for (int i = 0; i < cnt; i++)
                {
                    materialList.Add(MaterialHeroIconList[i].HeroCompanionData);
                }

                evolutionSlotType = (isMaxImprintLevel(materialList)) ? eMaterialType.Evolution : eMaterialType.ImprintEvolution;
            }

            if (evolutionSlotType == eMaterialType.Evolution || evolutionSlotType == eMaterialType.Imprint)
                break;
        }

        if (evolutionSlotType == eMaterialType.ImprintEvolution && MaterialHeroIconList.Count == MaxMaterialHeroCount)
            evolutionSlotType = eMaterialType.Evolution;
    }

    public void OnClickHeroFilteringButton()
    {
        PopupManager.instance.MakePopup<PopupHeroFiltering>(ePopupType.HeroFiltering).Initialize(GameData.Heroes, HeroFilteringCallBack);
    }

    private void HeroFilteringCallBack(List<HeroCompanion> list)
    {
        if (list == null)
            return;

        switch (GrowthUIType)
        {
            case HeroGrowthUIType.ExpObtain:
                {
                    SetExpObtainHeroList(list);
                    SetExpObtainSortingButton();
                }
                break;

            case HeroGrowthUIType.Evolution:
                {
                    SetEvolutionHeroList(list);
                    SetEvolutionSortingButton();
                }
                break;
        }
    }

    public void OnClickClearSlotButton()
    {
        ClearMaterialList();
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

    private void CheckMaterialHeroCount()
    {
        EmptyObj.SetActive(HeroIconList.Count <= 0);
    }
}
