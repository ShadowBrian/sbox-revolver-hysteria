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
	public partial class VRHand : AnimatedEntity
	{

		[Net] public HandSide hand { get; set; }

		[Net] public float ThumbClamp { get; set; } = 1f;
		[Net] public float IndexClamp { get; set; } = 1f;
		[Net] public float MiddleClamp { get; set; } = 1f;
		[Net] public float RingClamp { get; set; } = 1f;

		[Net] public bool Initialized { get; set; }

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

		public void HandleHand()
		{
			if ( hand == HandSide.None )
			{
				return;
			}
			else if ( !Initialized )
			{
				switch ( hand )
				{
					case HandSide.Left:
						SetModel( "models/revolver/revolver.vmdl" );
						break;
					case HandSide.Right:
						SetModel( "models/revolver/revolver.vmdl" );
						break;
					default:
						break;
				}

				Initialized = true;
			}

			Input.VrHand vrhand = Input.VR.LeftHand;

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

			Transform = vrhand.Transform;//.WithScale( 0.8f );

			Rotation = vrhand.Transform.Rotation * new Angles( 45, 0, 0 ).ToRotation();

			Position = vrhand.Transform.Position - vrhand.Transform.Rotation.Forward * 5f;

			/*if ( Input.VR.IsKnuckles || Input.VR.IsRift )
			{
				SetAnimParameter( "Thumb", MathX.Clamp( vrhand.GetFingerValue( FingerValue.ThumbCurl ), 0f, ThumbClamp ) );
				SetAnimParameter( "Index", MathX.Clamp( vrhand.GetFingerValue( FingerValue.IndexCurl ), 0f, IndexClamp ) );
				SetAnimParameter( "Middle", MathX.Clamp( vrhand.GetFingerValue( FingerValue.MiddleCurl ), 0f, MiddleClamp ) );
				SetAnimParameter( "Ring", MathX.Clamp( vrhand.GetFingerValue( FingerValue.RingCurl ), 0f, RingClamp ) );
			}
			else
			{
				SetAnimParameter( "Thumb", MathX.Clamp( (vrhand.ButtonA.IsPressed || vrhand.ButtonB.IsPressed) ? 1f : 0f, 0f, ThumbClamp ) );
				SetAnimParameter( "Index", MathX.Clamp( vrhand.Trigger.Value, 0f, IndexClamp ) );
				SetAnimParameter( "Middle", MathX.Clamp( vrhand.Grip.Value, 0f, MiddleClamp ) );
				SetAnimParameter( "Ring", MathX.Clamp( vrhand.Grip.Value, 0f, RingClamp ) );
			}*/
		}
	}
}
