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
	public partial class HealthMeter : Panel
	{
		public int index = 0;

		public Pawn player;

		Image heart;

		int MaxRange, MinRange;

		string[] heartimages = new string[6] { "ui/vitals/hp_5.png", "ui/vitals/hp_4.png", "ui/vitals/hp_3.png", "ui/vitals/hp_2.png", "ui/vitals/hp_1.png", "ui/vitals/hp_0.png" };

		public HealthMeter()
		{
			heart = Add.Image( "ui/vitals/hp_5.png" );
			Add.Label( "\n" );
		}

		public override void Tick()
		{
			base.Tick();

			MaxRange = (index + 1) * 4;
			MinRange = ((index + 1) * 4) - 4;

			if ( player.IsValid() )
			{
				int hp = (GameManager.Current as RevolverHysteriaGame).VRPlayers[player.PlayerIndex - 1].HeadEnt.HitPoints;

				heart.SetTexture( "ui/vitals/hp_" + ((int)MathX.Clamp( hp, 0, 5 )) + ".png" );//heartimages[(int)MathX.Clamp(MathF.Abs( hp - 5 ),0,4)] );

			}
			else
			{
				if ( Local.Pawn is not Pawn playerref )
					return;

				player = playerref;
			}
		}
	}
}
