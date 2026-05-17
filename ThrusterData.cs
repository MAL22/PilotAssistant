using System.Collections.Generic;
using Digi;
using Sandbox.Definitions;
using Sandbox.ModAPI;

namespace PilotAssistant
{
    public enum ThrustEfficiency
    {
        Vacuum = 0,
        Atmospheric = 1,
        Current = 2,
    }
    
    public class ThrusterData
    {
        public float ThrustVacuum { get; private set; }
        public float ThrustAtmosphere { get; private set; }
        public float DampenerMultiplier { get; private set; }
        
        public ThrusterData(MyThrustDefinition thrusterDefinition)
        {
            ThrustVacuum = thrusterDefinition.ForceMagnitude * thrusterDefinition.EffectivenessAtMinInfluence;
            ThrustAtmosphere = thrusterDefinition.ForceMagnitude * thrusterDefinition.EffectivenessAtMaxInfluence;
            DampenerMultiplier = thrusterDefinition.SlowdownFactor;
        }
    }

    public partial class Session
    {
        private Dictionary<string, ThrusterData> LoadThrusterStats()
        {
            var thrusterDefinitions = new Dictionary<string, ThrusterData>();

            foreach (var definition in MyDefinitionManager.Static.GetDefinitionsOfType<MyThrustDefinition>())
            {
                thrusterDefinitions.Add(definition.Id.SubtypeId.ToString(), new ThrusterData(definition));
                Log.Info($"Loaded thruster -> {definition.Id.SubtypeId.ToString()}");
            }

            return thrusterDefinitions;
        }
    }
}