using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
namespace rh
{
	public enum EnemyType
	{
		Naked,
		Clothed,
		Armored,
		SuperArmored,
		Boss
	}

	public enum SpawnRarity
	{
		Common,
		Rare,
		UltraRare,
		Never
	}

	public enum EnemyWeapon
	{
		Pistol,
		Machinegun,
		Boxing,
		Unarmed
	}

	public enum EnemyMovementType
	{
		Walking,
		Flying
	}

	public partial class BaseEnemyClass : AnimatedEntity
	{

		[Net] public EnemyResource enemyResource { get; set; }

		public static BaseEnemyClass FromPath( string assetPath )
		{
			var enemyAsset = ResourceLibrary.Get<EnemyResource>( "resources/" + assetPath );

			var enemy = new BaseEnemyClass();

			foreach ( var item in enemyAsset.Clothing )
			{
				enemy.clothes.Add( ResourceLibrary.Get<Clothing>( item ) );
			}

			enemy.enemyResource = enemyAsset;

			return enemy;
		}

		public List<Clothing> clothes = new List<Clothing>();

		EnemyType type;
		EnemyWeapon weaponType;

		CitizenAnimationHelper helper;

		[Net] public Vector3 InputVelocity { get; set; }

		[Net] public Vector3 LookDir { get; set; }

		[Net] public Vector3 TargetDestination { get; set; }

		[Net] public Transform TargetHead { get; set; }

		[Net] public bool FlyingType { get; set; }

		PlayerPlatform platform;

		int ChosenShootTarget;

		[Net] public ModelEntity Pistol { get; set; }

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/citizen/citizen.vmdl" );

			SetAnimParameter( "holdtype_handedness", Rand.Int( 0, 2 ) );

			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			platform = All.OfType<PlayerPlatform>().FirstOrDefault();
			Tags.Add( "npc" );
			Health = 100f;

			DressEnemy();

			PickRandomTarget();
		}

		public async Task DressEnemy()
		{
			await Task.DelayRealtimeSeconds( 0.1f );
			weaponType = enemyResource.WeaponType;

			switch ( weaponType )
			{
				case EnemyWeapon.Pistol:
					Pistol = new ModelEntity( "weapons/rust_pistol/rust_pistol.vmdl" );
					Pistol.SetParent( this, true );
					break;
				case EnemyWeapon.Machinegun:
					Pistol = new ModelEntity( "weapons/rust_smg/rust_smg.vmdl" );
					Pistol.SetParent( this, true );
					break;
				case EnemyWeapon.Boxing:
					Pistol = new ModelEntity();
					Pistol.SetParent( this, true );
					break;
				case EnemyWeapon.Unarmed:
					SetAnimParameter( "idle_states", 3 );
					DeleteAsync( 10f );
					break;
				default:
					break;
			}

			if ( clothes.Count > 0 )
			{
				ClothingContainer container = new ClothingContainer();
				foreach ( var item in clothes )
				{
					container.Clothing.Add( item );
				}

				container.DressEntity( this, false );

			}

			SetMaterialGroup( "Skin0" + Rand.Int( 1, 5 ) );

			GetMaterialGroup();

			if ( enemyResource.Type == EnemyType.Armored )
			{
				Health *= 2f;
				Scale *= 1.1f;
			}

			if ( enemyResource.Type == EnemyType.SuperArmored )
			{
				Health *= 4f;
				Scale *= 1.2f;
			}

			if ( enemyResource.Type == EnemyType.Naked )
			{
				Health *= 0.5f;
				Scale *= 0.8f;
			}

			if ( enemyResource.Type == EnemyType.Boss )
			{
				Health *= 15f;
				Scale *= 2.5f;
			}

			if ( enemyResource.MovementType == EnemyMovementType.Flying )
			{
				if ( MathF.Abs( TargetDestination.z - Position.z ) < 5f )
				{
					TargetDestination += Vector3.Up * 100f;
					FlyingType = true;
				}
			}

			if ( Pistol != null )
			{
				ShootBullets();
			}
		}

		public void PickRandomTarget()
		{
			ChosenShootTarget = Rand.Int( 1, 4 );
		}

		enum EnemyActionStates
		{
			Shooting,
			Reloading
		}

		public async Task ShootBullets()
		{
			int BulletCount = 0;
			switch ( weaponType )
			{
				case EnemyWeapon.Pistol:
					BulletCount = 8;
					break;
				case EnemyWeapon.Machinegun:
					BulletCount = 20;
					break;
				case EnemyWeapon.Boxing:
					BulletCount = 0;
					break;
				case EnemyWeapon.Unarmed:
					break;
				default:
					break;
			}
			await Task.DelayRealtimeSeconds( Rand.Float( 2f, 5f ) * (BulletCount == 0 ? 0.1f : 1f) );
			bool IsBoss = (enemyResource != null && enemyResource.Type == EnemyType.Boss);
			if ( BulletCount > 0 )
			{
				for ( int i = 0; i < BulletCount; i++ )
				{
					var Bullet = new NPCBullet();
					Bullet.Owner = this;
					if ( Pistol.IsValid() )
					{
						Bullet.Position = Pistol.Position;
						Bullet.Rotation = Rotation.LookAt( LookDir - Pistol.Position );

						PlaySound( "rust_pistol.shoot" );
						SetAnimParameter( "b_attack", true );
						TargetDestination += (Vector3.Random * 60f).WithZ( 0 );
						await Task.DelayRealtimeSeconds( (Rand.Float( 0.5f, 2f ) * (IsBoss ? 0.25f : 1f)) / (BulletCount / 8f) );
						if ( Rand.Float() > 0.9f || IsBoss )
						{
							SetAnimParameter( "duck", 1f );
						}
						else if ( Rand.Float() > 0.5f )
						{
							SetAnimParameter( "duck", 0f );
						}
						PickRandomTarget();
					}
					else if ( this.IsValid() )
					{
						Bullet.Position = Transform.Position + Vector3.Up * 55f;
						Bullet.Rotation = Transform.Rotation;

						PlaySound( "rust_pistol.shoot" );
						SetAnimParameter( "b_attack", true );
						TargetDestination += (Vector3.Random * 60f).WithZ( 0 );
						await Task.DelayRealtimeSeconds( (Rand.Float( 0.5f, 2f ) * (IsBoss ? 0.25f : 1f)) / (BulletCount / 8f) );
						if ( Rand.Float() > 0.9f || IsBoss )
						{
							SetAnimParameter( "duck", 1f );
						}
						else if ( Rand.Float() > 0.5f )
						{
							SetAnimParameter( "duck", 0f );
						}
						PickRandomTarget();
					}
					else
					{
						Bullet.Delete();
					}

				}
				SetAnimParameter( "duck", 1f );
				SetAnimParameter( "b_reload", true );
				await Task.DelayRealtimeSeconds( 4f * (IsBoss ? 0.25f : 1f) * (BulletCount / 8f) );
				SetAnimParameter( "duck", 0f );

			}
			else
			{
				TargetDestination = LookDir.WithZ( platform.Position.z );
				for ( int i = 0; i < 10; i++ )
				{
					SetAnimParameter( "b_attack", true );
					if ( this.IsValid() && Vector3.DistanceBetween( Position, LookDir ) < 120f )
					{
						if ( ChosenShootTarget <= (Game.Current as RevolverHysteriaGame).VRPlayers.Count )
						{
							(Game.Current as RevolverHysteriaGame).VRPlayers[ChosenShootTarget - 1].HeadEnt.TakeMeleeDamage();
						}
						DeleteAsync( 0.5f );
						break;
					}

					TargetDestination += (Vector3.Random * 60f).WithZ( 0 );

					await Task.DelayRealtimeSeconds( Rand.Float( 0.2f, 0.5f ) * (IsBoss ? 0.25f : 1f) );
					PickRandomTarget();
					TargetDestination = LookDir.WithZ( platform.Position.z );

					await Task.DelayRealtimeSeconds( Rand.Float( 0.5f, 2f ) * (IsBoss ? 0.25f : 1f) );

				}
				TargetDestination = LookDir.WithZ( platform.Position.z );
			}

			TargetDestination += (Vector3.Random * 200f).WithZ( 0 );


			if ( this.IsValid() )
			{
				ShootBullets();
			}
		}


		public override void TakeDamage( DamageInfo info )
		{
			if ( GetBoneName( info.BoneIndex ) == "head" )
			{
				info.Damage *= 2.5f;
			}

			base.TakeDamage( info );

			SetAnimParameter( "hit_bone", info.BoneIndex );
			SetAnimParameter( "hit_direction", info.Position - Position );
			SetAnimParameter( "hit_offset", info.Position - Position );
			SetAnimParameter( "hit_strength", 0.05f );
			SetAnimParameter( "hit", true );

			if ( Health <= 0f )
			{
				NPCCorpse.FromNPC( this );
				if ( info.Attacker is VRPlayer pawn )
				{
					int scorecount = 100;

					scorecount += (enemyResource.Type == EnemyType.Armored ? 100 : 0);
					scorecount += (enemyResource.Type == EnemyType.SuperArmored ? 200 : 0);
					scorecount += (enemyResource.Type == EnemyType.Boss ? 2000 : 0);
					scorecount -= (enemyResource.WeaponType == EnemyWeapon.Unarmed ? 600 : 0);

					info.Attacker.Client.AddInt( "score", scorecount );
				}
			}
		}

		[Event.Tick.Server]
		public void Tick()
		{
			if ( platform == null )
			{
				return;
			}

			if ( TargetDestination == Vector3.Zero || Position == Vector3.Zero )
			{
				Delete();
				return;
			}

			helper = new CitizenAnimationHelper( this );

			if ( ChosenShootTarget <= (Game.Current as RevolverHysteriaGame).VRPlayers.Count )
			{
				LookDir = (Game.Current as RevolverHysteriaGame).VRPlayers[ChosenShootTarget - 1].HeadEnt.Position;//- Vector3.Up * 50f * Scale

				//DebugOverlay.Line( Position, (Game.Current as RevolverHysteriaGame).VRPlayers[ChosenShootTarget - 1].HeadEnt.Position - Vector3.Up * 10f );
			}
			else
			{
				LookDir = platform.GetAttachment( "player" + ChosenShootTarget ).Value.Position + Vector3.Up * 60f;
			}

			helper.WithLookAt( LookDir - Vector3.Up * 60f );
			helper.WithVelocity( Velocity );
			helper.WithWishVelocity( InputVelocity );

			switch ( weaponType )
			{
				case EnemyWeapon.Pistol:
					helper.HoldType = CitizenAnimationHelper.HoldTypes.Pistol;
					break;
				case EnemyWeapon.Machinegun:
					helper.HoldType = CitizenAnimationHelper.HoldTypes.Rifle;
					break;
				case EnemyWeapon.Boxing:
					helper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
					break;
				case EnemyWeapon.Unarmed:
					helper.HoldType = CitizenAnimationHelper.HoldTypes.None;
					if ( Velocity.Length < 0.5f )
					{
						SetAnimParameter( "idle_states", 3 );
					}
					break;
				default:
					break;
			}

			helper.IsNoclipping = FlyingType;

			if ( FlyingType )
			{
				TargetDestination = TargetDestination.WithZ( platform.Position.z + 100f );
			}

			Rotation = Rotation.LookAt( (LookDir - Position).WithZ( 0 ) );

			helper.IsGrounded = GroundEntity != null;

			if ( !helper.IsGrounded && !FlyingType )
			{
				helper.WithVelocity( -Vector3.Up * 400f );
				Velocity += -Vector3.Up * 400f;
			}

			if ( Vector3.DistanceBetween( Position, TargetDestination ) > 10f )
			{
				Velocity = -(Position - TargetDestination).Normal * 150f;
				InputVelocity = -(Position - TargetDestination).Normal * 150f;
			}
			else
			{
				Velocity = Vector3.Zero;
			}

			Move( Time.Delta );
		}

		protected virtual void Move( float timeDelta )
		{
			var bbox = BBox.FromHeightAndRadius( 30f + (30f * MathF.Abs( helper.DuckLevel - 1f )), 4 );
			//DebugOverlay.Box( Position, bbox.Mins, bbox.Maxs, Color.Green );

			MoveHelper move = new( Position, Velocity );
			move.MaxStandableAngle = 50;
			move.Trace = move.Trace.Ignore( this ).Size( bbox );

			if ( !Velocity.IsNearlyZero( 0.001f ) )
			{
				//	Sandbox.Debug.Draw.Once
				//						.WithColor( Color.Red )
				//						.IgnoreDepth()
				//						.Arrow( Position, Position + Velocity * 2, Vector3.Up, 2.0f );

				//using ( Sandbox.Debug.Profile.Scope( "TryUnstuck" ) )
				move.TryUnstuck();

				//using ( Sandbox.Debug.Profile.Scope( "TryMoveWithStep" ) )
				move.TryMoveWithStep( timeDelta, 30 );
			}

			//using ( Sandbox.Debug.Profile.Scope( "Ground Checks" ) )
			//{
			var tr = move.TraceDirection( Vector3.Down * 10.0f );
			if ( !FlyingType )
			{
				if ( move.IsFloor( tr ) )
				{
					GroundEntity = tr.Entity;

					if ( !tr.StartedSolid )
					{
						move.Position = tr.EndPosition;
					}

					if ( InputVelocity.Length > 0 )
					{
						var movement = move.Velocity.Dot( InputVelocity.Normal );
						move.Velocity = move.Velocity - movement * InputVelocity.Normal;
						move.ApplyFriction( tr.Surface.Friction * 10.0f, timeDelta );
						move.Velocity += movement * InputVelocity.Normal;

					}
					else
					{
						move.ApplyFriction( tr.Surface.Friction * 10.0f, timeDelta );
					}
				}
				else
				{
					GroundEntity = null;
					move.Velocity += Vector3.Down * 900 * timeDelta;
					//Sandbox.Debug.Draw.Once.WithColor( Color.Red ).Circle( Position, Vector3.Up, 10.0f );
				}
			}
			//}

			Position = move.Position;
			Velocity = move.Velocity;
		}
	}
}
