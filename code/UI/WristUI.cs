using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace rh
{
	public partial class WristUI : WorldPanel
	{
		public ModelEntity Wristwatch;

		bool initialized;

		Label ScoreLabel;

		bool ScoreOnly;

		public WristUI()
		{
			StyleSheet.Load( "UI/WristUI.scss" );

			PanelBounds = new Rect( -70, -33, 1280, 720 );

		}

		public WristUI(bool IsScore )
		{
			ScoreOnly = IsScore;
			StyleSheet.Load( "UI/WristUI.scss" );

			PanelBounds = new Rect( -70, -33, 1280, 720 );
		}

		public override void Tick()
		{
			if ( Wristwatch != null )
			{
				Transform = Wristwatch.GetAttachment( "UIPanel" ).Value.WithScale(0.04f);
				

				if( ScoreOnly )
				{
					Position -= Transform.Rotation.Left * 1.25f;
					Position += Transform.Rotation.Up * 0.2f;
				}
				else
				{
					Position -= Transform.Rotation.Left * 1.25f;
					Position += Transform.Rotation.Up * 0.65f;
				}

				if(ScoreLabel != null )
				{
					ScoreLabel.Text = Local.Client.GetInt( "score" ) + "";
				}

				if ( !initialized )
				{
					if ( !ScoreOnly )
					{
						AddChild<HealthMeter>();
					}
					else
					{
						ScoreLabel = Add.Label( "Score", "Title" );
					}

					initialized = true;
				}
			}
		}
	}
}
