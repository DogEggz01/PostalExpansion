using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(Mission), nameof(Mission.DeliverGood))]
	internal static class LetterRewardCurrencyPatch
	{
		private static readonly FieldInfo DestinationPortField =
			AccessTools.Field(typeof(Mission), nameof(Mission.destinationPort));
		private static readonly FieldInfo PortRegionField =
			AccessTools.Field(typeof(Port), nameof(Port.region));
		private static readonly MethodInfo MissionLogAddMethod =
			AccessTools.Method(typeof(MissionLog), nameof(MissionLog.AddToLog));
		private static readonly MethodInfo ResolveCurrencyMethod =
			AccessTools.Method(
					typeof(LetterRewardCurrencyPatch),
				nameof(ResolveCurrency));

		private static IEnumerable<CodeInstruction> Transpiler(
			IEnumerable<CodeInstruction> instructions)
		{
			var code = new List<CodeInstruction>(instructions);
			var insertionIndexes = new List<int>();

			for (int i = 0; i < code.Count; i++)
			{
				if (!LoadsDestinationRegion(code, i))
				{
					continue;
				}

				int regionLoadIndex = i + 2;
				bool initializesRewardRegion =
					regionLoadIndex + 1 < code.Count &&
					code[regionLoadIndex + 1].opcode == OpCodes.Stloc_0;
				bool suppliesMissionLogCurrency =
					regionLoadIndex + 1 < code.Count &&
					code[regionLoadIndex + 1].Calls(MissionLogAddMethod);

				if (initializesRewardRegion || suppliesMissionLogCurrency)
				{
					insertionIndexes.Add(regionLoadIndex + 1);
				}
			}

			if (insertionIndexes.Count != 2)
			{
				throw new InvalidOperationException(
					"Could not locate Mission.DeliverGood's reward currency paths.");
			}

			for (int i = insertionIndexes.Count - 1; i >= 0; i--)
			{
				code.InsertRange(
					insertionIndexes[i],
					new[]
					{
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Call, ResolveCurrencyMethod)
					});
			}

			return code;
		}

		private static bool LoadsDestinationRegion(
			IReadOnlyList<CodeInstruction> code,
			int index)
		{
			return index + 2 < code.Count &&
				code[index].opcode == OpCodes.Ldarg_0 &&
				code[index + 1].LoadsField(DestinationPortField) &&
				code[index + 2].LoadsField(PortRegionField);
		}

		private static int ResolveCurrency(
			PortRegion destinationRegion,
			Mission mission)
		{
			return LetterMissions.TryGetDefinition(mission, out _)
				? (int)Currency.gold
				: (int)destinationRegion;
		}
	}
}
