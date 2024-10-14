using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace PlatformFighter.Menus
{
    public static class SelectMenu
    {
        public static List<PlayerRegistryEntry> registry;
        public static void Load()
        {
            registry = new List<PlayerRegistryEntry>();
        }
        public static void Update()
        {
            foreach(var registry in registry)
            {
                if(registry.Controller.Select)
                {

                }
            }

            if(Keyboard.IsKeyPressedFirst(Keys.C))
            {
                RegisterPlayer(null);
            }
            for (int i = 0; i < Input.Gamepads.Count; i++)
            {
                GamepadInfo gamepad = Input.Gamepads[i];
                if (gamepad.State.IsButtonDown(Buttons.X))
                {
                    RegisterPlayer(i);
                }
            }
        }
        public static void RegisterPlayer(int? index)
        {
            registry.Add(new PlayerRegistryEntry(index));
        }
    }

    public struct PlayerRegistryEntry
    {
        public bool IsKeyboard => _index is null;
        public int GamepadIndex => _index.Value;
        private readonly int? _index;
        public readonly ushort ControllerId {get; init;}
        public short CharacterIndex = 0;
        public bool Selected {get; set;}
        public IPlayerDataController Controller {get; init;}
        public PlayerRegistryEntry(int? index)
        {
            _index = index;
            Controller = IsKeyboard ? 
                new KeyboardDataController() : 
                new GamepadDataController(GamepadIndex);
                ControllerId = PlayerController.RegisterPlayer(Controller);
        }
        public void Unregister()
        {
            PlayerController.UnregisterPlayer(ControllerId);
        }
    }
}