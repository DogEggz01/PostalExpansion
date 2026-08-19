using UnityEngine;

namespace PostalExpansion
{
	internal sealed class TurtleIslandSunShamanMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly TurtleIslandSunShamanMission Instance = new();

		private TurtleIslandSunShamanMission()
			: base(
				"turtle_island_sun_shaman",
				"Turtle Island",
				SailwindPortIndex.TurtleIsland,
				new Vector3(-12.675f, 4.695f, 38.000f),
				new Vector3(0f, 49.052f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.Onna,
					SailwindPortIndex.Senna,
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.FeyValley,
					SailwindPortIndex.MountMalefic,
					SailwindPortIndex.AlchemistsIsland,
					SailwindPortIndex.SageHills,
					SailwindPortIndex.OldAnkhTown
				},
				"Sun Shaman")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Onna:
					return "....No.";

				case SailwindPortIndex.Neverdin:
				case SailwindPortIndex.AlAnkhAcademy:
					return "Sun is good.";

				case SailwindPortIndex.Senna:
				case SailwindPortIndex.Chronos:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.FeyValley:
				case SailwindPortIndex.MountMalefic:
				case SailwindPortIndex.AlchemistsIsland:
				case SailwindPortIndex.SageHills:
				case SailwindPortIndex.OldAnkhTown:
					return "Uh-um.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
