using Microsoft.Xna.Framework;

using PlatformFighter.Entities.Actions;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Physics;
using PlatformFighter.Rendering;

using System;

namespace PlatformFighter.Entities.Characters
{
    public class ElmoDefinition : CharacterDefinition
    {
        public override string FighterName => "Elmo";
        public override float WalkAcceleration => 0.12f;
        public override float GroundDashAcceleration => 0.12f;
        public override float AirAcceleration => 0.01f;
        public override float WalkMaxSpeed => 2.5f;
        public override float GroundDashMaxSpeed => 4f;
        public override float AirMaxSpeed => 1f;
        public override float FloorFriction => 0.9f;
        public override float HigherSpeedSlowingValue => 0.9f;
        public override float JumpVelocity => 2f;
        public override float FallingGravity => 0.15f;
        public override float FallingGravityMax => 5f;
        public override int MaxAirJumpCount => 2;
        public override float FastFallAcceleration => 0.2f;
        public override float FastFallMaxSpeed => 6f;
        public override float Tankiness => 1f;
        public override int JumpStartupFrames => 7;
        public override int DashStartupFrames => 20;
        public override Vector2 GroundJumpVelocity => new Vector2(0, -4);
        public override Vector2 GroundSideJumpVelocity => GroundJumpVelocity.RotateRad(MathHelper.ToRadians(40f));
        public override Vector2 AirborneJumpVelocity => GroundJumpVelocity;
        public override Vector2 AirborneSideJumpVelocity => GroundJumpVelocity.RotateRad(MathHelper.ToRadians(40f));
        public override Vector2 WallJumpVelocity => GroundJumpVelocity.RotateRad(MathHelper.ToRadians(50f));
        public override int JumpHoldMaxFrames => 30;
        public override Vector2 CollisionSize => new Vector2(40, 80);
        public override ActionBase<Player> ResolveAttackAction(Player player, AttackDirection attackDirection, bool isShot, bool isSpecial)
        {
            if (player.Grounded)
            {
                if (isShot)
                {

                }
                else
                {
                    switch (attackDirection)
                    {
                        case AttackDirection.Neutral:
                        default:
                            return new ElmoNeutralMeleeGrounded(player);
                    }
                }
                return new ElmoNeutralMeleeGrounded(player);
            }
            if (isShot)
            {

            }
            else
            {
                return new ElmoNeutralMeleeGrounded(player);
            }
            return new ElmoNeutralMeleeGrounded(player);
        }
    }
    public class ElmoNeutralMeleeGrounded : EndingActionToIdle
    {
        public ElmoNeutralMeleeGrounded(Player entity) : base(entity, "elmogroundneutralmelee")
        {
        }

        public override ActionTags Tags => ActionTags.Attack;
    }
    public class ElmoTurnWalkAction : AnimationActionBase
    {
        public ElmoTurnWalkAction(Player entity, string animName, int duration) : base(entity, animName, duration)
        {
        }

        public ElmoTurnWalkAction(Player entity, string animName) : base(entity, animName)
        {
        }

        public override void OnTimeEnd()
        {
            throw new NotImplementedException();
        }
    }
    public class ElmoWalkAction : AnimationActionBase
    {
        public override bool OverrideActions => false;
        public int WalkStartFrames { get; init; }
        public FacingDirection WalkingDirection { get; init; }
        public bool Walking { get; internal set; }
        public bool DoingStart { get; internal set; } = true;
        public ElmoWalkAction(Player entity, FacingDirection direction) : base(entity, "elmowalkstart")
        {
            if (entity.ActionManager.FacingDirection != direction)
            {
                AnimationData = AnimationRenderer.GetAnimation("elmoturn");
            }
            WalkStartFrames = AnimationData.LastFrame;
            WalkingDirection = direction;
            Entity.ActionManager.FacingDirection = direction;
        }

        public override void ProcessQueue(QueuedActionList queuedAction)
        {
            if (WalkingDirection == FacingDirection.Left) // turn code
            {
                if (queuedAction.ConsumeAction(ActionType.RightStart) || (queuedAction.ConsumeAction(ActionType.LeftEnd) && Entity.GetController().Right == ControlState.Pressed))
                {
                    ChangeTo(new ElmoWalkAction(Entity, FacingDirection.Right));
                }
            }
            else
            {
                if (queuedAction.ConsumeAction(ActionType.LeftStart) || (queuedAction.ConsumeAction(ActionType.RightEnd) && Entity.GetController().Left == ControlState.Pressed)) 
                {
                    ChangeTo(new ElmoWalkAction(Entity, FacingDirection.Left));
                }
            }
        }

        public override void Update()
        {
            if (DoingStart)
            {
                if(Entity.ActionManager.Impulse.X != 0)
                {
                    Entity.AddWalkAcceleration(WalkingDirection);
                    if (Frame >= WalkStartFrames)
                    {
                        AnimationData = AnimationRenderer.GetAnimation("elmowalk");
                        DoingStart = false;
                        Frame = 0;
                    }
                    Frame++;
                }
                else
                {
                    Frame--;
                }

                if (Frame == -1)
                {
                    ChangeTo(null);
                }
            }
            else
            {
                if(Entity.ActionManager.Impulse.X != 0)
                {
                    Entity.AddWalkAcceleration(WalkingDirection);
                    Frame++;
                    Frame %= AnimationData.LastFrame;
                }
                else
                {
                    AnimationData = AnimationRenderer.GetAnimation("elmowalkstart");
                    DoingStart = true;
                    Frame = WalkStartFrames;
                }
            }
        }

        public override void OnTimeEnd()
        {
            
        }

        public override void Draw()
        {
            AnimationRenderer.DrawJsonData(Main.spriteBatch, AnimationData.JsonData, Frame, Entity.MovableObject.Center, Entity.GetScaleWithFacing);
        }
    }
    public class ElmoJumpEndAction : EndingActionToIdle
    {
        public override bool OverrideActions => false;
        public ElmoJumpEndAction(Player entity) : base(entity, "elmojump", 30)
        {
        }
        
        public override float FrameToDraw => 30 + Frame;
    }
    public class ElmoJumpAction : AnimationActionBase
    {
        public bool WallJump;
        public int GravityId;
        public override bool OverrideActions => false;
        public ElmoJumpAction(Player entity, bool wallJump) : base(entity, "elmojump")
        {
            Duration = Entity.CharacterData.Definition.JumpStartupFrames + Entity.CharacterData.Definition.JumpHoldMaxFrames;
            GravityId = entity.NoGravityFrames.Register(Duration);
            WallJump = wallJump;
        }
        public override void Update()
        {
            Entity.ActionManager.UpdateFacingToInputs();
            FacingDirection? JumpDirection = Utils.GetFacingDirectionFromWithZero(Entity.ActionManager.Impulse.X);
            if (Frame < CharacterDefinitions[0].Inst.JumpStartupFrames && Math.Abs(Entity.MovableObject.VelocityY) > 0.1f && !Entity.Grounded)
            {
                Entity.MovableObject.Velocity *= 0.9f;
            }
            else if (Frame == CharacterDefinitions[0].Inst.JumpStartupFrames)
            {
                bool neutralJump = JumpDirection == null;
                bool grounded = Collision.GetCollidingDirection(Entity.MovableObject, new Vector2(0, 1)).HasFlag(Direction.Up);
                CharacterDefinition definition = Entity.CharacterData.Definition;
                Vector2 UsedVelocity = WallJump ? definition.WallJumpVelocity :
                    grounded ? neutralJump ? definition.GroundJumpVelocity : definition.GroundSideJumpVelocity :
                    neutralJump ? definition.AirborneJumpVelocity :
                    definition.AirborneSideJumpVelocity;

                UsedVelocity.X *= Utils.GetFacingDirectionMult(JumpDirection);
                Entity.MovableObject.VelocityX = Entity.MovableObject.VelocityX * 0.4f + UsedVelocity.X;
                Entity.MovableObject.VelocityY = UsedVelocity.Y;
            }
            if (Frame > CharacterDefinitions[0].Inst.JumpStartupFrames && !Entity.GetController().Jump || Frame > Duration)
            {
                OnTimeEnd();
            }
            base.Update();
        }
        public override void OnTimeEnd()
        {
            Entity.NoGravityFrames.RemoveId(GravityId);
            ChangeTo(new ElmoJumpEndAction(Entity));
        }

        public override ActionTags Tags => ActionTags.Attack;
    }
}