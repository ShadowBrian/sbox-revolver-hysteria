using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace rh
{
	public partial class VRHead : ModelEntity
	{
		[Net] public int HitPoints { get; set; } = 5;

		ModelEntity RedPostEnt { get; set; }

		ModelEntity GreenPostEnt { get; set; }

		[Net] public Entity VRPlayerEnt { get; set; }

		float RedAlpha, GreenAlpha;

		public override void Spawn()
		{
			base.Spawn();
			Capsule cap = new Capsule( Position, Position - Rotation.Up * 50f, 12.5f );
			SetupPhysicsFromCapsule( PhysicsMotionType.Keyframed, cap ).SetSurface( "flesh" );
			Transform = Input.VR.Head;
		}

		[Event.Tick]
		public void Tick()
		{
			if ( VRPlayerEnt == null )
			{
				return;
			}
			Position = Input.VR.Head.Position - Input.VR.Head.Rotation.Forward * 10f;
			Rotation = Rotation.LookAt( Input.VR.Head.Rotation.Forward.WithY( 0 ) );
			//DebugOverlay.Sphere( Position - Rotation.Forward * 10f, 10f, Color.Red );
			//DebugOverlay.Sphere( Position - Rotation.Forward * 10f - Rotation.Up * 50f, 10f, Color.Red );
		}

		[Event.Frame]
		public void Frame()
		{
			if ( RedPostEnt != null )
			{
				RedPostEnt.Transform = Input.VR.Head;
				RedPostEnt.RenderColor = RedPostEnt.RenderColor.WithAlpha( MathX.Lerp( RedPostEnt.RenderColor.a, RedAlpha, 0.1f ) );
			}

			if ( GreenPostEnt != null )
			{
				GreenPostEnt.Transform = Input.VR.Head;
				GreenPostEnt.RenderColor = GreenPostEnt.RenderColor.WithAlpha( MathX.Lerp( GreenPostEnt.RenderColor.a, GreenAlpha, 0.1f ) );
			}
		}

		public void AddHealth()
		{
			HitPoints++;
			DoPostFXGreen();
		}

		[ClientRpc]
		public void DoPostFXGreen()
		{
			GreenFade();
		}

		public async Task GreenFade()
		{
			if ( GreenPostEnt == null )
			{
				GreenPostEnt = new ModelEntity( "models/player/headbox.vmdl" );
				GreenPostEnt.EnableDrawOverWorld = true;
				GreenPostEnt.Owner = Owner;
				GreenPostEnt.Transform = Transform;
				GreenPostEnt.SetMaterialGroup( "green" );
			}

			GreenAlpha = 0.75f;
			await Task.DelayRealtimeSeconds( 0.5f );
			GreenAlpha = 0f;
		}


		[ClientRpc]
		public void DoPostFXRed()
		{
			RedFade();
		}

		public async Task RedFade()
		{
			if ( RedPostEnt == null )
			{
				RedPostEnt = new ModelEntity( "models/player/headbox.vmdl" );
				RedPostEnt.EnableDrawOverWorld = true;
				RedPostEnt.Owner = Owner;
				RedPostEnt.Transform = Transform;
			}

			RedAlpha = 0.75f;
			await Task.DelayRealtimeSeconds( 0.5f );
			RedAlpha = 0f;
		}

		public void TakeMeleeDamage()
		{
			if ( HitPoints > 0 )
			{
				HitPoints--;

				DoPostFXRed();
			}
			if ( HitPoints <= 0 )
			{
				GoIntoCage();
			}
		}

		public void GoIntoCage()
		{
			List<RespawnCage> cagesAvailable = new List<RespawnCage>();
			RespawnCage chosencage = null;
			foreach ( RespawnCage cage in All.OfType<RespawnCage>() )
			{
				if ( !cage.UsedCage )
				{
					cagesAvailable.Add( cage );
				}
			}

			if ( cagesAvailable.Count > 0 )
			{
				float closest = 100000f;

				foreach ( RespawnCage cage in cagesAvailable )
				{
					if ( Vector3.DistanceBetween( Position, cage.Position ) < closest )
					{
						closest = Vector3.DistanceBetween( Position, cage.Position );
						chosencage = cage;
					}
				}
			}


			if ( chosencage != null )
			{
				(VRPlayerEnt as VRPlayer).cage = chosencage;
				chosencage.OccupyingPlayer = (VRPlayerEnt as VRPlayer);
			}
		}

		public override void TakeDamage( DamageInfo info )
		{
			base.TakeDamage( info );
			if ( HitPoints > 0 )
			{
				HitPoints--;

				DoPostFXRed();
			}
			if ( HitPoints <= 0 )
			{
				GoIntoCage();
			}
		}
	}
}
