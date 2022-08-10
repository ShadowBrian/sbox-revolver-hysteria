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

			foreach ( var file in FileSystem.Mounted.FindFile( "resources/", "*.rhenmy", true ) )
			{
				var asset = ResourceLibrary.Get<EnemyResource>( "resources/" + file );
				if ( asset == null )
					continue;
				AllEnemies.Add( file );
				if ( asset.Rarity == SpawnRarity.Common )
				{
					AllEnemies.Add( file );
					AllEnemies.Add( file );
					AllEnemies.Add( file );
					AllEnemies.Add( file );
				}

				if ( asset.Rarity == SpawnRarity.Rare )
				{
					AllEnemies.Add( file );
				}

				if ( asset.Rarity == SpawnRarity.Never )
				{
					AllEnemies.Remove( file );
				}
			}
		}

		[Event.Tick.Server]
		public void Tick()
		{
			if ( platform.GameHasStarted && platform.currentnode == AssociatedNodeNumber )
			{
				if ( ActiveNPC == null && AllEnemies.Count > 0 && (spawnlimit == 0 || EnemiesSpawned < spawnlimit) )
				{
					if ( enemytype == null )
					{
						ActiveNPC = BaseEnemyClass.FromPath( Rand.FromList( AllEnemies ) );
					}
					else
					{
						ActiveNPC = BaseEnemyClass.FromPath( enemytype.Replace( "resources/", "" ) );
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

			if ( ActiveNPC != null && (platform.currentnode - AssociatedNodeNumber) > 2 )
			{
				//ActiveNPC.Task.Expire();
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
