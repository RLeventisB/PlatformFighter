using Microsoft.Xna.Framework;

using PlatformFighter.Entities.Characters;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Rendering;

using System;
using System.Text;

namespace PlatformFighter.Entities.Actions
{
    public class PlayerActionManager
    {
        public QueuedActionList QueuedAction = new QueuedActionList();
        public int AnimationFrame;
        public ActionBase<Player> CurrentAction;
        public FacingDirection FacingDirection = FacingDirection.Right;
        public AnalogValue<sbyte>
            FastFalling = new AnalogValue<sbyte>(0, 50, 1),
            Crouching = new AnalogValue<sbyte>(0, 30, 3),
            Shielding = new AnalogValue<sbyte>(0, 10, 1);
        public bool Dashing;
        public Vector2 Impulse;
        public int RecoveryFrames;
        public int ShieldBreakStun;
        public ushort WalkTime, TurningTime, AirTime, DashTime;

        public PlayerActionManager(Player player)
        {
            Player = player;
        }

        public Player Player { get; init; }
        
        public void SetDefaults()
        {
            Impulse = Vector2.Zero;
            Shielding.State = false;
            Crouching.State = false;
            FastFalling.State = false;
        }
        public void Update()
        {
            IPlayerDataReceiver controller = Player.GetController();
            Impulse = Vector2.Zero;
            if (controller.Up)
                Impulse.Y -= 1;
            if (controller.Down)
                Impulse.Y += 1;
            if (controller.Left)
                Impulse.X -= 1;
            if (controller.Right)
                Impulse.X += 1;

            QueuedAction.AddActions(controller);

            QueuedAction.TickActions();
            if (CurrentAction is not null)
            {
                CurrentAction.Update();
                if (CurrentAction is not null)
                {
                    CurrentAction.ProcessQueue(QueuedAction);

                    if (CurrentAction.OverrideActions)
                    {
                        return;
                    }
                }
            }

            Shielding.State = controller.Shield;
            Crouching.State = Player.Grounded && controller.Down;

            if (Dashing && Impulse.X != 0)
            {
                if(DashTime < 1000)
                    DashTime++;
                
                
            }
            else
            {
                if (DashTime > 0) // dash end animation if any
                {
                    DashTime--;
                    DashTime = (ushort)MathHelper.Clamp(DashTime, 0, 30);
                }
                
                if (TurningTime > 0) // reduce turning time if any (only applied when changing direction)
                    TurningTime--;

                if (Impulse.X == 0 && WalkTime > 0) // if put and walk time is high, lower walk time so it interpolates better
                {
                    WalkTime--;
                    WalkTime = (ushort)MathHelper.Clamp(WalkTime, 0, 20);
                }

                if (Impulse.X != 0 && WalkTime < 1000) // if not put and walk time doesnt overflow, tick walk time
                {
                    WalkTime++;
                    if (TurningTime > 0) // limit walk time so turning animation smoothly interpolates into walk animation
                        WalkTime = (ushort)MathHelper.Clamp(WalkTime, 0, 30);
                }
            }
            
            if (Shielding && Crouching)
            {
                Crouching.Value = Crouching.Max;
            }

            if (Player.Grounded)
            {
                AirTime = 0;
            }
            else if (AirTime < 1000)
            {
                AirTime++;
            }

            Shielding.Update();
            Crouching.Update();
            FastFalling.Update();

            TickActionsDefault(this, controller);

            CurrentAction?.Update();
        }
        public static void TickActionsDefault(PlayerActionManager actionManager, IPlayerDataReceiver controller)
        {
            while (actionManager.QueuedAction.HasActions)
            {
                QueuedAction action = actionManager.QueuedAction.DequeueAction();
                switch (action.ActionType)
                {
                    case ActionType.Jump:
                        Direction collidedDirections = actionManager.Player.CollidedDirections;
                        actionManager.CurrentAction = new ElmoJumpAction(actionManager.Player, collidedDirections.HasFlag(Direction.Left) || collidedDirections.HasFlag(Direction.Right));
                        break;
                    case ActionType.LeftStart when actionManager.Player.Grounded:
                        actionManager.DoWalkAction(FacingDirection.Left);
                        break;
                    case ActionType.RightStart when actionManager.Player.Grounded:
                        actionManager.DoWalkAction(FacingDirection.Right);
                        break;
                }
            }
        }
        public void DoWalkAction(FacingDirection facingDirection)
        {
            CurrentAction = new ElmoWalkAction(Player, facingDirection);
        }
        public bool UpdateFacingToInputs()
        {
            bool inputChange = Utils.GetFacingDirectionMult(FacingDirection) != Impulse.X && Impulse.X != 0;
            if (inputChange)
                FacingDirection = Utils.GetFacingDirectionFrom(Impulse.X);

            return inputChange;
        }
        public void Draw(Player player)
        {
            if (CurrentAction is null)
            {
                AnimationData data = null;
                float facingScale = Utils.GetFacingDirectionMult(FacingDirection);

                if (player.Grounded)
                {
                    if (Shielding.Value > 0)
                    {
                        StringBuilder usedAnimation = new StringBuilder("elmoblock");

                        usedAnimation.Append(Crouching ? "crouch" : "ground");
                        data = AnimationRenderer.GetAnimation(usedAnimation.ToString());
                        AnimationFrame = Shielding.Value;
                    }
                    else if (Crouching.Value > 0)
                    {
                        data = AnimationRenderer.GetAnimation("elmocrouch");
                        AnimationFrame = Crouching.Value;
                    }
                    else if (DashTime > 0)
                    {
                        if (DashTime < player.CharacterData.Definition.DashStartupFrames)
                        {
                            data = AnimationRenderer.GetAnimation("elmodashgroundstart");
                            AnimationFrame = DashTime;
                        }
                        else
                        {
                            if (DashTime == player.CharacterData.Definition.DashStartupFrames)
                                AnimationFrame = -1;
                            data = AnimationRenderer.GetAnimation("elmodashground");
                            AnimationFrame++;
                        }
                    }
                    else if (WalkTime > 0)
                    {
                        if (TurningTime > 0)
                        {
                            data = AnimationRenderer.GetAnimation("elmoturn");
                            AnimationFrame = 20 - TurningTime;
                        }
                        else if (WalkTime <= 20)
                        {
                            data = AnimationRenderer.GetAnimation("elmowalkstart");
                            AnimationFrame = WalkTime - 1;
                        }
                        else
                        {
                            AnimationFrame++;
                            AnimationFrame %= 100;
                            if (WalkTime == 21)
                                AnimationFrame = 0;
                            data = AnimationRenderer.GetAnimation("elmowalk");
                        }
                    }
                    else if (Impulse.X == 0)
                    {
                        data = AnimationRenderer.GetAnimation("elmoidle");
                        AnimationFrame++;
                        AnimationFrame %= data.LastFrame;
                    }
                }
                else
                { 
                    if (FastFalling.Value > 0)
                    {
                        data = AnimationRenderer.GetAnimation("elmofastfall");
                        AnimationFrame = FastFalling.Value;
                    }
                    else
                    {
                        data = AnimationRenderer.GetAnimation("elmofalling");
                        AnimationFrame++;
                    }
                }

                int usedFrame = AnimationFrame;

                Vector2 scale = player.Scale;
                scale.X *= facingScale;
                if (data is not null)
                    AnimationRenderer.DrawJsonData(Main.spriteBatch, data.JsonData, usedFrame, player.MovableObject.Center, scale);

            }
            else
            {
                CurrentAction.Draw();
            }
        }

        public void Reset()
        {
            CurrentAction = null;
        }
    }
    public struct QueuedAction : IComparable<QueuedAction>
    {
        public readonly ActionType ActionType;
        public ushort BufferTime;
        public const ushort DefaultBufferTime = 14;
        public QueuedAction(ActionType actionType)
        {
            ActionType = actionType;
            BufferTime = DefaultBufferTime;
        }

        public bool TickBufferAndIsExpired() => BufferTime-- == 0;

        public int CompareTo(QueuedAction other) => ActionType.CompareTo(other.ActionType);
    }
    public struct QueuedActionList
    {
        public ExposedList<QueuedAction> ActionQueue = new ExposedList<QueuedAction>();
        public QueuedActionList()
        {
        }
        public bool HasActions => ActionQueue.Count != 0;
        public bool DequeueAction(ActionType type, out QueuedAction action)
        {
            int index = ActionQueue.FindIndex(v => v.ActionType == type);
            if (index != -1)
            {
                action = DequeueAction(index);
                return true;
            }
            action = default;
            return false;
        }
        public bool ConsumeAction(ActionType type)
        {
            int index = ActionQueue.FindIndex(v => v.ActionType == type);

            if (index == -1)
                return false;

            ActionQueue.RemoveAt(index);
            return true;
        }
        public QueuedAction DequeueAction(int index = 0)
        {
            QueuedAction action = ActionQueue[index];
            ActionQueue.RemoveAt(index);
            return action;
        }
        public void TickActions()
        {
            // tick all actions that weren't processed
            for (int i = 0; i < ActionQueue.Count; i++)
            {
                QueuedAction action = ActionQueue[i];

                if (!action.TickBufferAndIsExpired())
                    continue;

                ActionQueue.RemoveAt(i);
                i--;
            }
        }
        public void AddActions(IPlayerDataReceiver controller)
        {
            if (controller.Left == ControlState.JustPressed)
            {
                AddAction(ActionType.LeftStart);
            }
            if (controller.Left == ControlState.Releasing)
            {
                AddAction(ActionType.LeftEnd);
            }
            if (controller.Right == ControlState.JustPressed)
            {
                AddAction(ActionType.RightStart);
            }
            if (controller.Right == ControlState.Releasing)
            {
                AddAction(ActionType.RightEnd);
            }
            if (controller.Up == ControlState.JustPressed)
            {
                AddAction(ActionType.UpStart);
            }
            if (controller.Up == ControlState.Releasing)
            {
                AddAction(ActionType.UpEnd);
            }
            if (controller.Down == ControlState.JustPressed)
            {
                AddAction(ActionType.DownStart);
            }
            if (controller.Down == ControlState.Releasing)
            {
                AddAction(ActionType.DownEnd);
            }
            if (controller.ActivateMeter == ControlState.JustPressed)
            {
                AddAction(ActionType.Meter);
            }
            if (controller.Shield == ControlState.JustPressed)
            {
                AddAction(ActionType.BlockStart);
            }
            if (controller.Shield == ControlState.Releasing)
            {
                AddAction(ActionType.BlockEnd);
            }
            if (controller.Dash == ControlState.JustPressed)
            {
                AddAction(ActionType.DashStart);
            }
            if (controller.Dash == ControlState.Releasing)
            {
                AddAction(ActionType.DashEnd);
            }
            if (controller.Jump == ControlState.JustPressed)
            {
                AddAction(ActionType.Jump);
            }
            if (controller.SpecialToggle == ControlState.JustPressed)
            {
                AddAction(ActionType.SpecialOn);
            }
            if (controller.SpecialToggle == ControlState.Releasing)
            {
                AddAction(ActionType.SpecialOff);
            }
            if (controller.MeleeAttack == ControlState.JustPressed)
            {
                AddAction(ActionType.AttackMelee);
            }
            if (controller.ShotAttack == ControlState.JustPressed)
            {
                AddAction(ActionType.AttackShot);
            }
            if (controller.Dash == ControlState.JustPressed)
            {
                AddAction(ActionType.DashStart);
            }
            if (controller.Dash == ControlState.Releasing)
            {
                AddAction(ActionType.DashEnd);
            }
        }
        public void AddAction(ActionType actionType)
        {
            int index = ActionQueue.FindIndex(v => v.ActionType == actionType);
            if (index == -1)
            {
                ActionQueue.Add(new QueuedAction(actionType));
            }
            else
            {
                ActionQueue.items[index].BufferTime = QueuedAction.DefaultBufferTime;
            }
            ActionQueue.Sort();
        }
    }
    public enum FacingDirection : byte
    {
        Left, Right
    }
    public enum ActionType
    {
        DashStart,
        DashEnd,
        LeftStart,
        RightStart,
        UpStart,
        DownStart,
        LeftEnd,
        RightEnd,
        UpEnd,
        DownEnd,
        Jump,
        BlockStart,
        BlockEnd,
        AttackMelee,
        AttackShot,
        SpecialOn,
        SpecialOff,
        Meter
    }
    public static class ActionTypeUtils
    {
        public static int GetPriority(ActionType action)
        {
            switch (action)
            {
                case ActionType.DashStart:
                case ActionType.DashEnd:
                    return 95;
                case ActionType.LeftStart:
                case ActionType.RightStart:
                case ActionType.UpStart:
                case ActionType.DownStart:
                case ActionType.LeftEnd:
                case ActionType.RightEnd:
                case ActionType.UpEnd:
                case ActionType.DownEnd:
                    return 100;
                case ActionType.Jump: 
                    return 90;
                case ActionType.BlockEnd:
                case ActionType.BlockStart:
                    return 110;
                case ActionType.AttackMelee:
                case ActionType.AttackShot:
                    return 89;
                case ActionType.SpecialOn:
                case ActionType.SpecialOff:
                    return 120;
                case ActionType.Meter: 
                    return 70;
            }
            return 0;
        }
    }
}