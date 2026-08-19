using UnityEngine;

namespace PostalExpansion
{
	internal sealed class GoldRockCityPalaceGateMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly GoldRockCityPalaceGateMission Instance = new();

		private GoldRockCityPalaceGateMission()
			: base(
				"gold_rock_city_palace_gate",
				"Gold Rock City",
				SailwindPortIndex.GoldRockCity,
				new Vector3(-94.099f, 7.512f, -36.680f),
				new Vector3(0f, 261.509f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.Chronos
				},
				"Palace Gate")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			return "Letter Received. Leave.";
		}
	}
}
