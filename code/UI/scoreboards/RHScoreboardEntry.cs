
using Sandbox;
using Sandbox.Hooks;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;

namespace rh
{
	public partial class RHScoreboardEntry : Panel
	{

		public LeaderboardEntry assignedResult;

		public int Ranking = 0;

		public Label PlayerName;
		public Label Kills;

		public RHScoreboardEntry()
		{
			AddClass( "entry" );

			PlayerName = Add.Label( "PlayerName", "name" );
			Kills = Add.Label( "", "points" );
		}

		RealTimeSince TimeSinceUpdate = 0;

		public override void Tick()
		{
			base.Tick();

			/*if ( !IsVisible )
				return;

			if ( TimeSinceUpdate < 0.1f )
				return;

			TimeSinceUpdate = 0;
			UpdateData();*/
		}

		public virtual void UpdateData()
		{
			/*if( !ScoreManager.Scores.ContainsKey(assignedClient) )
			{
				//Delete();
				return;
			}*/

			PlayerName.Text = Ranking + "." + assignedResult.Name.ToLower().Truncate(18);
			Kills.Text = assignedResult.Score + "";// .GetInt( "score" ).ToString();
			SetClass( "me", assignedResult.PlayerId == Local.Client.PlayerId );

			//if(assignedClient == Local.Client)
			//Log.Trace( ScoreManager.KillPointLevel + (ScoreManager.Scores[assignedClient] + "is less than" + ScoreManager.KillPointLevel).ToString() );
		}

		public virtual void UpdateFrom( LeaderboardEntry client )
		{
			assignedResult = client;
			UpdateData();
		}

	}
}
