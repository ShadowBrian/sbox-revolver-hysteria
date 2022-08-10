﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace rh
{
	public partial class Revolver : WeaponBaseClass
	{

		bool CycleBreak;

		bool JustFired;

		float TargetCylinderRotation;

		bool Opening, Closing;

		float LastDegrees;

		bool stepReachedLastFrame;

		[Net, Predicted] public bool OpenCylinder { get; set; }

		public override void OnAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData )
		{
			base.OnAnimEventGeneric( name, intData, floatData, vectorData, stringData );
			if ( name.Contains( "eject" ) && IsClient )
			{
				for ( int i = 1; i <= 6 - AmmoLeft; i++ )
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

		public override void UpdateGun()
		{
			if ( !EnableDrawing )
			{
				return;
			}
			SetBodyGroup( "bullets", AmmoLeft );

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

			Rotation = vrhand.Transform.Rotation * new Angles( 45, 0, 0 ).ToRotation() * new Angles( -10f * TiltRecoil, 0, 0 ).ToRotation();

			Position = vrhand.Transform.Position - vrhand.Transform.Rotation.Forward * 5f + Rotation.Backward * BackRecoil + Rotation.Up * UpRecoil;

			TiltRecoil = MathX.Lerp( TiltRecoil, 0f, 0.5f );
			BackRecoil = MathX.Lerp( BackRecoil, 0f, 0.4f );
			UpRecoil = MathX.Lerp( UpRecoil, 0f, 0.4f );

			Opening = vrhand.ButtonB.WasPressed || vrhand.JoystickPress.WasPressed;

			Closing = vrhand.Velocity.z > 100f;

			if ( Opening && !OpenCylinder )
			{
				OpenCylinder = true;
				PlaySound( "revolver_open" );
				//AmmoLeft = 0;
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

			if ( OpenCylinder )
			{
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
					if ( AmmoLeft < 6 )
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
					if ( AmmoLeft < 6 )
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
	}
}