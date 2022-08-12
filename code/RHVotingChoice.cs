﻿using System;
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
	partial class RHVotingChoice : ModelEntity
	{
		public RHVotingBoard boardref;

		[Net] public string AssociatedMap { get; set; } = "";

		public RHMapVotePanel panel;

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/player/mapvote_panel.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			Tags.Add( "solid" );
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			panel = RHMapVotePanel.FromPackage(AssociatedMap, GetAttachment( "panel" ).Value.Position, GetAttachment( "panel" ).Value.Rotation * new Angles( 0, 180, 0 ).ToRotation() );
		}

		[Net] string VoteCount { get; set; } = "0";

		[Event.Tick.Client]
		public void ClientTick()
		{
			if ( panel != null )
			{
				panel.VoteCount.label.Text = VoteCount;
			}
		}

		public override void TakeDamage( DamageInfo info )
		{
			base.TakeDamage( info );

			VoteCount = boardref.SubmitVote( AssociatedMap ).ToString();
		}
	}

	public partial class WorldLabel : WorldPanel
	{
		public Label label;
		public WorldLabel()
		{
			StyleSheet.Load( "UI/WristUI.scss" );
			label = Add.Label( "", "Title2" );
		}

		public WorldLabel(string text, Transform trans)
		{
			StyleSheet.Load( "UI/WristUI.scss" );
			label = Add.Label( text.ToLower(), "Title2" );			
		}

		public override void Tick()
		{
			PanelBounds = new Rect( -500f, -250f, 1000f, 500f );
			Scale = 0.5f;
		}

	}

	public partial class RHMapVotePanel : WorldPanel
	{
		public RHVotingBoard associatedboard;

		public string mappackage;

		public WorldLabel VoteCount;

		public RHMapVotePanel()
		{

		}

		public static RHMapVotePanel FromPackage( string packageName, Vector3 pos, Rotation rot )
		{
			var packageTask = Package.Fetch( packageName, true ).ContinueWith( t =>
			{
				var package = t.Result;
				return new RHMapVotePanel( package.Title, package.Thumb );
			} );

			packageTask.Result.Position = pos;
			packageTask.Result.Rotation = rot;

			return packageTask.Result;
		}

		WorldLabel label1;

		WorldLabel label2;



		public RHMapVotePanel( string mapName, string backgroundImage )
		{

			VoteCount = new WorldLabel();

			label1 = new WorldLabel( "VOTES", new Transform(Position - Vector3.Up * 30f,Rotation) );

			label2 = new WorldLabel( mapName, new Transform( Position + Vector3.Up * 25f, Rotation ) );

			Style.BackgroundImage = Texture.Load( backgroundImage );

			Style.BackgroundRepeat = BackgroundRepeat.NoRepeat;

			Style.BackgroundSizeX = 300f;
			Style.BackgroundSizeY = 300f;

			PanelBounds = new Rect( -300f, -275f, 1000f, 1000f );

			Scale = 2f;
		}

		public override void Tick()
		{
			base.Tick();
			/*if ( associatedboard != null )
			{
				VoteCount.label.Text = associatedboard.MapChoices[mappackage].ToString().ToLower();
			}*/

			//associatedboard = Entity.All.OfType<RHVotingBoard>().FirstOrDefault();
			


			VoteCount.Transform = new Transform( Position - Vector3.Up * 35.5f, Rotation );
			label1.Transform = new Transform( Position - Vector3.Up * 30.5f, Rotation );
			label2.Transform = new Transform( Position + Vector3.Up * 8.5f + Rotation.Forward * 1f, Rotation ).WithScale( 0.75f );

			

		}
	}
}
