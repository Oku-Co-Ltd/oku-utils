using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Oku.Utils.Controllers
{
	[Serializable]
	public struct SwingWingParams
	{
		public string id;

		public GameObject pivot;

		public Vector3 baseRotation;

		public Vector3 maxRotation;
	}

	public class SwingWingController : MonoBehaviour
	{
		public List<SwingWingParams> wingPivots;

		public UnityAction<string, float> setPivot;

		private readonly Dictionary<string, UnityAction<string, float>> _activeHooks =
			new Dictionary<string, UnityAction<string, float>>();
		
		private void OnEnable()
		{
			wingPivots.ForEach(wing =>
			{
				if (!_activeHooks.ContainsKey(wing.id))
				{
					UnityAction<string, float> action = (id, extendParam) =>
					{
						if (id == null || !id.Equals(wing.id))
							return;
						var newRot = Vector3.Lerp(wing.baseRotation, wing.maxRotation, extendParam);
						wing.pivot.transform.localRotation = Quaternion.Euler(newRot);
					};
					_activeHooks.Add(wing.id, action);
				}
				setPivot += _activeHooks[wing.id];
			});
		}
		private void OnDisable()
		{
			wingPivots.ForEach(wing =>
			{
				setPivot -= _activeHooks[wing.id];
			});
		}

        public void AddWingPivot(SwingWingParams wing)
        {
            wingPivots.Add(wing);
            if (!enabled)
                return;
            if (!_activeHooks.ContainsKey(wing.id))
            {
                UnityAction<string, float> action = (id, extendParam) =>
                {
                    if (id == null || !id.Equals(wing.id))
                        return;
                    var newRot = Vector3.Lerp(wing.baseRotation, wing.maxRotation, extendParam);
                    wing.pivot.transform.localRotation = Quaternion.Euler(newRot);
                };
                _activeHooks.Add(wing.id, action);
            }
            setPivot += _activeHooks[wing.id];
		}

        public void RemoveWingPivot(string wingId)
        {
            if (!_activeHooks.ContainsKey(wingId))
                return;
			if (enabled)
                setPivot -= _activeHooks[wingId];
            
            _activeHooks.Remove(wingId);
        }
	}
}
