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
	
	string debugInfo = "";
	int frameCount = 0;

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
		
		HandleMovement(ref velocity);
		HandleWallSlide(ref velocity);
		HandleJump(delta, ref velocity);
		HandleWallJump(delta, ref velocity);

		Velocity = velocity;
		MoveAndSlide();
		UpdateState();
		ShowDebug();
		frameCount++;
	}


	void ShowDebug()
	{
		string newdebugInfo =
		$"🚶‍♂️‍➡️: {permissions.canWalkRun}  {(state.isWallSliding ? "sliding" : "notSliding")}  {Velocity}  ⌚: {jumpDuration}\n" +
		$"     {state.horizontal}  {state.vertical}  🔋: {airJumpCharge}";

		if(newdebugInfo != debugInfo)
			GD.Print(frameCount, ": ",newdebugInfo);

		debugInfo = newdebugInfo;
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
		// if(state.isWallSliding)
		// 	return;



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
		if(state.isWallSliding)
			return;


		
		if (Input.IsActionJustPressed("jump"))
		{
			if(IsOnFloor())
			{
				velocity.Y = JumpVelocity;
				state.vertical = VerticalState.groundJump;
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
	void HandleWallSlide(ref Vector2 velocity)
	{
		if (!(
			hasAbility.wallSlide &&
			state.vertical != VerticalState.wallJump))
			return;
		
		if(IsOnWall() && !IsOnFloor())
		{
			state.isWallSliding = true;
		}
	}
	void HandleWallJump(double delta, ref Vector2 velocity)
	{
		if(!(
			state.isWallSliding))
			return;


		if(Input.IsActionJustPressed("jump"))
		{
			Vector2 jumpDir = (GetWallNormal() + Vector2.Up).Normalized();

			state.vertical = VerticalState.wallJump;
			velocity = jumpDir*10000;
		}
	}

	void UpdateState()
	{
		if(state.isWallSliding)
		{
			if(!IsOnWall() || IsOnFloor())
			{
				state.isWallSliding = false;
			}
		}

		if(IsOnFloor() && !IsOnCeiling())
		{
			airJumpCharge = maxAirJumpCharge;
			jumpDuration = 0;
		}


		if(IsOnFloor()) state.vertical = VerticalState.none;
		else if(Velocity.Y >= 0) state.vertical = VerticalState.fall;
	}
}
