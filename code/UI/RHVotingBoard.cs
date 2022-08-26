using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.UI;
using Sandbox.UI.Construct;
using Sandbox;
using SandboxEditor;

namespace rh
{
	public partial class RHVotingBoard : ModelEntity
	{
		[Net] public Dictionary<string, int> MapChoices { get; set; } = new Dictionary<string, int>();
		[Net] List<string> allmaps { get; set; } = new List<string>();

		public override void Spawn()
		{
			//SetModel( "models/player/mapvote_panel.vmdl" );
			base.Spawn();
			Transform = Entity.All.OfType<PlayerPlatform>().FirstOrDefault().Transform;

			SetParent( Entity.All.OfType<PlayerPlatform>().FirstOrDefault() );

			allmaps = RevolverHysteriaGame.GetMaps();



			for ( int i = 0; i < allmaps.Count; i++ )
			{
				MapChoices.Add( allmaps[i], 0 );
				RHVotingChoice choice = new RHVotingChoice();
				if ( allmaps.Count > 1 )
				{
					choice.Position = Position + (Rotation.Left * 50f * (i - ((allmaps.Count - 1) / 2f)));
				}
				else
				{
					choice.Position = Position;
				}
				choice.Rotation = Rotation;

				choice.AssociatedMap = allmaps[i];

				choice.boardref = this;
				choice.SetParent( this );
			}
		}

		public string ReturnMostVotedMap()
		{
			int MostVotes = 0;
			string votedmap = "";
			foreach ( var choice in MapChoices )
			{
				if ( choice.Value > MostVotes )
				{
					MostVotes = choice.Value;
					votedmap = choice.Key;
				}
			}
			if ( votedmap != "" )
			{
				return votedmap;
			}
			else
			{
				return allmaps[Rand.Int( 0, allmaps.Count - 1 )];
			}
		}

		TimeSince timesincespawned;

		[Event.Tick.Client]
		public void Tick()
		{
			foreach ( var item in Entity.All.OfType<RHVotingChoice>() )
			{
				item.panel.associatedboard = this;
			}
		}

		public int SubmitVote( string mapname )
		{
			if ( MapChoices.ContainsKey( mapname ) )
			{
				MapChoices[mapname]++;
				Log.Trace( "Vote in for " + mapname + "now at " + MapChoices[mapname] + " votes" );
				return MapChoices[mapname];
			}
			else
			{
				Log.Trace( "Somehow triggered non-existent map!" );
				return 0;
			}
		}
	}
}
