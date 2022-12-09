using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;


namespace rh
{
	public enum HandSide
	{
		None, Left, Right
	}

	public enum HandPose
	{
		Revolver = 0,
		Empty = 1
	}
	public partial class VRHand : AnimatedEntity
	{

		[Net] public HandSide hand { get; set; }

		[Net] public Clothing skintone { get; set; }

		[Net] public float ThumbClamp { get; set; } = 1f;
		[Net] public float IndexClamp { get; set; } = 1f;
		[Net] public float MiddleClamp { get; set; } = 1f;
		[Net] public float RingClamp { get; set; } = 1f;

		[Net] public bool Initialized { get; set; }

		[ConVar.Replicated( "shotguns" )]
		public static bool UseShotguns { get; set; }

		[Net, Predicted] public WeaponBaseClass Gun { get; set; }

		[Net] public ModelEntity Wristwatch { get; set; }

		[Net] public ModelEntity Coin { get; set; }

		[Net] public bool PutCoin { get; set; }

		WristUI wristUI;
		WristUI wristUI2;

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			wristUI = new WristUI( false );
			wristUI2 = new WristUI( true );
		}

		public override void Spawn()
		{
			Predictable = true;
		}

		public void GrabClamp()
		{
			ThumbClamp = 0.8f;
			IndexClamp = 0.2f;
			MiddleClamp = 1f;
			RingClamp = 1f;
		}

		public void NoClamp()
		{
			ThumbClamp = 1f;
			IndexClamp = 1f;
			MiddleClamp = 1f;
			RingClamp = 1f;
		}

		[Net] bool dressedHand { get; set; }

		public void ReplaceGun()
		{
			if ( Gun is Revolver && Gun is not Shotgun )
			{
				Gun.Delete();
				Gun = new Shotgun();
				Gun.Owner = Owner;
				Gun.HandEnt = this;
			}

			else if ( Gun is Shotgun )
			{
				Gun.Delete();
				Gun = new Revolver();
				Gun.Owner = Owner;
				Gun.HandEnt = this;
			}
		}

		public void HandleHand()
		{
			/*if ( UseShotguns && Gun is not Shotgun )
			{
				ReplaceGun();
			} else if ( !UseShotguns && Gun is Shotgun )
			{
				ReplaceGun();
			}*/

			if ( IsServer && Initialized )
			{
				switch ( hand )
				{
					case HandSide.None:
						break;
					case HandSide.Left:
						if ( Input.VR.LeftHand.JoystickPress.WasPressed )
						{
							ReplaceGun();
						}
						break;
					case HandSide.Right:
						if ( Input.VR.RightHand.JoystickPress.WasPressed )
						{
							ReplaceGun();
						}
						break;
					default:
						break;
				}
			}

			if ( hand == HandSide.None )
			{
				return;
			}
			else if ( !Initialized )
			{
				switch ( hand )
				{
					case HandSide.Left:
						if ( !UseShotguns )
						{
							Gun = new Revolver();
						}
						else
						{
							Gun = new Shotgun();
						}
						Gun.Owner = Owner;
						Gun.HandEnt = this;

						Gun.EnableDrawing = false;

						Coin = new ModelEntity( "models/player/token.vmdl" );
						if ( !Coin.IsValid() )
						{
							return;
						}
						Coin.SetParent( this, true );

						SetModel( "models/player/vrhand_revolver_left.vmdl" );

						Wristwatch = new ModelEntity( "models/player/wristwatch.vmdl" );
						Wristwatch.SetParent( this, true );
						Wristwatch.LocalRotation *= new Angles( 0, 90, 0 ).ToRotation();
						break;
					case HandSide.Right:
						if ( !UseShotguns )
						{
							Gun = new Revolver();
						}
						else
						{
							Gun = new Shotgun();
						}
						Gun.Owner = Owner;
						Gun.HandEnt = this;

						SetModel( "models/player/vrhand_revolver_right.vmdl" );
						break;
					default:
						break;
				}

				Initialized = true;
			}

			if ( IsClient && wristUI != null && Wristwatch != null )
			{
				wristUI.Wristwatch = Wristwatch;
				wristUI2.Wristwatch = Wristwatch;
			}

			if ( !dressedHand && skintone != null )
			{
				ClothingContainer container = new ClothingContainer();
				container.Clothing.Add( skintone );
				container.DressEntity( this );
				dressedHand = true;
			}

			Gun.UpdateGun();

			bool ShowGun = (Owner as VRPlayer).HeadEnt.HitPoints > 0;

			if ( hand == HandSide.Left && Coin.IsValid() )
			{
				Gun.EnableDrawing = PutCoin && ShowGun;
				Coin.EnableDrawing = !PutCoin;
				SetAnimParameter( "handpose", (int)((Gun.EnableDrawing || Coin.EnableDrawing) ? HandPose.Revolver : HandPose.Empty) );
			}
			else
			{
				Gun.EnableDrawing = ShowGun;
				SetAnimParameter( "handpose", (int)(Gun.EnableDrawing ? HandPose.Revolver : HandPose.Empty) );
			}

			//EnableHideInFirstPerson = CurrentView.Viewer == Client;

			//DebugOverlay.Line( Position, Position + Vector3.Up );

			var vrhand = Input.VR.LeftHand;

			switch ( hand )
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


			if ( Input.VR.IsKnuckles || Input.VR.IsRift )
			{
				SetAnimParameter( "Thumb", Coin != null && Coin.EnableDrawing ? 0f : vrhand.GetFingerValue( FingerValue.ThumbCurl ) );
				SetAnimParameter( "Index", Coin != null && Coin.EnableDrawing ? 0f : vrhand.GetFingerValue( FingerValue.IndexCurl ) );
				SetAnimParameter( "Middle", vrhand.GetFingerValue( FingerValue.MiddleCurl ) );
				SetAnimParameter( "Ring", vrhand.GetFingerValue( FingerValue.RingCurl ) );
			}
			else
			{
				SetAnimParameter( "Thumb", 1f );
				SetAnimParameter( "Index", 1f );
				SetAnimParameter( "Middle", vrhand.Grip.Value );
				SetAnimParameter( "Ring", 1f );
			}

			Transform = vrhand.Transform.WithScale( 0.75f );

			Rotation = vrhand.Transform.Rotation * new Angles( 0, 0, 0 ).ToRotation() * new Angles( -10f * Gun.TiltRecoil, 0, 0 ).ToRotation();

			Position = vrhand.Transform.Position - vrhand.Transform.Rotation.Forward * 3.6f + Rotation.Backward * Gun.BackRecoil + Rotation.Up * Gun.UpRecoil;

			SetAnimParameter( "f_trigger", vrhand.Trigger.Value );

			SetAnimParameter( "f_hammer", -vrhand.Joystick.Value.y - ((Gun as Revolver).OpenCylinder ? 1f : 0f) );

		}
	}
}
