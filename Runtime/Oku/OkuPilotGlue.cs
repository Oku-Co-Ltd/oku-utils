using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

using VTNetworking;
using VTOLVR.Multiplayer;

using Oku.Utils;

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

        /// <summary>An aircraft's root <see cref="PlayerVehicleNetSync"/>.</summary>
        /// <remarks>This is absolutely required, for local/remote to work properly.</remarks>
        public PlayerVehicleNetSync PvNetSync;

        /// <summary>The battery to link to this pilot seat.</summary>
        /// <remarks>This is required.</remarks>
        public Battery Battery;

        /// <summary>The weapon manager to link to this pilot seat.</summary>
        /// <remarks>This is required.</remarks>
        public WeaponManager RootWm;

        /// <summary>The vehicle master for the aircraft.</summary>
        /// <remarks>This is required.</remarks>
        public VehicleMaster VehicleMaster;

        /// <summary>The input manager for the aircraft.</summary>
        /// <remarks>This is required.</remarks>
        /// <exception>[note] should investigate to see if we can't collapse associated logic
        /// and pipe to whatever needs it</exception>
        public VehicleInputManager VehicleInputManager;

        /// <summary>The vehicle control manifest for the aircraft.</summary>
        /// <remarks>This is optional.</remarks>
        public VehicleControlManifest VehicleControlManifest;

        /// <summary>The ship controller for the aircraft.</summary>
        /// <remarks>This is required.</remarks>
        /// <exception>[note] should investigate to see if we can't collapse associated logic
        /// and pipe to whatever needs it</exception>
        public ShipController ShipController;

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
        /// <remarks>This is optional, as a HUDCanvas typically uses this.</remarks>
        [Header("HUD/Helmet")]
        public HUDMaskToggler HudMaskToggler;

        /// <summary>An HMCS display to link to this pilot seat.</summary>
        /// <remarks>This is optional. Do NOT set <see cref="AlwaysOnHmcsDisplayObject"/> if this is used.</remarks>
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

        public GameObject InstancedPilot(int slotIndex)
            => !_instancedPilots.ContainsKey(slotIndex) ? null : _instancedPilots[slotIndex];

        public GameObject InstancedPilotGetLocal(int slotIndex)
        {
            var pilot = InstancedPilot(slotIndex);
            if (pilot == null) return null;
            var tr = pilot.transform.Find("Local");
            return tr != null ? tr.gameObject : null;
        }

        public GameObject InstancedPilotGetLocal(GameObject instancedPilot)
        {
            var tr = instancedPilot.transform.Find("Local");
            return tr != null ? tr.gameObject : null;
        }

        public GameObject InstancedPilotGetRemote(int slotIndex)
        {
            var pilot = InstancedPilot(slotIndex);
            if (pilot == null) return null;
            var tr = pilot.transform.Find("Remote");
            return tr != null ? tr.gameObject : null;
        }

        public GameObject InstancedPilotGetRemote(GameObject instancedPilot)
        {
            var tr = instancedPilot.transform.Find("Remote");
            return tr != null ? tr.gameObject : null;
        }
        
        private readonly Dictionary<string, UnityAction> _actionMapVoid
            = new Dictionary<string, UnityAction>();
        private readonly Dictionary<string, UnityAction<Vector3>> _actionMapVec3
            = new Dictionary<string, UnityAction<Vector3>>();

        private readonly List<OkuPilotSpawn> _pilotSpawns = new List<OkuPilotSpawn>();
        private readonly Dictionary<int,GameObject> _instancedPilots = new Dictionary<int, GameObject>();

        private void OnEnable()
        {
            OkuLog.Info("[OnEnable] Enabled, doing enable magic.");
            _pilotSpawns.AddRange(GetComponentsInChildren<OkuPilotSpawn>().ToList());
            if (_pilotSpawns.Count == 0)
            {
                OkuLog.Error("[OnEnable] No OkuPilotSpawns found on the aircraft! Returning early, things will break.");
                enabled = false;
                return;
            }

            // ... we might need these... (man this is not very effective)
            var pilotPrefab = OkuConstDefs.GetPilotPrefab();

            // phew... let's go! for every single pilot:
            foreach (var slot in _pilotSpawns)
            {
                var slotIndex = slot.SlotIndex;

                // the aircraft should exist with all its components underneath us somewhere...
                if (!_instancedPilots.ContainsKey(slotIndex))
                {
                    // first time enabling: let's build it
                    OkuLog.Info($"[OnEnable] Instantiating pilot for slot {slotIndex}...");
                    var instancedPilot = Instantiate(pilotPrefab, transform);
                    instancedPilot.name = "OkuSpawnedPilot";
                    _instancedPilots.Add(slotIndex, instancedPilot);
                }
                else
                {
                    OkuLog.Info($"[OnEnable] Retethering pilot for slot {slotIndex}...");
                }

                var instancedPilotLocal = InstancedPilotGetLocal(slotIndex);
                var instancedPilotRemote = InstancedPilotGetRemote(slotIndex);

                // step zero, configure local and remote objects on PlayerVehicleNetSync
                if (PvNetSync != null)
                {
                    // enforcing that these are at the front of their lists, since who knows...
                    // control blocks limit variable scope and improve GC cleanup
                    {
                        var enableOnRemote = PvNetSync.enableOnRemote.ToList();
                        if (!enableOnRemote.Contains(instancedPilotRemote))
                        {
                            enableOnRemote.Insert(0, instancedPilotRemote);
                            PvNetSync.enableOnRemote = enableOnRemote.ToArray();
                        }
                    }
                    {
                        var destroyOnLocal = PvNetSync.destroyOnLocal.ToList();
                        if (!destroyOnLocal.Contains(instancedPilotRemote))
                        {
                            destroyOnLocal.Insert(0, instancedPilotRemote);
                            PvNetSync.destroyOnLocal = destroyOnLocal.ToArray();
                        }
                    }
                    {
                        var disableOnRemote = PvNetSync.disableOnRemote.ToList();
                        if (!disableOnRemote.Contains(instancedPilotLocal))
                        {
                            disableOnRemote.Insert(0, instancedPilotLocal);
                            PvNetSync.disableOnRemote = disableOnRemote.ToArray();
                        }
                    }
                    {
                        var destroyOnRemote = PvNetSync.destroyOnRemote.ToList();
                        if (!destroyOnRemote.Contains(instancedPilotLocal))
                        {
                            destroyOnRemote.Insert(0, instancedPilotLocal);
                            PvNetSync.destroyOnRemote = destroyOnRemote.ToArray();
                        }
                    }
                }
                else
                {
                    OkuLog.Error("[OnEnable] No PlayerVehicleNetSync found on loaded vehicle!");
                }

                var playerModelSync = GetComponentInChildren<PlayerModelSync>();
                if (playerModelSync != null)
                {
                    // we're attached to the root, so we should be the reference for this
                    playerModelSync.referenceTf = transform;
                }

                /* ********** Local *********** */

                var camRig = InstancedPilotGetLocal(slotIndex).transform.Find("EjectorSeat/CameraRigParent/[CameraRig]")
                    ?.gameObject;

                var rudderFootAnimator = instancedPilotLocal.GetComponentInChildren<RudderFootAnimator>();
                if (rudderFootAnimator != null)
                {
                    var pyrOutputs = VehicleInputManager.pyrOutputs.ToList();
                    if (!pyrOutputs.Contains(rudderFootAnimator))
                    {
                        pyrOutputs.Add(rudderFootAnimator);
                        VehicleInputManager.pyrOutputs = pyrOutputs.ToArray();
                    }
                }

                var ejectionSeat = instancedPilotLocal.GetComponentInChildren<EjectionSeat>();
                if (ejectionSeat != null)
                {
                    var endMission = instancedPilotLocal.GetComponentInChildren<EndMission>();
                    if (endMission != null)
                        OnEject.AddListener(endMission.EnableThumbButtonToOpen);

                    var tmpKey = ejectionSeat.name + "-OnEject";
                    if (!_actionMapVoid.ContainsKey(tmpKey))
                        _actionMapVoid.Add(tmpKey, () => OnEject.Invoke());
                    ejectionSeat.OnEject.AddListener(_actionMapVoid[tmpKey]);

                    if (ShipController != null)
                    {
                        ShipController.ejectionSeat = ejectionSeat;
                        if (camRig != null)
                        {
                            ShipController.cameraRig = camRig;
                        }
                    }
                }

                var hmdWeaponInfo = instancedPilotLocal.GetComponentInChildren<HMDWeaponInfo>();
                if (hmdWeaponInfo != null)
                {
                    hmdWeaponInfo.wm = RootWm;
                }

                var helmetController = instancedPilotLocal.GetComponentInChildren<HelmetController>();
                if (helmetController != null)
                {
                    helmetController.battery = Battery;
                    if (HudMaskToggler != null) helmetController.hudMaskToggler = HudMaskToggler;
                    if (HmcsDisplayObject != null) helmetController.hmcsDisplayObject = HmcsDisplayObject;
                    if (AlwaysOnHmcsDisplayObject != null)
                        helmetController.alwaysOnHmcsObject = AlwaysOnHmcsDisplayObject;
                }

                var blackoutEffect = instancedPilotLocal.GetComponentInChildren<BlackoutEffect>();
                if (blackoutEffect != null)
                {
                    blackoutEffect.flightInfo = FlightInfo;
                    blackoutEffect.rb = RootRb;

                    var endMission = instancedPilotLocal.GetComponentInChildren<EndMission>();
                    if (endMission != null)
                        OnAccelDeath.AddListener(endMission.ShowEndMission);

                    var tmpKey = blackoutEffect.name + "-OnAccelDeath";
                    if (!_actionMapVoid.ContainsKey(tmpKey))
                        _actionMapVoid.Add(tmpKey, () => OnAccelDeath.Invoke());
                    blackoutEffect.OnAccelDeath.AddListener(_actionMapVoid[tmpKey]);
                }

                var autoJoyLight = instancedPilotLocal.GetComponentsInChildren<EmissiveTextureLight>()
                    .FirstOrDefault(etl => etl.name.Equals("autoJoyLight"));
                if (autoJoyLight != null)
                {
                    autoJoyLight.battery = Battery;
                }

                var joyInteractables = instancedPilotLocal.GetComponentsInChildren<VRJoystick>();
                foreach (var joyInteractable in joyInteractables)
                {
                    if (VehicleInputManager != null)
                    {
                        var vcmJoysticks = VehicleControlManifest.joysticks.ToList();
                        if (!vcmJoysticks.Contains(joyInteractable))
                        {
                            vcmJoysticks.Add(joyInteractable);
                            VehicleControlManifest.joysticks = vcmJoysticks.ToArray();
                        }
                    }

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
                    instancedPilotLocal.GetComponentInChildren<CockpitWindAudioController>();
                if (cockpitWindAudioController != null)
                {
                    cockpitWindAudioController.flightInfo = FlightInfo;
                }

                var seatAdjuster = instancedPilotLocal.GetComponentInChildren<SeatAdjuster>();
                if (seatAdjuster != null)
                {
                    seatAdjuster.battery = Battery;
                }

                var atmosphericAudio = InstancedPilot(slotIndex).GetComponentInChildren<AtmosphericAudio>();
                if (atmosphericAudio != null)
                {
                    atmosphericAudio.flightInfo = FlightInfo;
                }

                /* ********** Remote *********** */

                var playerNameText = instancedPilotRemote.GetComponentInChildren<PlayerNameText>();
                if (playerNameText != null)
                {
                    playerNameText.netEntity = NetEntity;
                }

                var perTeamTextColor = instancedPilotRemote.GetComponentInChildren<PerTeamTextColor>();
                if (perTeamTextColor != null)
                {
                    perTeamTextColor.actor = Actor;
                    perTeamTextColor.netEnt = NetEntity;
                }
            }
            OkuLog.Info($"[OnEnable] Enable logic complete! Plane should be ready to go.");
        }

        private void OnDisable()
        {
            OkuLog.Info("[OnDisable] Disabled, doing disable cleanup work.");
            // it should never, but just in case...
            if (_instancedPilots.Count == 0)
            {
                OkuLog.Warn("[OnDisable] No instanced pilots found, nothing to do.");
                return;
            }
            if (_pilotSpawns.Count == 0)
            {
                OkuLog.Warn("[OnDisable] No OkuPilotSpawns found, nothing to do.");
                return;
            }

            foreach (var pilot in _instancedPilots)
            {
                var slotIndex = pilot.Key;
                OkuLog.Info($"[OnDisable] Untethering pilot in slot {slotIndex}...");
                var instancedPilot = pilot.Value;

                var instancedPilotLocal = InstancedPilotGetLocal(instancedPilot);
                var instancedPilotRemote = InstancedPilotGetRemote(instancedPilot);

                /* ********** Local *********** */

                var ejectionSeat = instancedPilotLocal.GetComponentInChildren<EjectionSeat>();
                if (ejectionSeat != null)
                {
                    var endMission = instancedPilotLocal.GetComponentInChildren<EndMission>();
                    if (endMission != null)
                        OnEject.RemoveListener(endMission.EnableThumbButtonToOpen);

                    var tmpKey = ejectionSeat.name + "-OnEject";
                    if (_actionMapVoid.ContainsKey(tmpKey))
                        ejectionSeat.OnEject.RemoveListener(_actionMapVoid[tmpKey]);
                }

                var joyInteractables = instancedPilotLocal.GetComponentsInChildren<VRJoystick>();
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

                var blackoutEffect = instancedPilotLocal.GetComponentInChildren<BlackoutEffect>();
                if (blackoutEffect != null)
                {
                    var endMission = instancedPilotLocal.GetComponentInChildren<EndMission>();
                    if (endMission != null)
                        OnAccelDeath.RemoveListener(endMission.ShowEndMission);

                    var tmpKey = blackoutEffect.name + "-OnAccelDeath";
                    if (_actionMapVoid.ContainsKey(tmpKey))
                        blackoutEffect.OnAccelDeath.RemoveListener(_actionMapVoid[tmpKey]);
                }

                /* ********** Remote *********** */
            }
            OkuLog.Info("[OnDisable] Disable logic complete!");
        }
    }
}
