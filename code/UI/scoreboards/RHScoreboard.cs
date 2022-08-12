using Sandbox;
using Sandbox.Hooks;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rh
{
	public partial class RHScoreboard : WorldPanel
	{
		Dictionary<LeaderboardResult.Entry, RHScoreboardEntry> Rows = new();

		Panel Canvas, Header;
		
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

		bool AddedPing;

		protected virtual void AddHeader()
		{
			Header = Add.Panel( "header" );
			Header.Add.Label( "name", "name" );
			Header.Add.Label( "points", "points" );
		}
		
		LeaderboardResult results;
		
		bool GotScores;
		
		public async Task WaitingForScores()
		{
			results = await GameServices.Leaderboard.Query( ident: Global.GameIdent, bucket: Global.MapName );
			
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

			/*if ( results.Entries.Count > 0 )
			{
				TimeSpan t = TimeSpan.FromSeconds( float.Parse( results.Entries.First().Rating.ToString() ) );

				string answer = string.Format( "{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
								t.Hours,
								t.Minutes,
								t.Seconds,
								t.Milliseconds );

				BestLabel.Text = "Global Best: " + answer;
			}
			else
			{
				BestLabel.Text = "No best global score yet.";
			}

			results = await GameServices.Leaderboard.Query( ident: Global.GameIdent, bucket: Global.MapName, playerid: Local.PlayerId );

			if ( results.Entries.Count > 0 )
			{
				TimeSpan t = TimeSpan.FromSeconds( float.Parse( results.Entries.First().Rating.ToString() ) );

				string answer = string.Format( "{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
								t.Hours,
								t.Minutes,
								t.Seconds,
								t.Milliseconds );

				BestLabel.Text += "\nPersonal Best: " + answer;
			}
			else
			{
				BestLabel.Text += "\nNo personal best score yet.";
			}*/
		}

		public override void Tick()
		{
			base.Tick();



			/*foreach ( var client in Rows.Keys.Except( Client.All ) )
			{
				if ( Rows.TryGetValue( client, out var row ) )
				{
					row?.Delete();
					Rows.Remove( client );
				}
			}*/

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
