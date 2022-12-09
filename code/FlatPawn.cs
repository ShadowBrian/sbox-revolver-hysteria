using rh;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sandbox;

partial class FlatPawn : AnimatedEntity
{
	PlayerPlatform platform;
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
		platform = Entity.All.OfType<PlayerPlatform>().FirstOrDefault();
	}

	[Event.Client.PostCamera]
	public void PostCameraSetup()
	{
		Camera.FirstPersonViewer = this;
	}

	float Speedramp = 0f, fb = 0f, lr = 0f;

	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Angles ViewAngles { get; set; }

	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;

		var look = Input.AnalogLook;

		var viewAngles = ViewAngles;
		viewAngles += look;
		ViewAngles = viewAngles.Normal;
	}

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

		Rotation = Input.Down( InputButton.Duck ) ? Rotation.Slerp( Rotation, ViewAngles.ToRotation(), 0.05f ) : Rotation.Slerp( Rotation, ViewAngles.ToRotation(), 0.75f );

		//AimRay.Forward = Rotation.Forward;

		fb = MathX.Lerp( fb, InputDirection.x, 0.1f );
		lr = MathX.Lerp( lr, InputDirection.y, 0.1f );

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
		//helper.Trace = helper.Trace.Size( 16 );
		if ( helper.TryMove( Time.Delta ) > 0 )
		{
			Position = helper.Position;
		}

		if ( Input.Pressed( InputButton.PrimaryAttack ) && (GameManager.Current as RevolverHysteriaGame).VRPlayers.Count == 0 && platform.IsValid() && platform.GameHasStarted )
		{
			ShootBullet( 0.01f, 10f, 500f, 1f );
			PlaySound( "revolver_fire" );
		}

	}

	[ClientRpc]
	public void CreateTracerEffect( Vector3 hitPosition, Vector3 startPosition )
	{
		// get the muzzle position on our effect entity - either viewmodel or world model
		//var pos = Transform;//EffectEntity.GetAttachment( "muzzle" ) ??

		var system = Particles.Create( "particles/tracer.standard.vpcf" );
		system?.SetPosition( 0, startPosition );
		system?.SetPosition( 1, hitPosition );
	}

	public virtual IEnumerable<TraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		bool underWater = Trace.TestPoint( start, "water" );

		var trace = Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "npc" )
				.WithoutTags( "player" )
				.Ignore( this )
				.Size( radius );



		//
		// If we're not underwater then we can hit water
		//
		if ( !underWater )
			trace = trace.WithAnyTags( "water" );

		var tr = trace.Run();

		if ( tr.Entity is ButtonEntity butt )
		{
			butt.OnUse( Owner );
		}

		if ( tr.Hit )
			yield return tr;

		//
		// Another trace, bullet going through thin material, penetrating water surface?
		//
	}

	public virtual void ShootBullet( float spread, float force, float damage, float bulletSize, int bulletCount = 1 )
	{
		//
		// Seed rand using the tick, so bullet cones match on client and server
		//
		Rand.SetSeed( Time.Tick );

		for ( int i = 0; i < bulletCount; i++ )
		{
			var forward = Rotation.Forward;
			forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
			forward = forward.Normal;

			//
			// ShootBullet is coded in a way where we can have bullets pass through shit
			// or bounce off shit, in which case it'll return multiple results
			//
			foreach ( var tr in TraceBullet( Position, Position + forward * 5000, bulletSize ) )
			{
				tr.Surface.DoBulletImpact( tr );

				if ( tr.Distance > 200 )
				{
					CreateTracerEffect( tr.EndPosition, Position );
				}

				if ( !IsServer ) continue;
				if ( !tr.Entity.IsValid() ) continue;

				var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 100 * force, damage )
					.UsingTraceResult( tr )
					.WithAttacker( Owner )
					.WithWeapon( this );

				tr.Entity.TakeDamage( damageInfo );
			}
		}
	}

	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );

		// Update rotation every frame, to keep things smooth
		Rotation = Input.Down( InputButton.Duck ) ? Rotation.Slerp( Rotation, ViewAngles.ToRotation(), 0.05f ) : Rotation.Slerp( Rotation, ViewAngles.ToRotation(), 0.75f );
		//EyeRotation = Rotation;

		Camera.Position = Position;
		Camera.Rotation = Rotation;
	}
}
