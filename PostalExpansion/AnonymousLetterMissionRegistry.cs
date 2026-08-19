using System.Collections.Generic;

namespace PostalExpansion
{
	internal static class AnonymousLetterMissionRegistry
	{
		private static readonly LetterMissionRegistry<AnonymousLetterMissionDefinition>
			Registry = new LetterMissionRegistry<AnonymousLetterMissionDefinition>(
				"Anonymous Letter",
				OasisPirateHideoutMission.Instance,
				HappyBayUnnamedGraveMission.Instance,
				EastwindLighthouseMission.Instance,
				SaffronIslandTraderMission.Instance,
				AlNilemLionsFangObeliskMission.Instance,
				OldAnkhTownClearMindMission.Instance,
				AestraAbbeyOracleBenchMission.Instance,
				TurtleIslandPonderingPeakMission.Instance,
				DeadCoveNamelessRockMission.Instance);

		internal static IReadOnlyList<AnonymousLetterMissionDefinition> All =>
			Registry.All;
	}
}
