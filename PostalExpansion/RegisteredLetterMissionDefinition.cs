using UnityEngine;

namespace PostalExpansion
{
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
}
