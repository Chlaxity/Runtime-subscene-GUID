using Unity.Entities;

namespace CubeSample
{
    [GenerateAuthoringComponent]
    public struct CubePrefab : IComponentData
    {
        public Entity Prefab;
    }    
}