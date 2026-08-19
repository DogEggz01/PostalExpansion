using UnityEngine;

namespace PostalExpansion
{
	internal sealed class FireFishTownTempleHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FireFishTownTempleHouseMission Instance = new();

		private FireFishTownTempleHouseMission()
			: base(
				"fire_fish_town_temple_house",
				"Fire Fish Town",
				SailwindPortIndex.FireFishTown,
				new Vector3(-5.519f, 7.688f, 82.870f),
				new Vector3(0f, 35.728f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.Sanctuary
				},
				"Temple house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			return GetOriginPortIndex(mission) == SailwindPortIndex.Sanctuary
				? "All is well."
				: UnexpectedOrigin(mission);
		}
	}
}
