using UnityEngine;
using System;
using System.Collections.Generic;
using ServerCommon;
using ServerCommon.DataTables;
using ServerCommon.Tutorials;
using System.Collections;

public partial class TeamSettingUI : GameNotification
{
	// Data
	private List<TeamSquadParameter> LocalTeamSquadList;    // in UI 팀 리스트
	private int LocalUsingTeamSquadIdx = 0;                 // in UI 팀 인덱스
	private TeamSquadParameter LocalCurrentTeamSquad;       // 현재 팀

	private int CurrentTeamBattlePower = 0;

	private List<HeroCompanion> HeroList;           // 영웅 리스트
	private List<Icon_HeroList> HeroIconList;       // 영웅 아이콘 리스트

	private bool bTeamSettingMode = true;

	[Serializable]
	public class SlotObject
	{
		// Data
		[HideInInspector]
		public int slotidx;
		public HeroCompanion HeroCompanionData;
		public long midx { get { return (HeroCompanionData != null) ? HeroCompanionData.manidx : 0; } }
		public int uidx { get { return (HeroCompanionData != null) ? HeroCompanionData.idx : 0; } }

		// UI
		public CharModelViewUI CharacterModelView;

		public GameObject HeroInfoObject;
		public UISprite SynastryIcon;
		public UISprite HeroTypeIcon;
		public UILabel Level;
		public UIEventTrigger SlotSelectTrigger;
		public GameObject SlotEffect;
		public GameObject BuffUnitEffect;

		public GameObject NotAvailableObj;

		public enum ButtonType
		{
			None,       // 모든 버튼 Off

			Join,       // 팀 참가 버튼
			Switch,     // 슬롯 변경 버튼
			Out,        // 팀 탈퇴 버튼
			Detail,     // 상세보기 버튼

			Select,     // 팀 탈퇴 + 상세보기 버튼
		}
		public UIButton JoinButton;
		public UIButton SwitchButton;
		public UIButton OutButton;
		public UIButton DetailInfoButton;

		public delegate void OnClickButtonCallBack(SlotObject slot, ButtonType type);
		private OnClickButtonCallBack ButtonCallBack;

		public delegate void OnClickSlotSelectCallBack(SlotObject slot);
		private OnClickSlotSelectCallBack SlotSelectCallBack;


		public void InitSlot(int slot, OnClickSlotSelectCallBack slotCallBack, OnClickButtonCallBack btnCallBack, GameObject _NotAvailableObj)
		{
			slotidx = slot;
			SlotSelectCallBack = slotCallBack;
			ButtonCallBack = btnCallBack;
			NotAvailableObj = _NotAvailableObj;
			NotAvailableObj.SetActive(false);

			EventDelegate.Set(SlotSelectTrigger.onClick, OnClickSlot);
			EventDelegate.Set(JoinButton.onClick, OnClickJoinButton);
			EventDelegate.Set(SwitchButton.onClick, OnClickSwitchButton);
			EventDelegate.Set(OutButton.onClick, OnClickOutButton);
			EventDelegate.Set(DetailInfoButton.onClick, OnClickDetailInfoButton);

			ClearSlot();
		}

		public void ClearSlot()
		{
			HeroCompanionData = null;
			CharacterModelView.DestroyModel();
			HeroInfoObject.SetActive(false);
			NotAvailableObj.SetActive(false);
			ButtonSet(ButtonType.None);
		}

		public void SetSlot(HeroCompanion hero)
		{
			if (hero == null)
			{
				ClearSlot();
				return;
			}

			if (HeroCompanionData != null && HeroCompanionData.manidx == hero.manidx)
				return;

			ClearSlot();

			HeroCompanionData = hero;

			// 대충 영웅 세팅하는 내용
			CharacterModelView.MakeSimpleModel(HeroCompanionData.idx, HeroCompanionData.costidx, HeroCompanionData.cosmanidx);
			CharacterModelView.CharPosition.transform.localRotation = Quaternion.Euler(0f, 150f, 0f);

			HeroInfoObject.SetActive(true);
			SynastryIcon.spriteName = ManagerCS.instance.GetSynastryIcon(HeroCompanionData.c_base.synastry);
			HeroTypeIcon.spriteName = ManagerCS.instance.GetHeroTypeIcon(HeroCompanionData.c_base.herotype);
			Level.text = SSLocalization.Format("heroInfo", 1, HeroCompanionData.lv);

			if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge
			&& GameData.EnterTwistDungeonResult != null)
			{
				var check = GameData.EnterTwistDungeonResult.twistheros.Find(row => row.value == HeroCompanionData.manidx);

				if (check != null)
					NotAvailableObj.SetActive(true);
				else
					NotAvailableObj.SetActive(false);
			}
		}


		public void ButtonSet(ButtonType type)
		{
			JoinButton.gameObject.SetActive(type == ButtonType.Join);
			SwitchButton.gameObject.SetActive(type == ButtonType.Switch);
			OutButton.gameObject.SetActive(type == ButtonType.Select);
			DetailInfoButton.gameObject.SetActive(type == ButtonType.Select);
		}

		public void SlotEffectOn()
		{
			if (BuffUnitEffect.activeSelf)
				BuffUnitEffect.SetActive(false);

			if (SlotEffect.activeSelf)
				SlotEffect.SetActive(false);

			SlotEffect.SetActive(true);
		}

		public void BuffUnitEffectOn()
		{
			if (BuffUnitEffect.activeSelf)
				BuffUnitEffect.SetActive(false);

			BuffUnitEffect.SetActive(true);
		}

		private void OnClickSlot()
		{
			//if (HeroCompanionData == null)
			//    return;

			if (SlotSelectCallBack != null)
				SlotSelectCallBack(this);
		}

		private void OnClickJoinButton()
		{
			if (ButtonCallBack != null)
				ButtonCallBack(this, ButtonType.Join);
		}

		private void OnClickSwitchButton()
		{
			if (ButtonCallBack != null)
				ButtonCallBack(this, ButtonType.Switch);
		}

		private void OnClickOutButton()
		{
			if (ButtonCallBack != null)
				ButtonCallBack(this, ButtonType.Out);
		}

		private void OnClickDetailInfoButton()
		{
			if (ButtonCallBack != null)
				ButtonCallBack(this, ButtonType.Detail);
		}
	}

	enum SelectMode
	{
		None,

		HeroList,       // 일반 편성 및 교체
		IncludeHero,    // 동일한 uidx 영웅과 교체
		TeamSlot,       // 팀 내부 
	}

	private HeroCompanion CurrentTeam_LeaderData = null;
	private HeroCompanion[] CurrentTeam_SupporterData = new HeroCompanion[4];

	private SlotObject CurrentSelectSlot;
	private SelectMode CurrentSelectMode;
	private HeroCompanion CurrentSelectHero;

	private voidDelegate SaveTeamSquadCallBack;


	// UI - Left
	public UIGrid TeamButtonGrid;
	private GameObject TeamButtonObj;
	private List<TeamButton> TeamButtonList;

	public GameObject TeamSettingButton;
	public GameObject TeamSettingButtonLock;
	public TweenRotation TeamSettingButtonTween;

	public ImprintInfoBox SelectHeroImprintInfoBox;


	// UI - Center
	public List<SlotObject> SlotList;

	public UILabel TeamBattlePower;
	public UILabel LeaderStatus_Atk;
	public UILabel LeaderStatus_Def;
	public UILabel LeaderStatus_HP;
	public UILabel SupporterStatus_Atk;
	public UILabel SupporterStatus_Def;
	public UILabel SupporterStatus_HP;
	public List<GameObject> NotAvailableObjList;

	public GameObject AutoTeamSettingButton;
	public GameObject AutoTeamSettingButtonEffect;

	public GameObject AppositeGradeObj;
	public GameObject FriendIcon;


	// UI - Right
	public GameObject TeamSettingPanel;
	public GameObject StoryDungeonPanel;
	public GameObject CommonDungeonPanel;
	public GameObject GuildRaidBuffPanel;
	public GameObject ForgottenRuinsPanel;
	public GameObject TwistDungeonPanel;

	public UnitListScrollView HeroListScrollView;
	public HeroSortingButton HeroListSortingButton;
	public UIButton FilteringButton;
	public UILabel HeroStorageCount;
    public GameObject HeroListEmptyObj;

    public GameObject TwistRemainingTime;
	public UILabel TwistRemainingTimeText;
	private CountRef TwistRemainTimer;
	private float FlowSec = 1;

	// Status Font Format & Color
	private string LeaderStatusFormatStr = "+{0}{1}";
	private string SupporterStatusFormatStr = "+{0}";
	private string Text_Green = "[A0FF30]";
	private string Text_Red = "[BF1915]";

	// HeroScrollViewSize
	private UIPanel panel;
	private Vector4 PanelSize;



	private void Awake()
	{
		HeroIconList = new List<Icon_HeroList>();
		TeamButtonList = new List<TeamButton>();
		TeamButtonObj = Resmanager.Load("UI/01_TeamSetting/TeamButton") as GameObject;

		// HeroListScrollViewSize 저장
		panel = HeroListScrollView.GetComponent<UIPanel>();
		PanelSize = panel.baseClipRegion;
	}
    
    protected override void Init()
    {
        SetTeamData();
        SetHeroList();
        SetSortingButton();
        SetHeroStorageCount();

        SetTeam();

        // BackButton 기능 세팅
        ManagerCS.instance.uiController.UIFinance.SetBackButtonFuntion(UIState.TeamSetting, OnClickTeamSettingBackButton);

		// 보스 약화 아이콘은 항상 꺼준다
		SetMonsterBossWeakInfo();

		switch (ManagerCS.instance.uiController.State)
        {
            case UIState.TeamSetting:
				{
					TeamSettingPanel.SetActive(true);
					StoryDungeonPanel.SetActive(false);
					CommonDungeonPanel.SetActive(false);
					GuildRaidBuffPanel.SetActive(false);
					ForgottenRuinsPanel.SetActive(false);
					TwistDungeonPanel.SetActive(false);

					bTeamSettingMode = true;
					AutoTeamSettingButton.SetActive(true);
					TeamSettingButton.SetActive(false);
					AppositeGradeObj.SetActive(false);
					FriendIcon.SetActive(false);
					FriendListEmptyObj.SetActive(false);

					if (TutorialManager.instance.CurrentTutorial == TutorialID.TeamSetting ||
						TutorialManager.instance.CheckTutorial(TutorialID.TeamSetting) == false)
					{
						SetTeamSettingTutorial();
					}
				}
                break;

            case UIState.StoryModeChallenge:
                {
                    TeamSettingPanel.SetActive(false);
                    StoryDungeonPanel.SetActive(true);
                    CommonDungeonPanel.SetActive(false);
                    GuildRaidBuffPanel.SetActive(false);
					ForgottenRuinsPanel.SetActive(false);
                    TwistDungeonPanel.SetActive(false);

                    AutoTeamSettingButton.SetActive(false);

                    SetStoryMode();

                    if (ManagerCS.instance.SelectStageIdx == GameConstraints.TutorialGuideHeroOpenStage)
                    {
                        if (!TutorialManager.instance.CheckTutorial(TutorialID.HelperHero))
                            SetHelperTutorial();
                    }
                    else if (ManagerCS.instance.SelectStageIdx == GameConstraints.TutorialWInEnterStage)
                    {
                        if (TutorialManager.instance.CheckTutorial(TutorialID.Win))
                            TutorialManager.instance.StartTutorial(TutorialID.Win_Link);
                    }
                    else if (ManagerCS.instance.SelectStageIdx == GameConstraints.TutorialSummonHeroEnterStage)
                    {
                        if (TutorialManager.instance.CheckTutorial(TutorialID.SummonHero_Link1))
                            TutorialManager.instance.StartTutorial(TutorialID.SummonHero_Link2);
                    }

                    if (TutorialManager.instance.CurrentTutorial == TutorialID.None)
                    {
                        if (ManagerCS.instance.GetSettingBoolValue(SettingType.ST_AutoSelectFriendHero))
                            AutoSetFriendHero();
                    }
                    else
                    {
                        ManagerCS.instance.macroSystem.MacroStop();
                    }
                }
                break;
            case UIState.ArousalChallenge:
                {
                    TeamSettingPanel.SetActive(false);
                    StoryDungeonPanel.SetActive(false);
                    CommonDungeonPanel.SetActive(true);
                    GuildRaidBuffPanel.SetActive(false);
					ForgottenRuinsPanel.SetActive(false);
                    TwistDungeonPanel.SetActive(false);

                    AutoTeamSettingButton.SetActive(false);
                    FriendListEmptyObj.SetActive(false);

                    SetArousalChallengeMode();
                }
                break;
            case UIState.EventDungeonChallenge:
                {
                    TeamSettingPanel.SetActive(false);
                    StoryDungeonPanel.SetActive(false);
                    CommonDungeonPanel.SetActive(true);
                    GuildRaidBuffPanel.SetActive(false);
					ForgottenRuinsPanel.SetActive(false);
                    TwistDungeonPanel.SetActive(false);

                    AutoTeamSettingButton.SetActive(false);
                    FriendListEmptyObj.SetActive(false);

                    SetEventDungeonChallengeMode();
                }
                break;
            case UIState.TowerRush_InfiniteChallenge:
                {
                    TeamSettingPanel.SetActive(false);
                    StoryDungeonPanel.SetActive(false);
                    CommonDungeonPanel.SetActive(true);
                    GuildRaidBuffPanel.SetActive(false);
					ForgottenRuinsPanel.SetActive(false);
                    TwistDungeonPanel.SetActive(false);

                    AutoTeamSettingButton.SetActive(false);
                    FriendListEmptyObj.SetActive(false);

                    SetTowerRush_InfiniteChallengeMode();
                }
                break;
			case UIState.ForgottenRuinsChallenge:
				{
					TeamSettingPanel.SetActive(false);
					StoryDungeonPanel.SetActive(false);
					CommonDungeonPanel.SetActive(false);
					GuildRaidBuffPanel.SetActive(false);
                    TwistDungeonPanel.SetActive(false);

                    ForgottenRuinsPanel.SetActive(true);
					isOpen_FRPanel = true;

					AutoTeamSettingButton.SetActive(false);
					FriendListEmptyObj.SetActive(false);

					SetForgottenRuinsChallengeMode();
				}
				break;
            case UIState.TowerRush_DevilFortressChallenge:
                {
                    TeamSettingPanel.SetActive(false);
                    StoryDungeonPanel.SetActive(false);
                    CommonDungeonPanel.SetActive(true);
                    GuildRaidBuffPanel.SetActive(false);
					ForgottenRuinsPanel.SetActive(false);
                    TwistDungeonPanel.SetActive(false);

                    AutoTeamSettingButton.SetActive(false);
                    FriendListEmptyObj.SetActive(false);

                    SetTowerRush_DevilFortressChallengeMode();
                }
                break;
            case UIState.OutbreakDungeonChallenge:
                {
                    TeamSettingPanel.SetActive(false);
                    StoryDungeonPanel.SetActive(false);
                    CommonDungeonPanel.SetActive(true);
                    GuildRaidBuffPanel.SetActive(false);
					ForgottenRuinsPanel.SetActive(false);
                    TwistDungeonPanel.SetActive(false);

                    AutoTeamSettingButton.SetActive(false);
                    FriendListEmptyObj.SetActive(false);

                    SetOutbreakDungeonChallengeMode();
                }
                break;
            case UIState.GuildRaidChallenge:
                {
                    TeamSettingPanel.SetActive(false);
                    StoryDungeonPanel.SetActive(false);
                    CommonDungeonPanel.SetActive(false);
                    GuildRaidBuffPanel.SetActive(true);
					ForgottenRuinsPanel.SetActive(false);
                    TwistDungeonPanel.SetActive(false);

                    AutoTeamSettingButton.SetActive(false);
                    FriendListEmptyObj.SetActive(false);

                    SetGuildRaidChallengeMode();

                    TutorialManager.instance.StartTutorial(TutorialID.GuildRaidTeamSetting);
                }
                break;
            case UIState.TwistDungeonChallenge:
                {
                    TeamSettingPanel.SetActive(false);
                    StoryDungeonPanel.SetActive(false);
                    CommonDungeonPanel.SetActive(false);
                    GuildRaidBuffPanel.SetActive(false);
                    ForgottenRuinsPanel.SetActive(false);
                    TwistDungeonPanel.SetActive(true);

                    AutoTeamSettingButton.SetActive(false);
                    FriendListEmptyObj.SetActive(false);
                    
					SetTwistDungeonChallengeMode();
                }
                break;
        }
    }
    
	private void SetTeamData()
    {
        // TeamSquad Data Copy
        LocalTeamSquadList = new List<TeamSquadParameter>();
        
        List<TeamSquadParameter> _TeamSquad = ManagerCS.instance.TeamSquad;
        TeamSquadParameter _TempObject;

        if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
            _TeamSquad = GameData.EnterTwistDungeonResult.settings;

        for (int i = 0; i < _TeamSquad.Count; i++)
        {
            _TempObject = _TeamSquad[i].Get();
            LocalTeamSquadList.Add(_TempObject);
        }
        LocalTeamSquadList.Sort((x, y) => { return (x.teamidx > y.teamidx) ? 1 : -1; });

        LocalUsingTeamSquadIdx = ManagerCS.instance.ContentsUsingTeamSquadIdx;

        //if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
        //    LocalUsingTeamSquadIdx = GameData.EnterTwistDungeonResult.twistTeamIdx;

        TeamButton btn = null;
        for (int i = 0; i < _TeamSquad.Count; i++)
        {
            if (_TeamSquad[i].teamType != ServerCommon.Enums.TeamType.Adventure && _TeamSquad[i].teamType != ServerCommon.Enums.TeamType.TwistDungeon)
                continue;

            int teamidx = _TeamSquad[i].teamidx;

			if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
			{
				if (teamidx != (int)ManagerCS.instance.TwistStartInfo.type)
					continue;
				else
					LocalUsingTeamSquadIdx = teamidx;
			}

            btn = NGUITools.AddChild(TeamButtonGrid.gameObject, TeamButtonObj).GetComponent<TeamButton>();
            btn.Set(teamidx, SetTeam);

            TeamButtonList.Add(btn);
        }

        TeamButtonGrid.repositionNow = true;

		for (int i = 0; i < SlotList.Count; i++)
		{
			SlotList[i].InitSlot((i + 1), SlotSelectCallBack, SlotButtonCallBack, NotAvailableObjList[i]);
		}
    }

	private void SetTeam(int teamidx = 0)
	{
		if (teamidx == 0)
			teamidx = LocalUsingTeamSquadIdx;
		else
			LocalUsingTeamSquadIdx = teamidx;


		if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
			LocalUsingTeamSquadIdx = (int)ManagerCS.instance.TwistStartInfo.type;
		

		// 팀버튼 토글 세팅
		TeamButtonList.Find(row => row.teamidx == LocalUsingTeamSquadIdx).toggle.value = true;

		LocalCurrentTeamSquad = LocalTeamSquadList.Find(row => row.teamidx == LocalUsingTeamSquadIdx);
		LocalCurrentTeamSquad.herolist.Sort((x, y) => { return (x.slotidx > y.slotidx) ? 1 : -1; });

		SetTeamInterface();
		SetAppositeGrade();
		SetAutoTeamSettingButtonEffect();

		SettingDebuffUI();

		// 상단 속성 아이콘은 상황에 따라 켜준다.
		OnUpsideWeakIcon();
	}
    
    private void SetTeamInterface()
    {
        int slotidx = 0;
        for (int i = 0; i < LocalCurrentTeamSquad.herolist.Count; i++)
        {
            slotidx = LocalCurrentTeamSquad.herolist[i].slotidx;
            HeroCompanion _HeroCompanion = GameData.Heroes.Find(row => row.manidx == LocalCurrentTeamSquad.herolist[i].hsidx);

            // 영웅 모델링 및 정보 세팅
            SlotObject slot = SlotList.Find(row => row.slotidx == LocalCurrentTeamSquad.herolist[i].slotidx);
            slot.SetSlot(_HeroCompanion);

            if (slotidx == (int)TeamSquadSlot.Leader)
            {
                CurrentTeam_LeaderData = _HeroCompanion;
            }
            else if (slotidx >= (int)TeamSquadSlot.Supporter1 && slotidx <= (int)TeamSquadSlot.Supporter4)
            {
                CurrentTeam_SupporterData[slotidx - 2] = _HeroCompanion;
            }
        }

        // 아이콘 팀정보 세팅
        if (ManagerCS.instance.uiController.State != UIState.TwistDungeonChallenge)
        {
            ManagerCS.instance.SetHeroJoinTeamData(LocalTeamSquadList);

            foreach (Icon_HeroList icon in HeroIconList)
            {
                icon.SetJoinTeamIcon(true);
            }
        }

        
        // 능력치 정보 세팅
        SetStatusInterface(CurrentTeam_LeaderData, CurrentTeam_SupporterData);
        
        // 슬롯모드 세팅
        SetSlotMode(SelectMode.None);
    }

    private void SetStatusInterface(HeroCompanion leaderData, HeroCompanion[] supporterData)
    {
        // 팀 전투력
        // ("Hero_Equip", 137) => [b]전투력 : [E29432]{0}[-][/b]
        CurrentTeamBattlePower = HeroStatusManager.instance.Calc_BattlePower(leaderData, supporterData);
        TeamBattlePower.text = SSLocalization.Format("Hero_Equip", 137, CurrentTeamBattlePower);
        
        // 능력치 정보
        CombatUnitAbilities_Client[] _TeamStatus = HeroStatusManager.instance.Calc_TeamUnitStatus(leaderData, supporterData);
        CombatUnitAbilities_Client _LeaderStatus = _TeamStatus[0];
        CombatUnitAbilities_Client _SupportersEffectStatus = _TeamStatus[1];
        CombatUnitAbilities_Client _LeaderBeforeStatus = HeroStatusManager.instance.Calc_HeroBaseStatus(leaderData);


        string _ColorCode = string.Empty;

        if (_LeaderStatus.atkPW == _LeaderBeforeStatus.atkPW) _ColorCode = string.Empty;
        else if (_LeaderStatus.atkPW > _LeaderBeforeStatus.atkPW) _ColorCode = Text_Green;
        else if (_LeaderStatus.atkPW < _LeaderBeforeStatus.atkPW) _ColorCode = Text_Red;

        LeaderStatus_Atk.text = string.Format(LeaderStatusFormatStr, _ColorCode, _LeaderStatus.atkPW.ToString("#,##0"));


        if (_LeaderStatus.defPW == _LeaderBeforeStatus.defPW) _ColorCode = string.Empty;
        else if (_LeaderStatus.defPW > _LeaderBeforeStatus.defPW) _ColorCode = Text_Green;
        else if (_LeaderStatus.defPW < _LeaderBeforeStatus.defPW) _ColorCode = Text_Red;

        LeaderStatus_Def.text = string.Format(LeaderStatusFormatStr, _ColorCode, _LeaderStatus.defPW.ToString("#,##0"));


        if (_LeaderStatus.maxHp == _LeaderBeforeStatus.maxHp) _ColorCode = string.Empty;
        else if (_LeaderStatus.maxHp > _LeaderBeforeStatus.maxHp) _ColorCode = Text_Green;
        else if (_LeaderStatus.maxHp < _LeaderBeforeStatus.maxHp) _ColorCode = Text_Red;

        LeaderStatus_HP.text = string.Format(LeaderStatusFormatStr, _ColorCode, _LeaderStatus.maxHp.ToString("#,##0"));


        SupporterStatus_Atk.text = string.Format(SupporterStatusFormatStr, _SupportersEffectStatus.atkPW.ToString("#,##0"));
        SupporterStatus_Def.text = string.Format(SupporterStatusFormatStr, _SupportersEffectStatus.defPW.ToString("#,##0"));
        SupporterStatus_HP.text = string.Format(SupporterStatusFormatStr, _SupportersEffectStatus.maxHp.ToString("#,##0"));

        

#if UNITY_EDITOR
        Debug.Log(GameUtils.LogColor_Green(" Battle Power "));

        int LeaderBP = HeroStatusManager.instance.Calc_BattlePower(leaderData, supporterData); 
        Debug.Log(GameUtils.LogColor_Red("LeaderBP : [" + LeaderBP + "]"));

        int[] SupporterBP = new int[4];
        for (int i = 0; i < supporterData.Length; i++)
        {
            SynastryType synastry = (leaderData != null) ? leaderData.c_base.synastry : SynastryType.None;

            SupporterBP[i] = HeroStatusManager.instance.Calc_SupporterBattlePower(supporterData[i], synastry);
            Debug.Log(GameUtils.LogColor_Red("SupporterBP(" + (i + 1) + ") : [" + SupporterBP[i] + "]"));
        }
#endif
    }

	private void SetAutoTeamSettingButtonEffect()
	{
		bool bEmptySlot = false;
		bool bActiveHero = false;

		foreach (SlotObject slot in SlotList)
		{
			if (slot.HeroCompanionData == null)
			{
				bEmptySlot = true;
				break;
			}
		}

		if (bEmptySlot)
		{
			// 현재팀에 포함중인 영웅이 아니며, 포함 가능한 영웅이 있을 때
			var heroes = GameData.Heroes;
			
			// 뒤틀린 던전의 경우 속성 일치 하고 사용가능한 영웅만 체크하도록
			if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
			{
				heroes = heroes.FindAll(i => (int)TableManagerCS.instance.HeroTable.bases.Find(row => row.uidx == i.idx).synastry == (int)ManagerCS.instance.TwistStartInfo.type);
				heroes = heroes.FindAll(i => !GameData.EnterTwistDungeonResult.twistheros.Exists(row => row.value == i.manidx));
			}

			foreach (HeroCompanion hero in heroes)
			{
				if (hero.c_base.bSpecial == HeroSpecialType.ImprintType)
					continue;

				if (LocalCurrentTeamSquad.herolist.Find(o => o.hsidx == hero.manidx) != null)
					continue;

				bActiveHero = true;
				break;
			}
		}

		AutoTeamSettingButtonEffect.SetActive(bActiveHero);
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

        if (TutorialManager.instance.CurrentTutorial == TutorialID.TeamSetting ||
            TutorialManager.instance.CheckTutorial(TutorialID.TeamSetting) == false)
        {
            ManagerCS.instance.Clear_hfo_main();
            list = ManagerCS.instance.GetFilteringHero(GameData.Heroes);
        }

        SetHeroList(list);
    }

    private void SetHeroList(List<HeroCompanion> list)
    {
		if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge
			&& ManagerCS.instance.TwistStartInfo.type != ServerCommon.Enums.TwistDungeonSynastryType.None)
		{
			int synastry = (int)ManagerCS.instance.TwistStartInfo.type;

			list = list.FindAll(i => (int)TableManagerCS.instance.HeroTable.bases.Find(row => row.uidx == i.idx).synastry == synastry);
		}
        
        // Setting
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

			if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
				icon.SetNotavailable(GameData.EnterTwistDungeonResult.twistheros.Exists(row => row.value == HeroList[i].manidx));				

			icon.SetIcon(HeroList[i], true);
            icon.SetCallBack(OnClickHeroIcon);
            icon.SetSelectButton(OnClickSelectButton);

            if (ManagerCS.instance.uiController.State == UIState.GuildRaidChallenge &&
                ManagerCS.instance.GuildRaidInfo.SynastrySecondaryData.Find(o => o.skilluidx == icon.HeroBaseData.skillsecondary) != null)
            {
                icon.SetBuff();
                icon.SetBuffSelectBtn(OnClickSelectButton);
            }

            HeroIconList.Add(icon);
        }

        HeroListEmptyObj.SetActive(HeroIconList.Count <= 0);

        // GuildRaid Buff Setting
        if (ManagerCS.instance.uiController.State == UIState.GuildRaidChallenge)
        {
            List<GuildRaidBuffInfo> buffInfoList = ManagerCS.instance.GuildRaidInfo.SynastrySecondaryData;

            foreach (Icon_HeroList icon in HeroIconList)
            {
                if (buffInfoList.Find(o => o.skilluidx == icon.HeroBaseData.skillsecondary) != null)
                    icon.SetBuff();
            }
        }

        HeroListScrollView.SetCompare(SortingClass.CompareHeroList);
        HeroListScrollView.onDragStarted = 
            delegate ()
            {
                if (CurrentSelectMode != SelectMode.None)
                    SetSlotMode(SelectMode.None);
            };
        HeroListScrollView.onStoppedMoving =
            delegate ()
            {
                Icon_HeroList target = HeroListScrollView.GetTargetData<Icon_HeroList>();
                if (target == null)
                    return;

                ManagerCS.instance.targetHeroData = target.HeroCompanionData;
            };

        ManagerCS.instance.SetFilteringButtonColor(ref FilteringButton);
    }

    private void OnClickHeroIcon(Icon_HeroList target)
    {
        if (CurrentSelectMode != SelectMode.None)
            SetSlotMode(SelectMode.None);

        if (HeroListScrollView.GetTargetData<Icon_HeroList>() == target)
        {
            if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
                return;

            ManagerCS.instance.targetHeroData = target.HeroCompanionData;
            ManagerCS.instance.uiController.ChangeUI(UIState.UnitManagement);
            return;
        }
        
        HeroListScrollView.TargetOn(target.transform);
    }

    private void OnClickSelectButton(Icon_HeroList target)
    {
        if (target.HeroCompanionData == null)
        {
            Debug.Log("OnClickSelectButton() -> HeroCompanionData is Null !");
            return;
        }

        HeroCompanion targetHero = target.HeroCompanionData;

        // 0. 각인전용 영웅은 팀에 편성 불가능
        if (targetHero.c_base.bSpecial == HeroSpecialType.ImprintType)
        {
            PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice).Set(SSLocalization.Get("Hero_Equip", 159));
            return;
        }


		if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
		{
			if (target.isDisabled)
			{
				PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice, false, true).Set(SSLocalization.Format("TwistDungeon", 4));
				return;
			}
		}


		for (int i = 0; i < SlotList.Count; i++)
        {
            // 1. 빈슬롯이면 -> Continue
            if (SlotList[i].midx == 0)
                continue;

            // 2. 팀에 이미 편성되어있는지 확인 -> 에러팝업
            if (SlotList[i].midx == targetHero.manidx)
            {
                PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice).Set(SSLocalization.Get("Hero_Equip", 147));
                return;
            }
            
            // 3. 같은 인덱스의 영웅이 있는지 확인 -> 그 영웅과 교체가능
            if (SlotList[i].HeroCompanionData.c_base.limitGroup == targetHero.c_base.limitGroup)
            {
                // 이 영웅이랑만 교체 가능
                CurrentSelectHero = targetHero;
                SetSlotMode(SelectMode.IncludeHero, SlotList[i]);
                return;
            }
        }

        CurrentSelectHero = targetHero;
        SetSlotMode(SelectMode.HeroList);
    }

    private void SlotSelectCallBack(SlotObject targetObject)
    {
        if (bTeamSettingMode)
            SetSlotMode(SelectMode.TeamSlot, targetObject);
        else
            OnClickTeamSettingButton();
    }
    
    private void SetSlotMode(SelectMode mode, SlotObject slot = null)
    {
        if (slot != null && slot.HeroCompanionData == null)
            return;

        CurrentSelectMode = mode;
        CurrentSelectSlot = slot;

        // equal switch case
        for (int i = 0; i < SlotList.Count; i++)
        {
            if (mode == SelectMode.None)
            {
                SlotList[i].ButtonSet(SlotObject.ButtonType.None);
                SelectHeroImprintInfoBox.Close();
            }
            else if (mode == SelectMode.HeroList)
            {
                if (SlotList[i].midx == 0)
                    SlotList[i].ButtonSet(SlotObject.ButtonType.Join);
                else
                    SlotList[i].ButtonSet(SlotObject.ButtonType.Switch);

                SelectHeroImprintInfoBox.Open(CurrentSelectHero);
            }
            else if (mode == SelectMode.IncludeHero)
            {
                if (SlotList[i].slotidx == slot.slotidx)
                    SlotList[i].ButtonSet(SlotObject.ButtonType.Switch);
                else
                    SlotList[i].ButtonSet(SlotObject.ButtonType.None);

                SelectHeroImprintInfoBox.Open(CurrentSelectHero);
            }
            else if (mode == SelectMode.TeamSlot)
            {
                if (SlotList[i].slotidx == slot.slotidx)
                    SlotList[i].ButtonSet(SlotObject.ButtonType.Select);
                else
                    SlotList[i].ButtonSet(SlotObject.ButtonType.Switch);
            }
        }
    }

    public void DeselectSlot()
    {
        SetSlotMode(SelectMode.None);
    }

    private void SlotButtonCallBack(SlotObject slot, SlotObject.ButtonType type)
    {
        switch (type)
        {
            case SlotObject.ButtonType.Join:
                {
                    // CurrentSelectHero -> slot 에 편성
                    JoinTeam(CurrentSelectHero, slot);

                    SoundPlay.instance.Play(2156, SOUND_TYPE.EFFECT_TYPE, 0);
                }
                break;

            case SlotObject.ButtonType.Switch:
                {
                    if (CurrentSelectMode == SelectMode.HeroList ||
                        CurrentSelectMode == SelectMode.IncludeHero)
                    {
                        // 1. 리스트에서
                        // slot -> 탈퇴
                        // CurrentSelectHero -> 편성
                        OutTeam(slot, true);
                        JoinTeam(CurrentSelectHero, slot);
                    }
                    else if (slot.HeroCompanionData == null)
                    {
                        JoinTeam(CurrentSelectSlot.HeroCompanionData, slot, true);
                        OutTeam(CurrentSelectSlot);
                    }
                    else
                    {
                        // 2. 팀 내에서
                        // slot <-> CurrentSelectSlot
                        SwitchTeam(CurrentSelectSlot, slot);
                    }

                    SoundPlay.instance.Play(2156, SOUND_TYPE.EFFECT_TYPE, 0);
                }
                break;

            case SlotObject.ButtonType.Out:
                {
                    // slot -> 탈퇴
                    OutTeam(slot);

                    SoundPlay.instance.Play(2157, SOUND_TYPE.EFFECT_TYPE, 0);
                }
                break;

            case SlotObject.ButtonType.Detail:
                {
                    // slot -> 상세정보
                    DetailInfo(slot);
                }
                break;
        }
    }

    private void JoinTeam(HeroCompanion hero, SlotObject targetSlot, bool isSwitch = false)
    {
        // 팀 정보 변경
        TeamSquadParameter.TeamNode node = LocalCurrentTeamSquad.herolist.Find(row => row.slotidx == targetSlot.slotidx);
        node.hsidx = hero.manidx;

        // 참가중 아이콘 세팅
        Icon_HeroList icon = HeroIconList.Find(row => row.HeroCompanionData.manidx == hero.manidx);
        if (icon != null)
            icon.SetJoinTeam(LocalCurrentTeamSquad.teamidx, node.slotidx);

        // 슬롯 세팅
        targetSlot.SetSlot(hero);

        if (!isSwitch)
        {
            SetTeam();

            if (ManagerCS.instance.uiController.State == UIState.GuildRaidChallenge && hero.isGuildRaidBuffUnit)
                targetSlot.BuffUnitEffectOn();
        }
    }

    private void OutTeam(SlotObject targetSlot, bool isSwitch = false)
    {
        if (!isSwitch)
        {
            bool bEmptyLeaderSlot = true;
            for (int slotCnt = 0; slotCnt < LocalCurrentTeamSquad.herolist.Count; slotCnt++)
            {
                if (LocalCurrentTeamSquad.herolist[slotCnt].slotidx == targetSlot.slotidx)
                    continue;

                if (LocalCurrentTeamSquad.herolist[slotCnt].hsidx != 0)
                {
                    bEmptyLeaderSlot = false;
                    break;
                }
            }

            if (bEmptyLeaderSlot)
            {
				if(ManagerCS.instance.uiController.State != UIState.TwistDungeonChallenge)
				{
					PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice).Set(SSLocalization.Get("Hero_Equip", 148));
					return;
				}
            }
        }
        
        // 팀 정보 변경
        TeamSquadParameter.TeamNode node = LocalCurrentTeamSquad.herolist.Find(row => row.slotidx == targetSlot.slotidx);
        node.hsidx = 0;
        
        // 참가중 아이콘 세팅
        Icon_HeroList icon = HeroIconList.Find(row => row.HeroCompanionData.manidx == targetSlot.midx);
        if (icon != null)
            icon.SetJoinTeam(0, 0);

        // 슬롯 세팅
        targetSlot.ClearSlot();

        if (!isSwitch)
            SetTeam();
    }

    private void SwitchTeam(SlotObject selectedSlot, SlotObject targetSlot, bool isSwitch = false)
    {
        // 팀 정보 변경
        HeroCompanion hero_a = selectedSlot.HeroCompanionData;
        HeroCompanion hero_b = targetSlot.HeroCompanionData;
        
        TeamSquadParameter.TeamNode node_a = LocalCurrentTeamSquad.herolist.Find(row => row.slotidx == selectedSlot.slotidx);
        TeamSquadParameter.TeamNode node_b = LocalCurrentTeamSquad.herolist.Find(row => row.slotidx == targetSlot.slotidx);

        node_a.hsidx = hero_b.manidx;
        node_b.hsidx = hero_a.manidx;

        // 슬롯 세팅
        selectedSlot.SetSlot(hero_b);
        targetSlot.SetSlot(hero_a);

        if (!isSwitch)
            SetTeam();
    }
    
    private void DetailInfo(SlotObject targetSlot)
    {
        if (targetSlot.HeroCompanionData == null)
            return;

        ManagerCS.instance.targetHeroData = targetSlot.HeroCompanionData;
        ManagerCS.instance.uiController.ChangeUI(UIState.UnitManagement);
    }
    
    private void SetSortingButton()
    {
        HeroListSortingButton.Init(SortHeroList);
        HeroListSortingButton.Sort();
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

        if (bResetScroll || ManagerCS.instance.targetHeroData == null)
        {
            HeroIconList.Sort(SortingClass.CompareHeroList);
            HeroListScrollView.TargetOn(HeroIconList[0].transform, true);
        }
        else
        {
            Icon_HeroList icon = HeroIconList.Find(row => row.HeroCompanionData.manidx == ManagerCS.instance.targetHeroData.manidx);
            HeroListScrollView.TargetOn(icon.transform, true);
        }
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

    public void OnClickAutoTeamSetting()
    {
		// 뒤틀린 던전의 경우 사용 불가 캐릭터를 일단 슬롯에서 뺀다
		if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
		{
			foreach (var slot in SlotList)
			{
				if (slot.HeroCompanionData != null &&
					GameData.EnterTwistDungeonResult.twistheros.Exists(row => row.value == slot.HeroCompanionData.manidx))
					OutTeam(slot);
			}
		}

		// 비어있는 슬롯 리스트
		List<SlotObject> emptySlotList = new List<SlotObject>();

        for (int i = 0; i < SlotList.Count; i++)
        {
            if (SlotList[i].HeroCompanionData == null)
                emptySlotList.Add(SlotList[i]);
        }
        
        if (emptySlotList.Count == 0)
            return;


        // 현재팀에 포함중인 영웅이 아니며, 포함 가능한 영웅이 있을 때
        bool bActiveHero = false;


		// 뒤틀린 던전의 경우 속성 일치 하고 사용가능한 영웅만 체크하도록
		var Dataheroes = GameData.Heroes;
		if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
		{
			Dataheroes = Dataheroes.FindAll(i => (int)TableManagerCS.instance.HeroTable.bases.Find(row => row.uidx == i.idx).synastry == (int)ManagerCS.instance.TwistStartInfo.type);
			Dataheroes = Dataheroes.FindAll(i => !GameData.EnterTwistDungeonResult.twistheros.Exists(row => row.value == i.manidx));
		}

		foreach (HeroCompanion hero in Dataheroes)
        {
            if (hero.c_base.bSpecial == HeroSpecialType.ImprintType)
                continue;

            if (LocalCurrentTeamSquad.herolist.Find(o => o.hsidx == hero.manidx) != null)
                continue;

            bActiveHero = true;
            break;
        }

        if (bActiveHero == false)
            return;


        // 현재 팀에 지원형 영웅이 장착되어있는지 체크
        bool bHaveSupporterInSquad = false;

        for (int i = 0; i < SlotList.Count; i++)
        {
            if (SlotList[i].HeroCompanionData != null &&
                SlotList[i].HeroCompanionData.c_base != null &&
                SlotList[i].HeroCompanionData.c_base.herotype == HeroCombatRoleType.sup)
            {
                bHaveSupporterInSquad = true;
                break;
            }
        }
        

        // 장착할 영웅 우선순위 리스트
        List<Icon_HeroList> targetList = new List<Icon_HeroList>();


		// 뒤틀린 던전의 경우 속성 일치 하고 사용가능한 영웅만 체크하도록
		var heroes = HeroIconList;
		if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
		{
			heroes = heroes.FindAll(i => (int)TableManagerCS.instance.HeroTable.bases.Find(row => row.uidx == i.HeroCompanionData.idx).synastry == (int)ManagerCS.instance.TwistStartInfo.type);
			heroes = heroes.FindAll(i => !GameData.EnterTwistDungeonResult.twistheros.Exists(row => row.value == i.HeroCompanionData.manidx));
		}


		for (int i = 0; i < heroes.Count; i++)
        {
            if (heroes[i].HeroCompanionData.c_base.bSpecial == HeroSpecialType.ImprintType)
                continue;
            
            if (CheckDuplication(heroes[i].HeroCompanionData.c_base.limitGroup))
                continue;

            targetList.Add(heroes[i]);
        }

        targetList.Sort(
            (Icon_HeroList a, Icon_HeroList b) =>
            {
                if (a.HeroCompanionData.c_battlepower < b.HeroCompanionData.c_battlepower)
                    return 1;
                else if (a.HeroCompanionData.c_battlepower > b.HeroCompanionData.c_battlepower)
                    return -1;

                if (a.HeroCompanionData.c_base.grade < b.HeroCompanionData.c_base.grade)
                    return 1;
                else if (a.HeroCompanionData.c_base.grade > b.HeroCompanionData.c_base.grade)
                    return -1;

                return 0;
            });


        // 1.지원형 장착되어있음
        //  => 리더~서포터4 까지 순서대로, 비어있는 슬롯에 조건순으로 영웅 배치(지원형 영웅 제외)

        // 2.지원형 장착되어있지 않음
        //  => 리더~서포터4 까지 순서대로, 비어있는 슬롯에 조건순으로 영웅 배치(지원형 영웅 제외)
        //  => 비어있는 마지막 슬롯(예:서포터4가 장착되어있다면 서포터3)에는 지원형 영웅 배치

        if (bHaveSupporterInSquad == false)
        {
            for (int i = 0; i < targetList.Count; i++)
            {
                if (targetList[i].HeroBaseData.herotype != HeroCombatRoleType.sup)
                    continue;
                
                if (CheckDuplication(targetList[i].HeroCompanionData.c_base.limitGroup))
                    continue;

                JoinTeam(targetList[i].HeroCompanionData, emptySlotList[emptySlotList.Count-1]);
                targetList.Remove(targetList[i]);
                emptySlotList.Remove(emptySlotList[emptySlotList.Count - 1]);

                break;
            }
        }

        for (int i = 0; i < emptySlotList.Count; i++)
        {
            for (int j = 0; j < targetList.Count; j++)
            {
                if (targetList[j].HeroBaseData.herotype == HeroCombatRoleType.sup)
                    continue;

                if (CheckDuplication(targetList[j].HeroCompanionData.c_base.limitGroup))
                    continue;

                JoinTeam(targetList[j].HeroCompanionData, emptySlotList[i]);
                targetList.Remove(targetList[j]);

                break;
            }
        }

        foreach (SlotObject slot in SlotList)
        {
            //// 장착된 슬롯만 이펙트 발생
            //if (slot.HeroCompanionData == null)
            //    continue;
            
            slot.SlotEffectOn();
        }

        SoundPlay.instance.SetSoundPlay(2133, SOUND_TYPE.EFFECT_TYPE, 0);
    }
    
    public void OnClickTeamImprintInfo()
    {
        List<HeroCompanion> TeamUnitList = new List<HeroCompanion>();

        // 리더
        if (CurrentTeam_LeaderData != null)
            TeamUnitList.Add(CurrentTeam_LeaderData);

        // 서포터
        foreach (HeroCompanion unit in CurrentTeam_SupporterData)
        {
            if (unit != null)
                TeamUnitList.Add(unit);
        }

        PopupManager.instance.MakePopup<PopupTeamImprintInfo>(ePopupType.TeamImprintInfo).Set(TeamUnitList);
    }

    private bool CheckDuplication(int group)
    {
        // 중복영웅 체크
        // true : 슬롯에 이미 동일한 idx 영웅이 장착중이다

        return SlotList.Find(row =>
                             row.HeroCompanionData != null &&
                             row.HeroCompanionData.c_base.limitGroup == group) != null;
    }


    public void OnClickTeamSettingButton()
    {
        if (TutorialManager.instance.CheckTutorial(TutorialID.TeamSetting) == false)
        {
            PopupCommon popup = PopupManager.instance.MakePopup<PopupCommon>(ePopupType.Common, false, true);
            popup.Set(PopupCommon.Type.OneBtn, SSLocalization.Get("Hero_Equip", 157));

            return;
        }
        
        bTeamSettingMode = !bTeamSettingMode;

		if (bTeamSettingMode)
        {
            // FriendList -> TeamList
            TeamSettingPanel.SetActive(true);
            StoryDungeonPanel.SetActive(false);
            CommonDungeonPanel.SetActive(false);
            GuildRaidBuffPanel.SetActive(false);
			ForgottenRuinsPanel.SetActive(false);
            TwistDungeonPanel.SetActive(false);

			AutoTeamSettingButton.SetActive(true);

            HeroListSortingButton.Sort();

			SetMonsterBossWeakInfo();

			// 이전 디버프 값 저장
			precheckCount = checkCount;

			// 상단 속성 아이콘은 상황에 따라 켜준다.
			OnUpsideWeakIcon();

			// 영웅 리스트 크기 변경..
			if (CurGameType == GAME_TYPE.TWISTDUNGEON_GAME_TYPE)
				panel.baseClipRegion = new Vector4(PanelSize.x, PanelSize.y + 50f, PanelSize.z, PanelSize.w - 100f);
			else
				panel.baseClipRegion = PanelSize;
		}
        else
        {
			if (isOpen_FRPanel)
				isOpen_FRPanel = false;

			// TeamList -> FriendList
			SaveTeamSquad(
                () =>
                {
                    SetTeam();

                    TeamSettingPanel.SetActive(false);
                    AutoTeamSettingButton.SetActive(false);

                    switch (ManagerCS.instance.uiController.State)
                    {
                        case UIState.StoryModeChallenge:
                            {
                                StoryDungeonPanel.SetActive(true);
                            }
                            break;
                        case UIState.ArousalChallenge:
                        case UIState.EventDungeonChallenge:
                        case UIState.TowerRush_InfiniteChallenge:
                        case UIState.TowerRush_DevilFortressChallenge:
                        case UIState.OutbreakDungeonChallenge:
                            {
                                CommonDungeonPanel.SetActive(true);
                            }
                            break;
                        case UIState.GuildRaidChallenge:
                            {
                                GuildRaidBuffPanel.SetActive(true);
                            }
                            break;
						case UIState.ForgottenRuinsChallenge:
							{
								ForgottenRuinsPanel.SetActive(true);
								isOpen_FRPanel = true;

								SetForgottenRuinsWeakInfo();
							}
							break;
                        case UIState.TwistDungeonChallenge:
                            {
                                TwistDungeonPanel.SetActive(true);
                            }
                            break;
					}
                });
        }
    }

    private void OnClickTeamSettingBackButton()
    {
        SaveTeamSquad(
            () =>
            {
                PopupManager.instance.RemovePressPopup();
                ManagerCS.instance.uiController.PopUI();
            });
    }

    public void SaveTeamSquad(voidDelegate callBack = null)
    {
        SaveTeamSquadCallBack = callBack;

        // 슬롯 재정렬
        SortHeroInSlot();

        // 모든 리더슬롯이 비어있는지 확인
        if (CheckEmptyAllLeaderSlot())
        {
            // 뒤틀린 던전 입장화면에서 편성가능한 영웅이 아예 없으면 그냥 돌려준다
            if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
                callBack();
			else
				PopupManager.instance.MakePopup<PopupNotice>(ePopupType.Notice).Set(SSLocalization.Get("Hero_Equip", 129));

			return;
        }

        // 현재 팀 리더슬롯이 비어있으면 리더슬롯이 비어있지 않은 팀 선택
        CheckCurrentTeamLeaderSlot();

        // 팀 정보 바뀌었는지 체크
        if (CheckChangeTeamInfo())
        {
            if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
            {
                for (int i = 0; i < LocalTeamSquadList.Count; i++)
                    LocalTeamSquadList[i].teamType = ServerCommon.Enums.TeamType.TwistDungeon;

                ManagerCS.instance.Send_SaveTwistTeamSquadRequest(LocalUsingTeamSquadIdx, LocalTeamSquadList);
            }
            else
                ManagerCS.instance.Send_SaveTeamSquadRequest(LocalUsingTeamSquadIdx, LocalTeamSquadList);

        }
        else
        {
            OnUpdateSaveTeamSquad();
        }
    }

    internal override void OnUpdateSaveTeamSquad()
    {
        if (SaveTeamSquadCallBack != null)
            SaveTeamSquadCallBack();
    }

    private void SortHeroInSlot()
    {
        TeamSquadParameter team;
        int currentCnt = -1;
        int currentBP = 0;
        int supporterBP = 0;

        
        // 팀 리스트 확인
        for (int teamCnt = 0; teamCnt < LocalTeamSquadList.Count; teamCnt++)
        {
            team = LocalTeamSquadList[teamCnt];
            team.herolist.Sort((x, y) => { return (x.slotidx > y.slotidx) ? 1 : -1; });

            // 팀 슬롯 확인
            for (int slotCnt = 0; slotCnt < team.herolist.Count; slotCnt++)
            {
                // 타겟슬롯이 비어있다
                if (team.herolist[slotCnt].hsidx == 0)
                {
                    // 비교값 초기화
                    currentCnt = -1;
                    currentBP = 0;

                    // 비어있는 슬롯 뒤로 나머지 비교슬롯 확인
                    for (int heroCnt = slotCnt + 1; heroCnt < team.herolist.Count; heroCnt++)
                    {
                        // 비교슬롯이 비어있다
                        if (team.herolist[heroCnt].hsidx == 0)
                            continue;

                        // 타겟슬롯이 리더일 경우 전투력 비교
                        if (slotCnt == 0)
                        {
                            supporterBP = GameData.Heroes.Find(row => row.manidx == team.herolist[heroCnt].hsidx).c_battlepower;

                            if (currentBP < supporterBP)
                            {
                                currentCnt = heroCnt;
                                currentBP = supporterBP;
                            }
                        }
                        // 타겟슬롯이 서포터일 경우 앞으로 당김
                        else
                        {
                            currentCnt = heroCnt;
                            break;
                        }
                    }

                    // 팀 슬롯위치 변경
                    if (currentCnt != -1)
                    {
                        team.herolist[slotCnt].hsidx = team.herolist[currentCnt].hsidx;
                        team.herolist[currentCnt].hsidx = 0;
                    }
                }
            }
        }
    }

    private bool CheckEmptyAllLeaderSlot()
    {
        TeamSquadParameter team;

        // 팀 리스트 확인
        for (int teamCnt = 0; teamCnt < LocalTeamSquadList.Count; teamCnt++)
        {
            team = LocalTeamSquadList[teamCnt];

            // 팀 슬롯 확인
            for (int slotCnt = 0; slotCnt < team.herolist.Count; slotCnt++)
            {
                // 리더슬롯이 비어있지 않으면 통과
                if (team.herolist[slotCnt].slotidx == (int)TeamSquadSlot.Leader && team.herolist[slotCnt].hsidx != 0)
                    return false;
            }
        }
        
        return true;
    }

    private void CheckCurrentTeamLeaderSlot()
    {
        // 현재 팀 리더슬롯이 비어있으면
        if (LocalCurrentTeamSquad.herolist.Find(row => row.slotidx == (int)TeamSquadSlot.Leader).hsidx == 0)
        {
            LocalTeamSquadList.Sort((a, b) => { return (a.teamidx > b.teamidx) ? 1 : -1; });

            for (int i = 0; i < LocalTeamSquadList.Count; i++)
            {
                // 리더슬롯이 비어있지 않은 팀 선택
                if (LocalTeamSquadList[i].herolist.Find(row => row.slotidx == (int)TeamSquadSlot.Leader).hsidx != 0)
                {
                    LocalUsingTeamSquadIdx = LocalTeamSquadList[i].teamidx;
                    break;
                }
            }
        }
    }

    private bool CheckChangeTeamInfo()
    {
        bool bChange = false;

        // 사용중인 팀이 변경되었는지?
        if (ManagerCS.instance.UsingTeamSquadIdx != LocalUsingTeamSquadIdx)
        {
            bChange = true;
        }

        // 영웅이 변경되었는지?
        List<TeamSquadParameter> OriginalTeamSquadData = ManagerCS.instance.TeamSquad;

        if (ManagerCS.instance.uiController.State == UIState.TwistDungeonChallenge)
            OriginalTeamSquadData = GameData.EnterTwistDungeonResult.settings;

        for (int i = 0; i < OriginalTeamSquadData.Count; i++)
        {
            for (int j = 0; j < OriginalTeamSquadData[i].herolist.Count; j++)
            {
                if (OriginalTeamSquadData[i].herolist[j].hsidx != LocalTeamSquadList[i].herolist[j].hsidx)
                {
                    bChange = true;
                }
            }
        }

        return bChange;
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

    // 우편함에서 영웅 획득
    internal override void OnUpdateHeroList()
    {
        SetHeroList();
        SetSortingButton();
        SetHeroStorageCount();

        if (ManagerCS.instance.uiController.State == UIState.GuildRaidChallenge)
            RefreshBuffUnitList();
    }

    // 인벤토리에서 장비정보 변경
    internal override void OnUpdateHeroInfo()
    {
        SetStatusInterface(CurrentTeam_LeaderData, CurrentTeam_SupporterData);
        SetSlotMode(SelectMode.None);
    }

    private void SetTeamSettingTutorial()
    {
        Icon_HeroList tutoHero = HeroIconList.Find(row => row.HeroCompanionData.manidx == TutorialManager.instance.tutoSupporter_midx);
        if (tutoHero != null)
        {
            HeroListScrollView.TargetOn(tutoHero.transform);

            TutorialObject selectBtn = tutoHero.SelectBtn.gameObject.AddComponent<TutorialObject>();
            selectBtn.tag = "TUTORIAL";
            selectBtn.Key = "selectBtn";
        }

        TutorialManager.instance.StartTutorial(TutorialID.TeamSetting);
    }

    internal override void OnUpdateHeroEquipmentInfo(ManagerCS.EquipmentSetData setData)
    {
        OnUpdateHeroInfo();
    }
}
