using UnityEngine;

namespace PostalExpansion
{
	internal sealed class AestraAbbeyGraveRobberTableMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly AestraAbbeyGraveRobberTableMission Instance = new();

		private AestraAbbeyGraveRobberTableMission()
			: base(
				"aestra_abbey_grave_robber_table",
				"Aestra Abbey",
				SailwindPortIndex.AestraAbbey,
				new Vector3(232.158f, -0.924f, 194.327f),
				new Vector3(0f, 136.291f, 0f),
				new Vector3(2f, 2f, 2f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.SirenSong,
					SailwindPortIndex.Oasis
				},
				"Table of grave robber")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.SirenSong:
				case SailwindPortIndex.Oasis:
					return "(You put the letter under the table)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
