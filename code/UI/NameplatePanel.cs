﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.UI;
using Sandbox;
using Sandbox.UI.Construct;

namespace rh
{
	/*public static class StringExt
	{
		public static string Truncate( this string value, int maxLength )
		{
			if ( string.IsNullOrEmpty( value ) ) return value;
			return value.Length <= maxLength ? value : value.Substring( 0, maxLength );
		}
	}*/

	public partial class NameplatePanel : WorldPanel
	{
		Label PlayernameLabel;

		public int PlayerIndex;

		public NameplatePanel()
		{
			StyleSheet.Load( "UI/NameplatePanel.scss" );
		}

		public override void Tick()
		{
			base.Tick();
			if ( PlayernameLabel == null )
			{
				PlayernameLabel = Add.Label( "", "Title" );
			}

			if ( PlayerIndex <= (Game.Current as RevolverHysteriaGame).VRPlayers.Count - 1 )
			{
				if ( (Game.Current as RevolverHysteriaGame).VRPlayers[PlayerIndex].HeadEnt.IsValid() && (Game.Current as RevolverHysteriaGame).VRPlayers[PlayerIndex].HeadEnt.HitPoints <= 0 )
				{
					PlayernameLabel.SetClass( ".TitleDead", true );
					PlayernameLabel.SetClass( ".Title", false );
				}
				else
				{
					PlayernameLabel.SetClass( ".TitleDead", false );
					PlayernameLabel.SetClass( ".Title", true );
				}

				PlayernameLabel.Text = (Game.Current as RevolverHysteriaGame).VRPlayers[PlayerIndex].Client.Name.ToLower().Truncate( 12 );
				Position = (Game.Current as RevolverHysteriaGame).platform.GetAttachment( "name" + (PlayerIndex + 1) ).Value.Position - Rotation.Up * 19f;
				Rotation = (Game.Current as RevolverHysteriaGame).platform.GetAttachment( "name" + (PlayerIndex + 1) ).Value.Rotation * new Angles( 0, 180, 0 ).ToRotation();
				Scale = 0.33f;
				PlayernameLabel.Text += "\n" + (((PlayerIndex + 4) <= (Game.Current as RevolverHysteriaGame).VRPlayers.Count - 1) ? (Game.Current as RevolverHysteriaGame).VRPlayers[PlayerIndex].Client.Name.ToLower().Truncate( 12 ) : "");
				PlayernameLabel.Text += "\n" + (Game.Current as RevolverHysteriaGame).VRPlayers[PlayerIndex].Client.GetInt( "score" );
			}
			else
			{
				PlayernameLabel.Text = "\nno user";
				Position = (Game.Current as RevolverHysteriaGame).platform.GetAttachment( "name" + (PlayerIndex + 1) ).Value.Position - Rotation.Up * 19f;
				Rotation = (Game.Current as RevolverHysteriaGame).platform.GetAttachment( "name" + (PlayerIndex + 1) ).Value.Rotation * new Angles( 0, 180, 0 ).ToRotation();
				Scale = 0.33f;
			}
		}
	}
}
