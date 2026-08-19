using UnityEngine;

namespace PostalExpansion
{
	internal sealed class OnnaHoneyShamanMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly OnnaHoneyShamanMission Instance = new();

		private OnnaHoneyShamanMission()
			: base(
				"onna_honey_shaman",
				"On'na",
				SailwindPortIndex.Onna,
				new Vector3(-7.320f, 1.007f, 0.395f),
				new Vector3(0f, 99.218f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.TurtleIsland,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.FeyValley,
					SailwindPortIndex.MountMalefic,
					SailwindPortIndex.AlchemistsIsland,
					SailwindPortIndex.SageHills,
					SailwindPortIndex.OldAnkhTown
				},
				"Honey Shaman")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.TurtleIsland:
					return "Ha! You think I'm stupid?";

				case SailwindPortIndex.Chronos:
					return "My honey knows no boundary.";

				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.FeyValley:
				case SailwindPortIndex.MountMalefic:
				case SailwindPortIndex.AlchemistsIsland:
				case SailwindPortIndex.SageHills:
				case SailwindPortIndex.OldAnkhTown:
					return "I should grow more flowers... maybe a little rain...";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
