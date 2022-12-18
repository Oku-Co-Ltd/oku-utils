using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using VTNetworking;
using VTOLVR.Multiplayer;

/* All components to update on start:
 * > AudioListenerPosition
 * > EmissiveTextureLight
 * > HelmetController
 * > HMDWeaponInfo
 * > VRJoystick (any if present)
 */

namespace Oku
{
    /// <summary>
    /// This script is placed in the GameObject hierarchy where a pilot's local and remote prefab should be spawned.
    /// </summary>
    public class OkuPilotGlue : MonoBehaviour
    {
        /// <summary>An aircraft's FlightInfo instance.</summary>
        /// <remarks>This is required.</remarks>
        [Header("Aircraft Root")]
        public FlightInfo FlightInfo;

        /// <summary>An aircraft's root <see cref="Rigidbody"/>.</summary>
        /// <remarks>This is required.</remarks>
        public Rigidbody RootRb;

        /// <summary>The battery to link to this pilot seat.</summary>
        /// <remarks>This is required.</remarks>
        public Battery Battery;

        /// <summary>The weapon manager to link to this pilot seat.</summary>
        /// <remarks>This is required.</remarks>
        public WeaponManager RootWm;

        /// <summary>The vehicle master for the aircraft.</summary>
        /// <remarks>This is required.</remarks>
        public VehicleMaster VehicleMaster;

        /// <summary>The root net entity for the aircraft.</summary>
        /// <remarks>This is required.</remarks>
        public VTNetEntity NetEntity;

        /// <summary>The root actor representing the aircraft.</summary>
        /// <remarks>This is required.</remarks>
        public Actor Actor;

        /// <summary>Called when the pilot ejects.</summary>
        /// <remarks>Required. On the F/A-26B, is assigned to <see cref="FlightAssist.DisableAssist"/>,
        /// all <see cref="ModuleEngine.FailEngine"/>s, and <see cref="Battery.Disconnect"/>.</remarks>
        public UnityEvent OnEject;

        /// <summary>A HUD mask toggler to link to this pilot seat.</summary>
        /// <remarks>This is probably optional, as a HUDCanvas typically uses this.</remarks>
        [Header("Helmet")]
        public HUDMaskToggler HudMaskToggler;

        /// <summary>An HMCS display to link to this pilot seat.</summary>
        /// <remarks>This is probably optional. Do NOT set <see cref="AlwaysOnHmcsDisplayObject"/> if this is used.</remarks>
        public GameObject HmcsDisplayObject;

        /// <summary>An always-on HMCS display to link to this pilot seat.</summary>
        /// <remarks>This is optional. Do NOT set <see cref="HmcsDisplayObject"/> if this is used.</remarks>
        public GameObject AlwaysOnHmcsDisplayObject;

        /// <summary>Called when the joystick is rotated.</summary>
        /// <remarks>You probably want to assign this to <see cref="VehicleInputManager.SetJoystickPYR"/>.</remarks>
        [Header("Joystick")]
        public Vector3Event OnSetStick;
        /// <summary>Called when the thumbstick is rotated.</summary>
        /// <remarks>Optional. On the F/A-26B, is assigned to <see cref="MFDManager.OnInputAxis"/>.</remarks>
        public Vector3Event OnSetThumbstick;
        /// <summary>Called when the thumbstick is let go.</summary>
        /// <remarks>Optional. On the F/A-26B, is assigned to <see cref="MFDManager.OnInputAxisReleased"/>.</remarks>
        public UnityEvent OnResetThumbstick;
        /// <summary>Called when the thumbstick is pressed.</summary>
        /// <remarks>Optional. On the F/A-26B, is assigned to <see cref="MFDManager.OnInputButtonDown"/>.</remarks>
        public UnityEvent OnThumbstickButtonDown;
        /// <summary>Called when the thumbstick is released.</summary>
        /// <remarks>Optional. On the F/A-26B, is assigned to <see cref="MFDManager.OnInputButtonUp"/>.</remarks>
        public UnityEvent OnThumbstickButtonUp;
        /// <summary>Called when the thumbstick is pressed and released.</summary>
        /// <remarks>Optional. On the F/A-26B, is assigned to <see cref="MFDManager.OnInputButton"/>.</remarks>
        public UnityEvent OnThumbstickButton;
        /// <summary>Called when the menu button is pressed.</summary>
        /// <remarks>Optional. On the F/A-26B, is assigned to <see cref="WeaponManager.UserCycleActiveWeapon"/>.</remarks>
        public UnityEvent OnMenuButtonDown;
        /// <summary>Called when the trigger button is pulled.</summary>
        /// <remarks>Optional. On the F/A-26B, is assigned to <see cref="WeaponManager.StartFire"/>.</remarks>
        public UnityEvent OnTriggerDown;
        /// <summary>Called when the trigger button is pulled.</summary>
        /// <remarks>Optional. On the F/A-26B, is assigned to <see cref="WeaponManager.EndFire"/>.</remarks>
        public UnityEvent OnTriggerUp;

        /// <summary>Called when the player dies due to excessive G-force.</summary>
        /// <remarks>Required. On the F/A-26B, is assigned to <see cref="VehicleMaster.KillPilot"/>.</remarks>
        [Header("Blackout Effect")]
        public UnityEvent OnAccelDeath;

        public GameObject InstancedPilot { get; private set; }

        public GameObject InstancedPilotGetLocal => InstancedPilot.transform.Find("Local")?.gameObject;
        public GameObject InstancedPilotGetRemote => InstancedPilot.transform.Find("Remote")?.gameObject;


        private readonly Dictionary<string, UnityAction> _actionMapVoid
            = new Dictionary<string, UnityAction>();
        private readonly Dictionary<string, UnityAction<Vector3>> _actionMapVec3
            = new Dictionary<string, UnityAction<Vector3>>();
        
        private void OnEnable()
        {
            // the aircraft should exist with all its components underneath us somewhere...

            if (InstancedPilot == null)
            {
                var okuAb = AssetBundle.GetAllLoadedAssetBundles().First(ab => ab.name.Equals(OkuConstDefs.AssetBundleName));
                var pilotPrefab = okuAb.LoadAsset<GameObject>(OkuConstDefs.PilotPrefabBundlePath);
                InstancedPilot = Instantiate(pilotPrefab, transform);
                InstancedPilot.name = "OkuSpawnedPilot";
            }

            /* ********** Local *********** */

            var ejectionSeat = InstancedPilotGetLocal.GetComponentInChildren<EjectionSeat>();
            if (ejectionSeat != null)
            {
                var endMission = InstancedPilotGetLocal.GetComponentInChildren<EndMission>();
                if (endMission != null)
                    OnEject.AddListener(endMission.EnableThumbButtonToOpen);

                var tmpKey = ejectionSeat.name + "-OnEject";
                if (!_actionMapVoid.ContainsKey(tmpKey))
                    _actionMapVoid.Add(tmpKey, () => OnEject.Invoke());
                ejectionSeat.OnEject.AddListener(_actionMapVoid[tmpKey]);
            }

            var hmdWeaponInfo = InstancedPilotGetLocal.GetComponentInChildren<HMDWeaponInfo>();
            if (hmdWeaponInfo != null)
            {
                hmdWeaponInfo.wm = RootWm;
            }

            var helmetController = InstancedPilotGetLocal.GetComponentInChildren<HelmetController>();
            if (helmetController != null)
            {
                helmetController.battery = Battery;
                helmetController.hudMaskToggler = HudMaskToggler;
                helmetController.hmcsDisplayObject = HmcsDisplayObject;
                helmetController.alwaysOnHmcsObject = AlwaysOnHmcsDisplayObject;
            }

            var blackoutEffect = InstancedPilotGetLocal.GetComponentInChildren<BlackoutEffect>();
            if (blackoutEffect != null)
            {
                blackoutEffect.flightInfo = FlightInfo;
                blackoutEffect.rb = RootRb;

                var endMission = InstancedPilotGetLocal.GetComponentInChildren<EndMission>();
                if (endMission != null)
                    OnAccelDeath.AddListener(endMission.ShowEndMission);

                var tmpKey = blackoutEffect.name + "-OnAccelDeath";
                if (!_actionMapVoid.ContainsKey(tmpKey))
                    _actionMapVoid.Add(tmpKey, () => OnAccelDeath.Invoke());
                blackoutEffect.OnAccelDeath.AddListener(_actionMapVoid[tmpKey]);
            }

            var autoJoyLight = InstancedPilotGetLocal.GetComponentsInChildren<EmissiveTextureLight>()
                .FirstOrDefault(etl => etl.name.Equals("autoJoyLight"));
            if (autoJoyLight != null)
            {
                autoJoyLight.battery = Battery;
            }

            var joyInteractables = InstancedPilotGetLocal.GetComponentsInChildren<VRJoystick>();
            foreach (var joyInteractable in joyInteractables)
            {
                // let's build these out -- these can be added/removed on enable/disable
                // one-arg actions: Vector3
                var tmpKey = joyInteractable.name + "-OnSetStick";
                if (!_actionMapVec3.ContainsKey(tmpKey))
                    _actionMapVec3.Add(tmpKey, val => OnSetStick.Invoke(val));
                joyInteractable.OnSetStick.AddListener(_actionMapVec3[tmpKey]);

                tmpKey = joyInteractable.name + "-OnSetThumbstick";
                if (!_actionMapVec3.ContainsKey(tmpKey))
                    _actionMapVec3.Add(tmpKey, val => OnSetThumbstick.Invoke(val));
                joyInteractable.OnSetThumbstick.AddListener(_actionMapVec3[tmpKey]);

                // zero-argument actions
                tmpKey = joyInteractable.name + "-OnResetThumbstick";
                if (!_actionMapVoid.ContainsKey(tmpKey))
                    _actionMapVoid.Add(tmpKey, () => OnResetThumbstick.Invoke());
                joyInteractable.OnResetThumbstick.AddListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnThumbstickButtonDown";
                if (!_actionMapVoid.ContainsKey(tmpKey))
                    _actionMapVoid.Add(tmpKey, () => OnThumbstickButtonDown.Invoke());
                joyInteractable.OnThumbstickButtonDown.AddListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnThumbstickButtonUp";
                if (!_actionMapVoid.ContainsKey(tmpKey))
                    _actionMapVoid.Add(tmpKey, () => OnThumbstickButtonUp.Invoke());
                joyInteractable.OnThumbstickButtonUp.AddListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnThumbstickButton";
                if (!_actionMapVoid.ContainsKey(tmpKey))
                    _actionMapVoid.Add(tmpKey, () => OnThumbstickButton.Invoke());
                joyInteractable.OnThumbstickButton.AddListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnMenuButtonDown";
                if (!_actionMapVoid.ContainsKey(tmpKey))
                    _actionMapVoid.Add(tmpKey, () => OnMenuButtonDown.Invoke());
                joyInteractable.OnMenuButtonDown.AddListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnTriggerDown";
                if (!_actionMapVoid.ContainsKey(tmpKey))
                    _actionMapVoid.Add(tmpKey, () => OnTriggerDown.Invoke());
                joyInteractable.OnTriggerDown.AddListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnTriggerUp";
                if (!_actionMapVoid.ContainsKey(tmpKey))
                    _actionMapVoid.Add(tmpKey, () => OnTriggerUp.Invoke());
                joyInteractable.OnTriggerUp.AddListener(_actionMapVoid[tmpKey]);
            }

            var cockpitWindAudioController =
                InstancedPilotGetLocal.GetComponentInChildren<CockpitWindAudioController>();
            if (cockpitWindAudioController != null)
            {
                cockpitWindAudioController.flightInfo = FlightInfo;
            }

            var seatAdjuster = InstancedPilotGetLocal.GetComponentInChildren<SeatAdjuster>();
            if (seatAdjuster != null)
            {
                seatAdjuster.battery = Battery;
            }

            /* ********** Remote *********** */

            var playerNameText = InstancedPilotGetRemote.GetComponentInChildren<PlayerNameText>();
            if (playerNameText != null)
            {
                playerNameText.netEntity = NetEntity;
            }

            var perTeamTextColor = InstancedPilotGetRemote.GetComponentInChildren<PerTeamTextColor>();
            if (perTeamTextColor != null)
            {
                perTeamTextColor.actor = Actor;
                perTeamTextColor.netEnt = NetEntity;
            }
        }

        private void OnDisable()
        {
            // it should never, but just in case...
            if (InstancedPilot == null) return;

            /* ********** Local *********** */
            
            var ejectionSeat = InstancedPilotGetLocal.GetComponentInChildren<EjectionSeat>();
            if (ejectionSeat != null)
            {
                var endMission = InstancedPilotGetLocal.GetComponentInChildren<EndMission>();
                if (endMission != null)
                    OnEject.RemoveListener(endMission.EnableThumbButtonToOpen);

                var tmpKey = ejectionSeat.name + "-OnEject";
                if (_actionMapVoid.ContainsKey(tmpKey))
                    ejectionSeat.OnEject.RemoveListener(_actionMapVoid[tmpKey]);
            }

            var joyInteractables = InstancedPilotGetLocal.GetComponentsInChildren<VRJoystick>();
            foreach (var joyInteractable in joyInteractables)
            {
                // one-arg actions: Vector3
                var tmpKey = joyInteractable.name + "-OnSetStick";
                if (_actionMapVec3.ContainsKey(tmpKey))
                    joyInteractable.OnSetStick.RemoveListener(_actionMapVec3[tmpKey]);

                tmpKey = joyInteractable.name + "-OnSetThumbstick";
                if (_actionMapVec3.ContainsKey(tmpKey))
                    joyInteractable.OnSetThumbstick.RemoveListener(_actionMapVec3[tmpKey]);

                // zero-argument actions
                tmpKey = joyInteractable.name + "-OnResetThumbstick";
                if (_actionMapVoid.ContainsKey(tmpKey))
                    joyInteractable.OnResetThumbstick.RemoveListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnThumbstickButtonDown";
                if (_actionMapVoid.ContainsKey(tmpKey))
                    joyInteractable.OnThumbstickButtonDown.RemoveListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnThumbstickButtonUp";
                if (_actionMapVoid.ContainsKey(tmpKey))
                    joyInteractable.OnThumbstickButtonUp.RemoveListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnThumbstickButton";
                if (_actionMapVoid.ContainsKey(tmpKey))
                    joyInteractable.OnThumbstickButton.RemoveListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnMenuButtonDown";
                if (_actionMapVoid.ContainsKey(tmpKey))
                    joyInteractable.OnMenuButtonDown.RemoveListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnTriggerDown";
                if (_actionMapVoid.ContainsKey(tmpKey))
                    joyInteractable.OnTriggerDown.RemoveListener(_actionMapVoid[tmpKey]);

                tmpKey = joyInteractable.name + "-OnTriggerUp";
                if (_actionMapVoid.ContainsKey(tmpKey))
                    joyInteractable.OnTriggerUp.RemoveListener(_actionMapVoid[tmpKey]);
            }

            var blackoutEffect = InstancedPilotGetLocal.GetComponentInChildren<BlackoutEffect>();
            if (blackoutEffect != null)
            {
                var endMission = InstancedPilotGetLocal.GetComponentInChildren<EndMission>();
                if (endMission != null)
                    OnAccelDeath.RemoveListener(endMission.ShowEndMission);

                var tmpKey = blackoutEffect.name + "-OnAccelDeath";
                if (_actionMapVoid.ContainsKey(tmpKey))
                    blackoutEffect.OnAccelDeath.RemoveListener(_actionMapVoid[tmpKey]);
            }

            /* ********** Remote *********** */
        }
    }
}
