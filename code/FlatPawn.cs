using Sandbox;
using System;
using System.Linq;

namespace Sandbox;

partial class FlatPawn : AnimatedEntity
{
	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		base.Spawn();

		//
		// Use a watermelon model
		//
		SetModel( "models/editor/camera.vmdl" );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
	}

	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		base.PostCameraSetup( ref camSetup );

		camSetup.Viewer = this;
	}

	float Speedramp = 0f, fb = 0f, lr = 0f;

	/// <summary>
	/// Called every tick, clientside and serverside.
	/// </summary>
	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		if ( cl == Client )
		{
			EnableDrawing = true;
		}

		Rotation = Input.Down( InputButton.Duck ) ? Rotation.Slerp( Rotation, Input.Rotation, 0.05f ) : Rotation.Slerp( Rotation, Input.Rotation, 0.75f );

		EyeRotation = Rotation;

		fb = MathX.Lerp( fb, Input.Forward, 0.1f );
		lr = MathX.Lerp( lr, Input.Left, 0.1f );

		// build movement from the input values
		var movement = new Vector3( fb, lr, 0 );

		// rotate it to the direction we're facing
		Velocity = Rotation * movement;

		Speedramp = MathX.Lerp( Speedramp, Input.Down( InputButton.Run ) ? 300 : 150, 0.1f );

		// apply some speed to it
		Velocity *= Speedramp;

		// apply it to our position using MoveHelper, which handles collision
		// detection and sliding across surfaces for us
		MoveHelper helper = new MoveHelper( Position, Velocity );
		helper.Trace = helper.Trace.Size( 16 );
		if ( helper.TryMove( Time.Delta ) > 0 )
		{
			Position = helper.Position;
		}
	}

	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );

		// Update rotation every frame, to keep things smooth
		Rotation = Input.Down( InputButton.Duck ) ? Rotation.Slerp( Rotation, Input.Rotation, 0.05f ) : Rotation.Slerp( Rotation, Input.Rotation, 0.75f );
		EyeRotation = Rotation;
	}
}
