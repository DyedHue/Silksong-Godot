using Godot;
using System;

public class Permissions
{
	public bool canWalkRun = true;
}

public enum HorizontalState
{
	none, walk, run
}
public enum VerticalState
{
	none, groundJump, airJump, wallJump, fall, floating
}
public enum AbilityState
{
	none, dash
}

public class PlayerState
{
	public HorizontalState horizontal {get; set;} = HorizontalState.none;
	public VerticalState vertical {get; set;} = VerticalState.none;
	public AbilityState ability {get; set;} = AbilityState.none;

	public bool IsJumping =>
		vertical is VerticalState.groundJump or VerticalState.airJump;
	public bool isWallSliding = false;
}
public class HasAbility
{
	public bool wallSlide = false;
	public bool faydownCloak = false;
}



public partial class Hornet : CharacterBody2D
{
	public PlayerState state = new();
	public Permissions permissions = new();
	public HasAbility hasAbility = new();
	
	public const float walkSpeed = 300.0f, runSpeed = 600.0f, JumpVelocity = -550.0f, maxJumpDuration = 0.25f, maxAirJumpDuration = 0.2f;
	public const int maxAirJumpCharge = 1;

	int airJumpCharge;
	float jumpDuration = 0;
	

	//Run Once
	public override void _Ready()
	{
		airJumpCharge = maxAirJumpCharge;
		hasAbility.wallSlide = true;
	}
	

	//Per Frame
	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		HandleGravity(delta, ref velocity);

		if(state.isWallSliding)
			HandleWallJump(delta, ref velocity);
		else
			HandleJump(delta, ref velocity);

		if(hasAbility.wallSlide)
			HandleSliding(ref velocity);

		HandleMovement(ref velocity);
		
		Velocity = velocity;
		ShowDebug();
		MoveAndSlide();
		UpdateState();
	}


	void ShowDebug()
	{
		GD.Print("🚶‍♂️‍➡️:", permissions.canWalkRun,"  ",(state.isWallSliding?"sliding  ":"notSliding  "), Velocity, "  ⌚:", jumpDuration,
			"\n", state.horizontal, "  ", state.vertical, "  ", state.ability, "  🔋:", airJumpCharge);
	}
	void HandleGravity(double delta, ref Vector2 velocity)
	{
		float gravity = GetGravity().Y;

		if(state.isWallSliding && state.vertical == VerticalState.fall)
			gravity /= 5;

		if(!IsOnFloor())
		{
			velocity.Y += gravity * (float)delta;
		}
	}
	void HandleMovement(ref Vector2 velocity)
	{
		if(!permissions.canWalkRun)
		{
			// velocity.X = 0;
			state.horizontal = HorizontalState.none;
			return;
		}

		bool running = Input.IsActionPressed("dash");

		int direction = 0;
		if (Input.IsActionPressed("move_left")) direction -= 1;
		if (Input.IsActionPressed("move_right")) direction += 1;

		if (direction != 0)
		{
			velocity.X = direction * (running ? runSpeed: walkSpeed);
			state.horizontal = running ? HorizontalState.run: HorizontalState.walk;
		}
		else
		{
			velocity.X = 0;
			state.horizontal = HorizontalState.none;
		}
	}
	void HandleJump(double delta, ref Vector2 velocity)
	{
		if(IsOnFloor() && !IsOnCeiling())
		{
			airJumpCharge = maxAirJumpCharge;
			jumpDuration = 0;
		}

		if (Input.IsActionJustPressed("jump"))
		{
			if(IsOnFloor())
			{
				// if(!IsOnCeiling())
				// {
					velocity.Y = JumpVelocity;
					state.vertical = VerticalState.groundJump;
				//}
			}
			else
			{
				if(airJumpCharge != 0)
				{
					velocity.Y = JumpVelocity;
					state.vertical = VerticalState.airJump;
					airJumpCharge --;
				}
			}
		}
		else if(Input.IsActionPressed("jump"))
		{
			if(state.IsJumping)
			{
				if(jumpDuration < (state.vertical == VerticalState.groundJump? maxJumpDuration:maxAirJumpDuration))
				{
					velocity.Y = JumpVelocity;
					jumpDuration += (float)delta;
				}
			}
		}
		else
		{
			jumpDuration = 0;
		}
	}
	void HandleSliding(ref Vector2 velocity)
	{
		if (state.vertical == VerticalState.wallJump) return;
		
		if(IsOnWall() && !IsOnFloor())
		{
			state.isWallSliding = true;
			permissions.canWalkRun = false;
		}
		else
		{
			state.isWallSliding = false;
			permissions.canWalkRun = true;
		}
	}
	void HandleWallJump(double delta, ref Vector2 velocity)
	{
		if(Input.IsActionJustPressed("jump"))
		{
			Vector2 jumpDir = (GetWallNormal() + Vector2.Up).Normalized();

			state.vertical = VerticalState.wallJump;
			velocity = jumpDir*300;

			permissions.canWalkRun = true;
		}
	}

	void UpdateState()
	{
		if(IsOnFloor()) state.vertical = VerticalState.none;
		else if(Velocity.Y >= 0) state.vertical = VerticalState.fall;
	}
}
