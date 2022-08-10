using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace rh
{
	[GameResource( "Revolver Hysteria Enemy", "rhenmy", "A resource that holds an enemy's clothing and type" )]
	public class EnemyResource : GameResource
	{
		[Category( "Setup" ), Description( "The type/behavior of the enemy" )]
		public EnemyType Type { get; set; }

		[Category( "Setup" ), Description( "The movement type of the enemy" )]
		public EnemyMovementType MovementType { get; set; }

		[Category( "Setup" ), Description( "The rarity of the enemy" )]
		public SpawnRarity Rarity { get; set; }

		[Category( "Setup" ), Description( "The weapon this enemy holds (unarmed = innocent)" )]
		public EnemyWeapon WeaponType { get; set; }

		[Category( "Clothes" ), ResourceType( "clothing" ), Description( "List of clothes they should wear" )]
		public List<string> Clothing { get; set; }

		/*[Category( "Setup" ), Description( "Body groups to set" )]
		public Dictionary<string,int> Bodygroups { get; set; } = new Dictionary<string, int> { { "Head", 0 }, { "Chest", 0 }, { "Legs", 0 }, { "Hands", 0 }, { "Feet", 0 } };*/
	}
}
