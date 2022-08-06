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

		[Net, Predicted] int RotatedTick { get; set; }
		[Net, Predicted] bool JustRotated { get; set; }

		public override void Spawn()
		{
			Predictable = true;
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			if ( Input.VR.IsActive )
			{

				if ( RH != null )
				{
					if ( cl.Pawn is Pawn pawn )
					{
						VR.Anchor = Transform;
						Transform = pawn.Transform.WithRotation( Rotation );
					}

					HandleHands();
				}
				else if ( IsServer )
				{
					LH = new VRHand();
					LH.Owner = Owner;
					LH.hand = HandSide.Left;
					RH = new VRHand();
					RH.Owner = Owner;
					RH.hand = HandSide.Right;
				}

				Vector3 RightJoy = Input.VR.RightHand.Joystick.Value;

				if ( RightJoy.x > 0.5f && !JustRotated && MathF.Abs( RotatedTick - Time.Tick ) > 240 )
				{
					RotatedTick = Time.Tick;
					JustRotated = true;
					Rotation *= new Angles( 0f, -45, 0f ).ToRotation();

				}

				if ( RightJoy.x < -0.5f && !JustRotated && MathF.Abs( RotatedTick - Time.Tick ) > 240 )
				{
					RotatedTick = Time.Tick;
					JustRotated = true;
					Rotation *= new Angles( 0f, 45, 0f ).ToRotation();
				}

				if ( RightJoy.x < 0.5f && RightJoy.x > -0.5f && JustRotated )
				{
					RotatedTick = 0;
					JustRotated = false;
				}
			}
		}
		public override void FrameSimulate( Client cl )
		{
			base.FrameSimulate( cl );
		}

		public void HandleHands()
		{
			LH.HandleHand();
			RH.HandleHand();
		}
	}
}
