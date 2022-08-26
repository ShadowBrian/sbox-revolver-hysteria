using Sandbox;
using Sandbox.Hooks;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace rh
{
	public partial class RHScoreboard : WorldPanel
	{
		Dictionary<LeaderboardResult.Entry, RHScoreboardEntry> Rows = new();

		public int NumberToDisplay = 0;

		Panel Canvas, Header;

		public Entity followEnt;

		public RHScoreboard()
		{
			PanelBounds = new Rect( -500f, -500f, 1000f, 1000f );
			StyleSheet.Load( "/UI/scoreboards/RHScoreboard.scss" );
			AddClass( "scoreboard" );

			AddHeader();

			Canvas = Add.Panel( "canvas" );

			SetClass( "open", true );

			WaitingForScores();
		}

		Label NameLabel;

		bool AddedPing;

		protected virtual void AddHeader()
		{
			Header = Add.Panel( "header" );
			NameLabel = Header.Add.Label( "name", "name" );
			Header.Add.Label( "points", "points" );
		}

		LeaderboardResult results;

		bool GotScores;

		public async Task WaitingForScores()
		{
			if ( NumberToDisplay != 0 )
			{
				NameLabel.Text = NumberToDisplay + " players";
				results = await GameServices.Leaderboard.Query( ident: Global.GameIdent, bucket: Global.MapName + "_" + NumberToDisplay + "players" );
			}
			else
			{
				NameLabel.Text = VRPlayerCount + " players";

				results = await GameServices.Leaderboard.Query( ident: Global.GameIdent, bucket: Global.MapName + "_" + VRPlayerCount + "players" );
			}

			GotScores = true;

			//Log.Trace( "Got scores!" );

			int ranking = 0;

			foreach ( var client in results.Entries )
			{
				ranking++;
				var entry = AddClient( client, ranking );
				Rows[client] = entry;
				Rows[client].Ranking = ranking;
				//Log.Trace( ranking + "." + client.DisplayName + " " + client.Rating );
			}

		}

		public void ClearBoard()
		{
			foreach ( var client in Rows.Keys )
			{
				if ( Rows.TryGetValue( client, out var row ) )
				{
					row?.Delete();
					Rows.Remove( client );
				}
			}

			WaitingForScores();
		}

		int VRPlayerCount = 0;

		public override void Tick()
		{
			base.Tick();

			if ( followEnt != null )
			{
				Position = followEnt.Position + followEnt.Rotation.Forward * (15f * followEnt.Scale);
				Rotation = followEnt.Rotation;
			}

			if(NumberToDisplay == 0 && VRPlayerCount != (Game.Current as RevolverHysteriaGame).VRPlayers.Count && VRPlayerCount != 1)
			{
				VRPlayerCount = (Game.Current as RevolverHysteriaGame).VRPlayers.Count;
				ClearBoard();
			}

			if(VRPlayerCount == 0 )
			{
				VRPlayerCount = 1;
				ClearBoard();
			}
		}

		protected virtual RHScoreboardEntry AddClient( LeaderboardResult.Entry entry, int ranking )
		{
			var p = Canvas.AddChild<RHScoreboardEntry>();
			p.Ranking = ranking;
			p.assignedResult = entry;
			p.UpdateData();
			return p;
		}
	}
}
