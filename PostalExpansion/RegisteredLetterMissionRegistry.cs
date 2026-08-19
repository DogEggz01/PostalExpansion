using System.Collections.Generic;

namespace PostalExpansion
{
	internal static class RegisteredLetterMissionRegistry
	{
		private static readonly LetterMissionRegistry<RegisteredLetterMissionDefinition>
			Registry = new LetterMissionRegistry<RegisteredLetterMissionDefinition>(
				"Registered Letter",
				GoldRockCityPalaceGateMission.Instance,
				GoldRockCityPalaceFishVendorMission.Instance,
				NeverdinTempleMission.Instance,
				AlbacoreTownCreepyGuyMission.Instance,
				AlchemistsIslandApprenticeMission.Instance,
				AlAnkhAcademyKeyKeeperMission.Instance,
				DragonCliffsBrownTobaccoVendorMission.Instance,
				DragonCliffsTeaMerchantHouseMission.Instance,
				DragonCliffsChimesHutMission.Instance,
				SanctuaryMainBuildingMission.Instance,
				CrabBeachHouseMission.Instance,
				SageHillsWestTobaccoFarmerMission.Instance,
				FortAestrinUpperTownGateMission.Instance,
				FortAestrinWesternmostPierHouseMission.Instance,
				SunspireSpyglassMakerMission.Instance,
				HappyBayLocalPrinceMission.Instance,
				OasisLighthouseMission.Instance,
				SirenSongEmptyHouseMission.Instance,
				SerpentIsleWesternmostHouseMission.Instance,
				MountMaleficWheatManorMission.Instance,
				ChronosChurchHouseMission.Instance,
				FireFishTownTempleHouseMission.Instance,
				SennaAlchemistMission.Instance,
				SennaBlueTobaccoVendorMission.Instance,
				OnnaHoneyShamanMission.Instance,
				FireflyGrottoChurchMission.Instance,
				AestraAbbeyMainAbbeyMission.Instance,
				AestraAbbeyGraveRobberTableMission.Instance,
				FeyValleyWaterfallManorVendorMission.Instance,
				TurtleIslandSunShamanMission.Instance,
				OldAnkhTownCoffeeFarmerMission.Instance,
				MirageMountainMonasteryMission.Instance,
				FireFishTownPainterMission.Instance,
				KiciaBayWesternmostHouseMission.Instance);

		internal static IReadOnlyList<RegisteredLetterMissionDefinition> All =>
			Registry.All;
	}
}
