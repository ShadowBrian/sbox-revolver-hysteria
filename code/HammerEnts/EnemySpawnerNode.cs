using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using SandboxEditor;

namespace rh
{
	[Library( "ent_rh_enemyspawner_node" )]
	[HammerEntity]
	[EditorModel( "models/arrow.vmdl" )]
	public partial class EnemySpawnerNode : Entity
	{
		[Property]
		public string Howto { get; set; } = "Parent This To Enemy Spawner";
	}
}
