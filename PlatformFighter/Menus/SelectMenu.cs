using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using PlatformFighter.Entities;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Rendering;
using PlatformFighter.Stages;

using System;
using System.Linq;

namespace PlatformFighter.Menus
{
    public class SelectMenu : GameMenu
    {
        public bool CanStart;
        public ExposedList<PlayerRegistryEntry> registry;
        public void Load()
        {
            registry = new ExposedList<PlayerRegistryEntry>();
        }
        public void Update()
        {
            CanStart = registry.Count > 0;

            for (int i = 0; i < registry.Count; i++)
            {
                ref PlayerRegistryEntry playerRegistryEntry = ref registry.items[i];

                if (!playerRegistryEntry.Controller.IsConnected)
                {
                    UnregisterPlayer(i);

                    continue;
                }

                if (!playerRegistryEntry.SelectedCharacter)
                {
                    CanStart = false;
                }

                if (playerRegistryEntry.Controller.Select == ControlState.JustPressed || playerRegistryEntry.IsBot)
                {
                    playerRegistryEntry.SelectedCharacter = true;
                }
            }

            if (CanStart)
            {
                if (registry.Count >= 2 && registry.Any(registryEntry => registryEntry.Controller.Select))
                {
                    StartGame();
                }
            }

            if (Keyboard.IsKeyPressedFirst(Keys.C) && !registry.Any(v => v.IsKeyboard))
            {
                RegisterPlayer(null);
            }
            if (Keyboard.IsKeyPressedFirst(Keys.B))
            {
                RegisterPlayer(-1);
            }
            for (int i = 0; i < Input.Gamepads.Length; i++)
            {
                GamepadInfo gamepad = Input.Gamepads[i];
                if (gamepad.State.IsButtonDown(Buttons.X))
                {
                    RegisterPlayer(i);
                }
            }
        }

        public void Unload()
        {
            registry.Clear();
            registry = null;
        }

        public void Draw()
        {
            if (registry.Count == 0)
            {
                TextRenderer.DrawTextNoCheckNoRot(Main.spriteBatch, VirtualMidResolution, GameText.PressButtonToJoin, 2f, Color.White, GameText.PressButtonToJoin.Measure / 2, 0f, 1);
                return;
            }
            for (int i = 0; i < registry.Count; i++)
            {
                PlayerRegistryEntry entry = registry[i];
                MeasuredText playerLabel = new MeasuredText(entry.IsBot ? "Bot vegetal" : entry.IsKeyboard ? "Jugador Teclado" : "Jugador Gamepad " + entry.GamepadIndex);

                Instance<CharacterDefinition> def = CharacterDefinitions.Instances[entry.CharacterIndex];
                AnimationData data = AnimationRenderer.GetAnimation(def.Inst.FighterName.ToLowerInvariant() + "selection");
                Vector2 playerPos = new Vector2(100 + i * 170, VirtualHeight - 100);
                AnimationRenderer.DrawJsonData(Main.spriteBatch, data.JsonData, 0, playerPos, Vector2.One * 2);
                TextRenderer.DrawTextNoCheckNoRot(Main.spriteBatch, playerPos - new Vector2(0, 170), playerLabel, 1.5f, Color.White, playerLabel.Measure / 2, 0f, 1);
            }
            if (CanStart)
            {
                TextRenderer.DrawTextNoCheckNoRot(Main.spriteBatch, VirtualMidResolution, GameText.PressButtonToStart, 3f, Color.White, GameText.PressButtonToStart.Measure / 2, 0f, 1);
            }
        }

        public void StartGame()
        {
            GameWorld.StartGame(InstanceManager.Stages.GetID<DefaultStage>());

            if (registry.Count == 1)
            {
                throw new NotSupportedException();
            }
            for (int i = 0; i < registry.Count; i++)
            {
                PlayerRegistryEntry entry = registry[i];
                GameWorld.CreatePlayer(new Vector2(-300f + 600f * (i / (registry.Count - 1f)), -100), 0, entry.ControllerId, out _);
            }

            MenuManager.Load(null);
        }

        public void RegisterPlayer(int? index)
        {
            registry.Add(new PlayerRegistryEntry(index));
        }
        public void UnregisterPlayer(int index)
        {
            registry[index].Unregister();
            registry.RemoveAt(index);
            ;
        }
    }

    public struct PlayerRegistryEntry
    {
        public bool IsKeyboard => _index is null;
        public int GamepadIndex => _index.Value;
        public bool IsBot => _index == -1;
        private readonly int? _index;
        public readonly ushort ControllerId { get; init; }
        public ushort CharacterIndex = 0;
        public bool SelectedCharacter { get; set; }
        public IPlayerDataReceiver Controller { get; init; }
        public PlayerRegistryEntry(int? index)
        {
            _index = index;
            Controller =
                IsBot ? new InactiveDataReceiver() :
                IsKeyboard ? new KeyboardDataReceiver() :
                new GamepadDataReceiver(GamepadIndex);
            ControllerId = PlayerController.RegisterController(Controller);
        }
        public void Unregister()
        {
            PlayerController.UnregisterController(ControllerId);
        }
    }
}