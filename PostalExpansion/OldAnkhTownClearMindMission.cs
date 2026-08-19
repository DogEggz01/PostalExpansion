using UnityEngine;

namespace PostalExpansion
{
	internal sealed class OldAnkhTownClearMindMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly OldAnkhTownClearMindMission Instance = new();

		private OldAnkhTownClearMindMission()
			: base(
				"old_ankh_town_clear_mind_palms",
				"Old Ankh Town",
				SailwindPortIndex.OldAnkhTown,
				new Vector3(-1377.220f, 8.869f, -195.353f),
				new Vector3(0f, 139.835f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				1391.9f,
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.DeadCove,
					SailwindPortIndex.Senna,
					SailwindPortIndex.SageHills,
					SailwindPortIndex.CrabBeach,
					SailwindPortIndex.Eastwind
				},
				"Lion's Fang middle of 3 palm trees")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.DeadCove:
				case SailwindPortIndex.Senna:
				case SailwindPortIndex.SageHills:
				case SailwindPortIndex.CrabBeach:
				case SailwindPortIndex.Eastwind:
					return "(You put the letter in bottle and bury it as request.)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
