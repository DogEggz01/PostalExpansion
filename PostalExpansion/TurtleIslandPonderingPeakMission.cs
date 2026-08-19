using UnityEngine;

namespace PostalExpansion
{
	internal sealed class TurtleIslandPonderingPeakMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly TurtleIslandPonderingPeakMission Instance = new();

		private TurtleIslandPonderingPeakMission()
			: base(
				"turtle_island_pondering_peak_tea_merchant",
				"Turtle Island",
				SailwindPortIndex.TurtleIsland,
				new Vector3(2273.436f, 42.729f, 234.471f),
				new Vector3(0f, 219.358f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				2287.5f,
				new LetterDeliveryWindow(1f, 4f, "01-04"),
				new[]
				{
					SailwindPortIndex.Oasis
				},
				"Pondering Peak white tea merchant")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			return GetOriginPortIndex(mission) == SailwindPortIndex.Oasis
				? "You and your (Yawn)... Stupid secrecy. Can i go to sleep now?"
				: UnexpectedOrigin(mission);
		}
	}
}
