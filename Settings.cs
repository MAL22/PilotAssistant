using System;
using System.IO;
using System.Runtime.Serialization;
using Digi;
using Draygo.API;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;

namespace PilotAssistant
{
    [ProtoContract]
    public class Settings
    {
        public static Settings Instance;

        public static readonly Settings Default = new Settings()
        {
            Position = new Vector2D(-0.95, 0.95),
            Scale = 0.75f,
        };

        [ProtoMember(1)]
        public Vector2D Position { get; set; } = new Vector2D(-0.95, 0.95);
        [ProtoMember(2)]
        public float Scale { get; set; } = 0.75f;
    }

    public partial class Session
    {
        private const string Filename = "PilotAssistant.cfg";
        private HudAPIv2 hudAPI;

        private void InitConfig()
        {
            Settings settings = Settings.Default;

            try
            {
                if (MyAPIGateway.Utilities.FileExistsInLocalStorage(Filename, typeof(Settings)))
                {
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Filename, typeof(Settings));
                    string text = reader.ReadToEnd();
                    reader.Close();

                    settings = text.Length == 0 ? Settings.Default : MyAPIGateway.Utilities.SerializeFromXML<Settings>(text);
                    
                    Save(settings);
                }
                else
                {
                    settings = Settings.Default;
                    Save(settings);
                }
            }
            catch (Exception e)
            {
                Settings.Instance = Settings.Default;
                settings = Settings.Default;
                Save(settings);
                Log.Error("Pilot Assistant: Error while loading the configuration file. Reverting to default configuration.");
            }
        }

        private void Save(Settings settings)
        {
            try
            {
                TextWriter writer;
                writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Filename, typeof(Settings));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(settings));
                writer.Close();
                Settings.Instance = settings;
            }
            catch (Exception e)
            {
                Log.Error("Pilot Assistant: Error while saving the configuration file");
            }
        }

        private HudAPIv2.MenuRootCategory settingsMenu;
        private HudAPIv2.MenuSliderInput positionX, positionY, size;

        private void InitMenu()
        {
            settingsMenu = new HudAPIv2.MenuRootCategory("Pilot Assistant", HudAPIv2.MenuRootCategory.MenuFlag.PlayerMenu, "Pilot Assistant Settings");
            positionX = new HudAPIv2.MenuSliderInput($"Widget position X: {Settings.Instance.Position.X}", settingsMenu, Convert.ToSingle((Settings.Instance.Position.X + 1) / 2),"Select position X", ChangePositionX);
            positionY = new HudAPIv2.MenuSliderInput($"Widget position Y: {Settings.Instance.Position.Y}", settingsMenu, Convert.ToSingle((Settings.Instance.Position.Y + 1) / 2),"Select position Y", ChangePositionY);
            size = new HudAPIv2.MenuSliderInput($"Widget size: {Settings.Instance.Scale}", settingsMenu, Settings.Instance.Scale,"Select size", ChangeSize);
        }
        
        private void ChangePositionX(float x)
        {
            Settings.Instance.Position = new Vector2D(x / 0.5f - 1f, Settings.Instance.Position.Y);
            positionX.Text = $"Widget position X: {Settings.Instance.Position.X}";
            _pilotAssistantWidget.UpdatePosition();
            Save(Settings.Instance);
        }
        
        private void ChangePositionY(float y)
        {
            Settings.Instance.Position = new Vector2D(Settings.Instance.Position.X, y / 0.5f - 1f);
            positionY.Text = $"Widget position Y: {Settings.Instance.Position.Y}";
            _pilotAssistantWidget.UpdatePosition();
            Save(Settings.Instance);
        }
        
        private void ChangeSize(float s)
        {
            Settings.Instance.Scale = s;
            size.Text = $"Widget size: {Settings.Instance.Scale}";
            _pilotAssistantWidget.UpdateScale();
            Save(Settings.Instance);
        }
    }
}