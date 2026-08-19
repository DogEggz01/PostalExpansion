using UnityEngine;

namespace PostalExpansion
{
	internal sealed class OasisLighthouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly OasisLighthouseMission Instance = new();

		private OasisLighthouseMission()
			: base(
				"oasis_lighthouse",
				"Oasis",
				SailwindPortIndex.Oasis,
				new Vector3(284.163f, 5.081f, -96.620f),
				new Vector3(0f, 149.388f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.HappyBay
				},
				"Lighthouse")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.MirageMountain:
					return "Always vigilant.";

				case SailwindPortIndex.AlAnkhAcademy:
				case SailwindPortIndex.KiciaBay:
					return "We'll help with astrology research.";

				case SailwindPortIndex.HappyBay:
					return "...I will keep an eye out.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
