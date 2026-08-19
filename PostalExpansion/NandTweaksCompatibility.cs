using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace PostalExpansion
{
	internal static class NandTweaksCompatibility
	{
		internal const string PluginGuid = "com.nandbrew.nandtweaks";

		private const string DecalPatchTypeName =
			"NANDTweaks.MissionGoodsPatches+TextureReplacer";
		private const string DecalPostfixMethodName = "Postfix";

		internal static void Apply(Harmony harmony)
		{
			Type decalPatchType = AccessTools.TypeByName(DecalPatchTypeName);
			if (decalPatchType == null)
			{
				return;
			}

			MethodInfo decalPostfix = AccessTools.Method(
				decalPatchType,
				DecalPostfixMethodName,
				new[] { typeof(Good) });
			MethodInfo exclusionPrefix = AccessTools.Method(
				typeof(NandTweaksCompatibility),
				nameof(AllowMissionGoodsDecal));

			if (decalPostfix == null || exclusionPrefix == null)
			{
				Debug.LogWarning(
					"Postal Expansion: NANDTweaks mission decal compatibility could not be applied.");
				return;
			}

			harmony.Patch(decalPostfix, prefix: new HarmonyMethod(exclusionPrefix));
		}

		private static bool AllowMissionGoodsDecal(Good __0)
		{
			SaveablePrefab saveable = __0 == null
				? null
				: __0.GetComponent<SaveablePrefab>();

			return !PostalMail.IsRegisteredLetter(saveable);
		}
	}
}
