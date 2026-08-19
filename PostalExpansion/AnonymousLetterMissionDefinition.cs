using UnityEngine;

namespace PostalExpansion
{
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
}
