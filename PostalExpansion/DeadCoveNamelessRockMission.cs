using UnityEngine;

namespace PostalExpansion
{
	internal sealed class DeadCoveNamelessRockMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly DeadCoveNamelessRockMission Instance = new();

		private DeadCoveNamelessRockMission()
			: base(
				"dead_cove_nameless_rock",
				"Dead Cove",
				SailwindPortIndex.DeadCove,
				new Vector3(-852.584f, 2.539f, -826.316f),
				new Vector3(0f, 26.327f, 0f),
				new Vector3(8f, 8f, 8f),
				1195.2f,
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.AlNilem,
					SailwindPortIndex.Eastwind,
					SailwindPortIndex.CrabBeach
				},
				"Nameless rock southren tip")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AlNilem:
				case SailwindPortIndex.Eastwind:
				case SailwindPortIndex.CrabBeach:
					return "(There is no remarkable landmark so you just throw the letter on the rock randomly. You have no idea what you are doing.)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
