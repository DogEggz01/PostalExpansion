using UnityEngine;

namespace PostalExpansion
{
	internal sealed class SennaAlchemistMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SennaAlchemistMission Instance = new();

		private SennaAlchemistMission()
			: base(
				"senna_alchemist",
				"Sen'na",
				SailwindPortIndex.Senna,
				new Vector3(105.438f, 0.418f, -173.214f),
				new Vector3(0f, 354.712f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.AlchemistsIsland
				},
				"Alchemist")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
					return "Church force me out and they want me back? Not gonna happen!";

				case SailwindPortIndex.AlchemistsIsland:
					return "I wonder what they comes up with this time.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
