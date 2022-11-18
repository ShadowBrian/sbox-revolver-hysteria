using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace rh
{
	public partial class WeaponBaseClass : AnimatedEntity
	{
		[Net, Predicted] public VRHand HandEnt { get; set; }

		public virtual string ModelPath => "models/revolver/revolver.vmdl";

		[Net] public int AmmoLeft { get; set; } = 6;
		public float TiltRecoil, UpRecoil, BackRecoil;


		public override void Spawn()
		{
			base.Spawn();
			SetModel( ModelPath );
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
				var forward = GetAttachment( "muzzle" ).Value.Rotation.Forward;
				forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
				forward = forward.Normal;

				//
				// ShootBullet is coded in a way where we can have bullets pass through shit
				// or bounce off shit, in which case it'll return multiple results
				//
				foreach ( var tr in TraceBullet( GetAttachment( "muzzle" ).Value.Position, GetAttachment( "muzzle" ).Value.Position + forward * 5000, bulletSize ) )
				{
					tr.Surface.DoBulletImpact( tr );

					if ( tr.Distance > 200 )
					{
						CreateTracerEffect( tr.EndPosition, GetAttachment( "muzzle" ).Value.Position );
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

		public virtual void TryFireBullet()
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
				BackRecoil = Rand.Float( 0.2f, 0.6f );
				UpRecoil = Rand.Float( 0.2f, 0.6f );
				TiltRecoil = Rand.Float( 0.5f, 1.2f );
				AmmoLeft--;

				ShootBullet( 0.01f, 10f, 50f, 1f );

				PlaySound( "revolver_fire" );
			}
			else
			{
				PlaySound( "revolver_hammer" );
				PlaySound( "revolver_dryfire" );
			}

		}

		public virtual void UpdateGun()
		{

		}
	}
}
