using UnityEngine;

namespace PostalExpansion
{
	internal sealed class AestraAbbeyMainAbbeyMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly AestraAbbeyMainAbbeyMission Instance = new();

		private AestraAbbeyMainAbbeyMission()
			: base(
				"aestra_abbey_main_abbey",
				"Aestra Abbey",
				SailwindPortIndex.AestraAbbey,
				new Vector3(-117.400f, 56.962f, -81.718f),
				new Vector3(0f, 220.653f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.Sanctuary
				},
				"Abbey")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FireflyGrotto:
					return "Castigo corpus meum.";

				case SailwindPortIndex.Chronos:
					return "A solis ortus cardine.";

				case SailwindPortIndex.Sanctuary:
					return "Pacem in terries.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
