using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PostalExpansion
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	[BepInDependency(NandTweaksCompatibility.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(ModdedPortMail.TamarackGuid, BepInDependency.DependencyFlags.SoftDependency)]
	public class Plugin : BaseUnityPlugin
	{
		internal const string PluginGuid = "com.DogEggz.postalexpansion";
		internal const string PluginName = "Postal Expansion";
		internal const string PluginVersion = "1.1.5";

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
			ModdedPortMail.TamarackLoaded =
				BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ModdedPortMail.TamarackGuid);
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
		private static Transform templateHolder;

		// Clones the donor under an inactive holder: the template never runs as an item, and the
		// items made from it start active.
		internal static GameObject CreateTemplate(GameObject donor, string name)
		{
			if (templateHolder == null)
			{
				var holder = new GameObject("postal_expansion_item_templates");
				holder.SetActive(false);
				Object.DontDestroyOnLoad(holder);
				templateHolder = holder.transform;
			}

			GameObject template = Object.Instantiate(donor, templateHolder, false);
			template.name = name;
			return template;
		}

		internal static GameObject GetRegularMail()
		{
			GameObject[] directory = PrefabsDirectory.instance != null
				? PrefabsDirectory.instance.directory
				: null;
			return directory != null && directory.Length > PostalMail.RegularMailItemIndex
				? directory[PostalMail.RegularMailItemIndex]
				: null;
		}
	}

	[HarmonyPatch(typeof(PrefabsDirectory), "Start")]
	internal static class PrefabsDirectoryStartPatch
	{
		private static void Postfix()
		{
			Letter.EnsureTemplate();
			PostalExpressMail.EnsureTemplate();
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

	[HarmonyPatch(typeof(ShipItem), "AddLODGroup")]
	internal static class PostalShipItemLodPatch
	{
		private static bool Prefix(ShipItem __instance)
		{
			SaveablePrefab saveable = __instance != null ? __instance.GetComponent<SaveablePrefab>() : null;
			return !(PostalMail.IsExpressMail(saveable) || PostalMail.IsRegisteredLetter(saveable)) ||
				!Apply(__instance);
		}

		// The game's item distance group over every renderer of the item, the seal's too. Also run on a mail item turned into Express
		// Mail or a Registered Letter after the game built its group (an item loaded from a save).
		internal static bool Apply(ShipItem item)
		{
			LODGroup template = RefsDirectory.instance != null ? RefsDirectory.instance.LODtemplateItems : null;
			LOD[] lods = template != null ? template.GetLODs() : null;
			if (lods == null || lods.Length == 0)
			{
				return false;
			}

			LODGroup itemLodGroup = item.GetComponent<LODGroup>();
			if (itemLodGroup == null)
			{
				itemLodGroup = item.gameObject.AddComponent<LODGroup>();
			}

			LOD[] configuredLods = (LOD[])lods.Clone();
			configuredLods[0].renderers = item.GetComponentsInChildren<Renderer>(true);
			itemLodGroup.SetLODs(configuredLods);
			return true;
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
		// Each mission button's material as the game set it. A highlighted button is coloured on its own copy (renderer.material),
		// not by a property block: the game's outline draws the looked-at button with the renderer's property block, whose _Color
		// would replace the outline's colour.
		private static readonly Dictionary<Renderer, Material> MissionButtonBaseMaterials =
			new Dictionary<Renderer, Material>();

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

		internal static bool MailTabShown => mailMissions;

		internal static void SetMailMissions(bool mail)
		{
			mailMissions = mail;
			UpdateTabMaterials(MissionListUI.instance);
			PostalRegionFilter.Refresh();
		}

		// Returns the Local list's size, or -1 when the list is left as it is: the player's own
		// missions (Mission Rework's "Active" view passes PlayerMissions.missions).
		internal static int FilterInitialPortMissions(
			MissionListUI ui,
			PortDude dude,
			ref Mission[] missions)
		{
			if (ui == null ||
				dude == null ||
				(missions != null && missions == PlayerMissions.missions))
			{
				return -1;
			}

			List<Mission> available =
				GetAvailableMissions(dude.GetPort(), PostalMissionTab.Local);
			missions = GetPage(ui, available, 0);
			return available.Count;
		}

		internal static void RefreshPageCount(MissionListUI ui, int missionCount)
		{
			if (missionCount < 0 ||
				!HasMissionListPagingReflection() ||
				ui == null ||
				GetCurrentPortDude(ui) == null)
			{
				return;
			}

			SetPageCount(ui, missionCount);
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

			UISoundPlayer.instance.PlayParchmentSound();
			ShowFirstPage(ui);
			return true;
		}

		private static void ShowFirstPage(MissionListUI ui)
		{
			SetCurrentPage(ui, 0);
			List<Mission> available = GetCurrentTabMissions(ui);
			ui.DisplayMissions(GetPage(ui, available, 0));
			SetPageCount(ui, available.Count);
			UpdatePageCountText(ui);
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
				ui.DisplayMissions(
					GetPage(ui, GetCurrentTabMissions(ui), GetCurrentPage(ui)));
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
			ShowFirstPage(missionListUi);
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
				if (renderer == null)
				{
					continue;
				}

				Material baseMaterial;
				if (!MissionButtonBaseMaterials.TryGetValue(renderer, out baseMaterial))
				{
					baseMaterial = renderer.sharedMaterial;
					if (baseMaterial == null)
					{
						continue;
					}
					MissionButtonBaseMaterials[renderer] = baseMaterial;
				}

				bool active = button.gameObject.activeSelf;
				bool golden = active &&
					GoldenDeliveryMissions.IsGolden(button.mission);
				bool urgent = active &&
					UrgentExpressMail.IsUrgent(button.mission);
				bool highlighted = golden || urgent;
				if (!highlighted && renderer.sharedMaterial == baseMaterial)
				{
					continue;
				}

				Color highlightColor = golden
					? GoldenMissionColor
					: UrgentMissionColor;
				// The renderer's own copy, made on first use and kept.
				Material material = renderer.material;
				SetMissionMaterialColor(
					material,
					baseMaterial,
					"_Color",
					highlighted,
					highlightColor);
				SetMissionMaterialColor(
					material,
					baseMaterial,
					"_BaseColor",
					highlighted,
					highlightColor);
			}
		}

		private static List<Mission> GetCurrentTabMissions(MissionListUI ui)
		{
			return GetAvailableMissions(
				GetCurrentPortDude(ui).GetPort(),
				GetCurrentTab(ui));
		}

		private static Mission[] GetPage(
			MissionListUI ui,
			List<Mission> availableMissions,
			int page)
		{
			var pageMissions = new Mission[GetMissionPageSize(ui)];
			int startIndex = page * pageMissions.Length;
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
				ModdedPortMail.AddMissions(port, missions);
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

			if (tab == PostalMissionTab.Mail)
			{
				PostalRegionFilter.Apply(missions);
				SortByReputation(missions);
			}
			else
			{
				missions.Sort((left, right) => right.pricePerKm.CompareTo(left.pricePerKm));
			}

			return missions;
		}

		// Highest reputation first, ties by gold per mile; each mission's reputation worked out once.
		private static void SortByReputation(List<Mission> missions)
		{
			var reputation = new Dictionary<Mission, int>(missions.Count);
			foreach (Mission mission in missions)
			{
				reputation[mission] = MissionReputationRewardPatch.TotalReputation(mission);
			}

			missions.Sort((left, right) =>
			{
				int order = reputation[right].CompareTo(reputation[left]);
				return order != 0 ? order : right.pricePerKm.CompareTo(left.pricePerKm);
			});
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
			bool mail = PostalMail.IsMailPrefab(PostalMail.SaveableOf(candidate));
			return tab == PostalMissionTab.Mail ? mail : !mail;
		}

		private static void SetPageCount(MissionListUI ui, int missionCount)
		{
			int pageCount = Mathf.Max(
				1,
				Mathf.CeilToInt((float)missionCount / GetMissionPageSize(ui)));
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
			Material baseMaterial,
			string propertyName,
			bool highlighted,
			Color highlightColor)
		{
			if (!baseMaterial.HasProperty(propertyName))
			{
				return;
			}

			Color color = baseMaterial.GetColor(propertyName);
			if (highlighted)
			{
				color.r = highlightColor.r;
				color.g = highlightColor.g;
				color.b = highlightColor.b;
			}
			material.SetColor(propertyName, color);
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

	// The Mail tab's Region dropdown, built as EconomyOverhaul's board builds its own (which then filters every tab, so this one is made
	// only without it): plates copied from the list's back button, left of the list under its page buttons, so they show only while the
	// port's offers do. A click on the head opens the choices under it; a choice filters the Mail tab by the destination's region and goes
	// back to page 1. Kept for the game session, not saved.
	internal static class PostalRegionFilter
	{
		private const string EconomyOverhaulGuid = "DogEggz.EconomyOverhaul";
		private const string SourceButtonName = "mission button (back)";

		// In the page buttons' frame (+x is the screen's left): the list's tabs stand at y 0.19, its rows 0.049 apart. X and Width (a share
		// of the back button's width) only where the room left of the list can't be measured.
		private const float X = 0.40f;
		private const float Y = 0.19f;
		private const float Step = 0.049f;
		private const float Width = 0.62f;
		private const float MinWidth = 0.3f;
		private const float ListGap = 0.016f;
		private const float EdgeMargin = 0.02f;
		private const float Shade = 0.78f;

		private static readonly string[] Names = { "All regions", "Al'Ankh", "Emerald", "Aestrin" };
		private static readonly FieldInfo PageButtonsField =
			AccessTools.Field(typeof(MissionListUI), "pageButtons");
		private static readonly FieldInfo PortWelcomeField =
			AccessTools.Field(typeof(MissionListUI), "portWelcome");
		private static readonly FieldInfo CurrentPortDudeField =
			AccessTools.Field(typeof(MissionListUI), "currentPortDude");

		private static readonly List<Plate> Choices = new List<Plate>();

		private static bool? economyOverhaul;
		private static int region = -1;
		private static bool open;
		// shortcut: once the plates are not found the dropdown is not tried again this session; replace with a retry at the next opening if
		// the list ever lacks them only for a moment.
		private static bool failed;
		private static float builtRoom = float.NaN;
		private static MissionListUI builtFor;
		private static Plate head;
		private static GameObject stash;
		private static Camera uiCamera;

		private sealed class Plate
		{
			internal GameObject Root;
			internal TextMesh Text;
			internal Material Material;
			internal Vector3 TextScale;
			internal float TextWidth;
		}

		internal static void Apply(List<Mission> missions)
		{
			if (region < 0 || HasEconomyOverhaul())
			{
				return;
			}

			missions.RemoveAll(mission => (int)mission.destinationPort.region != region);
		}

		internal static void Opened(MissionListUI ui)
		{
			if (ui == null || failed || HasEconomyOverhaul())
			{
				return;
			}

			float room = Room(ui, out float listLeft);
			if (head != null &&
				(builtFor != ui || head.Root == null || !SameRoom(room, builtRoom)))
			{
				Unmake();
			}

			if (head == null && !Build(ui, room, listLeft))
			{
				Unmake();
				failed = true;
				Debug.LogWarning("Postal Expansion: the mission list's plates were not found; no region dropdown.");
				return;
			}

			open = false;
			Refresh();
		}

		internal static void Refresh()
		{
			if (head == null || head.Root == null)
			{
				return;
			}

			bool shown = PostalMissionUi.MailTabShown;
			head.Root.SetActive(shown);
			SetText(head, "Region: " + Names[region + 1]);
			for (int i = 0; i < Choices.Count; i++)
			{
				Plate choice = Choices[i];
				if (choice.Material.HasProperty("_Color"))
				{
					choice.Material.color = i == region + 1 ? Color.white : new Color(Shade, Shade, Shade, 1f);
				}

				choice.Root.SetActive(shown && open);
			}
		}

		private static bool HasEconomyOverhaul()
		{
			if (economyOverhaul == null)
			{
				economyOverhaul = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(EconomyOverhaulGuid);
			}

			return economyOverhaul.Value;
		}

		private static bool SameRoom(float a, float b)
		{
			return (float.IsNaN(a) && float.IsNaN(b)) || Mathf.Abs(a - b) < 0.002f;
		}

		private static bool Build(MissionListUI ui, float room, float listLeft)
		{
			var pages = PageButtonsField?.GetValue(ui) as GameObject;
			if (pages == null || pages.transform.parent == null)
			{
				return false;
			}

			// Mission Rework moves the back button out of the page buttons: looked for under their parent.
			Transform source = FindChild(pages.transform.parent, SourceButtonName);
			Transform sourceBg = source != null ? source.Find("bg+trigger") : null;
			MeshRenderer sourceRenderer = sourceBg != null ? sourceBg.GetComponent<MeshRenderer>() : null;
			if (sourceRenderer == null || sourceRenderer.GetComponent<MeshFilter>() == null)
			{
				return false;
			}

			// The plates fill the room left of the list, at most Width of a row, at least MinWidth.
			float full = InFrameBounds(sourceRenderer, pages.transform).size.x;
			float share = Width;
			float x = X;
			if (!float.IsNaN(room) && full > 0f)
			{
				share = Mathf.Clamp(room / full, MinWidth, Width);
				x = listLeft + ListGap + full * share / 2f;
			}

			builtFor = ui;
			builtRoom = room;
			head = MakePlate(pages.transform, source, "postal_expansion_region", x, Y, share, Toggle);
			if (head == null)
			{
				return false;
			}

			for (int i = 0; i < Names.Length; i++)
			{
				int choiceRegion = i - 1;
				Plate choice = MakePlate(
					pages.transform,
					source,
					"postal_expansion_region_choice_" + i,
					x,
					Y - Step * (i + 1),
					share,
					() => Pick(choiceRegion));
				if (choice == null)
				{
					return false;
				}

				SetText(choice, Names[i]);
				choice.Root.SetActive(false);
				Choices.Add(choice);
			}

			return true;
		}

		private static void Unmake()
		{
			foreach (Plate plate in Choices)
			{
				DestroyPlate(plate);
			}

			Choices.Clear();
			DestroyPlate(head);
			head = null;
			builtFor = null;
		}

		private static void DestroyPlate(Plate plate)
		{
			if (plate == null)
			{
				return;
			}

			if (plate.Material != null)
			{
				Object.Destroy(plate.Material);
			}

			if (plate.Root != null)
			{
				Object.Destroy(plate.Root);
			}
		}

		// A copy of the back button without its button and outline (removed while inactive), narrowed along the plate's width, its
		// middle at x, with its own material and button.
		private static Plate MakePlate(
			Transform parent,
			Transform source,
			string name,
			float x,
			float y,
			float share,
			Action action)
		{
			if (stash == null)
			{
				stash = new GameObject("postal_expansion_ui_stash");
				stash.SetActive(false);
				Object.DontDestroyOnLoad(stash);
			}

			GameObject copy = Object.Instantiate(source.gameObject, stash.transform, false);
			foreach (Component component in copy.GetComponentsInChildren<Component>(true))
			{
				if (component is GoPointerButton || (component != null && component.GetType().Name == "Outline"))
				{
					Object.DestroyImmediate(component);
				}
			}

			copy.transform.SetParent(parent, false);
			copy.SetActive(true);
			copy.name = name;
			copy.transform.localPosition = new Vector3(x, y, source.localPosition.z);

			Transform bg = copy.transform.Find("bg+trigger");
			Transform textObject = copy.transform.Find("text");
			TextMesh text = textObject != null ? textObject.GetComponent<TextMesh>() : null;
			MeshRenderer renderer = bg != null ? bg.GetComponent<MeshRenderer>() : null;
			MeshFilter filter = bg != null ? bg.GetComponent<MeshFilter>() : null;
			if (text == null || renderer == null || filter == null || bg.GetComponent<Collider>() == null)
			{
				Object.Destroy(copy);
				return null;
			}

			Vector3 scale = bg.localScale;
			int axis = 0;
			float most = -1f;
			for (int a = 0; a < 3; a++)
			{
				Vector3 direction = bg.localRotation *
					(a == 0 ? Vector3.right : a == 1 ? Vector3.up : Vector3.forward);
				if (Mathf.Abs(direction.x) > most)
				{
					most = Mathf.Abs(direction.x);
					axis = a;
				}
			}

			scale[axis] *= share;
			bg.localScale = scale;
			Vector3 position = copy.transform.localPosition;
			position.x += x - InFrameBounds(renderer, parent).center.x;
			copy.transform.localPosition = position;

			var material = new Material(renderer.sharedMaterial);
			renderer.sharedMaterial = material;
			PostalPlateButton button = bg.gameObject.AddComponent<PostalPlateButton>();
			button.Action = () =>
			{
				if (UISoundPlayer.instance != null)
				{
					UISoundPlayer.instance.PlayUIClickSound();
				}

				action();
			};

			text.color = Color.black;
			text.text = string.Empty;
			return new Plate
			{
				Root = copy,
				Text = text,
				Material = material,
				TextScale = text.transform.localScale,
				TextWidth = filter.sharedMesh != null ? filter.sharedMesh.bounds.size[axis] * scale[axis] * 0.9f : 0f
			};
		}

		// The text at its own size, shrunk to the plate's width when longer.
		private static void SetText(Plate plate, string value)
		{
			if (plate.Text.text == value)
			{
				return;
			}

			TextMesh text = plate.Text;
			text.text = value;
			text.transform.localScale = plate.TextScale;
			if (plate.TextWidth <= 0f || text.font == null)
			{
				return;
			}

			text.font.RequestCharactersInTexture(value, text.fontSize, text.fontStyle);
			float width = 0f;
			foreach (char character in value)
			{
				if (text.font.GetCharacterInfo(character, out CharacterInfo info, text.fontSize, text.fontStyle))
				{
					width += info.advance * 0.1f * text.characterSize * plate.TextScale.x;
				}
			}

			if (width > plate.TextWidth)
			{
				text.transform.localScale = plate.TextScale * (plate.TextWidth / width);
			}
		}

		private static void Toggle()
		{
			open = !open;
			Refresh();
		}

		private static void Pick(int choiceRegion)
		{
			region = choiceRegion;
			open = false;
			Refresh();
			MissionListUI ui = MissionListUI.instance;
			PortDude dude = ui != null ? CurrentPortDudeField?.GetValue(ui) as PortDude : null;
			if (dude != null)
			{
				ui.ResetPage();
			}
		}

		// The room left of the list on the player's screen, in the page buttons' frame: from the list's left edge (its back button's and
		// welcome box's) to the screen's edge, seen through the camera that draws the list. NaN where it can't be measured.
		private static float Room(MissionListUI ui, out float listLeft)
		{
			listLeft = 0f;
			var pages = PageButtonsField?.GetValue(ui) as GameObject;
			if (pages == null || pages.transform.parent == null)
			{
				return float.NaN;
			}

			Transform frame = pages.transform;
			Transform source = FindChild(frame.parent, SourceButtonName);
			Transform bg = source != null ? source.Find("bg+trigger") : null;
			MeshRenderer renderer = bg != null ? bg.GetComponent<MeshRenderer>() : null;
			if (renderer == null || renderer.GetComponent<MeshFilter>() == null)
			{
				return float.NaN;
			}

			listLeft = InFrameBounds(renderer, frame).max.x;
			var welcome = PortWelcomeField?.GetValue(ui) as GameObject;
			if (welcome != null)
			{
				foreach (MeshRenderer welcomeRenderer in welcome.GetComponentsInChildren<MeshRenderer>(true))
				{
					if (welcomeRenderer.GetComponent<MeshFilter>() != null)
					{
						listLeft = Mathf.Max(listLeft, InFrameBounds(welcomeRenderer, frame).max.x);
					}
				}
			}

			if (uiCamera == null)
			{
				GameObject cameraObject = GameObject.Find("UI camera");
				uiCamera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
			}

			Camera camera = uiCamera != null ? uiCamera : Camera.main;
			if (camera == null)
			{
				return float.NaN;
			}

			Vector3 at = frame.TransformPoint(new Vector3(listLeft, Y, source.localPosition.z));
			Ray ray = camera.ScreenPointToRay(new Vector3(
				camera.pixelRect.xMin + camera.pixelWidth * EdgeMargin,
				camera.WorldToScreenPoint(at).y,
				0f));
			if (!new Plane(frame.TransformDirection(Vector3.forward), at).Raycast(ray, out float distance))
			{
				return float.NaN;
			}

			return frame.InverseTransformPoint(ray.GetPoint(distance)).x - listLeft - ListGap;
		}

		private static Transform FindChild(Transform root, string name)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
			{
				if (child.name == name)
				{
					return child;
				}
			}

			return null;
		}

		private static Bounds InFrameBounds(MeshRenderer renderer, Transform frame)
		{
			Bounds raw = renderer.GetComponent<MeshFilter>().sharedMesh.bounds;
			var bounds = new Bounds(
				frame.InverseTransformPoint(renderer.transform.TransformPoint(raw.center)),
				Vector3.zero);
			for (int i = 0; i < 8; i++)
			{
				Vector3 corner = raw.center + Vector3.Scale(raw.extents, new Vector3(
					(i & 1) == 0 ? -1f : 1f,
					(i & 2) == 0 ? -1f : 1f,
					(i & 4) == 0 ? -1f : 1f));
				bounds.Encapsulate(frame.InverseTransformPoint(renderer.transform.TransformPoint(corner)));
			}

			return bounds;
		}
	}

	internal sealed class PostalPlateButton : GoPointerButton
	{
		internal Action Action;

		public override void OnActivate()
		{
			if (!unclickable)
			{
				Action?.Invoke();
			}
		}
	}

	[HarmonyPatch(typeof(MissionListUI), "EnablePortMissionUI")]
	internal static class MissionListUiEnablePortPatch
	{
		private static void Prefix(
			MissionListUI __instance,
			ref Mission[] missions,
			PortDude dude,
			out int __state)
		{
			__state = PostalMissionUi.FilterInitialPortMissions(
				__instance,
				dude,
				ref missions);
		}

		private static void Postfix(MissionListUI __instance, int __state)
		{
			PostalMissionUi.EnsureMailButton(__instance);
			PostalMissionUi.RefreshPageCount(__instance, __state);
			PostalRegionFilter.Opened(__instance);
		}
	}

	[HarmonyPatch(typeof(MissionListUI), "SetWorldMissions")]
	internal static class MissionListUiSetWorldPatch
	{
		private static void Postfix()
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

	// Shows the mission's reputation reward under its gold reward on every mission page: the gold
	// at 0.8 size, "N reputation" as big under it. N is the game's per-unit reputation
	// (GetDeliveryRep, with this mod's mail rules and other mods' patches) times the cargo count,
	// as of today, so a late mission shows its reduced reward or penalty.
	// Does nothing with EconomyOverhaul's MissionPage class loaded: that page draws the line itself.
	[HarmonyPatch(typeof(MissionDetailsUI), "InsertMissionData")]
	internal static class MissionReputationRewardPatch
	{
		internal const string ReputationObjectName = "postal_expansion_reputation_reward";

		private const float GoldSize = 0.8f;
		// In the page's own frame: the gap between the gold (-.170) and reputation (-.252) lines
		// of EconomyOverhaul's page. The raises keep the line clear of the Accept button.
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

		internal static int TotalReputation(Mission mission)
		{
			return GetDeliveryRep != null ? GetDeliveryRep(mission) * mission.goodCount : 0;
		}

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

			TextMesh reputationText = LetterMissionDetailsPatch.FindText(
				totalGoldText.transform.parent,
				ReputationObjectName) ?? CreateReputationText(__instance, totalGoldText);
			if (reputationText == null)
			{
				return;
			}

			reputationText.text = TotalReputation(mission) + " reputation";
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
			TextMesh rewardLabel = LetterMissionDetailsPatch.FindTextByContent(
				pageRoot != null ? pageRoot.transform : null,
				"Reward:");
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

	// The game takes an accepted mission's cargo out of the origin office's warehouse by its prefab number. Express Mail and Registered
	// Letters have the mail's number but come from no warehouse stock, so the office's mail is left.
	[HarmonyPatch(typeof(IslandMissionOffice), nameof(IslandMissionOffice.AcceptMission))]
	internal static class PostalWarehousePatch
	{
		private static bool Prefix(Mission mission)
		{
			SaveablePrefab saveable = PostalMail.SaveableOf(mission);
			return !PostalMail.IsExpressMail(saveable) && !PostalMail.IsRegisteredLetter(saveable);
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
			PostalItemKinds.SavePersistentState();
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
			PostalItemKinds.AfterLoad();
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
			Letter.EnsureTemplate();
			PostalExpressMail.EnsureTemplate();
		}
	}

	// Express Mail and Registered Letters save as the game's mail (prefab 221). The mod records which missions hold them and gives them
	// back their kind at load: each mission as the game builds it (before the mission log and the items), each item as it joins its
	// mission (so also an item a far boat makes from the save later).
	internal static class PostalItemKinds
	{
		private const string SaveKey = "PostalExpansion.ItemKinds.v1";
		private const int ExpressKind = 1;
		private const int LetterKind = 2;

		private static readonly Dictionary<int, int[]> PendingMissions = new Dictionary<int, int[]>();

		// "slot,kind,origin,destination" for each Express Mail and Registered Letter mission held.
		internal static void SavePersistentState()
		{
			if (GameState.modData == null)
			{
				GameState.modData = new Dictionary<string, string>();
			}

			var entries = new List<string>();
			Mission[] missions = PlayerMissions.missions;
			for (int i = 0; missions != null && i < missions.Length; i++)
			{
				int kind = KindOf(missions[i]);
				if (kind != 0 && missions[i].originPort != null && missions[i].destinationPort != null)
				{
					entries.Add(string.Join(",", i, kind, missions[i].originPort.portIndex, missions[i].destinationPort.portIndex));
				}
			}

			GameState.modData[SaveKey] = string.Join(";", entries);
		}

		// Before the game builds anything from the save: the recorded kinds, and a save from 1.1.3 or older turned to prefab 221.
		internal static void BeforeLoad(SaveContainer save)
		{
			PendingMissions.Clear();
			if (save == null)
			{
				return;
			}

			if (save.modData != null && save.modData.TryGetValue(SaveKey, out string data))
			{
				foreach (string entry in data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
				{
					string[] fields = entry.Split(',');
					if (fields.Length == 4 &&
						int.TryParse(fields[0], out int slot) &&
						int.TryParse(fields[1], out int kind) &&
						int.TryParse(fields[2], out int origin) &&
						int.TryParse(fields[3], out int destination) &&
						(kind == ExpressKind || kind == LetterKind))
					{
						PendingMissions[slot] = new[] { kind, origin, destination };
					}
				}
			}

			MigrateLegacySave(save);
		}

		internal static void MissionLoaded(Mission mission, SaveMissionData data)
		{
			if (mission == null ||
				data == null ||
				data.goodPrefabIndex != PostalMail.RegularMailItemIndex ||
				!PendingMissions.TryGetValue(data.missionIndex, out int[] entry) ||
				entry[1] != data.originPort ||
				entry[2] != data.destinationPort)
			{
				return;
			}

			GameObject template = entry[0] == ExpressKind ? PostalExpressMail.Prefab : Letter.Prefab;
			if (template == null || mission.destinationPort == null)
			{
				return;
			}

			mission.goodPrefab = template;
			mission.missionName = template.GetComponent<ShipItem>().name + " to " + mission.destinationPort.GetPortName();
		}

		internal static void AfterLoad()
		{
			PendingMissions.Clear();
		}

		// A mail item joining an Express or letter mission is turned into that item; one made from the mod's template already is.
		internal static void GoodJoining(Mission mission, GameObject good)
		{
			if (good == null || mission == null || mission.goodPrefab == null)
			{
				return;
			}

			int kind = KindOf(mission);
			SaveablePrefab saveable = good.GetComponent<SaveablePrefab>();
			if (kind == 0 ||
				saveable == null ||
				saveable.prefabIndex != PostalMail.RegularMailItemIndex ||
				PostalMail.IsExpressMail(saveable) ||
				PostalMail.IsRegisteredLetter(saveable))
			{
				return;
			}

			if (kind == ExpressKind)
			{
				PostalExpressMail.Configure(good);
			}
			else
			{
				Letter.Configure(good);
			}

			PostalShipItemLodPatch.Apply(good.GetComponent<ShipItem>());
		}

		private static int KindOf(Mission mission)
		{
			SaveablePrefab saveable = PostalMail.SaveableOf(mission);
			return PostalMail.IsExpressMail(saveable)
				? ExpressKind
				: PostalMail.IsRegisteredLetter(saveable) ? LetterKind : 0;
		}

		// Up to 1.1.3 a Registered Letter saved as prefab 238 and Express Mail as 239: missions, items and log rows. Turned to 221 here,
		// the missions' kinds kept as if this mod had recorded them, the log rows' kinds given to the mission log's history.
		private static void MigrateLegacySave(SaveContainer save)
		{
			if (save.savedMissions != null)
			{
				foreach (SaveMissionData mission in save.savedMissions)
				{
					int kind = LegacyKind(mission != null ? mission.goodPrefabIndex : 0);
					if (kind != 0)
					{
						mission.goodPrefabIndex = PostalMail.RegularMailItemIndex;
						PendingMissions[mission.missionIndex] = new[] { kind, mission.originPort, mission.destinationPort };
					}
				}
			}

			if (save.savedPrefabs != null)
			{
				foreach (SavePrefabData item in save.savedPrefabs)
				{
					if (item != null && LegacyKind(item.prefabIndex) != 0)
					{
						item.prefabIndex = PostalMail.RegularMailItemIndex;
					}
				}
			}

			var legacyRows = new Dictionary<int, SpecialMailKind>();
			for (int i = 0; save.loggedMissions != null && i < save.loggedMissions.Length; i++)
			{
				LoggedMission row = save.loggedMissions[i];
				int kind = LegacyKind(row != null ? row.goodIndex : 0);
				if (kind != 0)
				{
					row.goodIndex = PostalMail.RegularMailItemIndex;
					legacyRows[i] = kind == ExpressKind ? SpecialMailKind.ExpressMail : SpecialMailKind.RegisteredLetter;
				}
			}

			if (legacyRows.Count > 0)
			{
				if (save.modData == null)
				{
					save.modData = new Dictionary<string, string>();
				}

				SpecialMailHistory.MigrateLegacyRows(save.modData, save.loggedMissions, legacyRows);
			}
		}

		private static int LegacyKind(int prefabIndex)
		{
			return prefabIndex == PostalMail.LegacyExpressMailItemIndex
				? ExpressKind
				: prefabIndex == PostalMail.LegacyRegisteredLetterItemIndex ? LetterKind : 0;
		}
	}

	[HarmonyPatch(typeof(SaveLoadManager), "LoadNeeds")]
	internal static class PostalItemKindsLoadPatch
	{
		private static void Prefix(SaveContainer save)
		{
			MissingPortMissions.Drop(save);
			PostalItemKinds.BeforeLoad(save);
		}
	}

	internal static class MissingPortMissions
	{
		internal static void Drop(SaveContainer save)
		{
			if (save == null || save.savedMissions == null || Port.ports == null)
			{
				return;
			}

			for (int i = 0; i < save.savedMissions.Length; i++)
			{
				SaveMissionData mission = save.savedMissions[i];
				if (mission != null &&
					(!PortExists(mission.originPort) || !PortExists(mission.destinationPort)))
				{
					Debug.LogWarning(
						"Postal Expansion: the mission in slot " + i + ", from port " + mission.originPort +
						" to port " + mission.destinationPort + ", is dropped: its port is not in the game.");
					save.savedMissions[i] = null;
				}
			}
		}

		private static bool PortExists(int index)
		{
			return index >= 0 && index < Port.ports.Length && Port.ports[index] != null;
		}
	}

	[HarmonyPatch(typeof(Mission), MethodType.Constructor, new[] { typeof(SaveMissionData) })]
	internal static class PostalItemKindsMissionPatch
	{
		private static void Postfix(Mission __instance, SaveMissionData data)
		{
			PostalItemKinds.MissionLoaded(__instance, data);
		}
	}

	// Before the game registers the item (NANDTweaks' mission decal is a postfix on that), so the decal sees a Registered Letter.
	[HarmonyPatch(typeof(Mission), nameof(Mission.RegisterGood))]
	internal static class PostalItemKindsGoodPatch
	{
		private static void Prefix(Mission __instance, GameObject good)
		{
			PostalItemKinds.GoodJoining(__instance, good);
		}
	}

	// Express Mail and Registered Letters are kinds only for the mission log: both save as the game's mail, so the log tells them from
	// mail by kind. Only the first three rename a mission elsewhere (its page, the delivery notice, the item's look text).
	internal enum SpecialMailKind
	{
		None = 0,
		AnonymousLetter = 1,
		UrgentExpressMail = 2,
		GoldenDelivery = 3,
		ExpressMail = 4,
		RegisteredLetter = 5
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
			if (PostalMail.IsExpressMission(mission))
			{
				return SpecialMailKind.ExpressMail;
			}
			if (PostalMail.IsRegisteredLetter(PostalMail.SaveableOf(mission)))
			{
				return SpecialMailKind.RegisteredLetter;
			}

			return SpecialMailKind.None;
		}

		internal static bool Renames(SpecialMailKind kind)
		{
			return kind == SpecialMailKind.AnonymousLetter ||
				kind == SpecialMailKind.UrgentExpressMail ||
				kind == SpecialMailKind.GoldenDelivery;
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
				case SpecialMailKind.ExpressMail:
					return PostalExpressMail.DisplayName;
				case SpecialMailKind.RegisteredLetter:
					return Letter.DisplayName;
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
			if (!SpecialMailDisplay.Renames(kind) ||
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
			if (!SpecialMailDisplay.Renames(kind))
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

		// A save from 1.1.3 or older, its log rows of prefab 238 and 239 just turned to 221: the history's entries for them follow, and a
		// row with no entry gets its kind (Express Mail or Registered Letter), so the log still tells it from mail.
		internal static void MigrateLegacyRows(
			Dictionary<string, string> modData,
			LoggedMission[] rows,
			Dictionary<int, SpecialMailKind> legacyRows)
		{
			var entries = new List<string>();
			var marked = new HashSet<int>();
			if (modData.TryGetValue(SaveKey, out string data))
			{
				foreach (string entry in data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
				{
					string[] fields = entry.Split(',');
					if (fields.Length == 4 &&
						int.TryParse(fields[0], out int slot) &&
						legacyRows.ContainsKey(slot))
					{
						fields[3] = PostalMail.RegularMailItemIndex.ToString();
						marked.Add(slot);
					}

					entries.Add(string.Join(",", fields));
				}
			}

			foreach (KeyValuePair<int, SpecialMailKind> row in legacyRows)
			{
				if (!marked.Contains(row.Key))
				{
					entries.Add(string.Join(",",
						row.Key,
						(int)row.Value,
						rows[row.Key].day,
						PostalMail.RegularMailItemIndex));
				}
			}

			modData[SaveKey] = string.Join(";", entries);
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

				// An Anonymous Letter's port is its reference port, which its mission page hides too.
				__instance.texts[i].text =
					"day " + mission.day + ": " +
					SpecialMailDisplay.GetDisplayName(kind) +
					(kind == SpecialMailKind.AnonymousLetter ? string.Empty : " to " + mission.destinationName) +
					" (" + mission.deliveredGoodCount + ")";
			}
		}
	}
}
