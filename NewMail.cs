using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Random = System.Random;

namespace PostalExpansion
{
	internal static class PostalMail
	{
		// Express Mail and Registered Letters are copies of the game's mail and save under its prefab number; a marker component tells
		// them apart. Up to 1.1.3 they had prefab numbers of their own, 239 and 238, which old saves still hold.
		internal const int RegularMailItemIndex = 221;
		internal const int LegacyRegisteredLetterItemIndex = 238;
		internal const int LegacyExpressMailItemIndex = 239;

		internal static bool IsMailPrefab(SaveablePrefab saveable)
		{
			return saveable != null && saveable.prefabIndex == RegularMailItemIndex;
		}

		internal static bool IsRegisteredLetter(SaveablePrefab saveable)
		{
			return saveable != null && saveable.GetComponent<RegisteredLetterItem>() != null;
		}

		internal static bool IsExpressMail(SaveablePrefab saveable)
		{
			return saveable != null && saveable.GetComponent<ExpressMailItem>() != null;
		}

		internal static bool IsWithinVanillaReputationRange(Port origin, float distance)
		{
			return origin != null &&
				distance <= PlayerReputation.GetMaxDistance(origin.region);
		}

		internal static SaveablePrefab SaveableOf(Mission mission)
		{
			return mission != null && mission.goodPrefab != null
				? mission.goodPrefab.GetComponent<SaveablePrefab>()
				: null;
		}

		internal static bool IsExpressMission(Mission mission)
		{
			return IsExpressMail(SaveableOf(mission));
		}

		internal static bool PlayerHasMission(
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
	}

	internal sealed class ExpressMailItem : MonoBehaviour
	{
	}

	internal sealed class RegisteredLetterItem : MonoBehaviour
	{
	}

	internal static class MailDueDateCalculator
	{
		internal static int Calculate(
			Port origin,
			Port destination,
			Good mailGood,
			float baseSpeed)
		{
			float missionDistance =
				Mission.GetDistance(origin, destination);
			return CalculateFromMissionDistance(
				missionDistance,
				mailGood,
				baseSpeed);
		}

		internal static int CalculateFromMissionDistance(
			float missionDistance,
			Good mailGood,
			float baseSpeed)
		{
			float distance = missionDistance / 10f;
			float divisor =
				baseSpeed * (Sun.sun.GetRealtimeDayLength() / 60f / 60f);
			if (mailGood.requiredRepLevel > 1)
			{
				divisor *= 3.5f;
			}

			divisor *= mailGood.requiredRepLevel == 1 ? 2.5f : 1.5f;
			int deliveryDays = Mathf.Max(1, Mathf.RoundToInt(distance / divisor));
			return GameState.day + deliveryDays;
		}
	}

	[HarmonyPatch(typeof(IslandMissionOffice), "GenerateMailMission", new Type[]
	{
		typeof(Good),
		typeof(Port),
		typeof(float),
		typeof(int)
	})]
	internal static class RegularMissionPackageCountandGoldPatch
	{
		private static void Postfix(IslandMissionOffice __instance, float distance, Mission __result)
		{
			if (__result == null)
			{
				return;
			}

			int packageCount = Mathf.Clamp(
				PlayerReputation.GetRepLevel(__instance.GetPort().region),
				1,
				3);
			float distanceReward = distance * DebugMarketTracker.instance.missionDistanceFee;
			float packageMultiplier = packageCount * 0.5f + 0.5f;
			__result.goodCount = packageCount;
			__result.totalPrice = Mathf.RoundToInt(
				distanceReward *
				packageMultiplier *
				Plugin.RegularMailRewardMultiplier.Value);
			__result.pricePerKm = __result.totalPrice / __result.distance;
		}
	}

	internal static class ModdedPortMail
	{
		private const string BottleneckObjectName = "port 67 Bottleneck";
		internal const string TamarackGuid = "com.dixiecapt.sailwind.martellotower";
		private const string TamarackPortName = "Tamarack";
		private const int RegularMailGoodIndex = 51;

		private static readonly MethodInfo GenerateMailMissionMethod =
			AccessTools.Method(typeof(IslandMissionOffice), "GenerateMailMission");

		internal static bool TamarackLoaded;
		private static int tamarackIndex = -1;

		internal static void AddMissions(Port origin, List<Mission> missions)
		{
			if (origin == null ||
				missions == null ||
				GenerateMailMissionMethod == null)
			{
				return;
			}

			AddBottleneckMission(origin, missions);
			Port tamarack = GetTamarack();
			if (!Contains(origin.GetDestinationPorts(), tamarack))
			{
				AddMission(origin, tamarack, missions);
			}
		}

		private static void AddBottleneckMission(Port origin, List<Mission> missions)
		{
			Port bottleneck = GetBottleneck();
			if (bottleneck == null ||
				!Contains(bottleneck.GetDestinationPorts(), origin) ||
				Contains(origin.GetDestinationPorts(), bottleneck))
			{
				return;
			}

			var market = origin.GetComponent<IslandMarket>();
			if (market == null ||
				market.knownPrices == null ||
				bottleneck.portIndex >= market.knownPrices.Length)
			{
				return;
			}

			// The game offers mail only to a port whose price report this office has approved.
			PriceReport report = market.knownPrices[bottleneck.portIndex];
			if (report == null || !report.approved)
			{
				return;
			}

			AddMission(origin, bottleneck, missions);
		}

		private static void AddMission(Port origin, Port destination, List<Mission> missions)
		{
			var office = origin.GetComponent<IslandMissionOffice>();
			PrefabsDirectory directory = PrefabsDirectory.instance;
			if (destination == null ||
				destination == origin ||
				office == null ||
				directory == null ||
				CurrencyMarket.instance == null)
			{
				return;
			}

			float distance = Mission.GetDistance(origin, destination);
			ShipItem mail = directory.GetGood(RegularMailGoodIndex);
			Good mailGood = mail != null ? mail.GetComponent<Good>() : null;
			if (mailGood == null ||
				!PostalMail.IsWithinVanillaReputationRange(origin, distance) ||
				PostalMail.PlayerHasMission(origin, destination, mail.gameObject))
			{
				return;
			}

			var mission = GenerateMailMissionMethod.Invoke(
				office,
				new object[] { mailGood, destination, distance, 0 }) as Mission;
			if (mission == null)
			{
				return;
			}

			mission.totalPrice = CurrencyMarket.instance.GetSellPriceInCurrency(
				(Currency)destination.region,
				mission.totalPrice,
				false);
			mission.pricePerKm = mission.totalPrice / mission.distance;
			missions.Add(mission);
		}

		private static Port GetBottleneck()
		{
			if (Port.ports == null ||
				SailwindPortIndex.BetterPortsBottleneck >= Port.ports.Length)
			{
				return null;
			}

			Port port = Port.ports[SailwindPortIndex.BetterPortsBottleneck];
			return port != null && port.name == BottleneckObjectName ? port : null;
		}

		private static Port GetTamarack()
		{
			if (!TamarackLoaded || Port.ports == null)
			{
				return null;
			}

			if (!IsTamarack(tamarackIndex))
			{
				tamarackIndex = -1;
				for (int i = 0; i < Port.ports.Length && tamarackIndex < 0; i++)
				{
					if (IsTamarack(i))
					{
						tamarackIndex = i;
					}
				}
			}

			return tamarackIndex >= 0 ? Port.ports[tamarackIndex] : null;
		}

		private static bool IsTamarack(int index)
		{
			return index >= 0 &&
				index < Port.ports.Length &&
				Port.ports[index] != null &&
				Port.ports[index].GetPortName() == TamarackPortName;
		}

		private static bool Contains(Port[] ports, Port port)
		{
			if (ports == null)
			{
				return false;
			}

			foreach (Port candidate in ports)
			{
				if (candidate == port)
				{
					return true;
				}
			}

			return false;
		}
	}

	internal static class PostalExpressMail
	{
		private const float ExpressWorldDistance = 140f;
		private const float ExpressMailDueDayBaseSpeed = 2.6f;
		private const float ExpressRewardMultiplier = 1.5f;
		private const float HardPortRewardMultiplier = 2f;
		private const float ComparableRegularMailPackageMultiplier = 2f;
		internal const string DisplayName = "Express Mail";
		private const string ExpressMailSealTextureFile = "express_mail_seal.png";
		private const string ExpressMailSealObjectName = "postal_expansion_express_mail_seal";
		private const float ExpressMailSealSize = 0.32f;

		private static readonly Vector3 ExpressMailSealLocalPosition =
			new Vector3(0.303f, 0.215f, 0.171f);

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

		// The item template: a copy of the game's mail kept off its prefab list, so it saves as mail.
		internal static bool EnsureTemplate()
		{
			if (expressMailPrefab != null)
			{
				return true;
			}

			GameObject regularMailPrefab = PostalPrefabRegistration.GetRegularMail();
			if (regularMailPrefab == null || !HasRequiredComponents(regularMailPrefab))
			{
				Debug.LogWarning("Postal Expansion: regular mail prefab 221 was not found.");
				return false;
			}

			expressMailPrefab = PostalPrefabRegistration.CreateTemplate(
				regularMailPrefab,
				"postal_expansion_express_mail");
			expressMailPrefab.SetActive(true);
			Configure(expressMailPrefab);
			return true;
		}

		// Turns a copy of the game's mail into Express Mail: the template, or a mail item loaded from a save for an Express mission.
		internal static void Configure(GameObject item)
		{
			item.GetComponent<ShipItem>().name = DisplayName;
			item.GetComponent<Good>().requiredRepLevel = 3;
			ExpressMailSeal.Ensure(item, ExpressMailSealLocalPosition, Quaternion.identity);
			if (item.GetComponent<ExpressMailItem>() == null)
			{
				item.AddComponent<ExpressMailItem>();
			}
		}

		internal static void AddExpressMissions(Port origin, List<Mission> missions)
		{
			if (origin == null ||
				missions == null ||
				!EnsureTemplate() ||
				Port.ports == null ||
				origin.portIndex == SailwindPortIndex.SaffronIsland ||
				origin.portIndex == SailwindPortIndex.TestPort)
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

			// The day's draw is made from every destination, so it is the same before and after an accept; the destinations the player
			// already holds an Express from this port to are left out afterwards.
			generatedMissions.RemoveAll(mission =>
				PostalMail.PlayerHasMission(origin, mission.destinationPort, expressMailPrefab));
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
				if (!hardPort || reputationLevel >= 4)
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

	internal static class UrgentExpressMail
	{
		internal const int RequiredReputation = 6;
		internal const float RewardMultiplier = 2f;

		private const float DueDayBaseSpeed = 3.2f;
		private const string NamePrefix = "Urgent ";
		private const string ActiveMissionDataKey = "PostalExpansion.UrgentExpressMail.Active.v1";
		private const string ClaimedOfferDataKey = "PostalExpansion.UrgentExpressMail.Claimed.v1";

		private static readonly DailySpecialMissionState State =
			new DailySpecialMissionState(
				ActiveMissionDataKey,
				ClaimedOfferDataKey,
				SerializeActiveMission,
				MatchesSavedMission,
				ApplyMissionName);

		internal static bool IsUrgent(Mission mission)
		{
			return State.IsMarked(mission);
		}

		internal static bool CanOfferAt(Port origin)
		{
			return State.CanOfferAt(origin);
		}

		internal static void MarkGenerated(Mission mission)
		{
			if (!PostalMail.IsExpressMission(mission) || IsUrgent(mission))
			{
				return;
			}

			Good expressGood = mission.goodPrefab.GetComponent<Good>();
			if (expressGood == null)
			{
				return;
			}

			MarkRuntime(mission);
			mission.totalPrice = Mathf.RoundToInt(mission.totalPrice * RewardMultiplier);
			mission.dueDay = MailDueDateCalculator.Calculate(
				mission.originPort,
				mission.destinationPort,
				expressGood,
				DueDayBaseSpeed);
			mission.pricePerKm = mission.totalPrice / mission.distance;
		}

		internal static void MissionAccepted(Mission mission)
		{
			State.MissionAccepted(mission);
		}

		internal static void SavePersistentState()
		{
			State.SavePersistentState();
		}

		internal static void LoadPersistentState()
		{
			State.LoadPersistentState();
		}

		internal static void ResetRuntimeState()
		{
			State.ResetRuntimeState();
		}

		private static void MarkRuntime(Mission mission)
		{
			State.Mark(mission);
			ApplyMissionName(mission);
		}

		private static void ApplyMissionName(Mission mission)
		{
			if (string.IsNullOrEmpty(mission.missionName))
			{
				mission.missionName = NamePrefix.TrimEnd();
			}
			else if (!mission.missionName.StartsWith(NamePrefix, StringComparison.Ordinal))
			{
				mission.missionName = NamePrefix + mission.missionName;
			}
		}

		private static string SerializeActiveMission(int slot, Mission mission)
		{
			return string.Join(",",
				slot,
				mission.originPort.portIndex,
				mission.destinationPort.portIndex);
		}

		private static bool MatchesSavedMission(
			Mission mission,
			string[] fields)
		{
			return fields.Length == 3 &&
				int.TryParse(fields[1], out int originIndex) &&
				int.TryParse(fields[2], out int destinationIndex) &&
				PostalMail.IsExpressMission(mission) &&
				mission.originPort.portIndex == originIndex &&
				mission.destinationPort.portIndex == destinationIndex;
		}
	}

	internal static class GoldenDeliveryMissions
	{
		internal const string DisplayName = "Golden Delivery";
		internal const int RequiredReputation = 9;
		internal const float MoneyMultiplier = 10f;
		internal const float ReputationMultiplier = 15f;
		internal const int SeverelyLatePenalty = -50000;

		private const float SpawnChance = 0.5f;
		private const float MinimumDistance = 288f;
		private const float DueDayBaseSpeed = 4.0f;
		private const float ComparableRegularMailPackageMultiplier = 2f;
		private const string ActiveMissionDataKey =
			"PostalExpansion.GoldenDelivery.Active.v1";
		private const string ClaimedOfferDataKey =
			"PostalExpansion.GoldenDelivery.Claimed.v1";

		private static readonly DailySpecialMissionState State =
			new DailySpecialMissionState(
				ActiveMissionDataKey,
				ClaimedOfferDataKey,
				SerializeActiveMission,
				MatchesSavedMission,
				ApplyMissionName);

		internal static bool IsGolden(Mission mission)
		{
			return State.IsMarked(mission);
		}

		internal static void AddMission(Port origin, List<Mission> missions)
		{
			if (!CanOfferAt(origin) || missions == null)
			{
				return;
			}

			if (!PostalExpressMail.EnsureTemplate())
			{
				return;
			}

			var random = new System.Random(GetDailySeed(origin));
			if (random.NextDouble() >= SpawnChance)
			{
				return;
			}

			List<DestinationCandidate> candidates = GetCandidates(origin);
			if (candidates.Count == 0)
			{
				return;
			}

			DestinationCandidate selected = candidates[random.Next(candidates.Count)];
			Mission mission = GenerateMission(origin, selected);
			MarkRuntime(mission);
			missions.Add(mission);
		}

		internal static void MissionAccepted(Mission mission)
		{
			State.MissionAccepted(mission);
		}

		internal static void SavePersistentState()
		{
			State.SavePersistentState();
		}

		internal static void LoadPersistentState()
		{
			State.LoadPersistentState();
		}

		internal static void ResetRuntimeState()
		{
			State.ResetRuntimeState();
		}

		private static bool CanOfferAt(Port origin)
		{
			if (origin == null ||
				origin.portIndex == SailwindPortIndex.SaffronIsland ||
				origin.portIndex == SailwindPortIndex.TestPort ||
				PlayerReputation.GetRepLevel(origin.region) < RequiredReputation)
			{
				return false;
			}

			return State.CanOfferAt(origin);
		}

		private static List<DestinationCandidate> GetCandidates(Port origin)
		{
			var candidates = new List<DestinationCandidate>();
			if (Port.ports == null)
			{
				return candidates;
			}

			foreach (Port destination in Port.ports)
			{
				if (destination == null ||
					destination == origin ||
					destination.portIndex == SailwindPortIndex.TestPort ||
					PlayerAlreadyHasGoldenMission(origin, destination))
				{
					continue;
				}

				float distance = Mission.GetDistance(origin, destination);
				if (distance > MinimumDistance)
				{
					candidates.Add(new DestinationCandidate(destination, distance));
				}
			}

			return candidates;
		}

		private static Mission GenerateMission(
			Port origin,
			DestinationCandidate candidate)
		{
			Good expressGood = PostalExpressMail.Prefab.GetComponent<Good>();
			float distanceReward =
				candidate.Distance * DebugMarketTracker.instance.missionDistanceFee;
			int totalPrice = Mathf.RoundToInt(
				distanceReward *
					ComparableRegularMailPackageMultiplier *
					Plugin.RegularMailRewardMultiplier.Value *
					MoneyMultiplier);
			totalPrice = CurrencyMarket.instance.GetSellPriceInCurrency(
				(Currency)candidate.Destination.region,
				totalPrice,
				false);

			int dueDay = MailDueDateCalculator.Calculate(
				origin,
				candidate.Destination,
				expressGood,
				DueDayBaseSpeed);
			var mission = new Mission(
				origin,
				candidate.Destination,
				PostalExpressMail.Prefab,
				1,
				totalPrice,
				1f,
				0,
				dueDay);
			mission.pricePerKm = mission.totalPrice / mission.distance;
			return mission;
		}

		private static void MarkRuntime(Mission mission)
		{
			State.Mark(mission);
			ApplyMissionName(mission);
		}

		private static void ApplyMissionName(Mission mission)
		{
			mission.missionName =
				DisplayName + " to " + mission.destinationPort.GetPortName();
		}

		private static bool PlayerAlreadyHasGoldenMission(
			Port origin,
			Port destination)
		{
			if (PlayerMissions.missions == null)
			{
				return false;
			}

			foreach (Mission mission in PlayerMissions.missions)
			{
				if (IsGolden(mission) &&
					mission.originPort == origin &&
					mission.destinationPort == destination)
				{
					return true;
				}
			}

			return false;
		}

		private static int GetDailySeed(Port origin)
		{
			return ((1907 + GameState.day) * 31 + origin.portIndex) * 31 +
				RequiredReputation;
		}

		private static string SerializeActiveMission(int slot, Mission mission)
		{
			return string.Join(",",
				slot,
				mission.originPort.portIndex,
				mission.destinationPort.portIndex,
				mission.dueDay,
				mission.totalPrice);
		}

		private static bool MatchesSavedMission(
			Mission mission,
			string[] fields)
		{
			return fields.Length == 5 &&
				int.TryParse(fields[1], out int originIndex) &&
				int.TryParse(fields[2], out int destinationIndex) &&
				int.TryParse(fields[3], out int dueDay) &&
				int.TryParse(fields[4], out int totalPrice) &&
				PostalMail.IsExpressMission(mission) &&
				mission.goodCount == 1 &&
				mission.originPort.portIndex == originIndex &&
				mission.destinationPort.portIndex == destinationIndex &&
				mission.dueDay == dueDay &&
				mission.totalPrice == totalPrice;
		}

		private readonly struct DestinationCandidate
		{
			internal DestinationCandidate(Port destination, float distance)
			{
				Destination = destination;
				Distance = distance;
			}

			internal Port Destination { get; }
			internal float Distance { get; }
		}
	}

	internal sealed class DailySpecialMissionState
	{
		private readonly string activeMissionDataKey;
		private readonly string claimedOfferDataKey;
		private readonly Func<int, Mission, string> serializeActiveMission;
		private readonly Func<Mission, string[], bool> matchesSavedMission;
		private readonly Action<Mission> onMissionRestored;

		private ConditionalWeakTable<Mission, Marker> markedMissions =
			new ConditionalWeakTable<Mission, Marker>();
		private readonly HashSet<string> claimedOffers =
			new HashSet<string>(StringComparer.Ordinal);

		internal DailySpecialMissionState(
			string activeMissionDataKey,
			string claimedOfferDataKey,
			Func<int, Mission, string> serializeActiveMission,
			Func<Mission, string[], bool> matchesSavedMission,
			Action<Mission> onMissionRestored)
		{
			this.activeMissionDataKey = activeMissionDataKey;
			this.claimedOfferDataKey = claimedOfferDataKey;
			this.serializeActiveMission = serializeActiveMission;
			this.matchesSavedMission = matchesSavedMission;
			this.onMissionRestored = onMissionRestored;
		}

		internal bool IsMarked(Mission mission)
		{
			return mission != null && markedMissions.TryGetValue(mission, out _);
		}

		internal void Mark(Mission mission)
		{
			if (mission != null)
			{
				markedMissions.GetValue(mission, _ => new Marker());
			}
		}

		internal bool CanOfferAt(Port origin)
		{
			if (origin == null)
			{
				return false;
			}

			PruneClaimsForCurrentDay();
			return !claimedOffers.Contains(
				GetClaimKey(GameState.day, origin.portIndex));
		}

		internal void MissionAccepted(Mission mission)
		{
			if (!IsMarked(mission) ||
				mission.originPort == null ||
				mission.missionIndex < 0 ||
				PlayerMissions.missions == null ||
				mission.missionIndex >= PlayerMissions.missions.Length ||
				PlayerMissions.missions[mission.missionIndex] != mission)
			{
				return;
			}

			PruneClaimsForCurrentDay();
			claimedOffers.Add(
				GetClaimKey(GameState.day, mission.originPort.portIndex));
		}

		internal void SavePersistentState()
		{
			if (GameState.modData == null)
			{
				GameState.modData = new Dictionary<string, string>();
			}

			var activeEntries = new List<string>();
			if (PlayerMissions.missions != null)
			{
				for (int i = 0; i < PlayerMissions.missions.Length; i++)
				{
					Mission mission = PlayerMissions.missions[i];
					if (!IsMarked(mission) ||
						mission.originPort == null ||
						mission.destinationPort == null)
					{
						continue;
					}

					string entry = serializeActiveMission(i, mission);
					if (!string.IsNullOrEmpty(entry))
					{
						activeEntries.Add(entry);
					}
				}
			}

			GameState.modData[activeMissionDataKey] =
				string.Join(";", activeEntries);
			PruneClaimsForCurrentDay();
			var claims = new List<string>(claimedOffers);
			claims.Sort(StringComparer.Ordinal);
			GameState.modData[claimedOfferDataKey] = string.Join(";", claims);
		}

		internal void LoadPersistentState()
		{
			ResetRuntimeState();
			if (GameState.modData == null)
			{
				return;
			}

			LoadClaims();
			LoadActiveMissions();
		}

		internal void ResetRuntimeState()
		{
			markedMissions = new ConditionalWeakTable<Mission, Marker>();
			claimedOffers.Clear();
		}

		private void LoadClaims()
		{
			if (!GameState.modData.TryGetValue(
					claimedOfferDataKey,
					out string claimData))
			{
				return;
			}

			string currentDayPrefix = GameState.day + ",";
			foreach (string claim in claimData.Split(
				new[] { ';' },
				StringSplitOptions.RemoveEmptyEntries))
			{
				if (claim.StartsWith(currentDayPrefix, StringComparison.Ordinal))
				{
					claimedOffers.Add(claim);
				}
			}
		}

		private void LoadActiveMissions()
		{
			if (PlayerMissions.missions == null ||
				!GameState.modData.TryGetValue(
					activeMissionDataKey,
					out string missionData))
			{
				return;
			}

			foreach (string entry in missionData.Split(
				new[] { ';' },
				StringSplitOptions.RemoveEmptyEntries))
			{
				string[] fields = entry.Split(',');
				if (fields.Length == 0 ||
					!int.TryParse(fields[0], out int slot) ||
					slot < 0 ||
					slot >= PlayerMissions.missions.Length)
				{
					continue;
				}

				Mission mission = PlayerMissions.missions[slot];
				if (mission != null &&
					mission.missionIndex == slot &&
					mission.originPort != null &&
					mission.destinationPort != null &&
					matchesSavedMission(mission, fields))
				{
					Mark(mission);
					onMissionRestored?.Invoke(mission);
				}
			}
		}

		private static string GetClaimKey(int day, int originPortIndex)
		{
			return day + "," + originPortIndex;
		}

		private void PruneClaimsForCurrentDay()
		{
			string currentDayPrefix = GameState.day + ",";
			claimedOffers.RemoveWhere(
				claim => !claim.StartsWith(
					currentDayPrefix,
					StringComparison.Ordinal));
		}

		private sealed class Marker
		{
		}
	}

	[HarmonyPatch(typeof(Mission), "GetDeliveryPrice")]
	internal static class MailDeliveryPricePatch
	{
		private static bool Prefix(Mission __instance, ref int __result)
		{
			if (GoldenDeliveryMissions.IsGolden(__instance))
			{
				int goldenDaysLate = Mathf.Max(
					0,
					GameState.day - __instance.dueDay);
				__result = goldenDaysLate == 0
					? __instance.totalPrice / Mathf.Max(1, __instance.goodCount)
					: 0;
				return false;
			}

			if (AnonymousLetterMissions.TryGetDefinition(__instance, out _))
			{
				int anonymousDaysLate = Mathf.Max(
					0,
					GameState.day - __instance.dueDay);
				__result = AnonymousLetterMissions.GetGoldReward(
					anonymousDaysLate);
				return false;
			}

			if (RegisteredLetterMissions.TryGetDefinition(__instance, out _))
			{
				__result = RegisteredLetterMissions.FixedGoldReward;
				return false;
			}

			if (!UrgentExpressMail.IsUrgent(__instance))
			{
				return true;
			}

			int daysLate = Mathf.Max(0, GameState.day - __instance.dueDay);
			float payoutFraction;
			if (daysLate == 0)
			{
				payoutFraction = 1f;
			}
			else if (daysLate == 1)
			{
				payoutFraction = 0.5f;
			}
			else
			{
				payoutFraction = 0.05f;
			}

			int rewardPerPackage = __instance.totalPrice / __instance.goodCount;
			__result = Mathf.RoundToInt(rewardPerPackage * payoutFraction);
			return false;
		}
	}

	[HarmonyPatch(typeof(Mission), "GetDeliveryRep")]
	internal static class MailDeliveryRepPatch
	{
		private const int SeverelyLateExpressMissionPenalty = -100;
		private const int SeverelyLateUrgentMissionPenalty = -200;

		private static bool Prefix(Mission __instance, ref int __result)
		{
			if (__instance == null || __instance.goodPrefab == null)
			{
				return true;
			}

			SaveablePrefab saveable = __instance.goodPrefab.GetComponent<SaveablePrefab>();
			if (saveable == null)
			{
				return true;
			}

			if (GoldenDeliveryMissions.IsGolden(__instance))
			{
				int goldenDaysLate =
					Mathf.Max(0, GameState.day - __instance.dueDay);
				if (goldenDaysLate >= 2)
				{
					__result = GoldenDeliveryMissions.SeverelyLatePenalty;
				}
				else if (goldenDaysLate == 1)
				{
					__result = 0;
				}
				else
				{
					__result = Mathf.RoundToInt(
						__instance.distance /
							Mathf.Max(1, __instance.goodCount) *
							Plugin.RegularMailReputationMultiplier.Value *
							GoldenDeliveryMissions.ReputationMultiplier);
				}

				return false;
			}

			if (PostalMail.IsRegisteredLetter(saveable))
			{
				int letterDaysLate =
					Mathf.Max(0, GameState.day - __instance.dueDay);
				if (letterDaysLate >= 2)
				{
					__result =
						LetterMissions.GetSeverelyLatePenalty(__instance);
				}
				else if (letterDaysLate == 1)
				{
					__result = 0;
				}
				else
				{
					__result = Mathf.RoundToInt(
						__instance.distance /
						__instance.goodCount *
						Plugin.RegularMailReputationMultiplier.Value *
						LetterMissions.GetReputationMultiplier(__instance));
				}

				return false;
			}

			bool expressMail = PostalMail.IsExpressMail(saveable);
			bool urgentMail = expressMail && UrgentExpressMail.IsUrgent(__instance);
			float rewardMultiplier = Plugin.RegularMailReputationMultiplier.Value;
			if (expressMail)
			{
				rewardMultiplier *= PostalExpressMail.GetExpressRewardMultiplier(__instance.destinationPort);
				if (urgentMail)
				{
					rewardMultiplier *= UrgentExpressMail.RewardMultiplier;
				}
			}
			else if (saveable.prefabIndex != PostalMail.RegularMailItemIndex)
			{
				return true;
			}

			int daysLate = Mathf.Max(0, GameState.day - __instance.dueDay);
			float baseReputation =
				__instance.distance / __instance.goodCount * rewardMultiplier;

			if (!expressMail)
			{
				float latePenalty = Mathf.Min(daysLate * 0.2f, 1f);
				__result = Mathf.RoundToInt(
					baseReputation - baseReputation * latePenalty);
				return false;
			}

			if (urgentMail)
			{
				if (daysLate >= 2)
				{
					__result = SeverelyLateUrgentMissionPenalty;
				}
				else if (daysLate == 1)
				{
					__result = 0;
				}
				else
				{
					__result = Mathf.RoundToInt(baseReputation);
				}

				return false;
			}

			if (daysLate >= 3)
			{
				__result = SeverelyLateExpressMissionPenalty;
				return false;
			}

			float expressLatePenalty = Mathf.Min(daysLate * 0.5f, 1f);
			__result = Mathf.RoundToInt(
				baseReputation - baseReputation * expressLatePenalty);
			return false;
		}
	}
}
