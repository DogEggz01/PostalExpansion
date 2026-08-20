using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PostalExpansion
{
	internal static class PostalMissionUi
	{
		private const int VanillaMissionPageSize = 5;
		private const string MailTabObjectName = "postal_expansion_mail_tab";
		private const string MailTabLabel = "mail";
		private const float ThreeTabScale = 2f / 3f;

		private static readonly Color UrgentMissionColor =
			new Color(1f, 0.45f, 0.08f, 1f);
		private static readonly Color GoldenMissionColor =
			new Color(1f, 0.72f, 0.12f, 1f);
		private static readonly MaterialPropertyBlock MissionHighlightProperties =
			new MaterialPropertyBlock();

		private static readonly FieldInfo LocalButtonField =
			AccessTools.Field(typeof(MissionListUI), "localButton");
		private static readonly FieldInfo WorldButtonField =
			AccessTools.Field(typeof(MissionListUI), "worldButton");
		private static readonly FieldInfo MissionButtonsField =
			AccessTools.Field(typeof(MissionListUI), "missionButtons");
		private static readonly FieldInfo CurrentPortDudeField =
			AccessTools.Field(typeof(MissionListUI), "currentPortDude");
		private static readonly FieldInfo CurrentPageField =
			AccessTools.Field(typeof(MissionListUI), "currentPage");
		private static readonly FieldInfo CurrentPageCountField =
			AccessTools.Field(typeof(MissionListUI), "currentPageCount");
		private static readonly FieldInfo PageCountTextField =
			AccessTools.Field(typeof(MissionListUI), "pageCountText");

		private static readonly FieldInfo CurrentMissionField =
			AccessTools.Field(typeof(MissionDetailsUI), "currentMission");
		private static readonly FieldInfo DetailsUiField =
			AccessTools.Field(typeof(MissionDetailsUI), "UI");
		private static readonly FieldInfo ClickableField =
			AccessTools.Field(typeof(MissionDetailsUI), "clickable");
		private static readonly FieldInfo MapZoomedInField =
			AccessTools.Field(typeof(MissionDetailsUI), "mapZoomedIn");
		private static readonly MethodInfo UpdateTextsMethod =
			AccessTools.Method(typeof(MissionDetailsUI), "UpdateTexts");

		private static MissionListUI patchedUi;
		private static PostalMailTabButton mailButton;
		private static bool mailMissions;

		internal static void EnsureMailButton(MissionListUI ui)
		{
			if (ui == null || (patchedUi == ui && mailButton != null))
			{
				return;
			}

			if (LocalButtonField == null || WorldButtonField == null)
			{
				Debug.LogWarning("Postal Expansion: mission tab fields were not found.");
				return;
			}

			var localButton = LocalButtonField.GetValue(ui) as GPButtonMissionListWorld;
			var worldButton = WorldButtonField.GetValue(ui) as GPButtonMissionListWorld;
			if (localButton == null || worldButton == null)
			{
				Debug.LogWarning("Postal Expansion: mission tab buttons were not found.");
				return;
			}

			Transform localRoot = GetTabRoot(localButton);
			Transform worldRoot = GetTabRoot(worldButton);
			if (localRoot == null ||
				worldRoot == null ||
				localRoot.parent == null ||
				localRoot.parent != worldRoot.parent)
			{
				Debug.LogWarning("Postal Expansion: mission tab roots were not found.");
				return;
			}

			Transform existingMailRoot = worldRoot.parent.Find(MailTabObjectName);
			if (existingMailRoot != null)
			{
				mailButton = existingMailRoot.GetComponentInChildren<PostalMailTabButton>(true);
				if (mailButton != null)
				{
					patchedUi = ui;
					UpdateTabMaterials(ui);
					return;
				}
			}

			GameObject mailRootObject = Object.Instantiate(worldRoot.gameObject, worldRoot.parent);
			mailRootObject.name = MailTabObjectName;
			var clonedWorldButton =
				mailRootObject.GetComponentInChildren<GPButtonMissionListWorld>(true);
			if (clonedWorldButton == null)
			{
				Object.Destroy(mailRootObject);
				Debug.LogWarning("Postal Expansion: cloned world tab button was not found.");
				return;
			}

			GameObject mailButtonObject = clonedWorldButton.gameObject;
			clonedWorldButton.enabled = false;
			Object.Destroy(clonedWorldButton);

			TextMesh label = mailRootObject.GetComponentInChildren<TextMesh>(true);
			if (label != null)
			{
				label.text = MailTabLabel;
			}

			mailButton = mailButtonObject.AddComponent<PostalMailTabButton>();
			mailButton.activeMat = worldButton.activeMat;
			mailButton.inactiveMat = worldButton.inactiveMat;
			FitThreeTabs(localRoot, worldRoot, mailRootObject.transform);
			patchedUi = ui;
			UpdateTabMaterials(ui);
		}

		internal static void SetMailMissions(bool mail)
		{
			mailMissions = mail;
			UpdateTabMaterials(MissionListUI.instance);
		}

		internal static void FilterInitialPortMissions(
			MissionListUI ui,
			PortDude dude,
			ref Mission[] missions)
		{
			if (ui != null && dude != null)
			{
				missions = GetMissions(ui, dude.GetPort(), PostalMissionTab.Local, 0);
			}
		}

		internal static void RefreshPageCount(MissionListUI ui)
		{
			if (!HasMissionListPagingReflection() || ui == null || GetCurrentPortDude(ui) == null)
			{
				return;
			}

			UpdatePageCount(ui);
			UpdatePageCountText(ui);
		}

		internal static bool TryResetPage(MissionListUI ui)
		{
			if (!GameState.inPortMissionList ||
				!HasMissionListPagingReflection() ||
				ui == null ||
				GetCurrentPortDude(ui) == null)
			{
				return false;
			}

			SetCurrentPage(ui, 0);
			UISoundPlayer.instance.PlayParchmentSound();
			ui.DisplayMissions(GetMissions(ui, GetCurrentPage(ui)));
			UpdatePageCount(ui);
			UpdatePageCountText(ui);
			return true;
		}

		internal static bool TryChangePage(MissionListUI ui, int pageChange)
		{
			if (!GameState.inPortMissionList || !HasMissionListPagingReflection() || ui == null)
			{
				return false;
			}

			if (GetCurrentPortDude(ui) == null)
			{
				Debug.LogError("MissionListUI.ChangePage: currentPortDude is null.");
				return true;
			}

			SetCurrentPage(ui, GetCurrentPage(ui) + pageChange);
			if (GetCurrentPage(ui) < 0)
			{
				SetCurrentPage(ui, 0);
			}
			else if (GetCurrentPage(ui) > GetCurrentPageCount(ui) - 1)
			{
				SetCurrentPage(ui, GetCurrentPageCount(ui) - 1);
			}
			else
			{
				UISoundPlayer.instance.PlayParchmentSound();
				ui.DisplayMissions(GetMissions(ui, GetCurrentPage(ui)));
			}

			UpdatePageCountText(ui);
			return true;
		}

		internal static bool TryHandlePortMissionAcceptance(MissionDetailsUI detailsUi)
		{
			MissionListUI missionListUi = MissionListUI.instance;
			if (!GameState.inPortMissionList ||
				!HasMissionDetailsReflection() ||
				!HasMissionListPagingReflection() ||
				detailsUi == null ||
				missionListUi == null ||
				GetCurrentPortDude(missionListUi) == null ||
				!(bool)ClickableField.GetValue(detailsUi) ||
				(bool)MapZoomedInField.GetValue(detailsUi))
			{
				return false;
			}

			var currentMission = CurrentMissionField.GetValue(detailsUi) as Mission;
			if (currentMission == null || currentMission.missionIndex != -1)
			{
				return false;
			}

			PlayerMissions.AcceptMission(currentMission);
			missionListUi.DisplayMissions(GetMissions(missionListUi, 0));
			var detailsObject = DetailsUiField.GetValue(detailsUi) as GameObject;
			if (detailsObject != null)
			{
				detailsObject.SetActive(false);
			}
			UpdateTextsMethod.Invoke(detailsUi, null);
			return true;
		}

		internal static void UpdateMissionHighlights(MissionListUI ui)
		{
			if (ui == null || MissionButtonsField == null)
			{
				return;
			}

			var missionButtons = MissionButtonsField.GetValue(ui) as GPButtonListedMission[];
			if (missionButtons == null)
			{
				return;
			}

			foreach (GPButtonListedMission button in missionButtons)
			{
				if (button == null)
				{
					continue;
				}

				Renderer renderer = button.GetComponent<Renderer>();
				Material material = renderer != null ? renderer.sharedMaterial : null;
				if (material == null)
				{
					continue;
				}

				bool active = button.gameObject.activeSelf;
				bool golden = active &&
					GoldenDeliveryMissions.IsGolden(button.mission);
				bool urgent = active &&
					UrgentExpressMail.IsUrgent(button.mission);
				bool highlighted = golden || urgent;
				Color highlightColor = golden
					? GoldenMissionColor
					: UrgentMissionColor;
				MissionHighlightProperties.Clear();
				renderer.GetPropertyBlock(MissionHighlightProperties);
				SetMissionMaterialColor(
					material,
					"_Color",
					highlighted,
					highlightColor);
				SetMissionMaterialColor(
					material,
					"_BaseColor",
					highlighted,
					highlightColor);
				renderer.SetPropertyBlock(MissionHighlightProperties);
			}
		}

		private static Mission[] GetMissions(MissionListUI ui, int page)
		{
			return GetMissions(
				ui,
				GetCurrentPortDude(ui).GetPort(),
				GetCurrentTab(ui),
				page);
		}

		private static Mission[] GetMissions(
			MissionListUI ui,
			Port port,
			PostalMissionTab tab,
			int page)
		{
			int pageSize = GetMissionPageSize(ui);
			var pageMissions = new Mission[pageSize];
			int startIndex = page * pageSize;
			List<Mission> availableMissions = GetAvailableMissions(port, tab);
			for (int i = 0; i < pageMissions.Length; i++)
			{
				int missionIndex = startIndex + i;
				if (missionIndex < availableMissions.Count)
				{
					pageMissions[i] = availableMissions[missionIndex];
				}
			}

			return pageMissions;
		}

		private static int GetMissionPageSize(MissionListUI ui)
		{
			var missionButtons = MissionButtonsField?.GetValue(ui) as GPButtonListedMission[];
			return missionButtons == null || missionButtons.Length == 0
				? VanillaMissionPageSize
				: missionButtons.Length;
		}

		private static List<Mission> GetAvailableMissions(Port port, PostalMissionTab tab)
		{
			var missions = new List<Mission>();
			if (port == null)
			{
				return missions;
			}

			if (tab == PostalMissionTab.Mail)
			{
				AddFilteredVanillaMissions(port, false, tab, missions);
				AddFilteredVanillaMissions(port, true, tab, missions);
				PostalExpressMail.AddExpressMissions(port, missions);
				GoldenDeliveryMissions.AddMission(port, missions);
				RegisteredLetterMissions.AddMissions(port, missions);
				AnonymousLetterMissions.AddMissions(port, missions);
			}
			else
			{
				AddFilteredVanillaMissions(
					port,
					tab == PostalMissionTab.World,
					tab,
					missions);
			}

			missions.Sort((left, right) => right.pricePerKm.CompareTo(left.pricePerKm));
			return missions;
		}

		private static void AddFilteredVanillaMissions(
			Port port,
			bool world,
			PostalMissionTab tab,
			List<Mission> missions)
		{
			AddMissions(port.GetMissions(0, world), tab, missions);
			int pageCount = Mathf.Max(
				1,
				Mathf.CeilToInt((float)port.GetMissionCount() / VanillaMissionPageSize));
			for (int page = 1; page < pageCount; page++)
			{
				AddMissions(port.GetMissions(page, world), tab, missions);
			}
		}

		private static void AddMissions(
			Mission[] candidates,
			PostalMissionTab tab,
			List<Mission> missions)
		{
			if (candidates == null)
			{
				return;
			}

			foreach (Mission candidate in candidates)
			{
				if (candidate != null && ShouldShowMission(candidate, tab))
				{
					missions.Add(candidate);
				}
			}
		}

		private static bool ShouldShowMission(Mission candidate, PostalMissionTab tab)
		{
			bool mail = IsMailMission(candidate);
			return tab == PostalMissionTab.Mail ? mail : !mail;
		}

		private static bool IsMailMission(Mission candidate)
		{
			if (candidate == null || candidate.goodPrefab == null)
			{
				return false;
			}

			SaveablePrefab saveable = candidate.goodPrefab.GetComponent<SaveablePrefab>();
			return PostalMail.IsMailPrefab(saveable);
		}

		private static void UpdatePageCount(MissionListUI ui)
		{
			int pageSize = GetMissionPageSize(ui);
			int missionCount = GetAvailableMissions(
				GetCurrentPortDude(ui).GetPort(),
				GetCurrentTab(ui)).Count;
			int pageCount = Mathf.Max(1, Mathf.CeilToInt((float)missionCount / pageSize));
			CurrentPageCountField.SetValue(ui, pageCount);
		}

		private static void UpdatePageCountText(MissionListUI ui)
		{
			var pageCountText = PageCountTextField.GetValue(ui) as TextMesh;
			if (pageCountText != null)
			{
				pageCountText.text = $"{GetCurrentPage(ui) + 1} / {GetCurrentPageCount(ui)}";
			}
		}

		private static void UpdateTabMaterials(MissionListUI ui)
		{
			if (ui == null || LocalButtonField == null || WorldButtonField == null)
			{
				return;
			}

			var localButton = LocalButtonField.GetValue(ui) as GPButtonMissionListWorld;
			var worldButton = WorldButtonField.GetValue(ui) as GPButtonMissionListWorld;
			PostalMissionTab currentTab = GetCurrentTab(ui);
			if (localButton != null)
			{
				localButton.SetMaterial(currentTab == PostalMissionTab.Local);
			}
			if (worldButton != null)
			{
				worldButton.SetMaterial(currentTab == PostalMissionTab.World);
			}
			if (mailButton != null)
			{
				mailButton.SetMaterial(currentTab == PostalMissionTab.Mail);
			}
		}

		private static PostalMissionTab GetCurrentTab(MissionListUI ui)
		{
			if (mailMissions)
			{
				return PostalMissionTab.Mail;
			}

			return ui.worldMissions ? PostalMissionTab.World : PostalMissionTab.Local;
		}

		private static PortDude GetCurrentPortDude(MissionListUI ui)
		{
			return CurrentPortDudeField?.GetValue(ui) as PortDude;
		}

		private static int GetCurrentPage(MissionListUI ui)
		{
			return (int)CurrentPageField.GetValue(ui);
		}

		private static void SetCurrentPage(MissionListUI ui, int page)
		{
			CurrentPageField.SetValue(ui, page);
		}

		private static int GetCurrentPageCount(MissionListUI ui)
		{
			return (int)CurrentPageCountField.GetValue(ui);
		}

		private static Transform GetTabRoot(Component tabComponent)
		{
			return tabComponent.transform.parent != null
				? tabComponent.transform.parent
				: tabComponent.transform;
		}

		private static void FitThreeTabs(
			Transform localRoot,
			Transform worldRoot,
			Transform mailRoot)
		{
			Vector3 localPosition = localRoot.localPosition;
			Vector3 worldPosition = worldRoot.localPosition;
			Vector3 spacing = worldPosition - localPosition;
			localRoot.localPosition = localPosition - spacing / 6f;
			worldRoot.localPosition = localPosition + spacing * 0.5f;
			mailRoot.localPosition = worldPosition + spacing / 6f;
			ScaleTabRoot(localRoot);
			ScaleTabRoot(worldRoot);
			ScaleTabRoot(mailRoot);
		}

		private static void ScaleTabRoot(Transform tabRoot)
		{
			Vector3 localScale = tabRoot.localScale;
			localScale.x *= ThreeTabScale;
			tabRoot.localScale = localScale;
		}

		private static void SetMissionMaterialColor(
			Material material,
			string propertyName,
			bool highlighted,
			Color highlightColor)
		{
			if (!material.HasProperty(propertyName))
			{
				return;
			}

			Color color = material.GetColor(propertyName);
			if (highlighted)
			{
				color.r = highlightColor.r;
				color.g = highlightColor.g;
				color.b = highlightColor.b;
			}
			MissionHighlightProperties.SetColor(propertyName, color);
		}

		private static bool HasMissionListPagingReflection()
		{
			return MissionButtonsField != null &&
				CurrentPortDudeField != null &&
				CurrentPageField != null &&
				CurrentPageCountField != null &&
				PageCountTextField != null;
		}

		private static bool HasMissionDetailsReflection()
		{
			return CurrentMissionField != null &&
				DetailsUiField != null &&
				ClickableField != null &&
				MapZoomedInField != null &&
				UpdateTextsMethod != null;
		}
	}
}
