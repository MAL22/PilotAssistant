using System;
using System.Collections.Generic;
using System.Text;
using Digi;
using Draygo.API;
using Sandbox.ModAPI;
using VRageMath;
using VRageRender;

using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace PilotAssistant
{
    public class PilotAssistantWidget
    {
        private HudAPIv2.HUDMessage _hudMessage;
        private StringBuilder _stringBuilder = new StringBuilder();
        
        public ThrusterBurnData DampenedThrusterBurnBackward;
        public ThrusterBurnData DampenedThrusterBurnDown;
        public ThrusterBurnData ActualThrusterBurn;
        
        public float CurrentLiftWeightBackward = 0;
        public float SurfaceLiftWeightBackward = 0;
        
        public float CurrentLiftWeightDown = 0;
        public float SurfaceLiftWeightDown = 0;
        
        public float AccelerationBackward = 0;
        public float AccelerationDown = 0;

        private float _minThrustOverride = 0;

        public float MinThrustOverride
        {
            set
            {
                _minThrustOverride = value;
                BuildMessage();
            }
        }
        
        public void BuildMessage()
        {
            _stringBuilder.Clear();
            
            if ((int)AccelerationBackward > 0)
            {
                Log.Info($"{AccelerationBackward} | {Session.ReferenceGravity}");
                
                var acceleration = AccelerationBackward / Session.ReferenceGravity;
                _stringBuilder.AppendLine($"Acceleration: <color=255,0,0>{acceleration:N1} g<reset>");
            }
            
            if ((int)DampenedThrusterBurnBackward.Distance > 0)
            {
                var dampenedDecelDistBackward = DampenedThrusterBurnBackward.Distance >= 1000 ? DampenedThrusterBurnBackward.Distance / 1000 : DampenedThrusterBurnBackward.Distance;
                var dampenedDecelTimeBackward = TimeSpan.FromSeconds(DampenedThrusterBurnBackward.Time).TotalSeconds;

                _stringBuilder.Append($"Braking Burn (Rear): <color=255,255,0>{dampenedDecelDistBackward:N1}");
                _stringBuilder.Append(DampenedThrusterBurnBackward.Distance > 1000 ? " km" : " m");
                _stringBuilder.AppendLine($" ({dampenedDecelTimeBackward:0.0}s)<reset>");
            }
            
            if ((int)DampenedThrusterBurnBackward.Distance < 0)
            {
                _stringBuilder.AppendLine($"Braking burn (Rear): <color=255,0,0>DANGER!<reset>");
            }
            
            if ((int)DampenedThrusterBurnDown.Distance > 0)
            {
                var dampenedDecelDistDown = DampenedThrusterBurnDown.Distance >= 1000 ? DampenedThrusterBurnDown.Distance / 1000 : DampenedThrusterBurnDown.Distance;
                var dampenedDecelTimeDown = TimeSpan.FromSeconds(DampenedThrusterBurnDown.Time).TotalSeconds;
                _stringBuilder.Append($"Braking burn (Bottom): <color=255,255,0>{dampenedDecelDistDown:N0}");
                _stringBuilder.Append(DampenedThrusterBurnDown.Distance > 1000 ? " km" : " m");
                _stringBuilder.AppendLine($" ({dampenedDecelTimeDown:0.0}s)<reset>");
            }

            if ((int)DampenedThrusterBurnDown.Distance < 0)
            {
                _stringBuilder.AppendLine($"Braking burn (Bottom): <color=255,0,0>DANGER!<reset>");
            }

            if ((int)SurfaceLiftWeightBackward > 0)
            {
                _stringBuilder.AppendLine($"Lift (Rear) | Current: <color=255,255,0>{WeightFormatter.Format(CurrentLiftWeightBackward)}<reset> | Surface: <color=255,255,0>{WeightFormatter.Format(SurfaceLiftWeightBackward)}<reset>");
            }
            
            if ((int)SurfaceLiftWeightDown > 0)
            {
                _stringBuilder.AppendLine($"Lift (Bottom) | Current: <color=255,255,0>{WeightFormatter.Format(CurrentLiftWeightDown)}<reset> | Surface: <color=255,255,0>{WeightFormatter.Format(SurfaceLiftWeightDown)}<reset>");
            }

            if (_minThrustOverride > 0)
            {
                _stringBuilder.AppendLine($"Min Thrust Override: <color=255,255,0>{_minThrustOverride:P}<reset>");
            }
        }

        public void RegisterHud()
        {
            _hudMessage = new HudAPIv2.HUDMessage(_stringBuilder, Settings.Instance.Position, null, -1, Settings.Instance.Scale, true, false, null,
                BlendTypeEnum.PostPP);
        }
        
        public void UpdateScale()
        {
            _hudMessage.Scale = Settings.Instance.Scale;
        }

        public void UpdatePosition()
        {
            _hudMessage.Origin = Settings.Instance.Position;
        }
    }
}
