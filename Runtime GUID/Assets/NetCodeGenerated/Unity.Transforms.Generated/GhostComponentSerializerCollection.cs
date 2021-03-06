//THIS FILE IS AUTOGENERATED BY GHOSTCOMPILER. DON'T MODIFY OR ALTER.
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms.Generated;

namespace Unity.Transforms.Generated
{
    [UpdateInGroup(typeof(ClientAndServerInitializationSystemGroup))]
    public class GhostComponentSerializerRegistrationSystem : SystemBase
    {
        protected override void OnCreate()
        {
            var ghostCollectionSystem = World.GetOrCreateSystem<GhostCollectionSystem>();
            ghostCollectionSystem.AddSerializer(UnityTransformsTranslationGhostComponentSerializer.State);
            ghostCollectionSystem.AddSerializer(UnityTransformsRotationGhostComponentSerializer.State);
        }

        protected override void OnUpdate()
        {
            var parentGroup = World.GetExistingSystem<InitializationSystemGroup>();
            if (parentGroup != null)
            {
                parentGroup.RemoveSystemFromUpdateList(this);
            }
        }
    }
}