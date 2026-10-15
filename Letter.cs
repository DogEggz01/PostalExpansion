using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace PostalExpansion
{
	internal static class LetterMissions
	{
		internal static bool TryGetDefinition(
			Mission mission,
			out LetterMissionDefinition definition)
		{
			if (AnonymousLetterMissions.TryGetDefinition(
					mission,
					out AnonymousLetterMissionDefinition anonymousDefinition))
			{
				definition = anonymousDefinition;
				return true;
			}

			if (RegisteredLetterMissions.TryGetDefinition(
					mission,
					out RegisteredLetterMissionDefinition registeredDefinition))
			{
				definition = registeredDefinition;
				return true;
			}

			definition = null;
			return false;
		}

		internal static void MissionDelivered(Mission mission)
		{
			AnonymousLetterMissions.MissionDelivered(mission);
			RegisteredLetterMissions.MissionDelivered(mission);
		}

		internal static int GetFixedGoldReward(Mission mission)
		{
			return AnonymousLetterMissions.TryGetDefinition(mission, out _)
				? AnonymousLetterMissions.FixedGoldReward
				: RegisteredLetterMissions.FixedGoldReward;
		}

		internal static float GetReputationMultiplier(Mission mission)
		{
			return AnonymousLetterMissions.TryGetDefinition(mission, out _)
				? AnonymousLetterMissions.ReputationMultiplier
				: RegisteredLetterMissions.ReputationMultiplier;
		}

		internal static int GetSeverelyLatePenalty(Mission mission)
		{
			return AnonymousLetterMissions.TryGetDefinition(mission, out _)
				? AnonymousLetterMissions.SeverelyLatePenalty
				: RegisteredLetterMissions.SeverelyLatePenalty;
		}
	}

	internal abstract class LetterMissionDefinition
	{
		protected LetterMissionDefinition(
			string id,
			string destinationPortName,
			int destinationPortIndex,
			Vector3 localPosition,
			Vector3 localEulerAngles,
			Vector3 triggerSize,
			LetterDeliveryWindow deliveryWindow,
			int[] spawnPortIndices,
			string locationDescription)
		{
			Id = id;
			DestinationPortName = destinationPortName;
			DestinationPortIndex = destinationPortIndex;
			LocalPosition = localPosition;
			LocalEulerAngles = localEulerAngles;
			TriggerSize = triggerSize;
			DeliveryWindow = deliveryWindow;
			SpawnPortIndices = spawnPortIndices;
			LocationDescription = locationDescription;
		}

		internal string Id { get; }
		internal string DestinationPortName { get; }
		internal int DestinationPortIndex { get; }
		internal Vector3 LocalPosition { get; }
		internal Vector3 LocalEulerAngles { get; }
		internal Vector3 TriggerSize { get; }
		internal LetterDeliveryWindow DeliveryWindow { get; }
		internal int[] SpawnPortIndices { get; }
		internal string LocationDescription { get; }
		internal virtual float DistanceToOffice =>
			new Vector2(LocalPosition.x, LocalPosition.z).magnitude;
		internal virtual bool UsePersistentWorldAnchor => false;
		internal virtual bool UseDeliveryLocationForRoute => false;
		internal virtual bool ShowDeliveryCoordinates => false;
		internal virtual bool HideDestination => false;

		internal abstract string GetDeliveryDialogue(Mission mission);

		protected static int GetOriginPortIndex(Mission mission)
		{
			return mission?.originPort?.portIndex ?? -1;
		}

		protected string UnexpectedOrigin(Mission mission)
		{
			Port origin = mission?.originPort;
			string originDescription = origin != null
				? origin.GetPortName() + " (" + origin.portIndex + ")"
				: "missing origin";

			Debug.LogWarning(
				"Postal Expansion: letter mission " + Id +
				" received unexpected origin " + originDescription + ".");
			return string.Empty;
		}
	}

	internal sealed class LetterMissionRegistry<TDefinition>
		where TDefinition : LetterMissionDefinition
	{
		private readonly List<TDefinition> definitions =
			new List<TDefinition>();

		internal LetterMissionRegistry(
			string categoryName,
			params TDefinition[] candidates)
		{
			var definitionIds = new HashSet<string>(StringComparer.Ordinal);
			foreach (TDefinition candidate in candidates)
			{
				if (candidate == null || string.IsNullOrEmpty(candidate.Id))
				{
					Debug.LogError(
						"Postal Expansion: ignored a " + categoryName +
						" mission without an ID.");
					continue;
				}

				if (!definitionIds.Add(candidate.Id))
				{
					Debug.LogError(
						"Postal Expansion: ignored duplicate " + categoryName +
						" mission ID " + candidate.Id + ".");
					continue;
				}

				definitions.Add(candidate);
			}
		}

		internal IReadOnlyList<TDefinition> All => definitions;
	}

	internal static class LetterMissionDefinitions
	{
		private static readonly IReadOnlyList<LetterMissionDefinition> Definitions =
			BuildDefinitions();

		internal static IReadOnlyList<LetterMissionDefinition> All => Definitions;

		private static IReadOnlyList<LetterMissionDefinition> BuildDefinitions()
		{
			var definitions = new List<LetterMissionDefinition>();
			foreach (RegisteredLetterMissionDefinition definition in
				RegisteredLetterMissionRegistry.All)
			{
				definitions.Add(definition);
			}

			foreach (AnonymousLetterMissionDefinition definition in
				AnonymousLetterMissionRegistry.All)
			{
				definitions.Add(definition);
			}

			return definitions.AsReadOnly();
		}
	}

	internal readonly struct LetterMissionSaveKeys
	{
		internal LetterMissionSaveKeys(string activeMissions, string claimedOffers)
		{
			ActiveMissions = activeMissions;
			ClaimedOffers = claimedOffers;
		}

		internal string ActiveMissions { get; }
		internal string ClaimedOffers { get; }
	}

	internal sealed class LetterMissionSet<TDefinition>
		where TDefinition : LetterMissionDefinition
	{
		private readonly string categoryName;
		private readonly IReadOnlyList<TDefinition> definitions;
		private readonly Dictionary<string, TDefinition> definitionsById;
		private readonly LetterMissionSaveKeys saveKeys;
		private readonly int requiredReputation;
		private readonly float dueDayBaseSpeed;
		private readonly int fixedGoldReward;
		private readonly Func<Port, string> getMissionName;

		private ConditionalWeakTable<Mission, MissionData> missionData =
			new ConditionalWeakTable<Mission, MissionData>();
		private readonly Dictionary<string, Mission> dailyOffers =
			new Dictionary<string, Mission>(StringComparer.Ordinal);
		private readonly HashSet<string> claimedOffers =
			new HashSet<string>(StringComparer.Ordinal);
		private int dailyOfferDay = int.MinValue;

		internal LetterMissionSet(
			string categoryName,
			IReadOnlyList<TDefinition> definitions,
			LetterMissionSaveKeys saveKeys,
			int requiredReputation,
			float dueDayBaseSpeed,
			int fixedGoldReward,
			Func<Port, string> getMissionName)
		{
			this.categoryName = categoryName;
			this.definitions = definitions;
			this.saveKeys = saveKeys;
			this.requiredReputation = requiredReputation;
			this.dueDayBaseSpeed = dueDayBaseSpeed;
			this.fixedGoldReward = fixedGoldReward;
			this.getMissionName = getMissionName;

			definitionsById = new Dictionary<string, TDefinition>(
				StringComparer.Ordinal);
			foreach (TDefinition definition in definitions)
			{
				definitionsById.Add(definition.Id, definition);
			}
		}

		internal void AddMissions(Port origin, List<Mission> missions)
		{
			if (origin == null || missions == null)
			{
				return;
			}

			EnsureDailyOffers();
			foreach (TDefinition definition in definitions)
			{
				if (!dailyOffers.TryGetValue(definition.Id, out Mission offer) ||
					offer == null ||
					offer.originPort != origin ||
					!CanOfferFrom(origin, offer.destinationPort) ||
					offer.missionIndex != -1 ||
					IsClaimed(definition.Id) ||
					HasActiveMission(definition.Id))
				{
					continue;
				}

				missions.Add(offer);
			}
		}

		internal List<string> GetDebugStatusLines()
		{
			EnsureDailyOffers();
			var lines = new List<string>();
			foreach (TDefinition definition in definitions)
			{
				string status;
				if (TryGetActiveMission(definition.Id, out Mission activeMission))
				{
					status = "active from " + GetPortName(activeMission.originPort);
				}
				else if (IsClaimed(definition.Id))
				{
					status = "already claimed today";
				}
				else if (dailyOffers.TryGetValue(
						definition.Id,
						out Mission offer) &&
					offer != null &&
					offer.missionIndex == -1 &&
					CanOfferFrom(offer.originPort, offer.destinationPort))
				{
					status = GetPortName(offer.originPort);
				}
				else
				{
					status = "not available today";
				}

				lines.Add(
					categoryName + " | " +
					definition.LocationDescription + ": " + status);
			}

			return lines;
		}

		internal bool TryGetDefinition(
			Mission mission,
			out TDefinition definition)
		{
			if (mission != null &&
				missionData.TryGetValue(mission, out MissionData data))
			{
				definition = data.Definition;
				return true;
			}

			definition = null;
			return false;
		}

		internal void MissionAccepted(Mission mission)
		{
			if (!TryGetDefinition(mission, out TDefinition definition) ||
				mission.missionIndex < 0 ||
				PlayerMissions.missions == null ||
				mission.missionIndex >= PlayerMissions.missions.Length ||
				PlayerMissions.missions[mission.missionIndex] != mission)
			{
				return;
			}

			claimedOffers.Add(GetClaimKey(GameState.day, definition.Id));
		}

		internal void MissionDelivered(Mission mission)
		{
			if (TryGetDefinition(mission, out TDefinition definition))
			{
				claimedOffers.Add(GetClaimKey(GameState.day, definition.Id));
			}
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
					if (!TryGetDefinition(mission, out TDefinition definition) ||
						mission.originPort == null ||
						mission.destinationPort == null)
					{
						continue;
					}

					activeEntries.Add(string.Join(",",
						i,
						mission.originPort.portIndex,
						mission.destinationPort.portIndex,
						definition.Id));
				}
			}

			GameState.modData[saveKeys.ActiveMissions] =
				string.Join(";", activeEntries);
			PruneClaimsForCurrentDay();
			var claims = new List<string>(claimedOffers);
			claims.Sort(StringComparer.Ordinal);
			GameState.modData[saveKeys.ClaimedOffers] = string.Join(";", claims);
		}

		internal void LoadPersistentState(
			params LetterMissionSaveKeys[] legacySaveKeys)
		{
			ResetRuntimeState();
			if (GameState.modData == null)
			{
				return;
			}

			LoadClaims(saveKeys);
			LoadActiveMissions(saveKeys);
			foreach (LetterMissionSaveKeys legacyKeys in legacySaveKeys)
			{
				LoadClaims(legacyKeys);
				LoadActiveMissions(legacyKeys);
			}
		}

		internal void ResetRuntimeState()
		{
			missionData = new ConditionalWeakTable<Mission, MissionData>();
			dailyOffers.Clear();
			claimedOffers.Clear();
			dailyOfferDay = int.MinValue;
		}

		private void LoadClaims(LetterMissionSaveKeys sourceKeys)
		{
			if (!GameState.modData.TryGetValue(
					sourceKeys.ClaimedOffers,
					out string claimData))
			{
				return;
			}

			string currentDayPrefix = GameState.day + ",";
			foreach (string claim in claimData.Split(
				new[] { ';' },
				StringSplitOptions.RemoveEmptyEntries))
			{
				if (!claim.StartsWith(currentDayPrefix, StringComparison.Ordinal))
				{
					continue;
				}

				string missionId = claim.Substring(currentDayPrefix.Length);
				if (definitionsById.ContainsKey(missionId))
				{
					claimedOffers.Add(claim);
				}
			}
		}

		private void LoadActiveMissions(LetterMissionSaveKeys sourceKeys)
		{
			if (PlayerMissions.missions == null ||
				!GameState.modData.TryGetValue(
					sourceKeys.ActiveMissions,
					out string activeData))
			{
				return;
			}

			foreach (string entry in activeData.Split(
				new[] { ';' },
				StringSplitOptions.RemoveEmptyEntries))
			{
				string[] fields = entry.Split(',');
				if (fields.Length != 4 ||
					!int.TryParse(fields[0], out int slot) ||
					!int.TryParse(fields[1], out int originIndex) ||
					!int.TryParse(fields[2], out int destinationIndex) ||
					slot < 0 ||
					slot >= PlayerMissions.missions.Length ||
					!definitionsById.TryGetValue(fields[3], out TDefinition definition))
				{
					continue;
				}

				Mission mission = PlayerMissions.missions[slot];
				if (IsLetterMission(mission) &&
					mission.missionIndex == slot &&
					mission.originPort != null &&
					mission.destinationPort != null &&
					mission.originPort.portIndex == originIndex &&
					mission.destinationPort.portIndex == destinationIndex &&
					definition.DestinationPortIndex == destinationIndex)
				{
					Attach(mission, definition);
					claimedOffers.Add(
						GetClaimKey(GameState.day, definition.Id));
				}
			}
		}

		private void EnsureDailyOffers()
		{
			if (dailyOfferDay == GameState.day)
			{
				return;
			}

			if (Port.ports == null || Letter.Prefab == null || Sun.sun == null)
			{
				return;
			}

			dailyOffers.Clear();
			dailyOfferDay = GameState.day;
			PruneClaimsForCurrentDay();

			foreach (TDefinition definition in definitions)
			{
				if (IsClaimed(definition.Id) || HasActiveMission(definition.Id))
				{
					continue;
				}

				Port destination = GetPort(definition.DestinationPortIndex);
				if (destination == null)
				{
					continue;
				}

				var eligibleOrigins = new List<Port>();
				foreach (int portIndex in definition.SpawnPortIndices)
				{
					Port origin = GetPort(portIndex);
					if (CanOfferFrom(origin, destination))
					{
						eligibleOrigins.Add(origin);
					}
				}

				if (eligibleOrigins.Count == 0)
				{
					continue;
				}

				ValidatePortName(definition, destination);
				var random = new Random(GetSelectionSeed(definition.Id));
				Port selectedOrigin =
					eligibleOrigins[random.Next(eligibleOrigins.Count)];
				dailyOffers[definition.Id] = CreateMission(
					selectedOrigin,
					destination,
					definition);
			}
		}

		private bool CanOfferFrom(Port origin, Port destination)
		{
			if (origin == null || destination == null ||
				PlayerReputation.GetRepLevel(origin.region) < requiredReputation)
			{
				return false;
			}

			float distance = Mission.GetDistance(origin, destination);
			return PostalMail.IsWithinVanillaReputationRange(origin, distance);
		}

		private Mission CreateMission(
			Port origin,
			Port destination,
			TDefinition definition)
		{
			Good letterGood = Letter.Prefab.GetComponent<Good>();
			float missionDistance = LetterMissionRoute.GetMissionDistance(
				origin,
				destination,
				definition);
			int dueDay = definition.UseDeliveryLocationForRoute
				? MailDueDateCalculator.CalculateFromMissionDistance(
					missionDistance,
					letterGood,
					dueDayBaseSpeed)
				: MailDueDateCalculator.Calculate(
					origin,
					destination,
					letterGood,
					dueDayBaseSpeed);

			var mission = new Mission(
				origin,
				destination,
				Letter.Prefab,
				1,
				fixedGoldReward,
				1f,
				0,
				dueDay);
			mission.missionName = getMissionName(destination);
			if (definition.UseDeliveryLocationForRoute)
			{
				mission.distance = missionDistance;
			}
			mission.pricePerKm = 0f;
			Attach(mission, definition);
			return mission;
		}

		private void Attach(Mission mission, TDefinition definition)
		{
			if (mission == null ||
				definition == null ||
				TryGetDefinition(mission, out _))
			{
				return;
			}

			missionData.Add(mission, new MissionData(definition));
			ApplyDeliveryRoute(mission, definition);
			mission.totalPrice = fixedGoldReward;
			mission.pricePerKm = 0f;
			mission.missionName = getMissionName(mission.destinationPort);
		}

		private void ApplyDeliveryRoute(Mission mission, TDefinition definition)
		{
			if (!definition.UseDeliveryLocationForRoute)
			{
				return;
			}

			float portDistance = Mission.GetDistance(
				mission.originPort,
				mission.destinationPort);
			float routeDistance = LetterMissionRoute.GetMissionDistance(
				mission.originPort,
				mission.destinationPort,
				definition);
			float oldRouteDifference = Mathf.Abs(mission.distance - portDistance);
			float newRouteDifference = Mathf.Abs(mission.distance - routeDistance);
			if (oldRouteDifference < newRouteDifference)
			{
				Good letterGood = mission.goodPrefab.GetComponent<Good>();
				int oldDueDay = MailDueDateCalculator.CalculateFromMissionDistance(
					portDistance,
					letterGood,
					dueDayBaseSpeed);
				int newDueDay = MailDueDateCalculator.CalculateFromMissionDistance(
					routeDistance,
					letterGood,
					dueDayBaseSpeed);
				mission.dueDay += newDueDay - oldDueDay;
			}

			mission.distance = routeDistance;
		}

		private bool HasActiveMission(string missionId)
		{
			return TryGetActiveMission(missionId, out _);
		}

		private bool TryGetActiveMission(
			string missionId,
			out Mission activeMission)
		{
			activeMission = null;
			if (PlayerMissions.missions == null)
			{
				return false;
			}

			foreach (Mission mission in PlayerMissions.missions)
			{
				if (TryGetDefinition(mission, out TDefinition definition) &&
					string.Equals(definition.Id, missionId, StringComparison.Ordinal))
				{
					activeMission = mission;
					return true;
				}
			}

			return false;
		}

		private static string GetPortName(Port port)
		{
			return port != null ? port.GetPortName() : "unknown port";
		}

		private static bool IsLetterMission(Mission mission)
		{
			if (mission == null || mission.goodPrefab == null)
			{
				return false;
			}

			SaveablePrefab saveable =
				mission.goodPrefab.GetComponent<SaveablePrefab>();
			return PostalMail.IsRegisteredLetter(saveable);
		}

		private static Port GetPort(int portIndex)
		{
			return Port.ports != null &&
				portIndex >= 0 &&
				portIndex < Port.ports.Length
					? Port.ports[portIndex]
					: null;
		}

		private void ValidatePortName(TDefinition definition, Port destination)
		{
			if (!string.Equals(
					definition.DestinationPortName,
					destination.GetPortName(),
					StringComparison.Ordinal))
			{
				Debug.LogWarning(
					"Postal Expansion: " + categoryName + " mission " +
					definition.Id + " expects port " +
					definition.DestinationPortName + " but index " +
					definition.DestinationPortIndex + " is " +
					destination.GetPortName() + ".");
			}
		}

		private static int GetSelectionSeed(string missionId)
		{
			unchecked
			{
				int hash = 17;
				foreach (char character in missionId)
				{
					hash = hash * 31 + character;
				}

				return (GameState.day + 761) * 31 + hash;
			}
		}

		private bool IsClaimed(string missionId)
		{
			return claimedOffers.Contains(GetClaimKey(GameState.day, missionId));
		}

		private static string GetClaimKey(int day, string missionId)
		{
			return day + "," + missionId;
		}

		private void PruneClaimsForCurrentDay()
		{
			string currentDayPrefix = GameState.day + ",";
			claimedOffers.RemoveWhere(
				claim => !claim.StartsWith(
					currentDayPrefix,
					StringComparison.Ordinal));
		}

		private sealed class MissionData
		{
			internal MissionData(TDefinition definition)
			{
				Definition = definition;
			}

			internal TDefinition Definition { get; }
		}
	}

	internal static class LetterMissionRoute
	{
		internal static float GetMissionDistance(
			Port origin,
			Port destination,
			LetterMissionDefinition definition)
		{
			float portDistance = Mission.GetDistance(origin, destination);
			if (!definition.UseDeliveryLocationForRoute ||
				destination == null ||
				destination.portIndex != definition.DestinationPortIndex)
			{
				return portDistance;
			}

			return portDistance + definition.DistanceToOffice / 100f;
		}
	}

	internal readonly struct LetterDeliveryWindow
	{
		internal static readonly LetterDeliveryWindow Day =
			new LetterDeliveryWindow(7f, 18f, "07-18");
		internal static readonly LetterDeliveryWindow Night =
			new LetterDeliveryWindow(19f, 5f, "19 - 05");

		internal LetterDeliveryWindow(
			float startHour,
			float endHour,
			string displayHours)
		{
			StartHour = startHour;
			EndHour = endHour;
			DisplayHours = displayHours;
		}

		internal float StartHour { get; }
		internal float EndHour { get; }
		internal string DisplayHours { get; }
	}

	internal static class LetterDeliveryHours
	{
		internal static bool IsOpen(LetterDeliveryWindow window)
		{
			if (Sun.sun == null)
			{
				return false;
			}

			float localTime = Sun.sun.localTime;
			return window.StartHour <= window.EndHour
				? localTime >= window.StartHour && localTime <= window.EndHour
				: localTime >= window.StartHour || localTime <= window.EndHour;
		}

		internal static string GetDisplayHours(LetterDeliveryWindow window)
		{
			return window.DisplayHours;
		}
	}

	internal sealed class LetterDeliveryArea : MonoBehaviour
	{
		private Port port;
		private LetterMissionDefinition definition;
		private LetterDialoguePresenceArea dialogueArea;
		private Good waitingLetter;
		private bool closedNoticeShown;

		internal void Initialize(
			Port destinationPort,
			LetterMissionDefinition missionDefinition,
			LetterDialoguePresenceArea presenceArea)
		{
			port = destinationPort;
			definition = missionDefinition;
			dialogueArea = presenceArea;
		}

		private void OnTriggerEnter(Collider other)
		{
			Good letter = other.GetComponentInParent<Good>();
			if (!IsLetter(letter))
			{
				return;
			}

			if (!TryGetMatchingMission(letter, out _))
			{
				ShowNotification("Wrong Letter!");
				return;
			}

			waitingLetter = letter;
			closedNoticeShown = false;
			TryDeliver();
		}

		private void OnTriggerExit(Collider other)
		{
			Good letter = other.GetComponentInParent<Good>();
			if (letter == waitingLetter)
			{
				waitingLetter = null;
				closedNoticeShown = false;
			}
		}

		private void Update()
		{
			if (waitingLetter != null &&
				LetterDeliveryHours.IsOpen(definition.DeliveryWindow))
			{
				TryDeliver();
			}
		}

		private void TryDeliver()
		{
			if (waitingLetter == null ||
				!TryGetMatchingMission(waitingLetter, out Mission mission))
			{
				waitingLetter = null;
				return;
			}

			if (!LetterDeliveryHours.IsOpen(definition.DeliveryWindow))
			{
				if (!closedNoticeShown)
				{
					closedNoticeShown = true;
					ShowNotification("Wrong Time!");
				}

				return;
			}

			Good deliveredLetter = waitingLetter;
			waitingLetter = null;
			closedNoticeShown = false;
			string dialogueText = definition.GetDeliveryDialogue(mission);
			LetterMissions.MissionDelivered(mission);
			deliveredLetter.Deliver();
			dialogueArea?.ShowForDelivery(dialogueText);
		}

		private bool TryGetMatchingMission(Good letter, out Mission mission)
		{
			mission = null;
			if (letter == null || PlayerMissions.missions == null)
			{
				return false;
			}

			int missionIndex = letter.GetMissionIndex();
			if (missionIndex < 0 || missionIndex >= PlayerMissions.missions.Length)
			{
				return false;
			}

			mission = PlayerMissions.missions[missionIndex];
			return mission != null &&
				mission.missionIndex == missionIndex &&
				mission.destinationPort == port &&
				LetterMissions.TryGetDefinition(
					mission,
					out LetterMissionDefinition assignedDefinition) &&
				string.Equals(
					assignedDefinition.Id,
					definition.Id,
					StringComparison.Ordinal);
		}

		private static bool IsLetter(Good good)
		{
			SaveablePrefab saveable =
				good != null ? good.GetComponent<SaveablePrefab>() : null;
			return PostalMail.IsRegisteredLetter(saveable);
		}

		private static void ShowNotification(string message)
		{
			if (NotificationUi.instance != null)
			{
				NotificationUi.instance.ShowNotification(message);
			}
		}
	}

	[HarmonyPatch(typeof(PortDude), "Awake")]
	internal static class LetterDeliveryAreaCreationPatch
	{
		private const string AreaRootPrefix =
			"postal_expansion_letter_";

		private static void Postfix(PortDude __instance)
		{
			if (__instance == null || __instance.GetPort() == null)
			{
				return;
			}

			foreach (LetterMissionDefinition definition in
				LetterMissionDefinitions.All)
			{
				if (definition.DestinationPortIndex ==
					__instance.GetPort().portIndex)
				{
					EnsureArea(__instance, definition);
				}
			}
		}

		private static void EnsureArea(
			PortDude portDude,
			LetterMissionDefinition definition)
		{
			string rootName = AreaRootPrefix + definition.Id;
			Transform parent = GetAreaParent(portDude, definition);
			if (parent == null || parent.Find(rootName) != null)
			{
				return;
			}

			var root = new GameObject(rootName);
			if (definition.UsePersistentWorldAnchor)
			{
				root.transform.position =
					portDude.transform.TransformPoint(definition.LocalPosition);
				root.transform.rotation =
					portDude.transform.rotation *
					Quaternion.Euler(definition.LocalEulerAngles);
				root.transform.SetParent(parent, true);
			}
			else
			{
				root.transform.SetParent(parent, false);
				root.transform.localPosition = definition.LocalPosition;
				root.transform.localRotation =
					Quaternion.Euler(definition.LocalEulerAngles);
			}

			LetterDialoguePresenceArea dialogueArea =
				CreateDialogueArea(root, definition);
			CreateDeliveryArea(
				root,
				portDude.GetPort(),
				definition,
				dialogueArea);

			if (definition.UsePersistentWorldAnchor)
			{
				PersistentLetterDeliveryAnchor anchor =
					root.AddComponent<PersistentLetterDeliveryAnchor>();
				anchor.Initialize(
					portDude.transform,
					definition.LocalPosition,
					definition.LocalEulerAngles);
			}
		}

		private static Transform GetAreaParent(
			PortDude portDude,
			LetterMissionDefinition definition)
		{
			if (!definition.UsePersistentWorldAnchor)
			{
				return portDude.transform;
			}

			if (Refs.shiftingWorld != null)
			{
				return Refs.shiftingWorld;
			}

			FloatingOriginManager manager =
				Object.FindObjectOfType<FloatingOriginManager>();
			if (manager != null)
			{
				return manager.transform;
			}

			Debug.LogWarning(
				"Postal Expansion: Cannot create persistent delivery area " +
				definition.Id + "; shifting world is unavailable.");
			return null;
		}

		internal static bool TryGetAreaTransform(
			LetterMissionDefinition definition,
			out Transform areaTransform)
		{
			areaTransform = null;
			if (definition == null)
			{
				return false;
			}

			Transform parent = null;
			if (definition.UsePersistentWorldAnchor)
			{
				parent = Refs.shiftingWorld;
			}
			else if (Port.ports != null &&
				definition.DestinationPortIndex >= 0 &&
				definition.DestinationPortIndex < Port.ports.Length)
			{
				Port destination = Port.ports[definition.DestinationPortIndex];
				PortDude portDude = destination != null
					? destination.GetDude()
					: null;
				parent = portDude != null ? portDude.transform : null;
			}

			areaTransform = parent != null
				? parent.Find(AreaRootPrefix + definition.Id)
				: null;
			return areaTransform != null;
		}

		private static LetterDialoguePresenceArea CreateDialogueArea(
			GameObject root,
			LetterMissionDefinition definition)
		{
			var areaObject = new GameObject(
				"postal_expansion_letter_dialogue_" + definition.Id);
			areaObject.layer = 2;
			areaObject.transform.SetParent(root.transform, false);

			BoxCollider collider = areaObject.AddComponent<BoxCollider>();
			collider.isTrigger = true;
			collider.size = definition.TriggerSize * 5f;
			AddTriggerRigidbody(areaObject);

			LetterDialoguePresenceArea area =
				areaObject.AddComponent<LetterDialoguePresenceArea>();
			area.Initialize(definition);
			return area;
		}

		private static void CreateDeliveryArea(
			GameObject root,
			Port port,
			LetterMissionDefinition definition,
			LetterDialoguePresenceArea dialogueArea)
		{
			var areaObject = new GameObject(
				"postal_expansion_letter_delivery_" + definition.Id);
			areaObject.layer = 2;
			areaObject.transform.SetParent(root.transform, false);

			BoxCollider collider = areaObject.AddComponent<BoxCollider>();
			collider.isTrigger = true;
			collider.size = definition.TriggerSize;
			AddTriggerRigidbody(areaObject);

			LetterDeliveryArea area = areaObject.AddComponent<LetterDeliveryArea>();
			area.Initialize(port, definition, dialogueArea);
		}

		private static void AddTriggerRigidbody(GameObject areaObject)
		{
			Rigidbody body = areaObject.AddComponent<Rigidbody>();
			body.isKinematic = true;
			body.useGravity = false;
		}
	}

	internal sealed class LetterDeliveryDialogue
	{
		private const string BubbleGraphicsName = "gfx";
		private const string BubbleReferenceText =
			"Share a drink with a fellow sailor?";
		private const int DialogueWrapCharacterLimit = 40;
		private const float ObserverOffset = 0.75f;
		private const float HeadHeightOffset = 0.5f;
		private const float MinimumBubbleWidthMultiplier = 0.35f;
		private const float MaximumBubbleWidthMultiplier = 1.8f;

		private static bool missingBubbleGraphicsWarningLogged;

		private readonly Transform anchor;
		private readonly int portIndex;
		private bool missingTemplateWarningLogged;
		private GameObject panel;
		private TextMesh text;
		private Transform bubbleGraphics;
		private Vector3 baseBubbleScale;
		private float baseTextWidth;

		internal LetterDeliveryDialogue(Transform dialogueAnchor, int destinationPortIndex)
		{
			anchor = dialogueAnchor;
			portIndex = destinationPortIndex;
		}

		internal bool Show(string dialogueText)
		{
			if (!EnsureCreated())
			{
				return false;
			}

			string wrappedText = Wrap(
				dialogueText,
				DialogueWrapCharacterLimit);
			text.text = wrappedText;
			ResizeBubble(wrappedText);
			PositionTowardObserver();
			panel.SetActive(true);
			FaceObserver();
			return true;
		}

		internal void Hide()
		{
			if (panel != null)
			{
				panel.SetActive(false);
			}
		}

		internal void FaceObserver()
		{
			if (panel == null ||
				!panel.activeInHierarchy ||
				Refs.observerMirror == null)
			{
				return;
			}

			PositionTowardObserver();
			panel.transform.rotation = Refs.observerMirror.transform.rotation;
		}

		private void PositionTowardObserver()
		{
			if (panel == null || Refs.observerMirror == null)
			{
				return;
			}

			Vector3 towardObserver =
				Refs.observerMirror.transform.position - anchor.position;
			towardObserver.y = 0f;

			if (towardObserver.sqrMagnitude < 0.001f)
			{
				towardObserver = -anchor.forward;
			}
			else
			{
				towardObserver.Normalize();
			}

			Vector3 panelPosition =
				anchor.position + towardObserver * ObserverOffset;
			panelPosition.y =
				Refs.observerMirror.transform.position.y + HeadHeightOffset;
			panel.transform.position = panelPosition;
		}

		private bool EnsureCreated()
		{
			if (panel != null && text != null)
			{
				return true;
			}

			panel = LetterDialogueTemplate.Create(anchor, out text);
			if (panel == null || text == null)
			{
				if (!missingTemplateWarningLogged)
				{
					missingTemplateWarningLogged = true;
					Debug.LogWarning(
						"Postal Expansion: the letter dialogue template is unavailable at port " +
						portIndex + ".");
				}

				return false;
			}

			InitializeBubbleLayout();
			panel.SetActive(false);
			return true;
		}

		private void InitializeBubbleLayout()
		{
			Transform bubbleRoot = text != null ? text.transform.parent : null;
			bubbleGraphics = bubbleRoot != null
				? bubbleRoot.Find(BubbleGraphicsName)
				: null;

			if (bubbleGraphics == null || text == null || text.font == null)
			{
				if (!missingBubbleGraphicsWarningLogged)
				{
					missingBubbleGraphicsWarningLogged = true;
					Debug.LogWarning(
						"Postal Expansion: letter dialogue bubble graphics could not be found; dynamic sizing is disabled.");
				}

				return;
			}

			baseBubbleScale = bubbleGraphics.localScale;
			baseTextWidth = MeasureMaximumLineWidth(BubbleReferenceText);
		}

		private void ResizeBubble(string dialogueText)
		{
			if (bubbleGraphics == null ||
				text == null ||
				text.font == null ||
				baseTextWidth <= 0f)
			{
				return;
			}

			bubbleGraphics.localScale = baseBubbleScale;
			float renderedTextWidth = MeasureMaximumLineWidth(dialogueText);
			if (renderedTextWidth <= 0f)
			{
				return;
			}

			float widthMultiplier = Mathf.Clamp(
				renderedTextWidth / baseTextWidth,
				MinimumBubbleWidthMultiplier,
				MaximumBubbleWidthMultiplier);
			Vector3 scale = baseBubbleScale;
			scale.x *= widthMultiplier;
			bubbleGraphics.localScale = scale;
		}

		private float MeasureMaximumLineWidth(string value)
		{
			text.font.RequestCharactersInTexture(
				value,
				text.fontSize,
				text.fontStyle);

			float lineWidth = 0f;
			float maximumLineWidth = 0f;
			foreach (char character in value)
			{
				if (character == '\n')
				{
					maximumLineWidth = Mathf.Max(
						maximumLineWidth,
						lineWidth);
					lineWidth = 0f;
					continue;
				}

				if (text.font.GetCharacterInfo(
						character,
						out CharacterInfo info,
						text.fontSize,
						text.fontStyle))
				{
					lineWidth += info.advance;
				}
			}

			return Mathf.Max(maximumLineWidth, lineWidth);
		}

		private static string Wrap(string value, int size)
		{
			value = value.TrimStart(Array.Empty<char>());
			if (value.Length <= size)
			{
				return value;
			}

			int breakIndex = value.LastIndexOf(' ', size);
			if (breakIndex == -1)
			{
				breakIndex = Mathf.Min(value.Length, size);
			}

			return value.Substring(0, breakIndex) +
				(breakIndex >= value.Length
					? string.Empty
					: "\n" + Wrap(value.Substring(breakIndex), size));
		}
	}

	internal sealed class LetterDialoguePresenceArea : MonoBehaviour
	{
		private const float DialogueRetryInterval = 0.5f;

		private readonly HashSet<Collider> playerColliders =
			new HashSet<Collider>();

		private LetterDeliveryDialogue dialogue;
		private string pendingText;
		private bool shown;
		private bool consumed;
		private float nextDialogueRetryTime;

		internal void Initialize(
			LetterMissionDefinition missionDefinition)
		{
			dialogue = new LetterDeliveryDialogue(
				transform,
				missionDefinition.DestinationPortIndex);
		}

		internal void ShowForDelivery(string text)
		{
			if (string.IsNullOrEmpty(text) ||
				shown ||
				pendingText != null)
			{
				return;
			}

			consumed = false;
			pendingText = text;
			nextDialogueRetryTime = 0f;
			TryShow();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.CompareTag("Player"))
			{
				playerColliders.Add(other);
				TryShow();
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (!other.CompareTag("Player"))
			{
				return;
			}

			playerColliders.Remove(other);
			if (playerColliders.Count != 0)
			{
				return;
			}

			if (shown)
			{
				dialogue.Hide();
			}

			shown = false;
			consumed = true;
			pendingText = null;
			nextDialogueRetryTime = 0f;
		}

		private void Update()
		{
			if (shown)
			{
				dialogue.FaceObserver();
				return;
			}

			if (pendingText != null &&
				playerColliders.Count > 0 &&
				Time.time >= nextDialogueRetryTime)
			{
				nextDialogueRetryTime = Time.time + DialogueRetryInterval;
				TryShow();
			}
		}

		private void TryShow()
		{
			if (shown || consumed || pendingText == null || playerColliders.Count == 0)
			{
				return;
			}

			shown = dialogue.Show(pendingText);
		}
	}

	internal static class LetterDialogueTemplate
	{
		private const string TemplateObjectName =
			"postal_expansion_letter_dialogue_template";

		private static GameObject template;
		private static string textRelativePath;

		internal static bool TryCapture(TavernRumorsDude source)
		{
			if (template != null)
			{
				return true;
			}

			if (source == null ||
				source.speechUI == null ||
				source.text == null)
			{
				return false;
			}

			string sourceTextPath = GetRelativePath(
				source.speechUI.transform,
				source.text.transform);
			if (sourceTextPath == null)
			{
				return false;
			}

			GameObject captured = Object.Instantiate(source.speechUI);
			captured.name = TemplateObjectName;
			captured.transform.SetParent(null, true);

			if (source.drinkButton != null)
			{
				string buttonPath = GetRelativePath(
					source.speechUI.transform,
					source.drinkButton.transform);
				Transform button = FindRelativeTransform(
					captured.transform,
					buttonPath);
				if (button != null)
				{
					button.gameObject.SetActive(false);
					Object.Destroy(button.gameObject);
				}
			}

			captured.SetActive(false);
			Object.DontDestroyOnLoad(captured);
			template = captured;
			textRelativePath = sourceTextPath;
			return true;
		}

		internal static GameObject Create(
			Transform parent,
			out TextMesh dialogueText)
		{
			dialogueText = null;
			if (parent == null || (!EnsureCaptured() && template == null))
			{
				return null;
			}

			GameObject panel = Object.Instantiate(template);
			panel.name = "postal_expansion_letter_dialogue";
			panel.transform.SetParent(parent, false);
			panel.transform.localPosition = Vector3.zero;
			panel.transform.localRotation = Quaternion.identity;

			Transform textTransform = FindRelativeTransform(
				panel.transform,
				textRelativePath);
			dialogueText = textTransform != null
				? textTransform.GetComponent<TextMesh>()
				: null;
			panel.SetActive(false);
			if (dialogueText != null)
			{
				return panel;
			}

			Object.Destroy(panel);
			return null;
		}

		internal static void Dispose()
		{
			if (template != null)
			{
				Object.Destroy(template);
			}

			template = null;
			textRelativePath = null;
		}

		private static bool EnsureCaptured()
		{
			if (template != null)
			{
				return true;
			}

			foreach (TavernRumorsDude source in
				Object.FindObjectsOfType<TavernRumorsDude>())
			{
				if (TryCapture(source))
				{
					return true;
				}
			}

			foreach (TavernRumorsDude source in
				Resources.FindObjectsOfTypeAll<TavernRumorsDude>())
			{
				if (TryCapture(source))
				{
					return true;
				}
			}

			return false;
		}

		private static Transform FindRelativeTransform(
			Transform root,
			string relativePath)
		{
			if (root == null || relativePath == null)
			{
				return null;
			}

			return relativePath.Length == 0
				? root
				: root.Find(relativePath);
		}

		private static string GetRelativePath(Transform root, Transform child)
		{
			if (root == null || child == null)
			{
				return null;
			}

			var names = new List<string>();
			Transform current = child;
			while (current != null && current != root)
			{
				names.Add(current.name);
				current = current.parent;
			}

			if (current != root)
			{
				return null;
			}

			names.Reverse();
			return string.Join("/", names.ToArray());
		}
	}

	[HarmonyPatch(typeof(TavernRumorsDude), "Awake")]
	internal static class LetterDialogueTemplateCapturePatch
	{
		private static void Postfix(TavernRumorsDude __instance)
		{
			LetterDialogueTemplate.TryCapture(__instance);
		}
	}

	[HarmonyPatch(typeof(MissionDetailsUI), "InsertMissionData")]
	internal static class LetterMissionDetailsPatch
	{
		private const float LocationLabelPosition = 0.45f;
		private const float LocationValuePosition = 0.80f;
		private const float DeliveryHoursValueOffset = 0.50f;

		private const string LocationLabelObjectName =
			"postal_expansion_letter_location_label";
		private const string LocationValueObjectName =
			"postal_expansion_letter_location_value";
		private const string DeliveryHoursLabelObjectName =
			"postal_expansion_letter_delivery_hours_label";
		private const string DeliveryHoursValueObjectName =
			"postal_expansion_letter_delivery_hours_value";

		private static readonly FieldInfo LocationTextField =
			AccessTools.Field(typeof(MissionDetailsUI), "locationText");
		private static readonly FieldInfo DestinationTextField =
			AccessTools.Field(typeof(MissionDetailsUI), "destination");
		private static readonly FieldInfo DueTextField =
			AccessTools.Field(typeof(MissionDetailsUI), "due");
		private static readonly FieldInfo DistanceTextField =
			AccessTools.Field(typeof(MissionDetailsUI), "distance");
		private static readonly FieldInfo TotalGoldTextField =
			AccessTools.Field(typeof(MissionDetailsUI), "totalGold");
		private static readonly FieldInfo MapRendererField =
			AccessTools.Field(typeof(MissionDetailsUI), "mapRenderer");

		private static void Postfix(MissionDetailsUI __instance, Mission mission)
		{
			if (__instance == null ||
				LocationTextField == null ||
				DestinationTextField == null ||
				DueTextField == null ||
				DistanceTextField == null ||
				TotalGoldTextField == null)
			{
				return;
			}

			var destinationValue = DestinationTextField.GetValue(__instance) as TextMesh;
			var dueValue = DueTextField.GetValue(__instance) as TextMesh;
			var distanceText = DistanceTextField.GetValue(__instance) as TextMesh;
			var totalGoldText = TotalGoldTextField.GetValue(__instance) as TextMesh;
			if (destinationValue == null ||
				dueValue == null ||
				distanceText == null ||
				totalGoldText == null)
			{
				return;
			}

			Transform textRoot = destinationValue.transform.parent;
			Transform deliveryTextRoot = distanceText.transform.parent;
			TextMesh locationLabel = FindText(textRoot, LocationLabelObjectName);
			TextMesh locationValue = FindText(textRoot, LocationValueObjectName);
			TextMesh deliveryHoursLabel = FindText(
				deliveryTextRoot,
				DeliveryHoursLabelObjectName);
			TextMesh deliveryHoursValue = FindText(
				deliveryTextRoot,
				DeliveryHoursValueObjectName);
			TextMesh destinationLabel = FindTextByContent(
				textRoot,
				"Destination:");
			TextMesh cargoLabel = FindTextStartingWith(textRoot, "Cargo:");
			if (!LetterMissions.TryGetDefinition(
					mission,
					out LetterMissionDefinition definition))
			{
				SetActive(destinationLabel, true);
				SetActive(destinationValue, true);
				SetActive(locationLabel, false);
				SetActive(locationValue, false);
				SetActive(deliveryHoursLabel, false);
				SetActive(deliveryHoursValue, false);
				return;
			}

			totalGoldText.text =
				LetterMissions.GetFixedGoldReward(mission) + " " +
				PlayerGold.GetCurrencyName((int)Currency.gold);

			if (destinationLabel == null || cargoLabel == null)
			{
				Debug.LogWarning(
					"Postal Expansion: mission detail labels were not found.");
				return;
			}

			locationLabel = locationLabel ?? CloneText(
				destinationLabel,
				LocationLabelObjectName);
			locationValue = locationValue ?? CloneText(
				destinationValue,
				LocationValueObjectName);
			if (locationLabel == null || locationValue == null)
			{
				return;
			}

			if (definition.HideDestination)
			{
				locationLabel.transform.localPosition =
					destinationLabel.transform.localPosition;
				locationValue.transform.localPosition =
					destinationValue.transform.localPosition;
			}
			else
			{
				float destinationY = destinationValue.transform.localPosition.y;
				float cargoY = cargoLabel.transform.localPosition.y;
				SetLocalY(
					locationLabel.transform,
					Mathf.Lerp(destinationY, cargoY, LocationLabelPosition));
				SetLocalY(
					locationValue.transform,
					Mathf.Lerp(destinationY, cargoY, LocationValuePosition));
			}
			locationLabel.text = "Location:";
			locationValue.text = definition.LocationDescription;
			SetActive(destinationLabel, !definition.HideDestination);
			SetActive(destinationValue, !definition.HideDestination);
			SetActive(locationLabel, true);
			SetActive(locationValue, true);

			deliveryHoursLabel = deliveryHoursLabel ?? CloneText(
				distanceText,
				DeliveryHoursLabelObjectName);
			deliveryHoursValue = deliveryHoursValue ?? CloneText(
				dueValue,
				DeliveryHoursValueObjectName);
			if (deliveryHoursLabel != null && deliveryHoursValue != null)
			{
				float deliveryHoursY = Mathf.Lerp(
					dueValue.transform.localPosition.y,
					distanceText.transform.localPosition.y,
					0.5f);
				SetLocalY(deliveryHoursLabel.transform, deliveryHoursY);
				SetLocalY(deliveryHoursValue.transform, deliveryHoursY);
				deliveryHoursLabel.text = "Delivery hours:";
				deliveryHoursValue.text =
					LetterDeliveryHours.GetDisplayHours(definition.DeliveryWindow);
				SetActive(deliveryHoursLabel, true);
				SetActive(deliveryHoursValue, true);
				PositionDeliveryHoursValue(
					deliveryHoursLabel,
					deliveryHoursValue);
			}

			var mapLocationText = LocationTextField.GetValue(__instance) as TextMesh;
			var mapRenderer = MapRendererField?.GetValue(__instance) as Renderer;
			if (definition.ShowDeliveryCoordinates)
			{
				if (mapRenderer != null)
				{
					mapRenderer.gameObject.SetActive(false);
				}

				if (mapLocationText != null)
				{
					mapLocationText.text =
						FloatingOriginManager.instance != null &&
						LetterDeliveryAreaCreationPatch.TryGetAreaTransform(
							definition,
							out Transform deliveryArea)
							? GetVanillaLocationText(deliveryArea)
							: "approximate location:\nunavailable";
				}
			}
			else
			{
				if (mapLocationText != null)
				{
					mapLocationText.text = string.Empty;
				}
			}
		}

		private static string GetVanillaLocationText(Transform deliveryArea)
		{
			Vector3 globeCoordinates =
				FloatingOriginManager.instance.GetGlobeCoords(deliveryArea);
			int longitude = Mathf.RoundToInt(globeCoordinates.x);
			int latitude = Mathf.RoundToInt(globeCoordinates.z);
			string eastWest = longitude < 0 ? "W" : "E";
			string northSouth = latitude < 0 ? "S" : "N";

			return "approximate location:\n" +
				latitude + " " + northSouth + ", " +
				longitude + " " + eastWest;
		}

		private static TextMesh CloneText(TextMesh source, string objectName)
		{
			if (source == null || source.transform.parent == null)
			{
				return null;
			}

			GameObject clone = Object.Instantiate(
				source.gameObject,
				source.transform.parent);
			clone.name = objectName;
			return clone.GetComponent<TextMesh>();
		}

		private static TextMesh FindText(Transform root, string objectName)
		{
			Transform child = root != null ? root.Find(objectName) : null;
			return child != null ? child.GetComponent<TextMesh>() : null;
		}

		private static TextMesh FindTextByContent(Transform root, string content)
		{
			return FindText(root, text => string.Equals(
				text.Trim(),
				content,
				StringComparison.Ordinal));
		}

		private static TextMesh FindTextStartingWith(Transform root, string content)
		{
			return FindText(root, text => text.TrimStart().StartsWith(
				content,
				StringComparison.Ordinal));
		}

		private static TextMesh FindText(
			Transform root,
			Func<string, bool> predicate)
		{
			if (root == null)
			{
				return null;
			}

			foreach (TextMesh candidate in root.GetComponentsInChildren<TextMesh>(true))
			{
				if (candidate != null && predicate(candidate.text ?? string.Empty))
				{
					return candidate;
				}
			}

			return null;
		}

		private static void SetLocalY(Transform target, float y)
		{
			Vector3 position = target.localPosition;
			position.y = y;
			target.localPosition = position;
		}

		private static void PositionDeliveryHoursValue(
			TextMesh label,
			TextMesh value)
		{
			Vector3 textDirection =
				label.transform.localRotation * Vector3.right;
			Vector3 valueLocalPosition =
				label.transform.localPosition +
				textDirection.normalized * DeliveryHoursValueOffset;
			valueLocalPosition.y = value.transform.localPosition.y;
			valueLocalPosition.z = value.transform.localPosition.z;
			value.transform.localPosition = valueLocalPosition;
		}

		private static void SetActive(TextMesh text, bool active)
		{
			if (text != null)
			{
				text.gameObject.SetActive(active);
			}
		}
	}

	[HarmonyPatch(typeof(Mission), nameof(Mission.DeliverGood))]
	internal static class LetterRewardCurrencyPatch
	{
		private static readonly FieldInfo DestinationPortField =
			AccessTools.Field(typeof(Mission), nameof(Mission.destinationPort));
		private static readonly FieldInfo PortRegionField =
			AccessTools.Field(typeof(Port), nameof(Port.region));
		private static readonly MethodInfo MissionLogAddMethod =
			AccessTools.Method(typeof(MissionLog), nameof(MissionLog.AddToLog));
		private static readonly MethodInfo ResolveCurrencyMethod =
			AccessTools.Method(
					typeof(LetterRewardCurrencyPatch),
				nameof(ResolveCurrency));

		private static IEnumerable<CodeInstruction> Transpiler(
			IEnumerable<CodeInstruction> instructions)
		{
			var code = new List<CodeInstruction>(instructions);
			var insertionIndexes = new List<int>();

			for (int i = 0; i < code.Count; i++)
			{
				if (!LoadsDestinationRegion(code, i))
				{
					continue;
				}

				int regionLoadIndex = i + 2;
				bool initializesRewardRegion =
					regionLoadIndex + 1 < code.Count &&
					code[regionLoadIndex + 1].opcode == OpCodes.Stloc_0;
				bool suppliesMissionLogCurrency =
					regionLoadIndex + 1 < code.Count &&
					code[regionLoadIndex + 1].Calls(MissionLogAddMethod);

				if (initializesRewardRegion || suppliesMissionLogCurrency)
				{
					insertionIndexes.Add(regionLoadIndex + 1);
				}
			}

			if (insertionIndexes.Count != 2)
			{
				throw new InvalidOperationException(
					"Could not locate Mission.DeliverGood's reward currency paths.");
			}

			for (int i = insertionIndexes.Count - 1; i >= 0; i--)
			{
				code.InsertRange(
					insertionIndexes[i],
					new[]
					{
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Call, ResolveCurrencyMethod)
					});
			}

			return code;
		}

		private static bool LoadsDestinationRegion(
			IReadOnlyList<CodeInstruction> code,
			int index)
		{
			return index + 2 < code.Count &&
				code[index].opcode == OpCodes.Ldarg_0 &&
				code[index + 1].LoadsField(DestinationPortField) &&
				code[index + 2].LoadsField(PortRegionField);
		}

		private static int ResolveCurrency(
			PortRegion destinationRegion,
			Mission mission)
		{
			return LetterMissions.TryGetDefinition(mission, out _)
				? (int)Currency.gold
				: (int)destinationRegion;
		}
	}

	internal static class LetterMissionDebug
	{
		private const int EntriesPerPage = 5;
		private static int page;

		internal static void ShowSpawnLocations()
		{
			var statusLines = new List<string>();
			statusLines.AddRange(RegisteredLetterMissions.GetDebugStatusLines());
			statusLines.AddRange(AnonymousLetterMissions.GetDebugStatusLines());

			int pageCount = Mathf.Max(
				1,
				Mathf.CeilToInt((float)statusLines.Count / EntriesPerPage));
			page = Mathf.Clamp(page, 0, pageCount - 1);
			int startIndex = page * EntriesPerPage;
			int entriesOnPage = Mathf.Min(
				EntriesPerPage,
				statusLines.Count - startIndex);

			var pageLines = new List<string>
			{
				"Letter mission spawns (" + (page + 1) + "/" + pageCount + "):"
			};
			for (int i = 0; i < entriesOnPage; i++)
			{
				pageLines.Add(statusLines[startIndex + i]);
			}

			Debug.Log(
				"Postal Expansion: letter mission spawns: | " +
				string.Join(" | ", statusLines));
			if (NotificationUi.instance != null)
			{
				NotificationUi.instance.ShowNotification(
					string.Join("\n", pageLines));
			}

			page = (page + 1) % pageCount;
		}

		internal static void Reset()
		{
			page = 0;
		}
	}

	internal sealed class PersistentLetterDeliveryAnchor : MonoBehaviour
	{
		private Transform source;
		private Vector3 sourceLocalPosition;
		private Quaternion sourceLocalRotation;
		private Rigidbody[] triggerBodies;

		internal void Initialize(
			Transform sourceTransform,
			Vector3 localPosition,
			Vector3 localEulerAngles)
		{
			source = sourceTransform;
			sourceLocalPosition = localPosition;
			sourceLocalRotation = Quaternion.Euler(localEulerAngles);
			triggerBodies = GetComponentsInChildren<Rigidbody>();
			ApplyPose();
		}

		private void FixedUpdate()
		{
			ApplyPose();
		}

		private void ApplyPose()
		{
			if (source == null)
			{
				return;
			}

			Vector3 position = source.TransformPoint(sourceLocalPosition);
			Quaternion rotation = source.rotation * sourceLocalRotation;
			transform.position = position;
			transform.rotation = rotation;

			if (triggerBodies == null)
			{
				return;
			}

			foreach (Rigidbody body in triggerBodies)
			{
				if (body != null)
				{
					body.position = position;
					body.rotation = rotation;
				}
			}
		}
	}

	internal static class VanillaQuestState
	{
		private const int HideoutQuestIndex = 0;
		private const int CompletedQuestState = -5;

		internal static bool IsHideoutQuestComplete()
		{
			Quests quests = Quests.instance;
			return quests != null &&
				quests.currentQuests != null &&
				quests.currentQuests.Length > HideoutQuestIndex &&
				quests.currentQuests[HideoutQuestIndex] == CompletedQuestState;
		}
	}

	internal static class Letter
	{
		internal const int LetterGoodIndex = 68;

		private const int QuestLetterItemIndex = 330;
		private const string LetterName = "Registered Letter";
		private const string InheritedLabelObjectName = "label";
		private const string LetterSealTextureFile = "registered_letter_seal.png";
		private const string LetterSealObjectName = "postal_expansion_registered_letter_seal";
		private const float LetterSealSize = 0.11f;
		private const float LetterSealSurfaceOffset = 0.002f;

		private static GameObject letterPrefab;
		private static readonly PostalSealVisual LetterSeal = new PostalSealVisual(
			LetterSealObjectName,
			LetterSealTextureFile,
			"registered_letter_seal",
			LetterSealSize);

		internal static GameObject Prefab => letterPrefab;

		internal static bool EnsureRegistered(PrefabsDirectory directory)
		{
			if (directory == null || directory.directory == null)
			{
				return false;
			}

			if (PostalPrefabRegistration.IsRegistered(
				directory,
				PostalMail.RegisteredLetterItemIndex,
				letterPrefab))
			{
				return true;
			}

			if (!HasPrefab(directory, PostalMail.RegularMailItemIndex))
			{
				Debug.LogWarning("Postal Expansion: regular mail prefab 221 was not found.");
				return false;
			}

			if (!HasPrefab(directory, QuestLetterItemIndex))
			{
				Debug.LogWarning("Postal Expansion: vanilla quest letter prefab 330 was not found.");
				return false;
			}

			if (PostalPrefabRegistration.IsSlotOccupiedByAnotherPrefab(
				directory,
				PostalMail.RegisteredLetterItemIndex,
				letterPrefab))
			{
				Debug.LogError("Postal Expansion: prefab index 238 is already occupied.");
				return false;
			}

			GameObject regularMailPrefab =
				directory.directory[PostalMail.RegularMailItemIndex];
			GameObject questLetterPrefab = directory.directory[QuestLetterItemIndex];
			if (!HasRequiredComponents(regularMailPrefab, questLetterPrefab) ||
				!PostalPrefabRegistration.EnsureCapacity(
					directory,
					PostalMail.RegisteredLetterItemIndex))
			{
				Debug.LogError(
					"Postal Expansion: Registered Letter prefab components could not be prepared.");
				return false;
			}

			if (letterPrefab == null)
			{
				letterPrefab = Object.Instantiate(regularMailPrefab);
				letterPrefab.name =
					$"{PostalMail.RegisteredLetterItemIndex} ({LetterGoodIndex}) {LetterName}";
				Object.DontDestroyOnLoad(letterPrefab);
			}

			PostalMailOutlineSanitizer.Attach(letterPrefab);
			ConfigurePrefab(letterPrefab, questLetterPrefab);
			if (!PostalPrefabRegistration.RegisterShipItem(
				directory,
				PostalMail.RegisteredLetterItemIndex,
				letterPrefab))
			{
				Debug.LogError(
					"Postal Expansion: Registered Letter prefab could not be registered as a ship item.");
				return false;
			}

			directory.directory[PostalMail.RegisteredLetterItemIndex] = letterPrefab;
			return true;
		}

		private static bool HasPrefab(PrefabsDirectory directory, int prefabIndex)
		{
			return directory.directory.Length > prefabIndex && directory.directory[prefabIndex] != null;
		}

		private static bool HasRequiredComponents(
			GameObject regularMailPrefab,
			GameObject questLetterPrefab)
		{
			return regularMailPrefab.GetComponent<SaveablePrefab>() != null &&
				regularMailPrefab.GetComponent<ShipItem>() != null &&
				regularMailPrefab.GetComponent<Good>() != null &&
				regularMailPrefab.GetComponent<MeshFilter>() != null &&
				regularMailPrefab.GetComponent<MeshRenderer>() != null &&
				regularMailPrefab.GetComponent<BoxCollider>() != null &&
				questLetterPrefab.GetComponent<ShipItem>() != null &&
				questLetterPrefab.GetComponent<MeshFilter>() != null &&
				questLetterPrefab.GetComponent<MeshRenderer>() != null &&
				questLetterPrefab.GetComponent<BoxCollider>() != null;
		}

		private static void ConfigurePrefab(GameObject prefab, GameObject questLetterPrefab)
		{
			prefab.SetActive(true);
			prefab.name =
				$"{PostalMail.RegisteredLetterItemIndex} ({LetterGoodIndex}) {LetterName}";
			prefab.GetComponent<SaveablePrefab>().prefabIndex =
				PostalMail.RegisteredLetterItemIndex;

			ShipItem targetItem = prefab.GetComponent<ShipItem>();
			ShipItem sourceItem = questLetterPrefab.GetComponent<ShipItem>();
			targetItem.name = LetterName;
			targetItem.mass = 1f;
			targetItem.big = sourceItem.big;
			targetItem.holdDistance = sourceItem.holdDistance;
			targetItem.holdHeight = sourceItem.holdHeight;
			targetItem.heldRotationOffset = sourceItem.heldRotationOffset;
			targetItem.inventoryScale = sourceItem.inventoryScale;
			targetItem.inventoryRotation = sourceItem.inventoryRotation;
			targetItem.inventoryRotationX = sourceItem.inventoryRotationX;

			prefab.GetComponent<Good>().requiredRepLevel =
				RegisteredLetterMissions.RequiredReputation;

			MeshFilter sourceFilter = questLetterPrefab.GetComponent<MeshFilter>();
			MeshRenderer sourceRenderer = questLetterPrefab.GetComponent<MeshRenderer>();
			BoxCollider sourceCollider = questLetterPrefab.GetComponent<BoxCollider>();
			prefab.GetComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
			prefab.GetComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;

			BoxCollider targetCollider = prefab.GetComponent<BoxCollider>();
			targetCollider.center = sourceCollider.center;
			targetCollider.size = sourceCollider.size;

			Transform inheritedLabel = prefab.transform.Find(InheritedLabelObjectName);
			if (inheritedLabel != null)
			{
				inheritedLabel.gameObject.SetActive(false);
				Object.Destroy(inheritedLabel.gameObject);
			}

			Vector3 sealPosition = targetCollider.center;
			sealPosition.z += targetCollider.size.z / 2f + LetterSealSurfaceOffset;
			LetterSeal.Ensure(prefab, sealPosition, Quaternion.identity);
		}
	}

	internal static class NandTweaksCompatibility
	{
		internal const string PluginGuid = "com.nandbrew.nandtweaks";

		private const string DecalPatchTypeName =
			"NANDTweaks.MissionGoodsPatches+TextureReplacer";
		private const string DecalPostfixMethodName = "Postfix";

		internal static void Apply(Harmony harmony)
		{
			// Only with NANDTweaks loaded: AccessTools.TypeByName logs a HarmonyX warning for a missing type.
			if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(PluginGuid))
			{
				return;
			}

			Type decalPatchType = AccessTools.TypeByName(DecalPatchTypeName);
			if (decalPatchType == null)
			{
				return;
			}

			MethodInfo decalPostfix = AccessTools.Method(
				decalPatchType,
				DecalPostfixMethodName,
				new[] { typeof(Good) });
			MethodInfo exclusionPrefix = AccessTools.Method(
				typeof(NandTweaksCompatibility),
				nameof(AllowMissionGoodsDecal));

			if (decalPostfix == null || exclusionPrefix == null)
			{
				Debug.LogWarning(
					"Postal Expansion: NANDTweaks mission decal compatibility could not be applied.");
				return;
			}

			harmony.Patch(decalPostfix, prefix: new HarmonyMethod(exclusionPrefix));
		}

		private static bool AllowMissionGoodsDecal(Good __0)
		{
			SaveablePrefab saveable = __0 == null
				? null
				: __0.GetComponent<SaveablePrefab>();

			return !PostalMail.IsRegisteredLetter(saveable);
		}
	}

	internal static class RegisteredLetterMissions
	{
		internal const int RequiredReputation = 7;
		internal const float DueDayBaseSpeed = 3.0f;
		internal const float ReputationMultiplier = 8f;
		internal const int FixedGoldReward = 1;
		internal const int SeverelyLatePenalty = -5000;

		private static readonly LetterMissionSaveKeys SaveKeys =
			new LetterMissionSaveKeys(
				"PostalExpansion.RegisteredLetter.Active.v1",
				"PostalExpansion.RegisteredLetter.Claimed.v1");

		private static readonly LetterMissionSet<RegisteredLetterMissionDefinition>
			Missions = new LetterMissionSet<RegisteredLetterMissionDefinition>(
				"Registered Letter",
				RegisteredLetterMissionRegistry.All,
				SaveKeys,
				RequiredReputation,
				DueDayBaseSpeed,
				FixedGoldReward,
				destination =>
					"Registered Letter to " + destination.GetPortName());

		internal static void AddMissions(Port origin, List<Mission> missions)
		{
			Missions.AddMissions(origin, missions);
		}

		internal static List<string> GetDebugStatusLines()
		{
			return Missions.GetDebugStatusLines();
		}

		internal static bool TryGetDefinition(
			Mission mission,
			out RegisteredLetterMissionDefinition definition)
		{
			return Missions.TryGetDefinition(mission, out definition);
		}

		internal static void MissionAccepted(Mission mission)
		{
			Missions.MissionAccepted(mission);
		}

		internal static void MissionDelivered(Mission mission)
		{
			Missions.MissionDelivered(mission);
		}

		internal static void SavePersistentState()
		{
			Missions.SavePersistentState();
		}

		internal static void LoadPersistentState()
		{
			Missions.LoadPersistentState();
		}

		internal static void ResetRuntimeState()
		{
			Missions.ResetRuntimeState();
		}
	}

	internal abstract class RegisteredLetterMissionDefinition
		: LetterMissionDefinition
	{
		protected RegisteredLetterMissionDefinition(
			string id,
			string destinationPortName,
			int destinationPortIndex,
			Vector3 localPosition,
			Vector3 localEulerAngles,
			Vector3 triggerSize,
			LetterDeliveryWindow deliveryWindow,
			int[] spawnPortIndices,
			string locationDescription)
			: base(
				id,
				destinationPortName,
				destinationPortIndex,
				localPosition,
				localEulerAngles,
				triggerSize,
				deliveryWindow,
				spawnPortIndices,
				locationDescription)
		{
		}
	}

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

	[HarmonyPatch(typeof(PortDude), "OnTriggerEnter")]
	internal static class RegisteredLetterOfficeDeliveryBlockPatch
	{
		private static bool Prefix(Collider other)
		{
			Good good = other != null ? other.GetComponentInParent<Good>() : null;
			SaveablePrefab saveable =
				good != null ? good.GetComponent<SaveablePrefab>() : null;
			return !PostalMail.IsRegisteredLetter(saveable);
		}
	}

	// Mission Rework (nandbrew) delivers mission goods from the office page: its own patch on this
	// method records each mission good in a port's warehouse area, and its Deliver button calls
	// Good.Deliver on them. Registered and Anonymous Letters are delivered only at their own spots, so
	// they are taken back out of its record; it then shows a letter mission as having no goods there.
	[HarmonyPatch(typeof(IslandMarketWarehouseArea), "OnTriggerEnter")]
	internal static class MissionReworkLetterExclusionPatch
	{
		internal const string MissionReworkGuid = "com.nandbrew.missionrework";

		private const string PortListsTypeName = "MissionRework.MissionPatches";
		private const string PortListsFieldName = "portLists";

		private static bool lookedUp;
		private static FieldInfo portListsField;

		[HarmonyAfter(MissionReworkGuid)]
		private static void Postfix(Collider other, IslandMarket ___market)
		{
			Good good = other != null ? other.GetComponent<Good>() : null;
			if (good == null ||
				___market == null ||
				!PostalMail.IsRegisteredLetter(good.GetComponent<SaveablePrefab>()) ||
				!(GetPortLists() is Dictionary<int, List<Good>> portLists))
			{
				return;
			}

			if (portLists.TryGetValue(___market.GetPortIndex(), out List<Good> goods))
			{
				goods.Remove(good);
			}
		}

		// Looked up once, and only with Mission Rework loaded (AccessTools.TypeByName logs a HarmonyX
		// warning for a missing type).
		private static object GetPortLists()
		{
			if (!lookedUp)
			{
				lookedUp = true;
				if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(MissionReworkGuid))
				{
					Type type = AccessTools.TypeByName(PortListsTypeName);
					portListsField = type != null
						? AccessTools.Field(type, PortListsFieldName)
						: null;
					if (portListsField == null)
					{
						Debug.LogWarning(
							"Postal Expansion: Mission Rework's warehouse goods list was not found; " +
							"its office button may deliver letters.");
					}
				}
			}

			return portListsField?.GetValue(null);
		}
	}

	internal static class AnonymousLetterMissions
	{
		internal const int RequiredReputation = 8;
		internal const float DueDayBaseSpeed =
			RegisteredLetterMissions.DueDayBaseSpeed;
		internal const float ReputationMultiplier = 10f;
		internal const int FixedGoldReward = 5;
		internal const int OneDayLateGoldReward = 1;
		internal const int SeverelyLatePenalty = -8000;

		private static readonly LetterMissionSaveKeys SaveKeys =
			new LetterMissionSaveKeys(
				"PostalExpansion.AnonymousLetter.Active.v1",
				"PostalExpansion.AnonymousLetter.Claimed.v1");
		// Pirate Hideout used Registered Letter persistence before version 0.5.13.
		private static readonly LetterMissionSaveKeys LegacyPirateHideoutSaveKeys =
			new LetterMissionSaveKeys(
				"PostalExpansion.RegisteredLetter.Active.v1",
				"PostalExpansion.RegisteredLetter.Claimed.v1");

		private static readonly LetterMissionSet<AnonymousLetterMissionDefinition>
			Missions = new LetterMissionSet<AnonymousLetterMissionDefinition>(
				"Anonymous Letter",
				AnonymousLetterMissionRegistry.All,
				SaveKeys,
				RequiredReputation,
				DueDayBaseSpeed,
				FixedGoldReward,
				destination => "Anonymous letter");

		internal static void AddMissions(Port origin, List<Mission> missions)
		{
			Missions.AddMissions(origin, missions);
		}

		internal static int GetGoldReward(int daysLate)
		{
			if (daysLate <= 0)
			{
				return FixedGoldReward;
			}

			return daysLate == 1 ? OneDayLateGoldReward : 0;
		}

		internal static List<string> GetDebugStatusLines()
		{
			return Missions.GetDebugStatusLines();
		}

		internal static bool TryGetDefinition(
			Mission mission,
			out AnonymousLetterMissionDefinition definition)
		{
			return Missions.TryGetDefinition(mission, out definition);
		}

		internal static void MissionAccepted(Mission mission)
		{
			Missions.MissionAccepted(mission);
		}

		internal static void MissionDelivered(Mission mission)
		{
			Missions.MissionDelivered(mission);
		}

		internal static void SavePersistentState()
		{
			Missions.SavePersistentState();
		}

		internal static void LoadPersistentState()
		{
			Missions.LoadPersistentState(LegacyPirateHideoutSaveKeys);
		}

		internal static void ResetRuntimeState()
		{
			Missions.ResetRuntimeState();
		}
	}

	internal abstract class AnonymousLetterMissionDefinition
		: LetterMissionDefinition
	{
		protected AnonymousLetterMissionDefinition(
			string id,
			string destinationPortName,
			int destinationPortIndex,
			Vector3 localPosition,
			Vector3 localEulerAngles,
			Vector3 triggerSize,
			float distanceToOffice,
			LetterDeliveryWindow deliveryWindow,
			int[] spawnPortIndices,
			string locationDescription)
			: base(
				id,
				destinationPortName,
				destinationPortIndex,
				localPosition,
				localEulerAngles,
				triggerSize,
				deliveryWindow,
				spawnPortIndices,
				locationDescription)
		{
			DistanceToOffice = distanceToOffice;
		}

		internal sealed override float DistanceToOffice { get; }
		internal sealed override bool UsePersistentWorldAnchor => true;
		internal sealed override bool UseDeliveryLocationForRoute => true;
		internal sealed override bool ShowDeliveryCoordinates => true;
		internal sealed override bool HideDestination => true;
	}

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

	[HarmonyPatch(typeof(ShipItem), nameof(ShipItem.UpdateLookText))]
	internal static class AnonymousLetterItemLookTextPatch
	{
		private static bool Prefix(ShipItem __instance)
		{
			if (__instance == null ||
				!PostalMail.IsRegisteredLetter(
					__instance.GetComponent<SaveablePrefab>()))
			{
				return true;
			}

			Good good = __instance.GetComponent<Good>();
			int missionIndex = good != null ? good.GetMissionIndex() : -1;
			if (missionIndex < 0 ||
				PlayerMissions.missions == null ||
				missionIndex >= PlayerMissions.missions.Length)
			{
				return true;
			}

			Mission mission = PlayerMissions.missions[missionIndex];
			if (!AnonymousLetterMissions.TryGetDefinition(mission, out _))
			{
				return true;
			}

			__instance.lookText = "due: " + mission.GetDueText();
			return false;
		}
	}

	internal sealed class AestraAbbeyGraveRobberTableMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly AestraAbbeyGraveRobberTableMission Instance = new();

		private AestraAbbeyGraveRobberTableMission()
			: base(
				"aestra_abbey_grave_robber_table",
				"Aestra Abbey",
				SailwindPortIndex.AestraAbbey,
				new Vector3(232.158f, -0.924f, 194.327f),
				new Vector3(0f, 136.291f, 0f),
				new Vector3(2f, 2f, 2f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.SirenSong,
					SailwindPortIndex.Oasis
				},
				"Table of grave robber")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.SirenSong:
				case SailwindPortIndex.Oasis:
					return "(You put the letter under the table)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

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

	internal sealed class AestraAbbeyOracleBenchMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly AestraAbbeyOracleBenchMission Instance = new();

		private AestraAbbeyOracleBenchMission()
			: base(
				"aestra_abbey_oracle_bench",
				"Aestra Abbey",
				SailwindPortIndex.AestraAbbey,
				new Vector3(-129.328f, -1.928f, 1355.791f),
				new Vector3(0f, 257.793f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				1362.3f,
				new LetterDeliveryWindow(19f, 7f, "19-07"),
				new[]
				{
					SailwindPortIndex.Onna,
					SailwindPortIndex.TurtleIsland,
					SailwindPortIndex.FeyValley
				},
				"Oracle bench")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Onna:
					return "(Letter open by itself once you approach one of the shrooms, showing totemic pattern before flying away. Closest shroom is gone)";

				case SailwindPortIndex.TurtleIsland:
					return "(The letter tries to let out the light but it got put out by shrooms quickly.)";

				case SailwindPortIndex.FeyValley:
					return "(You place the letter on bench. You heard a pleasent chime.)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class AlAnkhAcademyKeyKeeperMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly AlAnkhAcademyKeyKeeperMission Instance = new();

		private AlAnkhAcademyKeyKeeperMission()
			: base(
				"al_ankh_academy_key_keeper",
				"Al'Ankh Academy",
				SailwindPortIndex.AlAnkhAcademy,
				new Vector3(-0.173f, 0.833f, 1.175f),
				new Vector3(0f, 170.709f, 0f),
				new Vector3(2f, 2f, 2f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.Sunspire,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.Sanctuary,
					SailwindPortIndex.Senna,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.FireFishTown,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.Chronos
				},
				"Key Keeper")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.FireFishTown:
				case SailwindPortIndex.FortAestrin:
					return "Endless applications... I need a day off.";

				case SailwindPortIndex.MirageMountain:
					return "Oh! Ibn Doraid will be interested in this!";

				case SailwindPortIndex.Sunspire:
					return "These glasses cost a big fortune for sure.";

				case SailwindPortIndex.Sanctuary:
				case SailwindPortIndex.Senna:
					return "These ideas are... interesting.";

				case SailwindPortIndex.Chronos:
					return "(Whistles) Your journey must have been eventful.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class AlbacoreTownCreepyGuyMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly AlbacoreTownCreepyGuyMission Instance = new();

		private AlbacoreTownCreepyGuyMission()
			: base(
				"albacore_town_creepy_guy",
				"Albacore Town",
				SailwindPortIndex.AlbacoreTown,
				new Vector3(40.721f, 3.318f, -50.810f),
				new Vector3(0f, 55.492f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.Senna
				},
				"Creepy guy")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.NewPort:
					return "Were you seen? You'd better not.";

				case SailwindPortIndex.Senna:
					return "This makes me homesick... but I need to know where the fishing ground is.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class AlchemistsIslandApprenticeMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly AlchemistsIslandApprenticeMission Instance = new();

		private AlchemistsIslandApprenticeMission()
			: base(
				"alchemists_island_apprentice",
				"Alchemist's Island",
				SailwindPortIndex.AlchemistsIsland,
				new Vector3(-0.235f, 0.336f, 14.538f),
				new Vector3(0f, 35.018f, 0f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.Sanctuary,
					SailwindPortIndex.Senna,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.Eastwind,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.Chronos
				},
				"Alchemist's apprentice")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.MirageMountain:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.KiciaBay:
					return "I will inform my mentor about this. Thank you.";

				case SailwindPortIndex.AlAnkhAcademy:
					return "This again... When will they stop trying?";

				case SailwindPortIndex.Sanctuary:
				case SailwindPortIndex.Senna:
					return "That's... a brilliant idea! I need to tell my mentor now!";

				case SailwindPortIndex.Eastwind:
				case SailwindPortIndex.FireflyGrotto:
					return "Your God cannot help the exhausted miners? Heh.";

				case SailwindPortIndex.Chronos:
					return "...Really?";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class AlNilemLionsFangObeliskMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly AlNilemLionsFangObeliskMission Instance = new();

		private AlNilemLionsFangObeliskMission()
			: base(
				"al_nilem_lions_fang_obelisk",
				"Al'Nilem",
				SailwindPortIndex.AlNilem,
				new Vector3(807.097f, 4.672f, 1451.161f),
				new Vector3(0f, 318.985f, 0f),
				new Vector3(5f, 5f, 5f),
				1659.3f,
				new LetterDeliveryWindow(11f, 13f, "11-13"),
				new[]
				{
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.OldAnkhTown,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.TurtleIsland,
					SailwindPortIndex.Onna,
					SailwindPortIndex.FireflyGrotto
				},
				"Isle of Clear Mind Obelisk")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Neverdin:
				case SailwindPortIndex.OldAnkhTown:
				case SailwindPortIndex.MirageMountain:
					return "(Letter burns away after you place it on ground, you feel hotter)";

				case SailwindPortIndex.TurtleIsland:
					return "(Letter open by itself once you approach the obelisk, showing totemic pattern before flying away)";

				case SailwindPortIndex.Onna:
				case SailwindPortIndex.FireflyGrotto:
					return "(Letter got eject away before you can approach, falls into the seas)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class ChronosChurchHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly ChronosChurchHouseMission Instance = new();

		private ChronosChurchHouseMission()
			: base(
				"chronos_church_house",
				"Chronos",
				SailwindPortIndex.Chronos,
				new Vector3(-19.175f, 31.109f, -62.909f),
				new Vector3(0f, 17.783f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.DragonCliffs
				},
				"Church house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AestraAbbey:
				case SailwindPortIndex.FireflyGrotto:
					return "There is little common ground after the schism.";

				case SailwindPortIndex.AlAnkhAcademy:
					return "We appreciate the knowledge seeking.";

				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.DragonCliffs:
					return "The Church is under maintenance thus not open for visiting.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class CrabBeachHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly CrabBeachHouseMission Instance = new();

		private CrabBeachHouseMission()
			: base(
				"crab_beach_house",
				"Crab Beach",
				SailwindPortIndex.CrabBeach,
				new Vector3(-100.447f, 4.350f, 30.655f),
				new Vector3(0f, 188.793f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.Chronos
				},
				"House at beach")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.KiciaBay:
					return "I SAID I DON'T BUILD SHIPS ANYMORE! GO AWAY!";

				case SailwindPortIndex.Chronos:
					return "...Where?";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class DeadCoveNamelessRockMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly DeadCoveNamelessRockMission Instance = new();

		private DeadCoveNamelessRockMission()
			: base(
				"dead_cove_nameless_rock",
				"Dead Cove",
				SailwindPortIndex.DeadCove,
				new Vector3(-852.584f, 2.539f, -826.316f),
				new Vector3(0f, 26.327f, 0f),
				new Vector3(8f, 8f, 8f),
				1195.2f,
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.AlNilem,
					SailwindPortIndex.Eastwind,
					SailwindPortIndex.CrabBeach
				},
				"Nameless rock southren tip")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AlNilem:
				case SailwindPortIndex.Eastwind:
				case SailwindPortIndex.CrabBeach:
					return "(There is no remarkable landmark so you just throw the letter on the rock randomly. You have no idea what you are doing.)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class DragonCliffsBrownTobaccoVendorMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly DragonCliffsBrownTobaccoVendorMission Instance = new();

		private DragonCliffsBrownTobaccoVendorMission()
			: base(
				"dragon_cliffs_brown_tobacco_vendor",
				"Dragon Cliffs",
				SailwindPortIndex.DragonCliffs,
				new Vector3(47.025f, 1.993f, -11.703f),
				new Vector3(0f, 106.371f, 0f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.FireflyGrotto
				},
				"Brown Tobacco Vendor")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.FireflyGrotto:
					return "(Sigh) How much longer do I need to stay here...?";

				case SailwindPortIndex.GoldRockCity:
					return "Hmm...";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class DragonCliffsChimesHutMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly DragonCliffsChimesHutMission Instance = new();

		private DragonCliffsChimesHutMission()
			: base(
				"dragon_cliffs_small_hut_beside_chimes",
				"Dragon Cliffs",
				SailwindPortIndex.DragonCliffs,
				new Vector3(-45.037f, 0.070f, -119.983f),
				new Vector3(0f, 148.902f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.SageHills,
					SailwindPortIndex.TurtleIsland,
					SailwindPortIndex.Onna,
					SailwindPortIndex.Sunspire,
					SailwindPortIndex.AlNilem,
					SailwindPortIndex.Senna,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.Sanctuary,
					SailwindPortIndex.Chronos
				},
				"Small hut beside chimes")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.SageHills:
				case SailwindPortIndex.TurtleIsland:
				case SailwindPortIndex.Onna:
				case SailwindPortIndex.Sunspire:
				case SailwindPortIndex.AlNilem:
				case SailwindPortIndex.Senna:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.FireflyGrotto:
				case SailwindPortIndex.Sanctuary:
					return "(Gasp) It finally arrived! Thanks!";

				case SailwindPortIndex.Chronos:
					return "What took you so long?";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class DragonCliffsTeaMerchantHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly DragonCliffsTeaMerchantHouseMission Instance = new();

		private DragonCliffsTeaMerchantHouseMission()
			: base(
				"dragon_cliffs_house_behind_tea_merchant",
				"Dragon Cliffs",
				SailwindPortIndex.DragonCliffs,
				new Vector3(-6.196f, 2.711f, -52.022f),
				new Vector3(0f, 101.025f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.Oasis,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.SirenSong,
					SailwindPortIndex.GoldRockCity
				},
				"House behind tea merchant")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Oasis:
					return VanillaQuestState.IsHideoutQuestComplete()
						? "New order, matey?"
						: "Thank you.";

				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.SirenSong:
				case SailwindPortIndex.GoldRockCity:
					return VanillaQuestState.IsHideoutQuestComplete()
						? "Which scurvy dog brings ye 'ere?"
						: "...You can leave now.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class EastwindLighthouseMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly EastwindLighthouseMission Instance = new();

		private EastwindLighthouseMission()
			: base(
				"eastwind_lighthouse",
				"Eastwind",
				SailwindPortIndex.Eastwind,
				new Vector3(3070.795f, 8.574f, -4694.155f),
				new Vector3(0f, 266.217f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				5610.9f,
				new LetterDeliveryWindow(22f, 2f, "22-02"),
				new[]
				{
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.Sanctuary,
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.FireFishTown,
					SailwindPortIndex.Oasis
				},
				"Light house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Oasis:
					return "Aye matey, Me belong 'ere now.";

				case SailwindPortIndex.Neverdin:
				case SailwindPortIndex.Sanctuary:
				case SailwindPortIndex.AestraAbbey:
				case SailwindPortIndex.Chronos:
				case SailwindPortIndex.FireFishTown:
					return "YER GODS 'AVE NO POWER 'ERE LANDLUBBER!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class FeyValleyWaterfallManorVendorMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FeyValleyWaterfallManorVendorMission Instance = new();

		private FeyValleyWaterfallManorVendorMission()
			: base(
				"fey_valley_waterfall_manor_vendor",
				"Fey Valley",
				SailwindPortIndex.FeyValley,
				new Vector3(-126.239f, 51.409f, 42.779f),
				new Vector3(0f, 300.007f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.AestraAbbey
				},
				"Waterfall manor vendor")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AestraAbbey:
					return "Gods be praised! This year should be a good harvest year!";

				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.NewPort:
					return "We're supposed to bring their fruit back, not the other way around!";

				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.DragonCliffs:
					return "I will let the Abbot know about the orders.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class FireFishTownPainterMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FireFishTownPainterMission Instance = new();

		private FireFishTownPainterMission()
			: base(
				"fire_fish_town_painter",
				"Fire Fish Town",
				SailwindPortIndex.FireFishTown,
				new Vector3(7.186f, 3.075f, 216.135f),
				new Vector3(0f, 190.815f, 0f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.HappyBay
				},
				"Painter")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.GoldRockCity:
					return "Always happy to hear from fellow painters!";

				case SailwindPortIndex.DragonCliffs:
					return "Ugh! I escape there for a reason!";

				case SailwindPortIndex.HappyBay:
					return "Daddy...";

				case SailwindPortIndex.Chronos:
					return "I would like to visit there one day!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class FireFishTownTempleHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FireFishTownTempleHouseMission Instance = new();

		private FireFishTownTempleHouseMission()
			: base(
				"fire_fish_town_temple_house",
				"Fire Fish Town",
				SailwindPortIndex.FireFishTown,
				new Vector3(-5.519f, 7.688f, 82.870f),
				new Vector3(0f, 35.728f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.Sanctuary
				},
				"Temple house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			return GetOriginPortIndex(mission) == SailwindPortIndex.Sanctuary
				? "All is well."
				: UnexpectedOrigin(mission);
		}
	}

	internal sealed class FireflyGrottoChurchMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FireflyGrottoChurchMission Instance = new();

		private FireflyGrottoChurchMission()
			: base(
				"firefly_grotto_church",
				"Firefly Grotto",
				SailwindPortIndex.FireflyGrotto,
				new Vector3(128.273f, 5.458f, 37.452f),
				new Vector3(0f, 107.820f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.DragonCliffs
				},
				"Chruch")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AestraAbbey:
					return "Ora et Labora.";

				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.DragonCliffs:
					return "We must continued our missionary work.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class FortAestrinUpperTownGateMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FortAestrinUpperTownGateMission Instance = new();

		private FortAestrinUpperTownGateMission()
			: base(
				"fort_aestrin_upper_town_gate",
				"Fort Aestrin",
				SailwindPortIndex.FortAestrin,
				new Vector3(112.355f, 35.876f, -61.713f),
				new Vector3(359.526f, 116.915f, 359.692f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.Eastwind,
					SailwindPortIndex.Chronos
				},
				"Upper Town Gate")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.Eastwind:
					return "Your job here is done, courier. Now move along.";

				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.HappyBay:
					return "I wonder when we'll get the next shipment of that colorful tobacco.";

				case SailwindPortIndex.AestraAbbey:
				case SailwindPortIndex.FireflyGrotto:
					return "God be praised!";

				case SailwindPortIndex.Chronos:
					return "You've come a long way, my friend!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class FortAestrinWesternmostPierHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FortAestrinWesternmostPierHouseMission Instance = new();

		private FortAestrinWesternmostPierHouseMission()
			: base(
				"fort_aestrin_westernmost_house_at_pier",
				"Fort Aestrin",
				SailwindPortIndex.FortAestrin,
				new Vector3(-144.465f, -1.242f, -99.124f),
				new Vector3(359.794f, 152.539f, 359.474f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.OldAnkhTown
				},
				"Westernmost house on the pier")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Neverdin:
				case SailwindPortIndex.MirageMountain:
				case SailwindPortIndex.OldAnkhTown:
					return "Sabbih Ash-shams!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class GoldRockCityPalaceFishVendorMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly GoldRockCityPalaceFishVendorMission Instance = new();

		private GoldRockCityPalaceFishVendorMission()
			: base(
				"gold_rock_city_palace_fish_vendor",
				"Gold Rock City",
				SailwindPortIndex.GoldRockCity,
				new Vector3(-130.644f, 2.269f, 70.875f),
				new Vector3(0f, 226.858f, 0f),
				new Vector3(2f, 2f, 2f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.AlbacoreTown,
					SailwindPortIndex.FireFishTown,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.Oasis
				},
				"Fish vendor at palace")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AlbacoreTown:
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.FireFishTown:
					return "People love exotic fish!";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.HappyBay:
					return "Eels... not sure about that.";

				case SailwindPortIndex.Oasis:
					return VanillaQuestState.IsHideoutQuestComplete()
						? "Our mutual friend has come up with some new stuff, eh?"
						: "I will need a fast boat for this delivery.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

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

	internal sealed class HappyBayLocalPrinceMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly HappyBayLocalPrinceMission Instance = new();

		private HappyBayLocalPrinceMission()
			: base(
				"happy_bay_local_prince",
				"Happy Bay",
				SailwindPortIndex.HappyBay,
				new Vector3(102.092f, 17.839f, -23.281f),
				new Vector3(0f, 81.201f, 0f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.Oasis
				},
				"Local Prince")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.NewPort:
					return "More demands...";

				case SailwindPortIndex.AestraAbbey:
				case SailwindPortIndex.FireflyGrotto:
				case SailwindPortIndex.Neverdin:
					return "Take your god or whatever somewhere else.";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.KiciaBay:
					return "These merchants only care about gold.";

				case SailwindPortIndex.GoldRockCity:
					return "A surprise, but a welcome one.";

				case SailwindPortIndex.Chronos:
					return "At least they stay away from this.";

				case SailwindPortIndex.Oasis:
					return VanillaQuestState.IsHideoutQuestComplete()
						? "Nice doing business with you."
						: "You're going to have a hard time on the return trip.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class HappyBayUnnamedGraveMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly HappyBayUnnamedGraveMission Instance = new();

		private HappyBayUnnamedGraveMission()
			: base(
				"happy_bay_unnamed_grave",
				"Happy Bay",
				SailwindPortIndex.HappyBay,
				new Vector3(-6791.194f, 55.928f, -5663.258f),
				new Vector3(0f, 355.344f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				8844.2f,
				new LetterDeliveryWindow(5f, 7f, "05-07"),
				new[]
				{
					SailwindPortIndex.Neverdin
				},
				"Unnamed grave")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			return GetOriginPortIndex(mission) == SailwindPortIndex.Neverdin
				? "(You place the letter on the grave. It fades into light when sun raise up)"
				: UnexpectedOrigin(mission);
		}
	}

	internal sealed class KiciaBayWesternmostHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly KiciaBayWesternmostHouseMission Instance = new();

		private KiciaBayWesternmostHouseMission()
			: base(
				"kicia_bay_westernmost_house",
				"Kicia Bay",
				SailwindPortIndex.KiciaBay,
				new Vector3(204.082f, 3.362f, -107.532f),
				new Vector3(0f, 74.864f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.Oasis,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.SirenSong,
					SailwindPortIndex.HappyBay
				},
				"Western most house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			int originPortIndex = GetOriginPortIndex(mission);
			if (!VanillaQuestState.IsHideoutQuestComplete())
			{
				switch (originPortIndex)
				{
					case SailwindPortIndex.Oasis:
					case SailwindPortIndex.GoldRockCity:
						return "I wish for a dryer air...";

					case SailwindPortIndex.DragonCliffs:
					case SailwindPortIndex.SirenSong:
					case SailwindPortIndex.HappyBay:
						return "Oranges...Always oranges...";

					default:
						return UnexpectedOrigin(mission);
				}
			}

			switch (originPortIndex)
			{
				case SailwindPortIndex.Oasis:
					return "Aye...more work! Ye should be shipwreck in the fog!";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.SirenSong:
				case SailwindPortIndex.HappyBay:
					return "They just want me blue tobacco, feck off!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

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

	internal sealed class MountMaleficWheatManorMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly MountMaleficWheatManorMission Instance = new();

		private MountMaleficWheatManorMission()
			: base(
				"mount_malefic_wheat_manor",
				"Mount Malefic",
				SailwindPortIndex.MountMalefic,
				new Vector3(-105.357f, 0.931f, 172.698f),
				new Vector3(0f, 201.481f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.Eastwind,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.KiciaBay
				},
				"Wheat manor")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.Eastwind:
				case SailwindPortIndex.FireflyGrotto:
					return "I'll arrange next shipment.";

				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.NewPort:
					return "I fully support the colonization effort!";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.KiciaBay:
					return "You ever taste those rice noodles? It's like eating a whip!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class NeverdinTempleMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly NeverdinTempleMission Instance = new();

		private NeverdinTempleMission()
			: base(
				"neverdin_temple",
				"Neverdin",
				SailwindPortIndex.Neverdin,
				new Vector3(-11.756f, 3.429f, 148.984f),
				new Vector3(0f, 4.207f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Sanctuary,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.KiciaBay
				},
				"Temple")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.DragonCliffs:
					return "They need the guidance of the sun.";

				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Oasis:
					return "Sabbih Ash-shams!";

				case SailwindPortIndex.MirageMountain:
					return "The sun gives, and the sun takes away.";

				case SailwindPortIndex.Sanctuary:
					return "This interfaith dialogue is inspiring.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

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

	internal sealed class OasisPirateHideoutMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly OasisPirateHideoutMission Instance = new();

		private OasisPirateHideoutMission()
			: base(
				"oasis_pirate_hideout",
				"Oasis",
				SailwindPortIndex.Oasis,
				new Vector3(-10609.600f, 146.177f, -6544.561f),
				new Vector3(0f, 251.320f, 0f),
				new Vector3(2f, 2f, 2f),
				12463.9f,
				new LetterDeliveryWindow(12f, 17f, "12-17"),
				new[]
				{
					SailwindPortIndex.SirenSong,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.HappyBay
				},
				"Shitface on bench")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			int originPortIndex = GetOriginPortIndex(mission);
			if (!VanillaQuestState.IsHideoutQuestComplete())
			{
				if (originPortIndex == SailwindPortIndex.Chronos)
				{
					return "Quite an old salt ain't ye? Ye should talk to Cap'n.";
				}

				switch (originPortIndex)
				{
					case SailwindPortIndex.SirenSong:
					case SailwindPortIndex.DragonCliffs:
					case SailwindPortIndex.KiciaBay:
					case SailwindPortIndex.GoldRockCity:
					case SailwindPortIndex.HappyBay:
						return "If ye ain't afraid o' gettin' yer feet wet, talk to Cap'n.";

					default:
						return UnexpectedOrigin(mission);
				}
			}

			switch (originPortIndex)
			{
				case SailwindPortIndex.SirenSong:
					return "Cap'n should get a better 'ouse in Fort Aestrin...";

				case SailwindPortIndex.DragonCliffs:
					return "Them scallywag better not be squiffy on leaves this time...";

				case SailwindPortIndex.KiciaBay:
					return "Pegleg got shitty writing, an' now the letter be wet...";

				case SailwindPortIndex.GoldRockCity:
					return "Fishy landlubber better 'ave jolly target this time...";

				case SailwindPortIndex.HappyBay:
					return "Me freedom fighters now, Heh Heh.";

				case SailwindPortIndex.Chronos:
					return "WE NEED A BIGGER SHIP CAP'N!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class OldAnkhTownClearMindMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly OldAnkhTownClearMindMission Instance = new();

		private OldAnkhTownClearMindMission()
			: base(
				"old_ankh_town_clear_mind_palms",
				"Old Ankh Town",
				SailwindPortIndex.OldAnkhTown,
				new Vector3(-1377.220f, 8.869f, -195.353f),
				new Vector3(0f, 139.835f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				1391.9f,
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.DeadCove,
					SailwindPortIndex.Senna,
					SailwindPortIndex.SageHills,
					SailwindPortIndex.CrabBeach,
					SailwindPortIndex.Eastwind
				},
				"Lion's Fang middle of 3 palm trees")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.DeadCove:
				case SailwindPortIndex.Senna:
				case SailwindPortIndex.SageHills:
				case SailwindPortIndex.CrabBeach:
				case SailwindPortIndex.Eastwind:
					return "(You put the letter in bottle and bury it as request.)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class OldAnkhTownCoffeeFarmerMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly OldAnkhTownCoffeeFarmerMission Instance = new();

		private OldAnkhTownCoffeeFarmerMission()
			: base(
				"old_ankh_town_coffee_farmer",
				"Old Ankh Town",
				SailwindPortIndex.OldAnkhTown,
				new Vector3(-223.606f, 2.544f, 56.382f),
				new Vector3(0f, 303.025f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.Eastwind
				},
				"Coffee farmer")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
					return "Glory to Sultan!";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.KiciaBay:
					return "I'd love to get some tea in return.";

				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.Eastwind:
					return "Arabica beans are much better then Emerald's Robusta.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class OnnaHoneyShamanMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly OnnaHoneyShamanMission Instance = new();

		private OnnaHoneyShamanMission()
			: base(
				"onna_honey_shaman",
				"On'na",
				SailwindPortIndex.Onna,
				new Vector3(-7.320f, 1.007f, 0.395f),
				new Vector3(0f, 99.218f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.TurtleIsland,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.FeyValley,
					SailwindPortIndex.MountMalefic,
					SailwindPortIndex.AlchemistsIsland,
					SailwindPortIndex.SageHills,
					SailwindPortIndex.OldAnkhTown
				},
				"Honey Shaman")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.TurtleIsland:
					return "Ha! You think I'm stupid?";

				case SailwindPortIndex.Chronos:
					return "My honey knows no boundary.";

				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.FeyValley:
				case SailwindPortIndex.MountMalefic:
				case SailwindPortIndex.AlchemistsIsland:
				case SailwindPortIndex.SageHills:
				case SailwindPortIndex.OldAnkhTown:
					return "I should grow more flowers... maybe a little rain...";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class SaffronIslandTraderMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly SaffronIslandTraderMission Instance = new();

		private SaffronIslandTraderMission()
			: base(
				"saffron_island_trader",
				"Saffron Island",
				SailwindPortIndex.SaffronIsland,
				new Vector3(0.015f, 0.750f, 0.586f),
				new Vector3(0f, 177.864f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				3.5f,
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.DeadCove,
					SailwindPortIndex.CrabBeach,
					SailwindPortIndex.AlNilem
				},
				"Trader")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.DeadCove:
				case SailwindPortIndex.CrabBeach:
				case SailwindPortIndex.AlNilem:
					return "Thank you, we haven't heard from our family for a while.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class SageHillsWestTobaccoFarmerMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SageHillsWestTobaccoFarmerMission Instance = new();

		private SageHillsWestTobaccoFarmerMission()
			: base(
				"sage_hills_west_tobacco_farmer",
				"Sage Hills",
				SailwindPortIndex.SageHills,
				new Vector3(-165.170f, 41.067f, -82.020f),
				new Vector3(0f, 212.161f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.SerpentIsle,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.NewPort
				},
				"Green tobacco farmer (West Hill)")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.SerpentIsle:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.Chronos:
				case SailwindPortIndex.NewPort:
					return "Best tobacco in the world!";

				case SailwindPortIndex.KiciaBay:
					return "Heh! Tired of the blue one?";

				case SailwindPortIndex.DragonCliffs:
					return "Bi dien ha? This cut is too much!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class SanctuaryMainBuildingMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SanctuaryMainBuildingMission Instance = new();

		private SanctuaryMainBuildingMission()
			: base(
				"sanctuary_main_building",
				"Sanctuary",
				SailwindPortIndex.Sanctuary,
				new Vector3(28.065f, 3.492f, -28.618f),
				new Vector3(0f, 88.481f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.AlchemistsIsland,
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.TurtleIsland,
					SailwindPortIndex.SageHills,
					SailwindPortIndex.Chronos
				},
				"Main Building")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.AlchemistsIsland:
				case SailwindPortIndex.AlAnkhAcademy:
					return "(You hear one bell ring.)";

				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.AestraAbbey:
				case SailwindPortIndex.HappyBay:
					return "(You hear two bells ring.)";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.TurtleIsland:
				case SailwindPortIndex.SageHills:
					return "(You hear three bells ring.)";

				case SailwindPortIndex.Chronos:
					return "...Namo Amituofo.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

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

	internal sealed class SennaBlueTobaccoVendorMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SennaBlueTobaccoVendorMission Instance = new();

		private SennaBlueTobaccoVendorMission()
			: base(
				"senna_blue_tobacco_vendor",
				"Sen'na",
				SailwindPortIndex.Senna,
				new Vector3(85.640f, -0.105f, -173.198f),
				new Vector3(0f, 215.361f, 0f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.SerpentIsle
				},
				"Blue Tobacco vendor")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.DragonCliffs:
					return "Those tobacco from sage hills is obviously inferior.";

				case SailwindPortIndex.Chronos:
					return "Hmm...this might need special preservation.";

				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.SerpentIsle:
					return "Blue Dream worth every dragons you spend on it.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class SerpentIsleWesternmostHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SerpentIsleWesternmostHouseMission Instance = new();

		private SerpentIsleWesternmostHouseMission()
			: base(
				"serpent_isle_westernmost_house",
				"Serpent Isle",
				SailwindPortIndex.SerpentIsle,
				new Vector3(183.915f, 2.622f, -58.679f),
				new Vector3(0f, 171.934f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.FireFishTown,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.SirenSong,
					SailwindPortIndex.Sunspire,
					SailwindPortIndex.FeyValley,
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.OldAnkhTown,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.Chronos
				},
				"Westernmost house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Chronos:
					return "They must be rich if they can take a vacation here.";

				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.FireFishTown:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.SirenSong:
				case SailwindPortIndex.Sunspire:
				case SailwindPortIndex.FeyValley:
				case SailwindPortIndex.Neverdin:
				case SailwindPortIndex.OldAnkhTown:
				case SailwindPortIndex.Oasis:
					return "We'll check on cabin availability.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class SirenSongEmptyHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SirenSongEmptyHouseMission Instance = new();

		private SirenSongEmptyHouseMission()
			: base(
				"siren_song_empty_house",
				"Siren Song",
				SailwindPortIndex.SirenSong,
				new Vector3(16.545f, 1.400f, -21.654f),
				new Vector3(0f, 250.853f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.KiciaBay
				},
				"Empty house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.KiciaBay:
					return "(You slip the letter under the door)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

	internal sealed class SunspireSpyglassMakerMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SunspireSpyglassMakerMission Instance = new();

		private SunspireSpyglassMakerMission()
			: base(
				"sunspire_spyglass_maker",
				"Sunspire",
				SailwindPortIndex.Sunspire,
				new Vector3(37.768f, 6.183f, 133.613f),
				new Vector3(0f, 67.185f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.AlchemistsIsland,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.GoldRockCity
				},
				"Spyglass Maker")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AlAnkhAcademy:
					return "I'll make this my priority. Thank you for the letter.";

				case SailwindPortIndex.AlchemistsIsland:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.FireflyGrotto:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Chronos:
					return "I already have a two-month queue! Oh God...";

				case SailwindPortIndex.AestraAbbey:
					return "I wonder where they get all these coins...";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}

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

	internal sealed class TurtleIslandSunShamanMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly TurtleIslandSunShamanMission Instance = new();

		private TurtleIslandSunShamanMission()
			: base(
				"turtle_island_sun_shaman",
				"Turtle Island",
				SailwindPortIndex.TurtleIsland,
				new Vector3(-12.675f, 4.695f, 38.000f),
				new Vector3(0f, 49.052f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.Onna,
					SailwindPortIndex.Senna,
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.FeyValley,
					SailwindPortIndex.MountMalefic,
					SailwindPortIndex.AlchemistsIsland,
					SailwindPortIndex.SageHills,
					SailwindPortIndex.OldAnkhTown
				},
				"Sun Shaman")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Onna:
					return "....No.";

				case SailwindPortIndex.Neverdin:
				case SailwindPortIndex.AlAnkhAcademy:
					return "Sun is good.";

				case SailwindPortIndex.Senna:
				case SailwindPortIndex.Chronos:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.FeyValley:
				case SailwindPortIndex.MountMalefic:
				case SailwindPortIndex.AlchemistsIsland:
				case SailwindPortIndex.SageHills:
				case SailwindPortIndex.OldAnkhTown:
					return "Uh-um.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
