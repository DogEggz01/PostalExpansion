using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace PostalExpansion
{
	internal static class PostalExpressMail
	{
		private const int ExpressMailGoodIndex = 69;
		private const float ExpressWorldDistance = 140f;
		private const float ExpressMailDueDayBaseSpeed = 2.6f;
		private const float ExpressRewardMultiplier = 1.5f;
		private const float HardPortRewardMultiplier = 2f;
		private const float ComparableRegularMailPackageMultiplier = 2f;
		private const string ExpressMailName = "Express Mail";
		private const string ExpressMailSealTextureFile = "express_mail_seal.png";
		private const string ExpressMailSealObjectName = "postal_expansion_express_mail_seal";
		private const float ExpressMailSealSize = 0.32f;

		private static readonly Vector3 ExpressMailSealLocalPosition =
			new Vector3(0.303f, 0.215f, 0.171f);

		private static readonly HashSet<int> ExpressMailAvailablePortIndices =
			new HashSet<int>
			{
				SailwindPortIndex.FortAestrin,
				SailwindPortIndex.SirenSong,
				SailwindPortIndex.Eastwind,
				SailwindPortIndex.Sunspire,
				SailwindPortIndex.HappyBay,
				SailwindPortIndex.NewPort,
				SailwindPortIndex.DragonCliffs,
				SailwindPortIndex.CrabBeach,
				SailwindPortIndex.SageHills,
				SailwindPortIndex.FireFishTown,
				SailwindPortIndex.KiciaBay,
				SailwindPortIndex.GoldRockCity,
				SailwindPortIndex.Neverdin,
				SailwindPortIndex.AlNilem,
				SailwindPortIndex.OldAnkhTown,
				SailwindPortIndex.Oasis,
				SailwindPortIndex.Senna,
				SailwindPortIndex.FireflyGrotto,
				SailwindPortIndex.Sanctuary,
				SailwindPortIndex.AlAnkhAcademy,
				SailwindPortIndex.Chronos
			};

		private static readonly HashSet<int> HardPortIndices = new HashSet<int>
		{
			SailwindPortIndex.AestraAbbey,
			SailwindPortIndex.MirageMountain,
			SailwindPortIndex.OldAnkhTown,
			SailwindPortIndex.AlbacoreTown,
			SailwindPortIndex.SageHills,
			SailwindPortIndex.DeadCove,
			SailwindPortIndex.Onna,
			SailwindPortIndex.Senna,
			SailwindPortIndex.Chronos
		};

		private static GameObject expressMailPrefab;
		private static readonly PostalSealVisual ExpressMailSeal = new PostalSealVisual(
			ExpressMailSealObjectName,
			ExpressMailSealTextureFile,
			"express_mail_seal",
			ExpressMailSealSize);

		internal static GameObject Prefab => expressMailPrefab;

		internal static bool EnsureRegistered(PrefabsDirectory directory)
		{
			if (directory == null || directory.directory == null)
			{
				return false;
			}

			if (PostalPrefabRegistration.IsRegistered(
				directory,
				PostalMail.ExpressMailItemIndex,
				expressMailPrefab))
			{
				return true;
			}

			if (PostalPrefabRegistration.IsSlotOccupiedByAnotherPrefab(
				directory,
				PostalMail.ExpressMailItemIndex,
				expressMailPrefab))
			{
				Debug.LogError("Postal Expansion: prefab index 239 is already occupied.");
				return false;
			}

			if (directory.directory.Length <= PostalMail.RegularMailItemIndex ||
				directory.directory[PostalMail.RegularMailItemIndex] == null)
			{
				Debug.LogWarning("Postal Expansion: regular mail prefab was not found.");
				return false;
			}

			GameObject regularMailPrefab =
				directory.directory[PostalMail.RegularMailItemIndex];
			if (!HasRequiredComponents(regularMailPrefab) ||
				!PostalPrefabRegistration.EnsureCapacity(
					directory,
					PostalMail.ExpressMailItemIndex))
			{
				Debug.LogError("Postal Expansion: Express Mail prefab components could not be prepared.");
				return false;
			}

			if (expressMailPrefab == null)
			{
				expressMailPrefab = Object.Instantiate(regularMailPrefab);
				expressMailPrefab.name =
					$"{PostalMail.ExpressMailItemIndex} ({ExpressMailGoodIndex}) {ExpressMailName}";
				Object.DontDestroyOnLoad(expressMailPrefab);
			}

			PostalMailOutlineSanitizer.Attach(expressMailPrefab);
			ConfigureExpressMailPrefab(expressMailPrefab);
			EnsureExpressMailSeal(expressMailPrefab);
			if (!PostalPrefabRegistration.RegisterShipItem(
				directory,
				PostalMail.ExpressMailItemIndex,
				expressMailPrefab))
			{
				Debug.LogError("Postal Expansion: Express Mail prefab could not be registered as a ship item.");
				return false;
			}

			directory.directory[PostalMail.ExpressMailItemIndex] = expressMailPrefab;
			return true;
		}

		internal static void AddExpressMissions(Port origin, List<Mission> missions)
		{
			PrefabsDirectory directory = PrefabsDirectory.instance;
			if (origin == null ||
				missions == null ||
				(!PostalPrefabRegistration.IsRegistered(
					directory,
					PostalMail.ExpressMailItemIndex,
					expressMailPrefab) &&
				 !EnsureRegistered(directory)) ||
				Port.ports == null ||
				!ExpressMailAvailablePortIndices.Contains(origin.portIndex))
			{
				return;
			}

			Good expressGood = expressMailPrefab.GetComponent<Good>();
			if (expressGood == null)
			{
				return;
			}

			int reputationLevel = PlayerReputation.GetRepLevel(origin.region);
			if (reputationLevel < expressGood.requiredRepLevel)
			{
				return;
			}

			int targetCount = GetExpressMissionCount(reputationLevel);
			List<ExpressMissionDestinationCandidate> localCandidates =
				GetExpressMissionDestinationCandidates(origin, reputationLevel, false);
			List<ExpressMissionDestinationCandidate> worldCandidates =
				GetExpressMissionDestinationCandidates(origin, reputationLevel, true);
			var selectedPorts = new HashSet<int>();
			var generatedMissions = new List<Mission>(targetCount * 2);
			int localAdded = 0;
			int worldAdded = 0;

			if (reputationLevel >= UrgentExpressMail.RequiredReputation)
			{
				localAdded = AddRandomExpressMissions(
					origin,
					expressGood,
					GetHardPortCandidates(localCandidates),
					1,
					selectedPorts,
					generatedMissions,
					GetExpressSelectionSeed(origin, reputationLevel, 11));
				worldAdded = AddRandomExpressMissions(
					origin,
					expressGood,
					GetHardPortCandidates(worldCandidates),
					1,
					selectedPorts,
					generatedMissions,
					GetExpressSelectionSeed(origin, reputationLevel, 12));
			}

			AddRandomExpressMissions(
				origin,
				expressGood,
				localCandidates,
				targetCount - localAdded,
				selectedPorts,
				generatedMissions,
				GetExpressSelectionSeed(origin, reputationLevel, 21));
			AddRandomExpressMissions(
				origin,
				expressGood,
				worldCandidates,
				targetCount - worldAdded,
				selectedPorts,
				generatedMissions,
				GetExpressSelectionSeed(origin, reputationLevel, 22));

			if (reputationLevel >= UrgentExpressMail.RequiredReputation &&
				UrgentExpressMail.CanOfferAt(origin))
			{
				List<Mission> worldExpressMissions = generatedMissions.FindAll(
					mission => mission != null && mission.distance >= ExpressWorldDistance);
				if (worldExpressMissions.Count > 0)
				{
					var urgentSelection = new Random(
						GetExpressSelectionSeed(origin, reputationLevel, 31));
					UrgentExpressMail.MarkGenerated(
						worldExpressMissions[urgentSelection.Next(worldExpressMissions.Count)]);
				}
			}

			missions.AddRange(generatedMissions);
		}

		internal static float GetExpressRewardMultiplier(Port destination)
		{
			return IsHardPort(destination)
				? ExpressRewardMultiplier * HardPortRewardMultiplier
				: ExpressRewardMultiplier;
		}

		private static bool HasRequiredComponents(GameObject prefab)
		{
			return prefab.GetComponent<SaveablePrefab>() != null &&
				prefab.GetComponent<ShipItem>() != null &&
				prefab.GetComponent<Good>() != null;
		}

		private static void ConfigureExpressMailPrefab(GameObject prefab)
		{
			prefab.SetActive(true);
			prefab.GetComponent<SaveablePrefab>().prefabIndex =
				PostalMail.ExpressMailItemIndex;
			prefab.GetComponent<ShipItem>().name = ExpressMailName;
			prefab.GetComponent<Good>().requiredRepLevel = 3;
		}

		private static bool IsHardPort(Port port)
		{
			return port != null && HardPortIndices.Contains(port.portIndex);
		}

		private static int GetExpressMissionCount(int reputationLevel)
		{
			if (reputationLevel < 3)
			{
				return 0;
			}

			if (reputationLevel == 3)
			{
				return 1;
			}

			return reputationLevel == 4 ? 2 : 3;
		}

		private static List<ExpressMissionDestinationCandidate>
			GetExpressMissionDestinationCandidates(Port origin, int reputationLevel, bool world)
		{
			var candidates = new List<ExpressMissionDestinationCandidate>();
			foreach (Port destination in Port.ports)
			{
				if (destination == null ||
					destination == origin ||
					destination.portIndex == SailwindPortIndex.SaffronIsland ||
					destination.portIndex == SailwindPortIndex.TestPort)
				{
					continue;
				}

				float distance = Mission.GetDistance(origin, destination);
				bool inRequestedRange = world
					? distance >= ExpressWorldDistance
					: distance < ExpressWorldDistance;
				if (!PostalMail.IsWithinVanillaReputationRange(origin, distance) ||
					!inRequestedRange)
				{
					continue;
				}

				bool hardPort = IsHardPort(destination);
				if ((!hardPort || reputationLevel >= 4) &&
					!PlayerAlreadyHasMission(origin, destination, expressMailPrefab))
				{
					candidates.Add(
						new ExpressMissionDestinationCandidate(destination, distance, hardPort));
				}
			}

			return candidates;
		}

		private static List<ExpressMissionDestinationCandidate> GetHardPortCandidates(
			List<ExpressMissionDestinationCandidate> candidates)
		{
			var hardPorts = new List<ExpressMissionDestinationCandidate>();
			foreach (ExpressMissionDestinationCandidate candidate in candidates)
			{
				if (candidate.HardPort)
				{
					hardPorts.Add(candidate);
				}
			}

			return hardPorts;
		}

		private static int AddRandomExpressMissions(
			Port origin,
			Good expressGood,
			List<ExpressMissionDestinationCandidate> candidates,
			int count,
			HashSet<int> selectedPorts,
			List<Mission> missions,
			int seed)
		{
			ShuffleExpressCandidates(candidates, new Random(seed));
			int added = 0;
			foreach (ExpressMissionDestinationCandidate candidate in candidates)
			{
				if (added >= count)
				{
					break;
				}

				if (!selectedPorts.Add(candidate.Destination.portIndex))
				{
					continue;
				}

				missions.Add(GenerateExpressMailMission(
					origin,
					candidate.Destination,
					expressGood,
					candidate.Distance));
				added++;
			}

			return added;
		}

		private static void ShuffleExpressCandidates(
			List<ExpressMissionDestinationCandidate> candidates,
			Random random)
		{
			for (int i = candidates.Count - 1; i > 0; i--)
			{
				int swapIndex = random.Next(i + 1);
				ExpressMissionDestinationCandidate temporary = candidates[i];
				candidates[i] = candidates[swapIndex];
				candidates[swapIndex] = temporary;
			}
		}

		private static int GetExpressSelectionSeed(
			Port origin,
			int reputationLevel,
			int selectionGroupId)
		{
			return (((527 + GameState.day) * 31 + origin.portIndex) * 31 + reputationLevel) *
				31 + selectionGroupId;
		}

		private static Mission GenerateExpressMailMission(
			Port origin,
			Port destination,
			Good expressGood,
			float distance)
		{
			float distanceReward = distance * DebugMarketTracker.instance.missionDistanceFee;
			float rewardMultiplier = GetExpressRewardMultiplier(destination);
			int totalPrice = Mathf.RoundToInt(
				distanceReward *
				ComparableRegularMailPackageMultiplier *
				Plugin.RegularMailRewardMultiplier.Value *
				rewardMultiplier);
			int dueDay = GetExpressMailDueDay(origin, destination, expressGood);
			var mission = new Mission(origin, destination, expressGood.gameObject, 1, totalPrice, 1f, 0, dueDay);
			mission.totalPrice = CurrencyMarket.instance.GetSellPriceInCurrency(
				(Currency)destination.region,
				mission.totalPrice,
				false);
			mission.pricePerKm = mission.totalPrice / mission.distance;
			return mission;
		}

		private static int GetExpressMailDueDay(
			Port origin,
			Port destination,
			Good expressGood)
		{
			return MailDueDateCalculator.Calculate(
				origin,
				destination,
				expressGood,
				ExpressMailDueDayBaseSpeed);
		}

		private static bool PlayerAlreadyHasMission(
			Port origin,
			Port destination,
			GameObject goodPrefab)
		{
			Mission[] missions = PlayerMissions.missions;
			if (missions == null)
			{
				return false;
			}

			foreach (Mission mission in missions)
			{
				if (mission != null &&
					mission.originPort == origin &&
					mission.destinationPort == destination &&
					mission.goodPrefab == goodPrefab)
				{
					return true;
				}
			}

			return false;
		}

		private static void EnsureExpressMailSeal(GameObject prefab)
		{
			ExpressMailSeal.Ensure(prefab, ExpressMailSealLocalPosition, Quaternion.identity);
		}

		private readonly struct ExpressMissionDestinationCandidate
		{
			internal ExpressMissionDestinationCandidate(
				Port destination,
				float distance,
				bool hardPort)
			{
				Destination = destination;
				Distance = distance;
				HardPort = hardPort;
			}

			internal Port Destination { get; }
			internal float Distance { get; }
			internal bool HardPort { get; }
		}
	}
}
