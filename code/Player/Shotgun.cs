using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace rh
{
	public partial class Shotgun : Revolver
	{

		bool CycleBreak;

		bool JustFired;

		[Net] float TargetCylinderRotation { get; set; }

		bool Opening, Closing;

		float LastDegrees;

		bool stepReachedLastFrame;

		bool PlayedTutorial;

		WorldLabel TutorialLabel;

		public override string ModelPath => "models/shotgun/shotgun.vmdl";

		[Net] public new int AmmoLeft { get; set; } = 2;
		 
		[Net, Predicted] public bool OpenCylinder { get; set; }

		public override void OnAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData )
		{
			base.OnAnimEventGeneric( name, intData, floatData, vectorData, stringData );
			if ( name.Contains( "eject" ) && IsClient )
			{
				for ( int i = 1; i <= 2 - AmmoLeft; i++ )
				{
					Transform trans = GetBoneTransform( "bullet" + i );
					var bullet = new ModelEntity( "models/revolver/casing.vmdl" );
					bullet.SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
					bullet.Transform = trans;
					bullet.Rotation = Rotation * new Angles( 40f, 0, 0 ).ToRotation();
					bullet.Velocity = -bullet.Rotation.Forward * 50f + Rotation.Up * 10f;
					bullet.DeleteAsync( 10f );
				}
			}
		}

		public override void Spawn()
		{
			base.Spawn();
			Scale = 1.2f;
		}

		Vector3 lastforwardvel;
		public override void UpdateGun()
		{
			if ( !EnableDrawing )
			{
				return;
			}
			SetBodyGroup( "bullets", AmmoLeft);

			var vrhand = Input.VR.LeftHand;

			switch ( HandEnt.hand )
			{
				case HandSide.Left:
					vrhand = Input.VR.LeftHand;
					break;
				case HandSide.Right:
					vrhand = Input.VR.RightHand;
					break;
				default:
					break;
			}

			Rotation = vrhand.Transform.Rotation * new Angles( 45, 0, 0 ).ToRotation() * new Angles( -10f * TiltRecoil - 20f, 0, 0 ).ToRotation();

			Position = vrhand.Transform.Position - vrhand.Transform.Rotation.Forward * 5f + Rotation.Backward * BackRecoil + Rotation.Up * UpRecoil + Rotation.Up  * 0.5f + Rotation.Forward;

			if ( IsClientOnly )
			{
				Delete();
			}

			TiltRecoil = MathX.Lerp( TiltRecoil, 0f, 0.5f );
			BackRecoil = MathX.Lerp( BackRecoil, 0f, 0.4f );
			UpRecoil = MathX.Lerp( UpRecoil, 0f, 0.4f );

			Opening = vrhand.ButtonB.WasPressed || vrhand.JoystickPress.WasPressed;

			float tipspeed = (Rotation.Forward * 10f).z - lastforwardvel.z;

			Closing = vrhand.Velocity.z > 60f || (tipspeed > 0.5f);

			lastforwardvel = (Rotation.Forward * 10f).z;

			if ( Opening && !OpenCylinder )
			{
				OpenCylinder = true;
				PlaySound( "revolver_open" );
			}

			if ( Closing && OpenCylinder )
			{
				OpenCylinder = false;
				TargetCylinderRotation = 0f;
				PlaySound( "revolver_close" );
			}

			SetAnimParameter( "b_open", Opening );

			SetAnimParameter( "b_close", Closing );

			SetAnimParameter( "f_zvel", MathX.Lerp( GetAnimParameterFloat( "f_zvel" ), -vrhand.Velocity.z, 0.1f ) );

			SetAnimParameter( "f_trigger", vrhand.Trigger.Value );

			if ( vrhand.Trigger.Value >= 0.8f )
			{
				CycleBreak = true;
			}

			if ( vrhand.Trigger.Value <= 0.1f )
			{
				CycleBreak = false;
				JustFired = false;
			}

			if ( !CycleBreak )
			{
				SetAnimParameter( "f_hammer", MathF.Max( vrhand.Trigger.Value, -vrhand.Joystick.Value.y - (OpenCylinder ? 1f : 0f) ) );
			}
			else if ( -vrhand.Joystick.Value.y < 0.1f )
			{
				SetAnimParameter( "f_hammer", 0f );
				if ( !JustFired && !OpenCylinder )
				{

					JustFired = true;
					TargetCylinderRotation += 60f;
					//PlaySound( "revolver_cycle" );
					TryFireBullet();

					/*if(TargetCylinderRotation > 360 )
					{
						TargetCylinderRotation = 0f;
					}*/
				}
			}

			if ( AmmoLeft <= 0 && IsClient )
			{
				if ( TutorialLabel == null )
				{
					TutorialLabel = new WorldLabel( true );
					TutorialLabel.Position = vrhand.Transform.Position - Rotation.Up * 7f;
					TutorialLabel.Rotation = vrhand.Transform.Rotation * new Angles( -45, 180, 0 ).ToRotation();
					TutorialLabel.label.Text = "B button\nOpen gun";
				}
			}

			if ( TutorialLabel != null )
			{
				TutorialLabel.Position = vrhand.Transform.Position - Rotation.Up * 7f;
				TutorialLabel.Rotation = vrhand.Transform.Rotation * new Angles( -45, 180, 0 ).ToRotation();
				if ( !OpenCylinder && PlayedTutorial )
				{
					TutorialLabel.label.Text = "";
				}
			}

			if ( OpenCylinder )
			{

				if ( !PlayedTutorial )
				{
					if ( TutorialLabel != null )
					{
						if ( AmmoLeft < 2 )
						{
							TutorialLabel.label.Text = "Rotate joystick\nReload";
						}
						else
						{
							TutorialLabel.label.Text = "Flick up\nClose gun";
							PlayedTutorial = true;
						}
					}
				}

				float x = vrhand.Joystick.Value.x;
				float y = vrhand.Joystick.Value.y;

				float rads = MathF.Atan2( y, x );
				float degrees = MathX.RadianToDegree( rads );

				if ( LastDegrees > 0 && LastDegrees < 360 && (degrees - LastDegrees) < 60f )
				{
					TargetCylinderRotation += (degrees - LastDegrees);
				}

				float currentAngle = degrees;
				bool stepReached = (currentAngle + 1 * 0.5f) % 60 < 1;

				if ( stepReached && !stepReachedLastFrame )
				{
					if ( AmmoLeft < 2 )
					{
						AmmoLeft++;
						vrhand.TriggerHapticVibration( 0.1f, 200f, 0.5f );
						//PlaySound( "revolver_cycle" );
						PlaySound( "revolver_reload" );
						//Log.Trace( "added ammo" );
					}
				}
				else if ( !stepReached && stepReachedLastFrame )
				{
					if ( AmmoLeft < 2 )
					{
						AmmoLeft++;
						vrhand.TriggerHapticVibration( 0.1f, 200f, 0.5f );
						//PlaySound( "revolver_cycle" );
						PlaySound( "revolver_reload" );
						//Log.Trace( "added ammo" );
					}
				}
				stepReachedLastFrame = stepReached;

				LastDegrees = degrees;
			}

			if ( GetAnimParameterFloat( "f_cylinder" ) - TargetCylinderRotation / 360f > 90f )
			{
				SetAnimParameter( "f_cylinder", TargetCylinderRotation / 360f );
			}
			else
			{
				SetAnimParameter( "f_cylinder", MathX.Lerp( GetAnimParameterFloat( "f_cylinder" ), TargetCylinderRotation / 360f, 0.25f ) );
			}
		}

		public override void TryFireBullet()
		{
			var vrhand = Input.VR.LeftHand;

			switch ( HandEnt.hand )
			{
				case HandSide.Left:
					vrhand = Input.VR.LeftHand;
					break;
				case HandSide.Right:
					vrhand = Input.VR.RightHand;
					break;
				default:
					break;
			}
			if ( AmmoLeft > 0 )
			{
				vrhand.TriggerHapticVibration( 0.1f, 20f, 1f );
				BackRecoil = Rand.Float( 0.3f, 0.7f );
				UpRecoil = Rand.Float( 0.3f, 0.7f );
				TiltRecoil = Rand.Float( 0.7f, 1.2f );
				AmmoLeft--;

				ShootBullet( 0.2f, 10f, 20f, 1f, 6 );

				PlaySound( "revolver_fire" );
			}
			else
			{
				PlaySound( "revolver_hammer" );
				PlaySound( "revolver_dryfire" );
			}

		}

		bool Shot;

		public override void ShootBullet( float spread, float force, float damage, float bulletSize, int bulletCount = 1 )
		{
			//
			// Seed rand using the tick, so bullet cones match on client and server
			//
			Rand.SetSeed( Time.Tick );

			Shot = !Shot;

			for ( int i = 0; i < bulletCount; i++ )
			{
				Transform muzzle = Shot ? GetAttachment( "muzzle" ).Value : GetAttachment( "muzzle2" ).Value;
				var forward = muzzle.Rotation.Forward;
				forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
				forward = forward.Normal;

				//
				// ShootBullet is coded in a way where we can have bullets pass through shit
				// or bounce off shit, in which case it'll return multiple results
				//
				foreach ( var tr in TraceBullet( muzzle.Position, muzzle.Position + forward * 5000, bulletSize ) )
				{
					tr.Surface.DoBulletImpact( tr );

					if ( tr.Distance > 200 )
					{
						CreateTracerEffect( tr.EndPosition, muzzle.Position );
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
	}
}
