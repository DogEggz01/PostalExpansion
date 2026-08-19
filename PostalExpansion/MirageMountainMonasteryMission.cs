using UnityEngine;

namespace PostalExpansion
{
	internal sealed class MirageMountainMonasteryMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly MirageMountainMonasteryMission Instance = new();

		private MirageMountainMonasteryMission()
			: base(
				"mirage_mountain_monastery",
				"Mirage Mountain",
				SailwindPortIndex.MirageMountain,
				new Vector3(36.812f, 3.241f, -3.945f),
				new Vector3(0f, 175.062f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.Onna
				},
				"monastary")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Neverdin:
				case SailwindPortIndex.AlAnkhAcademy:
					return "Sabbih Ash-shams!";

				case SailwindPortIndex.GoldRockCity:
					return "We appreciate Sultan's relief effort.";

				case SailwindPortIndex.Onna:
					return "We place faith in our Academy scholars, instead of this .... primitive practice.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
