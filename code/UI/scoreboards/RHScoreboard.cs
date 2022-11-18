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
		Dictionary<LeaderboardEntry, RHScoreboardEntry> Rows = new();

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

		public async void WaitingForScores()
		{
			/*if ( NumberToDisplay != 0 )
			{
				NameLabel.Text = NumberToDisplay + " players";
				results = await GameServices.Leaderboard.Query( ident: Global.GameIdent, bucket: Global.MapName + "_" + NumberToDisplay + "players" );
			}
			else
			{
				NameLabel.Text = VRPlayerCount + " players";

				results = await GameServices.Leaderboard.Query( ident: Global.GameIdent, bucket: Global.MapName + "_" + VRPlayerCount + "players" );
			}*/

			NameLabel.Text = (NumberToDisplay != 0 ? NumberToDisplay : VRPlayerCount) + " players";

			Leaderboard? board = await Leaderboard.Find( Global.MapName + "_" + (NumberToDisplay != 0 ? NumberToDisplay : VRPlayerCount) + "players" );

			if ( board.HasValue )
			{
				LeaderboardEntry[]? entries = await board.Value.GetGlobalScores( 25 );

				LeaderboardEntry[]? friendentries = await board.Value.GetFriendScores();

				List<long> friendIDs = new List<long>();

				if(friendentries.Length > 0 )
				{
					foreach ( var friendtry in friendentries )
					{
						friendIDs.Add( friendtry.PlayerId );
					}
				}

				if ( entries.Length > 0 )
				{
					foreach ( LeaderboardEntry boardentry in entries )
					{
						var entry = AddClient( boardentry, boardentry.GlobalRank );
						Rows[boardentry] = entry;
						Rows[boardentry].Ranking = boardentry.GlobalRank;
						entry.FriendEntry = friendIDs.Contains( boardentry.PlayerId );
						//Log.Trace( ranking + "." + client.DisplayName + " " + client.Rating );
					}
				}
			}

			GotScores = true;
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

			if ( NumberToDisplay == 0 && VRPlayerCount != (Game.Current as RevolverHysteriaGame).VRPlayers.Count && VRPlayerCount != 1 )
			{
				VRPlayerCount = (Game.Current as RevolverHysteriaGame).VRPlayers.Count;
				ClearBoard();
			}

			if ( VRPlayerCount == 0 )
			{
				VRPlayerCount = 1;
				ClearBoard();
			}
		}

		protected virtual RHScoreboardEntry AddClient( LeaderboardEntry entry, int ranking )
		{
			var p = Canvas.AddChild<RHScoreboardEntry>();
			p.Ranking = ranking;
			p.assignedResult = entry;
			p.UpdateData();
			return p;
		}
	}
}
