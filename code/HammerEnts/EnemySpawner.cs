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

		[Property( Name = "spawndelay" )]
		public float spawndelay { get; set; } = 0;

		[Property( Name = "UseSpawnDelayEveryTime" )]
		public bool UseSpawnDelayEveryTime { get; set; }

		[Net] int EnemiesSpawned { get; set; }

		PlayerPlatform platform;

		[Net] BaseEnemyClass ActiveNPC { get; set; }

		[Property( Name = "enemytype" ), ResourceType( "rhenmy" )]
		public string enemytype { get; set; }

		public List<string> AllEnemies = new List<string>();

		bool InputSpawnedEnemy = false;

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

		[Net] public bool DirectTargetMode { get; set; } = false;

		bool StartedSpawnDelayTimer;

		TimeSince TimeSinceSpawnDelayStart;

		[Event.Tick.Server]
		public void Tick()
		{
			if ( platform == null )
			{
				return;
			}

			if ( ((platform.GameHasStarted && platform.currentnode == AssociatedNodeNumber) || (InputSpawnedEnemy)) && !(Game.Current as RevolverHysteriaGame).EndTriggered )
			{
				if ( spawndelay > 0 )
				{
					if ( !StartedSpawnDelayTimer )
					{
						TimeSinceSpawnDelayStart = 0f;
						StartedSpawnDelayTimer = true;
					}
					if ( TimeSinceSpawnDelayStart < spawndelay )
					{
						return;
					}
				}

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
						if ( Children.Count > 1 )
						{
							ActiveNPC.TargetDestination = Children[Rand.Int( 0, Children.Count - 1 )].Position;
						}
						else
						{
							ActiveNPC.TargetDestination = Children[0].Position;
						}
					}
					if ( UseSpawnDelayEveryTime )
					{
						TimeSinceSpawnDelayStart = 0;
						StartedSpawnDelayTimer = false;
					}

					ActiveNPC.DirectTargetMode = DirectTargetMode;
				}
			}

			if ( ActiveNPC != null && (Game.Current as RevolverHysteriaGame).EndTriggered )
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

		[Input]
		public void StopSpawning()
		{
			InputSpawnedEnemy = false;
		}

		[Input]
		public void SpawnEnemy()
		{
			if ( (Game.Current as RevolverHysteriaGame).EndTriggered )
			{
				return;
			}
			if ( spawndelay > 0 )
			{
				SpawnDelayedEnemyOutput();
				return;
			}

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
				if ( UseSpawnDelayEveryTime )
				{
					TimeSinceSpawnDelayStart = 0;
					StartedSpawnDelayTimer = false;
				}
				InputSpawnedEnemy = true;
			}
		}

		public async Task SpawnDelayedEnemyOutput()
		{
			await Task.DelaySeconds( spawndelay );
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
				if ( UseSpawnDelayEveryTime )
				{
					TimeSinceSpawnDelayStart = 0;
					StartedSpawnDelayTimer = false;
				}
				InputSpawnedEnemy = true;
			}

		}
	}
}
