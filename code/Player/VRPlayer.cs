using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;


namespace rh
{

	public partial class VRPlayer : AnimatedEntity
	{
		[Net, Predicted] public bool Initialized { get; set; }
		[Net, Predicted] public VRHand LH { get; set; }
		[Net, Predicted] public VRHand RH { get; set; }

		[Net] public Transform Head { get; set; }

		[Net, Predicted] int RotatedTick { get; set; }
		[Net, Predicted] bool JustRotated { get; set; }

		[Net, Predicted] public VRHead HeadEnt { get; set; }

		[Net] public RespawnCage cage { get; set; }

		public override void Spawn()
		{
			Predictable = true;
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			if ( Input.VR.IsActive && cl == Client )
			{

				if ( RH != null )
				{
					if ( cl.Pawn is Pawn pawn && !cage.IsValid() )
					{
						VR.Anchor = Transform;
						Transform = pawn.Transform.WithRotation( Rotation );
					}
					else if ( cl.Pawn is Pawn pawn2 && cage.IsValid() )
					{
						VR.Anchor = Transform;
						Transform = cage.Transform.WithRotation( Rotation );
					}

					HandleHands();
				}
				else if ( IsServer )
				{
					LH = new VRHand();
					LH.Owner = this;
					LH.hand = HandSide.Left;
					RH = new VRHand();
					RH.Owner = this;
					RH.hand = HandSide.Right;

					/*LH.SetParent( this );
					RH.SetParent( this );
					HeadEnt.SetParent( this );*/

					HeadEnt = new VRHead();
					HeadEnt.Owner = Owner;
					HeadEnt.VRPlayerEnt = this;
				}

				Head = Input.VR.Head;


				if ( Input.VR.RightHand.ButtonA.WasPressed && !JustRotated )
				{
					RotatedTick = Time.Tick;
					JustRotated = true;
					Rotation *= new Angles( 0f, -22.5f, 0f ).ToRotation();

				}

				if ( Input.VR.LeftHand.ButtonA.WasPressed && !JustRotated )
				{
					RotatedTick = Time.Tick;
					JustRotated = true;
					Rotation *= new Angles( 0f, 22.5f, 0f ).ToRotation();
				}

				if ( !Input.VR.RightHand.ButtonA.WasPressed && !Input.VR.LeftHand.ButtonA.WasPressed && JustRotated )
				{
					RotatedTick = 0;
					JustRotated = false;
				}
			}
		}

		public override void FrameSimulate( Client cl )
		{
			base.FrameSimulate( cl );

			if ( cl == Client )
			{
				if ( cl.Pawn is Pawn pawn && !cage.IsValid() )
				{
					VR.Anchor = Transform;
					Transform = pawn.Transform.WithRotation( Rotation );
				}
				else if ( cl.Pawn is Pawn pawn2 && cage.IsValid() )
				{
					VR.Anchor = Transform;
					Transform = cage.Transform.WithRotation( Rotation );
				}
			}
		}

		public override void BuildInput()
		{
			base.BuildInput();
			HandleHands();
		}

		public void HandleHands()
		{
			LH.HandleHand();
			RH.HandleHand();
			HeadEnt.HandleHead();
		}
	}
}
