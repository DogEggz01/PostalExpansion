using UnityEngine;

namespace PostalExpansion
{
	internal sealed class SaffronIslandTraderMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly SaffronIslandTraderMission Instance = new();

		private SaffronIslandTraderMission()
			: base(
				"saffron_island_trader",
				"Saffron Island",
				SailwindPortIndex.SaffronIsland,
				new Vector3(0.015f, 0.750f, 0.586f),
				new Vector3(0f, 177.864f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				3.5f,
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.DeadCove,
					SailwindPortIndex.CrabBeach,
					SailwindPortIndex.AlNilem
				},
				"Trader")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.DeadCove:
				case SailwindPortIndex.CrabBeach:
				case SailwindPortIndex.AlNilem:
					return "Thank you, we haven't heard from our family for a while.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
