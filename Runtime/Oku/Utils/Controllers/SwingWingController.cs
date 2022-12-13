using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Oku.Utils.Controllers
{
	[Serializable]
	public struct SwingWingParams
	{
		public string Id;

		public GameObject Pivot;

		public Vector3 BaseRotation;

		public Vector3 MaxRotation;
	}

	public class SwingWingController : MonoBehaviour
	{
		public List<SwingWingParams> WingPivots;

		public UnityAction<string, float> OnSetPivot;

		private readonly Dictionary<string, UnityAction<string, float>> _activeHooks =
			new Dictionary<string, UnityAction<string, float>>();

		private void OnEnable()
		{
			WingPivots.ForEach(wing =>
			{
				if (!_activeHooks.ContainsKey(wing.Id))
				{
					UnityAction<string, float> action = (id, extendParam) =>
					{
						if (id == null || !id.Equals(wing.Id))
							return;
						var newRot = Vector3.Lerp(wing.BaseRotation, wing.MaxRotation, extendParam);
						wing.Pivot.transform.localRotation = Quaternion.Euler(newRot);
					};
					_activeHooks.Add(wing.Id, action);
				}
				OnSetPivot += _activeHooks[wing.Id];
			});
		}
		private void OnDisable()
		{
			WingPivots.ForEach(wing =>
			{
				OnSetPivot -= _activeHooks[wing.Id];
			});
		}

		private void Update()
		{
			TestActivePivots
				.Where(kv => kv.Value)
				.Select(kv => kv.Key)
				.ToList().ForEach(key => OnSetPivot(key, TestPivotValue));
		}

		[Header("Testing")] public Oku.SerializableDictionary<string, bool> TestActivePivots = new Oku.SerializableDictionary<string, bool>();

		[Range(0, 1)] public float TestPivotValue = 0;
	}
}