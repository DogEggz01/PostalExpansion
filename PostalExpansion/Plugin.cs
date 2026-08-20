using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PostalExpansion
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	[BepInDependency(NandTweaksCompatibility.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
	public class Plugin : BaseUnityPlugin
	{
		internal const string PluginGuid = "com.DogEggz.postalexpansion";
		internal const string PluginName = "Postal Expansion";
		internal const string PluginVersion = "1.1.1";

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
}
