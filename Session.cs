using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using Digi;
using Draygo.API;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using VRage.Game;
using IMyControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;

namespace PilotAssistant
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public partial class Session : MySessionComponentBase
    {
        public const float ReferenceGravity = 9.81f;
        
        private PilotAssistantWidget _pilotAssistantWidget = new PilotAssistantWidget();

        private static Dictionary<string, ThrusterData> _thrusterStats;
        private List<IMyThrust> _thrusters;
        private MyPlanet _closestPlanet;

        private IMyShipController _currentShipController;
        
        private int _tick = 0;
        private bool listenerRegistered = false;

        internal bool isMultiplayerSession;
        internal bool isServer;
        internal bool isClient;
        
        public override void BeforeStart()
        {
            if (isServer)
            {
               _thrusterStats = LoadThrusterStats();
            }
            
            if (isClient)
            {
                InitConfig();
                hudAPI = new HudAPIv2(InitMenu);
            }
        }

        public override void LoadData()
        {
            isMultiplayerSession = MyAPIGateway.Multiplayer.MultiplayerActive;
            isServer = isMultiplayerSession && MyAPIGateway.Multiplayer.IsServer || !isMultiplayerSession;
            isClient = isMultiplayerSession && MyAPIGateway.Utilities.IsDedicated || !isMultiplayerSession;
        }

        protected override void UnloadData()
        {
            if (isClient)
            {
                Session.Player.Controller.ControlledEntityChanged -= OnControlledEntityChanged;
                hudAPI.Unload();
            }
        }

        public override void UpdateBeforeSimulation()
        {
            if (isClient)
            {
                if (!listenerRegistered)
                {
                    Session.Player.Controller.ControlledEntityChanged += OnControlledEntityChanged;
                    
                    if (hudAPI.Heartbeat)
                    {
                        _pilotAssistantWidget.RegisterHud();
                    }
                    
                    listenerRegistered = true;
                }
            }
        }

        private void OnControlledEntityChanged(IMyControllableEntity previousEntity, IMyControllableEntity newEntity)
        {
            var newShipController = newEntity as IMyShipController;
            
            if (newEntity is IMyCharacter || newShipController == null)
            {
                _currentShipController = null;
                return;
            }

            _currentShipController = newShipController;
            _thrusters = GetAllThrusters(_currentShipController.CubeGrid);
        }

        public override void UpdateAfterSimulation()
        {
            if (isClient && Session.Player.Character != null)
            {
                if (_tick % 300 == 0)
                {
                    //TODO: cache the planet at a lower tick rate so that we don't look for one everytime the main logic runs
                    _closestPlanet = GetClosestPlanet();
                }
                if (_tick % 60 == 0)
                {
                    _pilotAssistantWidget.DampenedThrusterBurnBackward = CalculateBrakingBurn(Vector3I.Backward);
                    _pilotAssistantWidget.DampenedThrusterBurnDown = CalculateBrakingBurn(Vector3I.Down);

                    _pilotAssistantWidget.CurrentLiftWeightBackward = CalculateShipLiftWeight(Vector3I.Backward, false);
                    _pilotAssistantWidget.SurfaceLiftWeightBackward = CalculateShipLiftWeight(Vector3I.Backward);
                
                    _pilotAssistantWidget.CurrentLiftWeightDown = CalculateShipLiftWeight(Vector3I.Down, false);
                    _pilotAssistantWidget.SurfaceLiftWeightDown = CalculateShipLiftWeight(Vector3I.Down);
                
                    _pilotAssistantWidget.AccelerationBackward = CalculateAcceleration(Vector3I.Backward);
                    //_pilotAssistantWidget.AccelerationDown = CalculateAcceleration(Vector3I.Down);
                
                    //_pilotAssistantWidget.MinThrustOverride = CalculateMinThrustOverride(Vector3I.Backward);
                
                    _pilotAssistantWidget.BuildMessage();
                }
            }
            
            _tick++;
        }

        public override void Draw()
        {
            //_pilotAssistantWidget?.Draw();
        }

        private List<IMyThrust> GetAllThrusters(IMyCubeGrid grid)
        {
            return grid.GetFatBlocks<IMyThrust>().ToList();
        }
        
        private MyPlanet GetClosestPlanet()
        {
            return MyGamePruningStructure.GetClosestPlanet(Session.Player.GetPosition());
        }

        private bool IsNearPlanet()
        {
            if (_closestPlanet == null) return false;
            
            var gravComponent = _closestPlanet.Components.Get<MyGravityProviderComponent>();
            var gravity = gravComponent.GetWorldGravity(Session.Player.GetPosition());
            
            return gravity.Length() > 0f;
        }
        
        private float GetPlanetGravity()
        {
            if (_closestPlanet == null) return 0f;
            
            var gravComponent = _closestPlanet.Components.Get<MyGravityProviderComponent>();
            Vector3D gravity = gravComponent.GetWorldGravity(Session.Player.GetPosition());
            return Convert.ToSingle(gravity.Length());
        }

        private float GetDirectionalThrust(Vector3I direction, ThrustEfficiency thrustEfficiency = ThrustEfficiency.Vacuum, bool dampenersEnabled = false)
        {
            float thrust = 0;

            foreach (var thruster in _thrusters)
            {
                if (thruster.GridThrustDirection != direction || !thruster.Enabled) continue;
                ThrusterData thrusterData;
                if (_thrusterStats.TryGetValue(thruster.BlockDefinition.SubtypeId, out thrusterData))
                {
                    switch (thrustEfficiency)
                    {
                        case ThrustEfficiency.Vacuum:
                        {
                            thrust += dampenersEnabled ? thrusterData.ThrustVacuum * thrusterData.DampenerMultiplier : thrusterData.ThrustVacuum;
                            break;
                        }
                        case ThrustEfficiency.Atmospheric:
                        {
                            thrust += dampenersEnabled ? thrusterData.ThrustAtmosphere * thrusterData.DampenerMultiplier : thrusterData.ThrustAtmosphere;
                            break;
                        }
                        case ThrustEfficiency.Current:
                        {
                            thrust += dampenersEnabled ? thruster.MaxEffectiveThrust * thrusterData.DampenerMultiplier : thruster.MaxEffectiveThrust;
                            break;
                        }
                    }
                }
            }
            
            return thrust;
        }

        private ThrusterBurnData CalculateBrakingBurn(Vector3I direction, bool useDampeners = false)
        {
            var thrusterBurnData = new ThrusterBurnData();
            thrusterBurnData.Achievable = true;
   
            if (_currentShipController != null)
            {
                var force = GetDirectionalThrust(direction, ThrustEfficiency.Current, useDampeners);
                var velocity = Convert.ToSingle(_currentShipController.CubeGrid.LinearVelocity.Length());
                var accel = force / _currentShipController.CalculateShipMass().PhysicalMass;

                var burnDistance = (velocity * velocity) / (2 * (accel - GetPlanetGravity()));

                if (_closestPlanet != null)
                {
                    var shipPosition = _currentShipController.CubeGrid.GetPosition();
                    var closestPlanetPosition = _closestPlanet.GetClosestSurfacePointGlobal(shipPosition);
                    var distanceToPlanet = (shipPosition - closestPlanetPosition).Length();

                    thrusterBurnData.Distance = burnDistance;
                    
                    if (burnDistance > distanceToPlanet || burnDistance < 0)
                    {
                        thrusterBurnData.Achievable = false;
                    }
                }
                else
                {
                    thrusterBurnData.Distance = burnDistance;
                }

                thrusterBurnData.Time = velocity / accel;
            }
            else
            {
                var characterDefinition = Session.Player.Character.Definition as MyCharacterDefinition;
                var force = characterDefinition?.Jetpack.ThrustProperties.ForceMagnitude ?? 0f;
                var velocity = Session.Player.Character.Physics.LinearVelocity.Length();
                var accel = force / Session.Player.Character.Physics.Mass;
                
                thrusterBurnData.Distance = (velocity * velocity) / (2 * (accel - GetPlanetGravity()));
                thrusterBurnData.Time = velocity / accel;
            }

            return thrusterBurnData;
        }

        private float CalculateShipLiftWeight(Vector3I direction, bool atSeaLevel = true)
        {
            var planet = GetClosestPlanet();
      
            if (planet == null || _currentShipController == null)
            {
                return 0;
            }

            var thrustEnvironmentEfficiency = atSeaLevel ? planet.HasAtmosphere ? ThrustEfficiency.Atmospheric : ThrustEfficiency.Vacuum : ThrustEfficiency.Current;
            var force = GetDirectionalThrust(direction, thrustEnvironmentEfficiency);
            var acceleration = atSeaLevel ? planet.Generator.SurfaceGravity * ReferenceGravity : _currentShipController.CubeGrid.NaturalGravity.Length();
            var maxWeight = force / acceleration; 
            
            return maxWeight;
        }

        private float CalculateAcceleration(Vector3I direction)
        {
            float force, mass, accel;

            if (_currentShipController != null)
            {
                force = GetDirectionalThrust(direction, ThrustEfficiency.Current, false);
                mass = _currentShipController.CalculateShipMass().PhysicalMass;
                accel = force / mass;
            
                Log.Info($"Currently piloting {_currentShipController.CustomName} force: {force} mass: {mass} accel: {accel}");
            }
            else
            {
                if (Session.Player.Character.CurrentMovementState != MyCharacterMovementEnum.Flying) return 0;
                var characterDefinition = Session.Player.Character.Definition as MyCharacterDefinition;
                force = characterDefinition?.Jetpack.ThrustProperties.ForceMagnitude ?? 0f;
                mass = Session.Player.Character.BaseMass;
                accel = force / mass;
            }
            return accel;
        }

        private float CalculateMinThrustOverride(IMyCharacter character, Vector3I direction)
        {
            if (_closestPlanet == null || _currentShipController == null)
            {
                return 0;
            }

            var maxForce = GetDirectionalThrust(Vector3I.Backward, ThrustEfficiency.Current);
            var accel = _closestPlanet.Generator.SurfaceGravity * ReferenceGravity;
            var mass = _currentShipController.CalculateShipMass().PhysicalMass;
            
            var minThrustOverride =  (mass * accel) / maxForce;

            return minThrustOverride;
        }
    }

}
