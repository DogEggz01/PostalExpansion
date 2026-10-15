using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using cakeslice;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PostalExpansion
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	[BepInDependency(NandTweaksCompatibility.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
	public class Plugin : BaseUnityPlugin
	{
		internal const string PluginGuid = "com.DogEggz.postalexpansion";
		internal const string PluginName = "Postal Expansion";
		internal const string PluginVersion = "1.1.3";

		private const float DefaultRegularMailRewardMultiplier = 2f;
		private const float DefaultRegularMailReputationMultiplier = 4.5f;

		internal static ConfigEntry<float> RegularMailRewardMultiplier { get; private set; }

		internal static ConfigEntry<float> RegularMailReputationMultiplier { get; private set; }

		private static readonly float[] RegularMailRewardOptions =
		{
			2f,
			3f,
			4f,
			6f,
			8f
		};

		private static readonly float[] RegularMailReputationOptions =
		{
			3f,
			3.75f,
			4.5f,
			6f
		};

		private Harmony harmony;

		private void Awake()
		{
			BindConfiguration();
			harmony = new Harmony(PluginGuid);
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			NandTweaksCompatibility.Apply(harmony);
			Logger.LogInfo("Postal Expansion loaded.");
		}

		private void BindConfiguration()
		{
			var rewardAttributes = new ConfigurationManagerAttributes
			{
				CustomDrawer = DrawRegularMailRewardButtons,
				DefaultValue = DefaultRegularMailRewardMultiplier
			};
			RegularMailRewardMultiplier = Config.Bind(
				"Regular Mail",
				"Gold Reward Multiplier",
				DefaultRegularMailRewardMultiplier,
				new ConfigDescription(
					"Gold reward multiplier for newly generated regular mail missions. 4f is the vanilla value.",
					new AcceptableValueList<float>(RegularMailRewardOptions),
					new object[] { rewardAttributes }));

			var reputationAttributes = new ConfigurationManagerAttributes
			{
				CustomDrawer = DrawRegularMailReputationButtons,
				DefaultValue = DefaultRegularMailReputationMultiplier
			};
			RegularMailReputationMultiplier = Config.Bind(
				"Regular Mail",
				"Reputation Reward Multiplier",
				DefaultRegularMailReputationMultiplier,
				new ConfigDescription(
					"Reputation reward multiplier for regular mail deliveries only. 3f is the vanilla value.",
					new AcceptableValueList<float>(RegularMailReputationOptions),
					new object[] { reputationAttributes }));
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.F4))
			{
				LetterMissionDebug.ShowSpawnLocations();
			}
		}

		private static void DrawRegularMailRewardButtons(ConfigEntryBase setting)
		{
			DrawFloatButtons(setting, RegularMailRewardOptions);
		}

		private static void DrawRegularMailReputationButtons(ConfigEntryBase setting)
		{
			DrawFloatButtons(setting, RegularMailReputationOptions);
		}

		private static void DrawFloatButtons(ConfigEntryBase setting, float[] options)
		{
			float currentValue = (float)setting.BoxedValue;
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			foreach (float option in options)
			{
				bool selected = Mathf.Approximately(currentValue, option);
				if (GUILayout.Toggle(
					selected,
					string.Format("{0:0.##}f", option),
					GUI.skin.button,
					GUILayout.ExpandWidth(true)) && !selected)
				{
					setting.BoxedValue = option;
				}
			}
			GUILayout.EndHorizontal();
		}

		private void OnDestroy()
		{
			LetterDialogueTemplate.Dispose();
			harmony?.UnpatchSelf();
		}
	}

	internal sealed class ConfigurationManagerAttributes
	{
		public Action<ConfigEntryBase> CustomDrawer;

		public object DefaultValue;
	}

	internal static class SailwindPortIndex
	{
		internal const int GoldRockCity = 0;
		internal const int AlNilem = 1;
		internal const int Neverdin = 2;
		internal const int AlbacoreTown = 3;
		internal const int AlchemistsIsland = 4;
		internal const int AlAnkhAcademy = 5;
		internal const int Oasis = 6;
		internal const int TestPort = 7;
		internal const int DragonCliffs = 9;
		internal const int Sanctuary = 10;
		internal const int CrabBeach = 11;
		internal const int NewPort = 12;
		internal const int SageHills = 13;
		internal const int SerpentIsle = 14;
		internal const int FortAestrin = 15;
		internal const int Sunspire = 16;
		internal const int MountMalefic = 17;
		internal const int SirenSong = 18;
		internal const int Eastwind = 19;
		internal const int HappyBay = 20;
		internal const int Chronos = 21;
		internal const int KiciaBay = 22;
		internal const int FireFishTown = 23;
		internal const int Onna = 24;
		internal const int Senna = 25;
		internal const int AestraAbbey = 26;
		internal const int FeyValley = 27;
		internal const int FireflyGrotto = 28;
		internal const int TurtleIsland = 29;
		internal const int DeadCove = 30;
		internal const int OldAnkhTown = 31;
		internal const int MirageMountain = 32;
		internal const int SaffronIsland = 33;

		// Added by the Better Ports mod (its object "port 67 Bottleneck"); absent without it.
		internal const int BetterPortsBottleneck = 67;
	}

	internal static class PostalPrefabRegistration
	{
		internal static bool IsRegistered(PrefabsDirectory directory, int prefabIndex, GameObject prefab)
		{
			return directory != null &&
				directory.directory != null &&
				directory.shipItems != null &&
				prefab != null &&
				directory.directory.Length > prefabIndex &&
				directory.shipItems.Length > prefabIndex &&
				directory.directory[prefabIndex] == prefab &&
				directory.shipItems[prefabIndex] == prefab.GetComponent<ShipItem>();
		}

		internal static bool IsSlotOccupiedByAnotherPrefab(
			PrefabsDirectory directory,
			int prefabIndex,
			GameObject expectedPrefab)
		{
			if (directory == null)
			{
				return false;
			}

			if (directory.directory != null &&
				directory.directory.Length > prefabIndex &&
				directory.directory[prefabIndex] != null &&
				directory.directory[prefabIndex] != expectedPrefab)
			{
				return true;
			}

			ShipItem expectedShipItem = expectedPrefab != null
				? expectedPrefab.GetComponent<ShipItem>()
				: null;
			return directory.shipItems != null &&
				directory.shipItems.Length > prefabIndex &&
				directory.shipItems[prefabIndex] != null &&
				directory.shipItems[prefabIndex] != expectedShipItem;
		}

		internal static bool EnsureCapacity(PrefabsDirectory directory, int prefabIndex)
		{
			if (directory == null || directory.directory == null)
			{
				return false;
			}

			int requiredSize = prefabIndex + 1;
			if (directory.directory.Length < requiredSize)
			{
				Array.Resize(ref directory.directory, requiredSize);
			}

			if (directory.shipItems == null)
			{
				directory.shipItems = new ShipItem[directory.directory.Length];
			}
			else if (directory.shipItems.Length < requiredSize)
			{
				Array.Resize(ref directory.shipItems, requiredSize);
			}

			return true;
		}

		internal static bool RegisterShipItem(PrefabsDirectory directory, int prefabIndex, GameObject prefab)
		{
			if (directory == null ||
				directory.shipItems == null ||
				directory.shipItems.Length <= prefabIndex ||
				prefab == null)
			{
				return false;
			}

			ShipItem shipItem = prefab.GetComponent<ShipItem>();
			if (shipItem == null)
			{
				return false;
			}

			directory.shipItems[prefabIndex] = shipItem;
			return true;
		}
	}

	[HarmonyPatch(typeof(PrefabsDirectory), "Start")]
	internal static class PrefabsDirectoryStartPatch
	{
		private static void Postfix(PrefabsDirectory __instance)
		{
			Letter.EnsureRegistered(__instance);
			PostalExpressMail.EnsureRegistered(__instance);
		}
	}

	internal sealed class PostalSealVisual
	{
		private readonly string objectName;
		private readonly string textureFileName;
		private readonly string resourceName;
		private readonly float size;

		private Texture2D texture;
		private Material material;
		private Mesh mesh;

		internal PostalSealVisual(
			string objectName,
			string textureFileName,
			string resourceName,
			float size)
		{
			this.objectName = objectName;
			this.textureFileName = textureFileName;
			this.resourceName = resourceName;
			this.size = size;
		}

		internal void Ensure(GameObject prefab, Vector3 localPosition, Quaternion localRotation)
		{
			if (prefab == null)
			{
				return;
			}

			MeshRenderer parentRenderer = prefab.GetComponent<MeshRenderer>();
			Material sealMaterial = GetMaterial(parentRenderer);
			if (sealMaterial == null)
			{
				return;
			}

			Transform sealTransform = prefab.transform.Find(objectName);
			if (sealTransform == null)
			{
				sealTransform = new GameObject(objectName).transform;
				sealTransform.SetParent(prefab.transform, false);
			}

			sealTransform.localPosition = localPosition;
			sealTransform.localRotation = localRotation;
			sealTransform.localScale = Vector3.one;

			GameObject sealObject = sealTransform.gameObject;
			sealObject.SetActive(true);
			sealObject.layer = prefab.layer;
			MeshFilter meshFilter = sealObject.GetComponent<MeshFilter>() ??
				sealObject.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = GetMesh();

			MeshRenderer renderer = sealObject.GetComponent<MeshRenderer>() ??
				sealObject.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = sealMaterial;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = true;
			if (parentRenderer != null)
			{
				renderer.lightProbeUsage = parentRenderer.lightProbeUsage;
				renderer.reflectionProbeUsage = parentRenderer.reflectionProbeUsage;
				renderer.renderingLayerMask = parentRenderer.renderingLayerMask;
			}
		}

		private Material GetMaterial(MeshRenderer parentRenderer)
		{
			Texture2D sealTexture = LoadTexture();
			if (sealTexture == null)
			{
				return null;
			}

			if (material != null)
			{
				return material;
			}

			Material parentMaterial = parentRenderer?.sharedMaterial;
			if (parentMaterial == null || parentMaterial.shader == null)
			{
				Debug.LogWarning(
					$"Postal Expansion: no source material was found for the {resourceName}.");
				return null;
			}

			material = new Material(parentMaterial)
			{
				name = resourceName + "_material",
				mainTexture = sealTexture,
				color = Color.white,
				renderQueue = (int)RenderQueue.AlphaTest
			};
			ConfigureLitCutoutMaterial(material);
			return material;
		}

		private static void ConfigureLitCutoutMaterial(Material targetMaterial)
		{
			targetMaterial.SetOverrideTag("RenderType", "TransparentCutout");
			if (targetMaterial.HasProperty("_Mode"))
			{
				targetMaterial.SetFloat("_Mode", 1f);
			}
			if (targetMaterial.HasProperty("_Cutoff"))
			{
				targetMaterial.SetFloat("_Cutoff", 0.1f);
			}
			if (targetMaterial.HasProperty("_SrcBlend"))
			{
				targetMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
			}
			if (targetMaterial.HasProperty("_DstBlend"))
			{
				targetMaterial.SetInt("_DstBlend", (int)BlendMode.Zero);
			}
			if (targetMaterial.HasProperty("_ZWrite"))
			{
				targetMaterial.SetInt("_ZWrite", 1);
			}
			if (targetMaterial.HasProperty("_Cull"))
			{
				targetMaterial.SetInt("_Cull", (int)CullMode.Back);
			}
			if (targetMaterial.HasProperty("_Glossiness"))
			{
				targetMaterial.SetFloat("_Glossiness", 0f);
			}
			if (targetMaterial.HasProperty("_Metallic"))
			{
				targetMaterial.SetFloat("_Metallic", 0f);
			}
			if (targetMaterial.HasProperty("_SpecularHighlights"))
			{
				targetMaterial.SetFloat("_SpecularHighlights", 0f);
			}
			if (targetMaterial.HasProperty("_GlossyReflections"))
			{
				targetMaterial.SetFloat("_GlossyReflections", 0f);
			}
			if (targetMaterial.HasProperty("_EmissionColor"))
			{
				targetMaterial.SetColor("_EmissionColor", Color.black);
			}

			targetMaterial.EnableKeyword("_ALPHATEST_ON");
			targetMaterial.DisableKeyword("_ALPHABLEND_ON");
			targetMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			targetMaterial.DisableKeyword("_EMISSION");
		}

		private Texture2D LoadTexture()
		{
			if (texture != null)
			{
				return texture;
			}

			string texturePath = GetPostalAssetPath(textureFileName);
			if (texturePath == null)
			{
				Debug.LogWarning(
					$"Postal Expansion: {resourceName} texture was not found.");
				return null;
			}

			byte[] imageData;
			try
			{
				imageData = File.ReadAllBytes(texturePath);
			}
			catch (IOException exception)
			{
				Debug.LogWarning(
					$"Postal Expansion: {resourceName} texture could not be read: " +
					exception.Message);
				return null;
			}
			catch (UnauthorizedAccessException exception)
			{
				Debug.LogWarning(
					$"Postal Expansion: {resourceName} texture could not be read: " +
					exception.Message);
				return null;
			}

			var loadedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			if (!loadedTexture.LoadImage(imageData))
			{
				Debug.LogWarning(
					$"Postal Expansion: {resourceName} texture could not be loaded.");
				Object.Destroy(loadedTexture);
				return null;
			}

			loadedTexture.name = resourceName + "_texture";
			loadedTexture.filterMode = FilterMode.Bilinear;
			loadedTexture.wrapMode = TextureWrapMode.Clamp;
			loadedTexture.anisoLevel = 8;
			texture = loadedTexture;
			return texture;
		}

		private Mesh GetMesh()
		{
			if (mesh != null)
			{
				return mesh;
			}

			float halfSize = size / 2f;
			mesh = new Mesh
			{
				name = resourceName + "_mesh",
				vertices = new[]
				{
					new Vector3(-halfSize, -halfSize, 0f),
					new Vector3(-halfSize, halfSize, 0f),
					new Vector3(halfSize, halfSize, 0f),
					new Vector3(halfSize, -halfSize, 0f),
					new Vector3(-halfSize, -halfSize, 0f),
					new Vector3(-halfSize, halfSize, 0f),
					new Vector3(halfSize, halfSize, 0f),
					new Vector3(halfSize, -halfSize, 0f)
				},
				uv = new[]
				{
					new Vector2(0f, 0f),
					new Vector2(0f, 1f),
					new Vector2(1f, 1f),
					new Vector2(1f, 0f),
					new Vector2(0f, 0f),
					new Vector2(0f, 1f),
					new Vector2(1f, 1f),
					new Vector2(1f, 0f)
				},
				normals = new[]
				{
					Vector3.back,
					Vector3.back,
					Vector3.back,
					Vector3.back,
					Vector3.forward,
					Vector3.forward,
					Vector3.forward,
					Vector3.forward
				},
				triangles = new[]
				{
					0, 1, 2,
					0, 2, 3,
					6, 5, 4,
					7, 6, 4
				}
			};
			mesh.RecalculateBounds();
			return mesh;
		}

		private static string GetPostalAssetPath(string fileName)
		{
			string assemblyDirectory =
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string[] candidatePaths =
			{
				Path.Combine(assemblyDirectory ?? string.Empty, "assets", fileName),
				Path.Combine(Paths.PluginPath, "PostalExpansion", "assets", fileName)
			};

			foreach (string candidatePath in candidatePaths)
			{
				if (File.Exists(candidatePath))
				{
					return candidatePath;
				}
			}

			return null;
		}
	}

	internal sealed class PostalMailOutlineSanitizer : MonoBehaviour
	{
		internal static void Attach(GameObject prefab)
		{
			if (prefab != null && prefab.GetComponent<PostalMailOutlineSanitizer>() == null)
			{
				prefab.AddComponent<PostalMailOutlineSanitizer>();
			}
		}

		private void Awake()
		{
			// Runtime directory templates already contain the Outline added by the
			// donor's GoPointerButton.Start. Remove copied outlines before this
			// clone's own Start creates the one GoPointerButton controls.
			foreach (Outline outline in GetComponents<Outline>())
			{
				Object.DestroyImmediate(outline);
			}
		}
	}

	[HarmonyPatch(typeof(ShipItem), "AddLODGroup")]
	internal static class PostalShipItemLodPatch
	{
		private static bool Prefix(ShipItem __instance)
		{
			SaveablePrefab saveable = __instance?.GetComponent<SaveablePrefab>();
			if (saveable == null ||
				!PostalMail.IsMailPrefab(saveable) ||
				saveable.prefabIndex == PostalMail.RegularMailItemIndex)
			{
				return true;
			}

			LODGroup template = RefsDirectory.instance?.LODtemplateItems;
			LOD[] lods = template?.GetLODs();
			if (lods == null || lods.Length == 0)
			{
				return true;
			}

			LODGroup itemLodGroup = __instance.GetComponent<LODGroup>() ??
				__instance.gameObject.AddComponent<LODGroup>();
			LOD[] configuredLods = (LOD[])lods.Clone();
			configuredLods[0].renderers =
				__instance.GetComponentsInChildren<Renderer>(true);
			itemLodGroup.SetLODs(configuredLods);
			return false;
		}
	}

	internal enum PostalMissionTab
	{
		Local,
		World,
		Mail
	}

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

		// Set while the office list shows the player's own missions (Mission Rework's "Active" view
		// passes PlayerMissions.missions); that list and its page count are then left as they are.
		private static bool showingPlayerMissions;

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
			showingPlayerMissions =
				missions != null && missions == PlayerMissions.missions;
			if (ui != null && dude != null && !showingPlayerMissions)
			{
				missions = GetMissions(ui, dude.GetPort(), PostalMissionTab.Local, 0);
			}
		}

		internal static void RefreshPageCount(MissionListUI ui)
		{
			if (showingPlayerMissions ||
				!HasMissionListPagingReflection() ||
				ui == null ||
				GetCurrentPortDude(ui) == null)
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
				BottleneckMail.AddMissions(port, missions);
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

	internal sealed class PostalMailTabButton : GPButtonMissionListWorld
	{
		public override void Start()
		{
			Component clonedOutline = GetComponent("Outline");
			if (clonedOutline != null)
			{
				if (clonedOutline is Behaviour outlineBehaviour)
				{
					outlineBehaviour.enabled = false;
				}
				Object.Destroy(clonedOutline);
			}

			base.Start();
		}

		public override void OnActivate()
		{
			PostalMissionUi.SetMailMissions(true);
			MissionListUI.instance.ResetPage();
		}
	}

	[HarmonyPatch(typeof(MissionListUI), "EnablePortMissionUI")]
	internal static class MissionListUiEnablePortPatch
	{
		private static void Prefix(MissionListUI __instance, ref Mission[] missions, PortDude dude)
		{
			PostalMissionUi.FilterInitialPortMissions(__instance, dude, ref missions);
		}

		private static void Postfix(MissionListUI __instance)
		{
			PostalMissionUi.EnsureMailButton(__instance);
			PostalMissionUi.RefreshPageCount(__instance);
		}
	}

	[HarmonyPatch(typeof(MissionListUI), "SetWorldMissions")]
	internal static class MissionListUiSetWorldPatch
	{
		private static void Postfix(MissionListUI __instance)
		{
			if (GameState.inPortMissionList)
			{
				PostalMissionUi.SetMailMissions(false);
			}
		}
	}

	[HarmonyPatch(typeof(MissionListUI), "ResetPage")]
	internal static class MissionListUiResetPagePatch
	{
		private static bool Prefix(MissionListUI __instance)
		{
			return !PostalMissionUi.TryResetPage(__instance);
		}
	}

	[HarmonyPatch(typeof(MissionListUI), "ChangePage")]
	internal static class MissionListUiChangePagePatch
	{
		private static bool Prefix(MissionListUI __instance, int pageChange)
		{
			return !PostalMissionUi.TryChangePage(__instance, pageChange);
		}
	}

	[HarmonyPatch(typeof(MissionListUI), "DisplayMissions")]
	internal static class MissionListUiDisplayMissionsPatch
	{
		private static void Postfix(MissionListUI __instance)
		{
			PostalMissionUi.UpdateMissionHighlights(__instance);
		}
	}

	[HarmonyPatch(typeof(MissionDetailsUI), "ClickButton")]
	internal static class MissionDetailsUiClickButtonPatch
	{
		private static bool Prefix(MissionDetailsUI __instance)
		{
			return !PostalMissionUi.TryHandlePortMissionAcceptance(__instance);
		}
	}

	// Shows the mission's reputation reward under its gold reward on every mission page, as
	// EconomyOverhaul's approved mission page does: the gold at 0.8 size, "N reputation" as big
	// under it. The value is the game's own per-unit reputation (GetDeliveryRep, with this mod's
	// mail rules and any other mod's patches applied) times the cargo count, as of today, so a
	// late mission shows its reduced reward or penalty.
	// EconomyOverhaul's own mission page (its MissionPage class, from its mission rework) lays out
	// the reward itself, the reputation of every mission included, so with it this patch does nothing.
	[HarmonyPatch(typeof(MissionDetailsUI), "InsertMissionData")]
	internal static class MissionReputationRewardPatch
	{
		internal const string ReputationObjectName = "postal_expansion_reputation_reward";

		private const float GoldSize = 0.8f;
		// In the page's own frame; spacing from EconomyOverhaul's approved mockup (gold at -.170,
		// reputation at -.252). The raises keep the line clear of the Accept button.
		private const float ReputationSpacing = 0.082f;
		private const float GoldRaise = 0.021f;
		private const float RewardLabelRaise = 0.032f;

		private static readonly FieldInfo TotalGoldTextField =
			AccessTools.Field(typeof(MissionDetailsUI), "totalGold");
		private static readonly FieldInfo PageRootField =
			AccessTools.Field(typeof(MissionDetailsUI), "UI");
		private static readonly MethodInfo DeliveryRepMethod =
			AccessTools.Method(typeof(Mission), "GetDeliveryRep");
		private static readonly Func<Mission, int> GetDeliveryRep =
			DeliveryRepMethod != null
				? AccessTools.MethodDelegate<Func<Mission, int>>(DeliveryRepMethod)
				: null;

		// Looked up at the first page shown, when every plugin is loaded.
		private static bool? economyOverhaulPage;

		private static void Postfix(MissionDetailsUI __instance, Mission mission)
		{
			if (__instance == null ||
				mission == null ||
				TotalGoldTextField == null ||
				GetDeliveryRep == null)
			{
				return;
			}

			if (economyOverhaulPage == null)
			{
				economyOverhaulPage = HasEconomyOverhaulPage();
			}

			if (economyOverhaulPage.Value)
			{
				return;
			}

			var totalGoldText = TotalGoldTextField.GetValue(__instance) as TextMesh;
			if (totalGoldText == null || totalGoldText.transform.parent == null)
			{
				return;
			}

			TextMesh reputationText = FindText(
				totalGoldText.transform.parent,
				ReputationObjectName) ?? CreateReputationText(__instance, totalGoldText);
			if (reputationText == null)
			{
				return;
			}

			int total = GetDeliveryRep(mission) * mission.goodCount;
			reputationText.text = total + " reputation";
			reputationText.gameObject.SetActive(true);
		}

		// Runs once per page: shrinks and raises the gold, raises "Reward:", then clones the gold
		// for the reputation line, so its font, colour and anchor are the gold's.
		private static TextMesh CreateReputationText(
			MissionDetailsUI page,
			TextMesh totalGoldText)
		{
			totalGoldText.characterSize *= GoldSize;
			Raise(totalGoldText.transform, GoldRaise);

			var pageRoot = PageRootField?.GetValue(page) as GameObject;
			TextMesh rewardLabel = FindTextByContent(pageRoot, "Reward:");
			if (rewardLabel != null)
			{
				Raise(rewardLabel.transform, RewardLabelRaise);
			}
			else
			{
				Debug.LogWarning(
					"Postal Expansion: the mission page's Reward label was not found.");
			}

			GameObject clone = Object.Instantiate(
				totalGoldText.gameObject,
				totalGoldText.transform.parent);
			clone.name = ReputationObjectName;
			Raise(clone.transform, -ReputationSpacing);
			return clone.GetComponent<TextMesh>();
		}

		// Without a log line when the type is absent (AccessTools.TypeByName warns).
		private static bool HasEconomyOverhaulPage()
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.GetName().Name == "EconomyOverhaul" &&
					assembly.GetType("EconomyOverhaul.MissionPage", false) != null)
				{
					return true;
				}
			}

			return false;
		}

		private static void Raise(Transform target, float amount)
		{
			Vector3 position = target.localPosition;
			position.y += amount;
			target.localPosition = position;
		}

		private static TextMesh FindText(Transform root, string objectName)
		{
			Transform child = root.Find(objectName);
			return child != null ? child.GetComponent<TextMesh>() : null;
		}

		private static TextMesh FindTextByContent(GameObject root, string content)
		{
			if (root == null)
			{
				return null;
			}

			foreach (TextMesh candidate in root.GetComponentsInChildren<TextMesh>(true))
			{
				if (candidate != null &&
					string.Equals(
						(candidate.text ?? string.Empty).Trim(),
						content,
						StringComparison.Ordinal))
				{
					return candidate;
				}
			}

			return null;
		}
	}

	[HarmonyPatch(typeof(PlayerMissions), "AcceptMission")]
	internal static class PostalMissionAcceptancePatch
	{
		private static void Postfix(Mission mission)
		{
			UrgentExpressMail.MissionAccepted(mission);
			GoldenDeliveryMissions.MissionAccepted(mission);
			RegisteredLetterMissions.MissionAccepted(mission);
			AnonymousLetterMissions.MissionAccepted(mission);
		}
	}

	[HarmonyPatch(typeof(SaveLoadManager), "SaveModData")]
	internal static class PostalMissionSavePatch
	{
		private static void Prefix()
		{
			UrgentExpressMail.SavePersistentState();
			GoldenDeliveryMissions.SavePersistentState();
			RegisteredLetterMissions.SavePersistentState();
			AnonymousLetterMissions.SavePersistentState();
			SpecialMailHistory.SavePersistentState();
		}
	}

	[HarmonyPatch(typeof(SaveLoadManager), "LoadModData")]
	internal static class PostalMissionLoadPatch
	{
		private static void Postfix()
		{
			UrgentExpressMail.LoadPersistentState();
			GoldenDeliveryMissions.LoadPersistentState();
			RegisteredLetterMissions.LoadPersistentState();
			AnonymousLetterMissions.LoadPersistentState();
			SpecialMailHistory.LoadPersistentState();
			LetterMissionDebug.Reset();
		}
	}

	[HarmonyPatch(typeof(StartMenu), "StartNewGame")]
	internal static class PostalMissionNewGamePatch
	{
		private static void Prefix()
		{
			UrgentExpressMail.ResetRuntimeState();
			GoldenDeliveryMissions.ResetRuntimeState();
			RegisteredLetterMissions.ResetRuntimeState();
			AnonymousLetterMissions.ResetRuntimeState();
			SpecialMailHistory.ResetRuntimeState();
			LetterMissionDebug.Reset();
		}
	}

	[HarmonyPatch(typeof(SaveLoadManager), "LoadGame")]
	internal static class SaveLoadManagerLoadGamePatch
	{
		private static void Prefix()
		{
			PrefabsDirectory directory = PrefabsDirectory.instance;
			Letter.EnsureRegistered(directory);
			PostalExpressMail.EnsureRegistered(directory);
		}
	}

	internal enum SpecialMailKind
	{
		None = 0,
		AnonymousLetter = 1,
		UrgentExpressMail = 2,
		GoldenDelivery = 3
	}

	internal static class SpecialMailDisplay
	{
		internal static SpecialMailKind CurrentDeliveryKind { get; private set; }

		internal static SpecialMailKind GetKind(Mission mission)
		{
			if (GoldenDeliveryMissions.IsGolden(mission))
			{
				return SpecialMailKind.GoldenDelivery;
			}
			if (UrgentExpressMail.IsUrgent(mission))
			{
				return SpecialMailKind.UrgentExpressMail;
			}
			if (AnonymousLetterMissions.TryGetDefinition(mission, out _))
			{
				return SpecialMailKind.AnonymousLetter;
			}

			return SpecialMailKind.None;
		}

		internal static string GetDisplayName(SpecialMailKind kind)
		{
			switch (kind)
			{
				case SpecialMailKind.AnonymousLetter:
					return "Anonymous Letter";
				case SpecialMailKind.UrgentExpressMail:
					return "Urgent Express Mail";
				case SpecialMailKind.GoldenDelivery:
					return GoldenDeliveryMissions.DisplayName;
				default:
					return string.Empty;
			}
		}

		internal static void BeginDelivery(Mission mission)
		{
			CurrentDeliveryKind = GetKind(mission);
		}

		internal static void EndDelivery()
		{
			CurrentDeliveryKind = SpecialMailKind.None;
		}
	}

	[HarmonyPatch(typeof(Mission), nameof(Mission.DeliverGood))]
	internal static class SpecialMailDeliveryContextPatch
	{
		private static void Prefix(Mission __instance)
		{
			SpecialMailDisplay.BeginDelivery(__instance);
		}

		private static Exception Finalizer(Exception __exception)
		{
			SpecialMailDisplay.EndDelivery();
			return __exception;
		}
	}

	[HarmonyPatch(
		typeof(NotificationUi),
		nameof(NotificationUi.ShowNotification),
		new[] { typeof(string) })]
	internal static class SpecialMailDeliveryNotificationPatch
	{
		private static void Prefix(ref string notification)
		{
			SpecialMailKind kind = SpecialMailDisplay.CurrentDeliveryKind;
			if (kind == SpecialMailKind.None ||
				string.IsNullOrEmpty(notification) ||
				!notification.StartsWith("Delivered ", StringComparison.Ordinal))
			{
				return;
			}

			int lineBreak = notification.IndexOf('\n');
			string suffix = lineBreak >= 0
				? notification.Substring(lineBreak)
				: string.Empty;
			notification =
				"Delivered " + SpecialMailDisplay.GetDisplayName(kind) + suffix;
		}
	}

	[HarmonyPatch(typeof(MissionDetailsUI), "InsertMissionData")]
	internal static class SpecialMailMissionDetailsPatch
	{
		private static readonly FieldInfo CargoNameField =
			AccessTools.Field(typeof(MissionDetailsUI), "cargoName");

		private static void Postfix(MissionDetailsUI __instance, Mission mission)
		{
			SpecialMailKind kind = SpecialMailDisplay.GetKind(mission);
			if (kind == SpecialMailKind.None)
			{
				return;
			}

			var cargoName = CargoNameField?.GetValue(__instance) as TextMesh;
			if (cargoName != null)
			{
				cargoName.text = SpecialMailDisplay.GetDisplayName(kind);
			}
		}
	}

	[HarmonyPatch(typeof(ShipItem), nameof(ShipItem.UpdateLookText))]
	internal static class SpecialMailItemLookTextPatch
	{
		private static void Postfix(ShipItem __instance)
		{
			Good good = __instance?.GetComponent<Good>();
			int missionIndex = good != null ? good.GetMissionIndex() : -1;
			if (missionIndex < 0 ||
				PlayerMissions.missions == null ||
				missionIndex >= PlayerMissions.missions.Length)
			{
				return;
			}

			Mission mission = PlayerMissions.missions[missionIndex];
			SpecialMailKind kind = SpecialMailDisplay.GetKind(mission);
			if (kind != SpecialMailKind.GoldenDelivery &&
				kind != SpecialMailKind.UrgentExpressMail)
			{
				return;
			}

			__instance.lookText =
				SpecialMailDisplay.GetDisplayName(kind) +
				"\nto " + mission.destinationPort.GetPortName() +
				"\ndue: " + mission.GetDueText();
		}
	}

	internal static class SpecialMailHistory
	{
		private const string SaveKey =
			"PostalExpansion.SpecialMail.History.v1";

		private static ConditionalWeakTable<LoggedMission, Marker> historyKinds =
			new ConditionalWeakTable<LoggedMission, Marker>();

		internal static bool TryGetKind(
			LoggedMission mission,
			out SpecialMailKind kind)
		{
			if (mission != null && historyKinds.TryGetValue(mission, out Marker marker))
			{
				kind = marker.Kind;
				return true;
			}

			kind = SpecialMailKind.None;
			return false;
		}

		internal static void SavePersistentState()
		{
			if (GameState.modData == null)
			{
				GameState.modData = new Dictionary<string, string>();
			}

			var entries = new List<string>();
			LoggedMission[] missions = MissionLog.instance?.loggedMissions;
			if (missions != null)
			{
				for (int i = 0; i < missions.Length; i++)
				{
					LoggedMission mission = missions[i];
					if (TryGetKind(mission, out SpecialMailKind kind))
					{
						entries.Add(string.Join(",",
							i,
							(int)kind,
							mission.day,
							mission.goodIndex));
					}
				}
			}

			GameState.modData[SaveKey] = string.Join(";", entries);
		}

		internal static void LoadPersistentState()
		{
			ResetRuntimeState();
			LoggedMission[] missions = MissionLog.instance?.loggedMissions;
			if (missions == null ||
				GameState.modData == null ||
				!GameState.modData.TryGetValue(SaveKey, out string data))
			{
				return;
			}

			foreach (string entry in data.Split(
				new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string[] fields = entry.Split(',');
				if (fields.Length != 4 ||
					!int.TryParse(fields[0], out int slot) ||
					!int.TryParse(fields[1], out int kindValue) ||
					!int.TryParse(fields[2], out int day) ||
					!int.TryParse(fields[3], out int goodIndex) ||
					slot < 0 ||
					slot >= missions.Length ||
					!Enum.IsDefined(typeof(SpecialMailKind), kindValue))
				{
					continue;
				}

				LoggedMission mission = missions[slot];
				var kind = (SpecialMailKind)kindValue;
				if (kind != SpecialMailKind.None &&
					mission != null &&
					mission.day == day &&
					mission.goodIndex == goodIndex)
				{
					Mark(mission, kind);
				}
			}

			MissionLog.instance.UpdateTexts();
		}

		internal static void ResetRuntimeState()
		{
			historyKinds = new ConditionalWeakTable<LoggedMission, Marker>();
		}

		internal static void Mark(LoggedMission mission, SpecialMailKind kind)
		{
			if (mission == null || kind == SpecialMailKind.None)
			{
				return;
			}

			historyKinds.Remove(mission);
			historyKinds.Add(mission, new Marker(kind));
		}

		private sealed class Marker
		{
			internal Marker(SpecialMailKind kind)
			{
				Kind = kind;
			}

			internal SpecialMailKind Kind { get; }
		}
	}

	[HarmonyPatch(typeof(MissionLog), nameof(MissionLog.AddToLog))]
	internal static class SpecialMailHistoryAddPatch
	{
		private const string TemporaryDestinationPrefix =
			"\u001FPostalExpansion:";

		private static void Prefix(
			MissionLog __instance,
			int goodIndex,
			ref string destinationName,
			out AddState __state)
		{
			__state = new AddState(
				SpecialMailDisplay.CurrentDeliveryKind,
				destinationName);
			LoggedMission top =
				__instance.loggedMissions != null && __instance.loggedMissions.Length > 0
					? __instance.loggedMissions[0]
					: null;
			if (top == null ||
				top.day != GameState.day ||
				top.goodIndex != goodIndex ||
				top.destinationName != destinationName)
			{
				return;
			}

			SpecialMailKind topKind = SpecialMailKind.None;
			SpecialMailHistory.TryGetKind(top, out topKind);
			if (topKind != __state.Kind)
			{
				destinationName =
					TemporaryDestinationPrefix + (int)__state.Kind + ":" + destinationName;
				__state.ForceSeparateEntry = true;
			}
		}

		private static void Postfix(MissionLog __instance, AddState __state)
		{
			LoggedMission top =
				__instance.loggedMissions != null && __instance.loggedMissions.Length > 0
					? __instance.loggedMissions[0]
					: null;
			if (top == null)
			{
				return;
			}

			if (__state.ForceSeparateEntry)
			{
				top.destinationName = __state.DestinationName;
			}
			if (__state.Kind != SpecialMailKind.None)
			{
				SpecialMailHistory.Mark(top, __state.Kind);
			}

			if (__state.ForceSeparateEntry || __state.Kind != SpecialMailKind.None)
			{
				__instance.UpdateTexts();
			}
		}

		internal struct AddState
		{
			internal AddState(SpecialMailKind kind, string destinationName)
			{
				Kind = kind;
				DestinationName = destinationName;
				ForceSeparateEntry = false;
			}

			internal SpecialMailKind Kind;
			internal string DestinationName;
			internal bool ForceSeparateEntry;
		}
	}

	[HarmonyPatch(typeof(MissionLog), nameof(MissionLog.UpdateTexts))]
	internal static class SpecialMailHistoryTextPatch
	{
		private static void Postfix(MissionLog __instance)
		{
			if (__instance.loggedMissions == null || __instance.texts == null)
			{
				return;
			}

			int count = Math.Min(
				__instance.loggedMissions.Length,
				__instance.texts.Length);
			for (int i = 0; i < count; i++)
			{
				LoggedMission mission = __instance.loggedMissions[i];
				if (!SpecialMailHistory.TryGetKind(
						mission,
						out SpecialMailKind kind))
				{
					continue;
				}

				__instance.texts[i].text =
					"day " + mission.day + ": " +
					SpecialMailDisplay.GetDisplayName(kind) + " to " +
					mission.destinationName + " (" +
					mission.deliveredGoodCount + ")";
			}
		}
	}
}
