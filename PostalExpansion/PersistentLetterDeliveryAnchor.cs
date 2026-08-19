using UnityEngine;

namespace PostalExpansion
{
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
}
