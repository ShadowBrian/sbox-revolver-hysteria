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
		[Net,Predicted] public int HitPoints { get; set; } = 5;

		ModelEntity RedPostEnt { get; set; }

		ModelEntity GreenPostEnt { get; set; }

		[Net] public Entity VRPlayerEnt { get; set; }

		[Net] public AnimatedEntity HeadModel { get; set; }

		[Net, Predicted] public bool HeadDressed { get; set; }

		float RedAlpha, GreenAlpha;

		public override void Spawn()
		{
			base.Spawn();
			Capsule cap = new Capsule( Position, Position - Rotation.Up * 50f, 12.5f );
			SetupPhysicsFromCapsule( PhysicsMotionType.Keyframed, cap ).SetSurface( "flesh" );
			Tags.Add( "player" );
			Transform = Input.VR.Head;
			HeadModel = new AnimatedEntity( "models/player/vrhead.vmdl" );
			HeadModel.Owner = this;
			HeadModel.Position = Position;
			HeadModel.Position += HeadModel.Rotation.Forward * 100f;
			HeadModel.Rotation = Rotation;
		}

		public void HandleHead()
		{
			if ( VRPlayerEnt == null)
			{
				return;
			}
			Position = Input.VR.Head.Position - Input.VR.Head.Rotation.Forward * 10f;
			Rotation = Rotation.LookAt( Input.VR.Head.Rotation.Forward.WithY( 0 ) );


			if ( HeadModel != null && this.IsValid())
			{
				HeadModel.Position = Input.VR.Head.Position;
				HeadModel.Scale = 0.75f;
				//HeadModel.Position += Input.VR.Head.Rotation.Forward * 100f;

				HeadModel.Rotation = Input.VR.Head.Rotation;// * new Angles(0,180,0).ToRotation();

				if ( IsClient )
				{
					HeadModel.EnableDrawing = false;

					foreach ( var item in ClothingEntities )
					{
						item.EnableDrawing = HeadModel.EnableDrawing;
					}
				}
			}
			//DebugOverlay.Sphere( Position - Rotation.Forward * 10f, 10f, Color.Red );
			//DebugOverlay.Sphere( Position - Rotation.Forward * 10f - Rotation.Up * 50f, 10f, Color.Red );


			if ( !HeadDressed )
			{
				Clothing ??= new();
				Clothing.LoadFromClient( Owner.Client );

				HeadOnly ??= new();

				foreach ( var item in Clothing.Clothing )
				{
					if ( item.Category == Sandbox.Clothing.ClothingCategory.Facial || item.Category == Sandbox.Clothing.ClothingCategory.Hair || item.Category == Sandbox.Clothing.ClothingCategory.Skin || item.Category == Sandbox.Clothing.ClothingCategory.Hat )
					{
						HeadOnly.Clothing.Add( item );
					}

					if ( item.Category == Sandbox.Clothing.ClothingCategory.Skin )
					{
						(VRPlayerEnt as VRPlayer).LH.skintone = item;
						(VRPlayerEnt as VRPlayer).RH.skintone = item;
					}
				}

				HeadOnly.DressEntity( HeadModel );

				foreach ( var item in HeadModel.Children )
				{
					if ( item.Tags.Has( "clothing" ) )
					{
						ClothingEntities.Add( item as ModelEntity );
					}
				}

				HeadDressed = true;
			}
		}

		ClothingContainer Clothing, HeadOnly;

		List<ModelEntity> ClothingEntities = new List<ModelEntity>();

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

		public void AddHealth(long TargetPlayer)
		{
			HitPoints++;
			if ( TargetPlayer == Local.PlayerId )
			{
				DoPostFXGreen();
			}
		}

		[ClientRpc]
		public void DoPostFXGreen()
		{
			GreenFade();
		}

		public void ShowDeathScreen()
		{
			RedAlpha = 0.5f;
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
		public void DoPostFXRed(long player)
		{
			if ( player == Local.PlayerId )
			{
				RedFade();
			}
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

				DoPostFXRed(Local.PlayerId);
			}
			if ( HitPoints <= 0 )
			{
				GoIntoCage();
			}
		}
		public override void TakeDamage( DamageInfo info )
		{
			base.TakeDamage( info );

			if ( HitPoints > 0 )
			{
				HitPoints--;

				DoPostFXRed(Local.PlayerId);
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
				chosencage.UsedCage = true;
			}

		}

	}
}
