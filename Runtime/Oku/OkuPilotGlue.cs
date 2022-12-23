using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        /// <summary>An aircraft's root <see cref="PlayerVehicleSetup"/>.</summary>
        /// <remarks>This is required.</remarks>
        public PlayerVehicleSetup PvSetup;

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

        /// <summary>The visual target finder component, so the player can find targets.</summary>
        /// <remarks>This is  required.</remarks>
        public VisualTargetFinder VisualTargetFinder;

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

        /// <summary>The interactable responsible for toggling the HMCS power status.</summary>
        /// <remarks>This is optional.</remarks>
        public VRLever HmcsPowerInteractable;

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

        /// <summary>Used to swap between center and side joystick.</summary>
        /// <remarks>Optional, but preferred.</remarks>
        public ABObjectToggler JoystickToggler;

        /// <summary>Used to raise the seat.</summary>
        /// <remarks>Optional, but strongly preferred.</remarks>
        [Header("Seat Adjust")]
        public VRInteractable RaiseSeatInteractable;

        /// <summary>Used to lower the seat.</summary>
        /// <remarks>Optional, but strongly preferred.</remarks>
        public VRInteractable LowerSeatInteractable;

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
        private readonly Dictionary<string, UnityAction<int>> _actionMapInt
            = new Dictionary<string, UnityAction<int>>();
        private readonly Dictionary<string, UnityAction<Vector3>> _actionMapVec3
            = new Dictionary<string, UnityAction<Vector3>>();

        private readonly List<OkuPilotSpawn> _pilotSpawns = new List<OkuPilotSpawn>();
        private readonly Dictionary<int,GameObject> _instancedPilots = new Dictionary<int, GameObject>();
        
        // WARNING:
        // I am not liable for loss of braincells in reading this code. Remember that this is
        // designed to be the firmware driver interface for Baha systems incorporated, so you should
        // never have to touch or see it as an Oku user.
        //
        //                 Garuda 1
        // << Bahacode must be fought with Bahacode. >>

        private void Awake()
        {
            AttachPilots();
        }

        public void AttachPilots()
        {
            const string lpf = "[AttachPilots]";
            OkuLog.Info($"{lpf} Enabled, doing enable magic.");
            var allPilotSpawns = GetComponentsInChildren<OkuPilotSpawn>().ToList();
            allPilotSpawns.ForEach(ps =>
            {
                if (!_pilotSpawns.Contains(ps)) _pilotSpawns.Add(ps);
            });
            if (_pilotSpawns.Count == 0)
            {
                OkuLog.Error($"{lpf} No OkuPilotSpawns found on the aircraft! This isn't good. Returning early...");
                enabled = false;
                FlightSceneManager.instance.ReturnToBriefingOrExitScene();
                return;
            }

            // ... we might need these... (man this is not very effective)
            var pilotPrefab = OkuConstDefs.GetPilotPrefab();
            if (pilotPrefab == null)
            {
                OkuLog.Error($"{lpf} Pilot prefab returned null! This isn't good. Returning early...");
                enabled = false;
                FlightSceneManager.instance.ReturnToBriefingOrExitScene();
                return;
            }

            // phew... let's go! for every single pilot:
            foreach (var slot in _pilotSpawns)
            {
                var slotIndex = slot.SlotIndex;

                // the aircraft should exist with all its components underneath us somewhere...
                if (!_instancedPilots.ContainsKey(slotIndex))
                {

                    // first time enabling: let's build it
                    OkuLog.Info($"{lpf} Instantiating pilot for slot {slotIndex}...");
                    var instancedPilot = UnityUtils.InstantiateDisabled(pilotPrefab, transform);
                    //var instancedPilot = Instantiate(pilotPrefab, transform);
                    instancedPilot.name = $"OkuPilot_{slotIndex}";
                    _instancedPilots.Add(slotIndex, instancedPilot);
                }
                else
                {
                    OkuLog.Info($"{lpf} Retethering pilot for slot {slotIndex}...");
                }

                var dbgm = $"{lpf}[{slotIndex}]";
                OkuLog.Info($"{dbgm} Instantiate successful, acquiring local/remote");

                var instancedPilotLocal = InstancedPilotGetLocal(slotIndex);
                var instancedPilotRemote = InstancedPilotGetRemote(slotIndex);

                OkuLog.Info($"{dbgm} Local/remote grabbed!");


                OkuLog.Info($"{dbgm} Hook: PvNetSync");
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
                    OkuLog.Error($"{lpf} No PlayerVehicleNetSync found on loaded vehicle!");
                }

                OkuLog.Info($"{dbgm} Hook: PlayerModelSync");
                var playerModelSync = GetComponentInChildren<PlayerModelSync>();
                if (playerModelSync != null)
                {
                    // we're attached to the root, so we should be the reference for this
                    playerModelSync.referenceTf = transform;
                }
                else
                {
                    OkuLog.Warn($"{lpf} Warning: missing PlayerModelSync in pilot (tell Oku about this)");
                }

                var ejectSync = GetComponentInChildren<EjectSync>();
                if (ejectSync == null)
                {
                    OkuLog.Warn($"{lpf} Warning: missing EjectSync in pilot (tell Oku about this)");
                }
                
                // need to plug the net entity syncs so they are correctly synchronized by MP
                var vtNetEntity = GetComponent<VTNetEntity>();
                if (vtNetEntity != null)
                {
                    if (playerModelSync != null && !vtNetEntity.netSyncs.Contains(playerModelSync))
                        vtNetEntity.netSyncs.Add(playerModelSync);
                    if (ejectSync != null && !vtNetEntity.netSyncs.Contains(ejectSync))
                        vtNetEntity.netSyncs.Add(ejectSync);
                }
                else
                {
                    OkuLog.Warn($"{lpf} Warning: missing VTNetEntity in aircraft root");
                }

                /* ********** Local *********** */

                var cameraEyeObject = instancedPilotLocal.transform
                    .Find("EjectorSeat/CameraRigParent/[CameraRig]/Camera (eye)");

                OkuLog.Info($"{dbgm} Hook: VisualTargetFinder");
                if (VisualTargetFinder != null)
                {
                    VisualTargetFinder.fovReference = cameraEyeObject;
                }

                OkuLog.Info($"{dbgm} Hook: HelmetController");
                var helmetController = instancedPilotLocal.GetComponentInChildren<HelmetController>();
                
                if (helmetController == null)
                {
                    OkuLog.Info($"{lpf} No HelmetController found, skipping");
                }
                else if (helmetController != null && HmcsPowerInteractable != null)
                {
                    var tmpKey = HmcsPowerInteractable.name + $"{slotIndex}-OnSetState";
                    if (!_actionMapInt.ContainsKey(tmpKey))
                        _actionMapInt.Add(tmpKey, val => helmetController.SetPower(val));
                    HmcsPowerInteractable.OnSetState.AddListener(_actionMapInt[tmpKey]);
                }

                OkuLog.Info($"{dbgm} Hook: TargetingMFDPage");
                var targetingMfdPage = GetComponentInChildren<TargetingMFDPage>();
                if (targetingMfdPage != null && helmetController != null)
                {
                    targetingMfdPage.SetHelmet(helmetController);
                }

                OkuLog.Info($"{dbgm} Hook: HUDElevationLadder");
                var hudElevationLadder = GetComponentInChildren<HUDElevationLadder>();
                if (hudElevationLadder != null)
                {
                    if (cameraEyeObject != null)
                    {
                        hudElevationLadder.headTransform = cameraEyeObject;
                    }
                }
                else
                {
                    OkuLog.Info($"{lpf} No HUDElevationLadder found, skipping");
                }

                OkuLog.Info($"{dbgm} Hook: JoystickToggler");
                if (JoystickToggler != null)
                {
                    var sideStickObjects = instancedPilotLocal.transform
                        .Find("SideStickObjects");
                    if (sideStickObjects != null && !JoystickToggler.aObjects.Contains(sideStickObjects.gameObject))
                    {
                        JoystickToggler.aObjects = JoystickToggler.aObjects.Append(sideStickObjects.gameObject).ToArray();
                    }

                    var centerStickObjects = instancedPilotLocal.transform
                        .Find("CenterStickObjects");
                    if (centerStickObjects != null && !JoystickToggler.aObjects.Contains(centerStickObjects.gameObject))
                    {
                        JoystickToggler.aObjects = JoystickToggler.aObjects.Append(centerStickObjects.gameObject).ToArray();
                    }

                    var acesSideEjectBase = instancedPilotLocal.transform
                        .Find("EjectorSeat/acesSeatPos/acesSeatFrame/acesSideEjectBase");
                    if (acesSideEjectBase != null && !JoystickToggler.aObjects.Contains(acesSideEjectBase.gameObject))
                    {
                        JoystickToggler.aObjects = JoystickToggler.aObjects.Append(acesSideEjectBase.gameObject).ToArray();
                    }

                    var acesCenterEjectBase = instancedPilotLocal.transform
                        .Find("EjectorSeat/acesSeatPos/acesSeatFrame/acesCenterEjectBase");
                    if (acesCenterEjectBase != null && !JoystickToggler.aObjects.Contains(acesCenterEjectBase.gameObject))
                    {
                        JoystickToggler.aObjects = JoystickToggler.aObjects.Append(acesCenterEjectBase.gameObject).ToArray();
                    }
                }
                else
                {
                    OkuLog.Warn($"{lpf} Warning: no JoystickToggler found, was this intentional?");
                }

                OkuLog.Info($"{dbgm} Hook: PlayerGTransform");
                var gRef = instancedPilotLocal.transform
                    .Find("EjectorSeat/CameraRigParent/PlayerGTransform");
                if (gRef != null && FlightInfo != null)
                {
                    FlightInfo.playerGTransform = gRef;
                }
                else
                {
                    OkuLog.Warn($"{lpf} Warning: no PlayerGTransform found on the pilot (tell Oku about this)");
                }

                var camRig = instancedPilotLocal.transform.Find("EjectorSeat/CameraRigParent/[CameraRig]")
                    ?.gameObject;

                if (camRig == null)
                {
                    OkuLog.Error($"{lpf} Error: no camera rig found on the pilot! (tell Oku about this)");
                }

                OkuLog.Info($"{dbgm} Hook: RudderFootAnimator");
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
                else
                {
                    OkuLog.Warn($"{lpf} Warning: no RudderFootAnimator found on the pilot (tell Oku about this)");
                }

                OkuLog.Info($"{dbgm} Hook: EjectionSeat");
                var ejectionSeat = instancedPilotLocal.GetComponentInChildren<EjectionSeat>();
                if (ejectionSeat != null)
                {
                    var endMission = instancedPilotLocal.GetComponentInChildren<EndMission>();
                    if (endMission != null)
                        OnEject.AddListener(endMission.EnableThumbButtonToOpen);

                    var tmpKey = ejectionSeat.name + $"{slotIndex}-OnEject";
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

                OkuLog.Info($"{dbgm} Hook: HMDWeaponInfo");
                var hmdWeaponInfo = instancedPilotLocal.GetComponentInChildren<HMDWeaponInfo>();
                if (hmdWeaponInfo != null)
                {
                    hmdWeaponInfo.wm = RootWm;
                }
                else
                {
                    OkuLog.Info($"{lpf} No HMDWeaponInfo found, skipping");
                }

                OkuLog.Info($"{dbgm} Hook: HelmetController <2>");
                if (helmetController != null)
                {
                    helmetController.battery = Battery;
                    if (HudMaskToggler != null) helmetController.hudMaskToggler = HudMaskToggler;
                    if (HmcsDisplayObject != null) helmetController.hmcsDisplayObject = HmcsDisplayObject;
                    if (AlwaysOnHmcsDisplayObject != null)
                        helmetController.alwaysOnHmcsObject = AlwaysOnHmcsDisplayObject;
                }

                OkuLog.Info($"{dbgm} Hook: BlackoutEffect");
                var blackoutEffect = instancedPilotLocal.GetComponentInChildren<BlackoutEffect>();
                if (blackoutEffect != null)
                {
                    blackoutEffect.flightInfo = FlightInfo;
                    blackoutEffect.rb = RootRb;

                    var endMission = instancedPilotLocal.GetComponentInChildren<EndMission>();
                    if (endMission != null)
                        OnAccelDeath.AddListener(endMission.ShowEndMission);

                    var tmpKey = blackoutEffect.name + $"{slotIndex}-OnAccelDeath";
                    if (!_actionMapVoid.ContainsKey(tmpKey))
                        _actionMapVoid.Add(tmpKey, () => OnAccelDeath.Invoke());
                    blackoutEffect.OnAccelDeath.AddListener(_actionMapVoid[tmpKey]);

                    if (PvSetup != null && !PvSetup.disableComponentOnConfig.Contains(blackoutEffect))
                    {
                        PvSetup.disableComponentOnConfig.Add(blackoutEffect);
                    }
                }
                else
                {
                    OkuLog.Warn($"{lpf} Warning: no BlackoutEffect found on the pilot (tell Oku about this)");
                }

                OkuLog.Info($"{dbgm} Hook: EmissiveTextureLight");
                var autoJoyLight = instancedPilotLocal.GetComponentsInChildren<EmissiveTextureLight>()
                    .FirstOrDefault(etl => etl.name.Equals("autoJoyLight"));
                if (autoJoyLight != null)
                {
                    autoJoyLight.battery = Battery;
                }

                OkuLog.Info($"{dbgm} Hook: VRJoystick");
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
                    else
                    {
                        OkuLog.Error($"{lpf} Error: no VehicleInputManager found! Please check your aircraft prefab for missing links.");
                    }

                    // let's build these out -- these can be added/removed on enable/disable
                    // one-arg actions: Vector3
                    var tmpKey = joyInteractable.name + $"{slotIndex}-OnSetStick";
                    if (!_actionMapVec3.ContainsKey(tmpKey))
                        _actionMapVec3.Add(tmpKey, val => OnSetStick.Invoke(val));
                    joyInteractable.OnSetStick.AddListener(_actionMapVec3[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnSetThumbstick";
                    if (!_actionMapVec3.ContainsKey(tmpKey))
                        _actionMapVec3.Add(tmpKey, val => OnSetThumbstick.Invoke(val));
                    joyInteractable.OnSetThumbstick.AddListener(_actionMapVec3[tmpKey]);

                    // zero-argument actions
                    tmpKey = joyInteractable.name + $"{slotIndex}-OnResetThumbstick";
                    if (!_actionMapVoid.ContainsKey(tmpKey))
                        _actionMapVoid.Add(tmpKey, () => OnResetThumbstick.Invoke());
                    joyInteractable.OnResetThumbstick.AddListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnThumbstickButtonDown";
                    if (!_actionMapVoid.ContainsKey(tmpKey))
                        _actionMapVoid.Add(tmpKey, () => OnThumbstickButtonDown.Invoke());
                    joyInteractable.OnThumbstickButtonDown.AddListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnThumbstickButtonUp";
                    if (!_actionMapVoid.ContainsKey(tmpKey))
                        _actionMapVoid.Add(tmpKey, () => OnThumbstickButtonUp.Invoke());
                    joyInteractable.OnThumbstickButtonUp.AddListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnThumbstickButton";
                    if (!_actionMapVoid.ContainsKey(tmpKey))
                        _actionMapVoid.Add(tmpKey, () => OnThumbstickButton.Invoke());
                    joyInteractable.OnThumbstickButton.AddListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnMenuButtonDown";
                    if (!_actionMapVoid.ContainsKey(tmpKey))
                        _actionMapVoid.Add(tmpKey, () => OnMenuButtonDown.Invoke());
                    joyInteractable.OnMenuButtonDown.AddListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnTriggerDown";
                    if (!_actionMapVoid.ContainsKey(tmpKey))
                        _actionMapVoid.Add(tmpKey, () => OnTriggerDown.Invoke());
                    joyInteractable.OnTriggerDown.AddListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnTriggerUp";
                    if (!_actionMapVoid.ContainsKey(tmpKey))
                        _actionMapVoid.Add(tmpKey, () => OnTriggerUp.Invoke());
                    joyInteractable.OnTriggerUp.AddListener(_actionMapVoid[tmpKey]);
                }

                OkuLog.Info($"{dbgm} Hook: CockpitWindAudioController");
                var cockpitWindAudioController =
                    instancedPilotLocal.GetComponentInChildren<CockpitWindAudioController>();
                if (cockpitWindAudioController != null)
                {
                    cockpitWindAudioController.flightInfo = FlightInfo;
                }
                else
                {
                    OkuLog.Warn($"{lpf} Warning: no CockpitWindAudioController found on the pilot (tell Oku about this)");
                }

                OkuLog.Info($"{dbgm} Hook: SeatAdjuster");
                var seatAdjuster = instancedPilotLocal.GetComponentInChildren<SeatAdjuster>();
                if (seatAdjuster != null)
                {
                    seatAdjuster.battery = Battery;
                    var ejectionSeatAdjust = ejectionSeat.GetComponent<SeatAdjuster>();
                    if (RaiseSeatInteractable != null && ejectionSeatAdjust != null)
                    {
                        // we keep one instance of the listener instead of racking up 20 doing this, because
                        // RemoveListener removes all instances of the specified action
                        RaiseSeatInteractable.OnInteract.RemoveListener(ejectionSeatAdjust.StartRaiseSeat);
                        RaiseSeatInteractable.OnInteract.AddListener(ejectionSeatAdjust.StartRaiseSeat);
                        RaiseSeatInteractable.OnStopInteract.RemoveListener(ejectionSeatAdjust.Stop);
                        RaiseSeatInteractable.OnStopInteract.AddListener(ejectionSeatAdjust.Stop);
                    }

                    if (LowerSeatInteractable != null && ejectionSeatAdjust != null)
                    {
                        LowerSeatInteractable.OnInteract.RemoveListener(ejectionSeatAdjust.StartLowerSeat);
                        LowerSeatInteractable.OnInteract.AddListener(ejectionSeatAdjust.StartLowerSeat);
                        LowerSeatInteractable.OnStopInteract.RemoveListener(ejectionSeatAdjust.Stop);
                        LowerSeatInteractable.OnStopInteract.AddListener(ejectionSeatAdjust.Stop);
                    }
                }
                else
                {
                    OkuLog.Warn($"{lpf} Warning: no SeatAdjuster found on the pilot (tell Oku about this)");
                }

                OkuLog.Info($"{dbgm} Hook: AtmosphericAudio");
                var atmosphericAudio = InstancedPilot(slotIndex).GetComponentInChildren<AtmosphericAudio>();
                if (atmosphericAudio != null)
                {
                    atmosphericAudio.flightInfo = FlightInfo;
                }
                else
                {
                    OkuLog.Warn($"{lpf} Warning: no AtmosphericAudio found on the pilot (tell Oku about this)");
                }

                /* ********** Remote *********** */

                OkuLog.Info($"{dbgm} Hook: PlayerNameText");
                var playerNameText = instancedPilotRemote.GetComponentInChildren<PlayerNameText>();
                if (playerNameText != null)
                {
                    playerNameText.netEntity = NetEntity;
                }
                else
                {
                    OkuLog.Warn($"{lpf} Warning: no PlayerNameText found on the pilot (tell Oku about this)");
                }

                OkuLog.Info($"{dbgm} Hook: PerTeamTextColor");
                var perTeamTextColor = instancedPilotRemote.GetComponentInChildren<PerTeamTextColor>();
                if (perTeamTextColor != null)
                {
                    perTeamTextColor.actor = Actor;
                    perTeamTextColor.netEnt = NetEntity;
                }
                else
                {
                    OkuLog.Warn($"{lpf} Warning: no PerTeamTextColor found on the pilot (tell Oku about this)");
                }

                //InstancedPilot(slotIndex).SetActive(true);
                OkuLog.Info($"{dbgm} Pilot slot complete.");
            }
            //OkuLog.Info($"{lpf} Lastly enabling pilots...");
            OkuLog.Info($"{lpf} Enable logic complete! Plane should be ready to go.");
        }

        public void Start()
        {
            foreach (var kvp in _instancedPilots)
            {
                kvp.Value.SetActive(true);
            }
        }

        // currently unused, probably doesn't need to be?
        public void DetachPilots()
        {
            const string lpf = "[DetachPilots]";
            OkuLog.Info($"{lpf} Disabled, doing disable cleanup work.");
            // it should never, but just in case...
            if (_instancedPilots.Count == 0)
            {
                OkuLog.Warn($"{lpf} No instanced pilots found, nothing to do.");
                return;
            }
            if (_pilotSpawns.Count == 0)
            {
                OkuLog.Warn($"{lpf} No OkuPilotSpawns found, nothing to do.");
                return;
            }

            foreach (var pilot in _instancedPilots)
            {
                var slotIndex = pilot.Key;
                OkuLog.Info($"{lpf} Untethering pilot in slot {slotIndex}...");
                var instancedPilot = pilot.Value;

                var instancedPilotLocal = InstancedPilotGetLocal(instancedPilot);
                var instancedPilotRemote = InstancedPilotGetRemote(instancedPilot);

                /* ********** Local *********** */
                
                if (HmcsPowerInteractable != null)
                {
                    var tmpKey = HmcsPowerInteractable.name + $"{slotIndex}-OnSetState";
                    if (_actionMapInt.ContainsKey(tmpKey))
                        HmcsPowerInteractable.OnSetState.RemoveListener(_actionMapInt[tmpKey]);
                }

                var ejectionSeat = instancedPilotLocal.GetComponentInChildren<EjectionSeat>();
                if (ejectionSeat != null)
                {
                    var endMission = instancedPilotLocal.GetComponentInChildren<EndMission>();
                    if (endMission != null)
                        OnEject.RemoveListener(endMission.EnableThumbButtonToOpen);

                    var tmpKey = ejectionSeat.name + $"{slotIndex}-OnEject";
                    if (_actionMapVoid.ContainsKey(tmpKey))
                        ejectionSeat.OnEject.RemoveListener(_actionMapVoid[tmpKey]);
                }

                var joyInteractables = instancedPilotLocal.GetComponentsInChildren<VRJoystick>();
                foreach (var joyInteractable in joyInteractables)
                {
                    // one-arg actions: Vector3
                    var tmpKey = joyInteractable.name + $"{slotIndex}-OnSetStick";
                    if (_actionMapVec3.ContainsKey(tmpKey))
                        joyInteractable.OnSetStick.RemoveListener(_actionMapVec3[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnSetThumbstick";
                    if (_actionMapVec3.ContainsKey(tmpKey))
                        joyInteractable.OnSetThumbstick.RemoveListener(_actionMapVec3[tmpKey]);

                    // zero-argument actions
                    tmpKey = joyInteractable.name + $"{slotIndex}-OnResetThumbstick";
                    if (_actionMapVoid.ContainsKey(tmpKey))
                        joyInteractable.OnResetThumbstick.RemoveListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnThumbstickButtonDown";
                    if (_actionMapVoid.ContainsKey(tmpKey))
                        joyInteractable.OnThumbstickButtonDown.RemoveListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnThumbstickButtonUp";
                    if (_actionMapVoid.ContainsKey(tmpKey))
                        joyInteractable.OnThumbstickButtonUp.RemoveListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnThumbstickButton";
                    if (_actionMapVoid.ContainsKey(tmpKey))
                        joyInteractable.OnThumbstickButton.RemoveListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnMenuButtonDown";
                    if (_actionMapVoid.ContainsKey(tmpKey))
                        joyInteractable.OnMenuButtonDown.RemoveListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnTriggerDown";
                    if (_actionMapVoid.ContainsKey(tmpKey))
                        joyInteractable.OnTriggerDown.RemoveListener(_actionMapVoid[tmpKey]);

                    tmpKey = joyInteractable.name + $"{slotIndex}-OnTriggerUp";
                    if (_actionMapVoid.ContainsKey(tmpKey))
                        joyInteractable.OnTriggerUp.RemoveListener(_actionMapVoid[tmpKey]);
                }

                var blackoutEffect = instancedPilotLocal.GetComponentInChildren<BlackoutEffect>();
                if (blackoutEffect != null)
                {
                    var endMission = instancedPilotLocal.GetComponentInChildren<EndMission>();
                    if (endMission != null)
                        OnAccelDeath.RemoveListener(endMission.ShowEndMission);

                    var tmpKey = blackoutEffect.name + $"{slotIndex}-OnAccelDeath";
                    if (_actionMapVoid.ContainsKey(tmpKey))
                        blackoutEffect.OnAccelDeath.RemoveListener(_actionMapVoid[tmpKey]);
                }

                /* ********** Remote *********** */
            }
            OkuLog.Info($"{lpf} Disable logic complete!");
        }

        /************* Helper functions **************/
    }
}
