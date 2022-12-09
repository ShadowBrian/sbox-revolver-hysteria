using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.UI;
using Sandbox.UI.Construct;
using Sandbox;
using Editor;

namespace rh
{
	[HammerEntity]
	[Library( "ent_rh_worldscore", Description = "World Score Display" )]
	[Model( Model = "models/scoreboard.vmdl" )]
	partial class RHWorldBoard : ModelEntity
	{

		[Property]
		public int PlayerCount { get; set; } = 0;

		[Net, Predicted]
		RHScoreboard board { get; set; }

		public override void Spawn()
		{
			SetModel( "models/scoreboard.vmdl" );
			base.Spawn();
		}

		public override void ClientSpawn()
		{
			board = new RHScoreboard();
			board.NumberToDisplay = PlayerCount;
			board.Position = Position + Rotation.Forward * (15f * Scale);
			board.Rotation = Rotation;
			board.WorldScale = 7.75f * Scale;
			board.followEnt = this;

			base.ClientSpawn();
		}
	}
}
