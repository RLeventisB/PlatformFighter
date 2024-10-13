namespace PlatformFighter.Entities
{
	public class CharacterData
	{
		public CharacterDefinition Definition { get; set; }

		public void SetDefinition(ushort characterDefinitionId)
		{
			Definition = CharacterDefinitions.CreateInstance(characterDefinitionId);
		}

		public void ApplyDefaults(Player player)
		{
			player.MovableObject.Size = Definition.CollisionSize;
		}
	}
}