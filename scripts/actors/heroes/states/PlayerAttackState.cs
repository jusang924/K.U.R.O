using Godot;
using System;

namespace Kuros.Actors.Heroes.States
{
	public partial class PlayerAttackState : PlayerState
	{
		private bool _hasPerformedAttack = false;
		private float _attackDelayTimer = 0.0f;
		private const float ATTACK_HIT_DELAY = 0.2f; 
		
		// Feel Tuning Parameters
		private const float COMBAT_SPEED_MULTIPLIER = 0.6f; // Slower than walk for "weighty" feel
		private const float COMBAT_FRICTION = 25.0f; // Very snappy stop

		public override void Enter()
		{
			_hasPerformedAttack = false;
			_attackDelayTimer = ATTACK_HIT_DELAY;
			
			if (Actor.AnimPlayer != null)
			{
				Actor.AnimPlayer.Play("animations/attack");
			}
			
			// Immediate deceleration logic:
			// If we entered from Run state (high velocity), clamp it down instantly.
			// We don't want to zero it, but we don't want to slide.
			float maxCombatSpeed = Actor.Speed * COMBAT_SPEED_MULTIPLIER;
			if (Actor.Velocity.Length() > maxCombatSpeed)
			{
				if (Actor.Velocity != Vector2.Zero)
				{
					Actor.Velocity = Actor.Velocity.Normalized() * maxCombatSpeed;
				}
			}
		}

		public override void PhysicsUpdate(double delta)
		{
			// 1. Movement Logic (Combat Pace)
			Vector2 input = GetMovementInput();
			
			if (input != Vector2.Zero)
			{
				// Direct Control: Set velocity directly to input direction * combat speed.
				// No lerping here ensures "snappy" responsiveness.
				Vector2 targetVelocity = input * (Actor.Speed * COMBAT_SPEED_MULTIPLIER);
				Actor.Velocity = targetVelocity;

				// Direction Lock: Do NOT flip facing during attack
				// Usually games prevent flipping once attack starts to avoid sliding backwards moonwalk
				// if (input.X != 0)
				// {
				// 	Actor.FlipFacing(input.X > 0);
				// }
			}
			else
			{
				// High Friction: Stop almost instantly when input is released.
				// This prevents the "sliding on ice" feeling.
				Actor.Velocity = Actor.Velocity.MoveToward(Vector2.Zero, Actor.Speed * COMBAT_FRICTION * (float)delta);
			}

			Actor.MoveAndSlide();
			Actor.ClampPositionToScreen();

			// 2. Attack Hit Logic (Fallback)
			if (!_hasPerformedAttack)
			{
				_attackDelayTimer -= (float)delta;
				if (_attackDelayTimer <= 0)
				{
					Player.PerformAttackCheck();
					_hasPerformedAttack = true;
				}
			}

			// 3. Exit Logic
			if (Actor.AnimPlayer != null && !Actor.AnimPlayer.IsPlaying())
			{
				ChangeState("Idle");
			}
			else if (Actor.AnimPlayer == null)
			{
				 ChangeState("Idle");
			}
		}
		
		public void OnAnimationHitFrame()
		{
			if (!_hasPerformedAttack)
			{
				Player.PerformAttackCheck();
				_hasPerformedAttack = true;
			}
		}
	}
}
