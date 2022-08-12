using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using SandboxEditor;

namespace rh
{
	[Library( "ent_rh_enemyspawner" )]
	[HammerEntity]
	[Model( Model = "models/citizen/citizen.vmdl" )]
	public partial class EnemySpawner : Entity
	{
		[Property( Name = "associatednodeindex" )]
		public int AssociatedNodeNumber { get; set; } = 0;

		[Property( Name = "walkpoint" )]
		public Vector3 walkpoint { get; set; }

		[Property( Name = "spawnlimit" )]
		public int spawnlimit { get; set; } = 0;

		[Net] int EnemiesSpawned { get; set; }

		PlayerPlatform platform;

		[Net] BaseEnemyClass ActiveNPC { get; set; }

		[Property( Name = "enemytype" ), ResourceType( "rhenmy" )]
		public string enemytype { get; set; }

		public List<string> AllEnemies = new List<string>();

		public override void Spawn()
		{
			platform = All.OfType<PlayerPlatform>().FirstOrDefault();

			foreach ( var file in ResourceLibrary.GetAll<EnemyResource>() )
			{
				AllEnemies.Add( file.ResourcePath );
				if ( file.Rarity == SpawnRarity.Common )
				{
					AllEnemies.Add( file.ResourcePath );
					AllEnemies.Add( file.ResourcePath );
					AllEnemies.Add( file.ResourcePath );
					AllEnemies.Add( file.ResourcePath );
				}

				if ( file.Rarity == SpawnRarity.Rare )
				{
					AllEnemies.Add( file.ResourcePath );
				}

				if ( file.Rarity == SpawnRarity.Never )
				{
					AllEnemies.Remove( file.ResourcePath );
				}

			}
		}

		[Event.Tick.Server]
		public void Tick()
		{
			if(platform == null )
			{
				return;
			}
			if ( platform.GameHasStarted && platform.currentnode == AssociatedNodeNumber && !(Game.Current as RevolverHysteriaGame).EndTriggered )
			{
				if ( !ActiveNPC.IsValid() && AllEnemies.Count > 0 && (spawnlimit == 0 || EnemiesSpawned < spawnlimit) )
				{
					if ( enemytype == null )
					{
						ActiveNPC = BaseEnemyClass.FromPath( Rand.FromList( AllEnemies ) );
					}
					else
					{
						ActiveNPC = BaseEnemyClass.FromPath( enemytype );
					}
					EnemiesSpawned++;
					ActiveNPC.Position = Position + Vector3.Up;
					if ( Children.Count == 0 )
					{
						ActiveNPC.TargetDestination = walkpoint;
					}
					else
					{
						ActiveNPC.TargetDestination = Children[0].Position;
					}

				}
			}

			if ( ActiveNPC != null && (platform.currentnode - AssociatedNodeNumber) > 2 || ((Game.Current as RevolverHysteriaGame).EndTriggered && ActiveNPC != null) )
			{
				for ( int i = 0; i < ActiveNPC.Children.Count; i++ )
				{
					if ( i < ActiveNPC.Children.Count )
					{
						ActiveNPC.Children[i].Delete();
					}
				}
				ActiveNPC.Delete();
			}
		}
	}
}
