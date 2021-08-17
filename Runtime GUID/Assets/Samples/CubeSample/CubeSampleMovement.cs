using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace CubeSample
{
    [GenerateAuthoringComponent, MaterialProperty("Color_4b060f9a9be842759f085512047de9ee", MaterialPropertyFormat.Float4)]
    public struct CubeSampleMovementData : IComponentData
    {
        [GhostField]
        public float4 color;
    }

    public struct PlayerInput : ICommandData
    {
        public uint Tick { get; set; }
        public int horizontal;
        public int vertical;
    }

    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class CubeSampleInputSystem : SystemBase
    {
        private ClientSimulationSystemGroup clientSimulationSystemGroup;
        private BufferFromEntity<PlayerInput> playerInputGroup;
        private EntityQuery thinClientQuery;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<NetworkIdComponent>();
            clientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();
            RequireSingletonForUpdate<EnableCubeSample>();
            thinClientQuery = GetEntityQuery(typeof(ThinClientComponent));
        }

        protected override void OnUpdate()
        {
            // Is there a better way to not run a system when a component (or singleton) exists?
            if (!thinClientQuery.IsEmpty)
                return;
            
            Entity localInput = GetSingleton<CommandTargetComponent>().targetEntity;

            if (localInput == Entity.Null)
            {
                int localPlayerId = GetSingleton<NetworkIdComponent>().Value;
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                Entity commandTargetEntity = GetSingletonEntity<CommandTargetComponent>();
                Entities.WithNone<PlayerInput>().ForEach(
                    (Entity entity, ref GhostOwnerComponent ghostOwner, in CubeSampleMovementData movementData) =>
                    {
                        if (ghostOwner.NetworkId == localPlayerId)
                        {
                            ecb.AddBuffer<PlayerInput>(entity);
                            ecb.SetComponent(commandTargetEntity, new CommandTargetComponent {targetEntity = entity});
                            //ecb.AddComponent(entity, new MaterialColor{Value = movementData.color});
                        }
                    }).Run();
                ecb.Playback(EntityManager);
                return;
            }

            playerInputGroup = GetBufferFromEntity<PlayerInput>(false);
            if (!playerInputGroup.HasComponent(localInput))
                return;

            PlayerInput input = default;
            input.Tick = clientSimulationSystemGroup.ServerTick;
            
            if (Input.GetKey("a"))
                input.horizontal -= 1;
            if (Input.GetKey("d"))
                input.horizontal += 1;
            if (Input.GetKey("s"))
                input.vertical -= 1;
            if (Input.GetKey("w"))
                input.vertical += 1;

            var inputBuffer = EntityManager.GetBuffer<PlayerInput>(localInput);
            inputBuffer.AddCommandData(input);
        }
    }

    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    public class CubeSampleMovementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var group = World.GetExistingSystem<GhostPredictionSystemGroup>();
            var tick = group.PredictingTick;
            var deltaTime = Time.DeltaTime;

            Entities.ForEach((DynamicBuffer<PlayerInput> inputBuffer, ref Translation translation,
                ref PredictedGhostComponent prediction) =>
            {
                if (!GhostPredictionSystemGroup.ShouldPredict(tick, prediction))
                    return;

                inputBuffer.GetDataAtTick(tick, out var input);
                if (input.horizontal > 0)
                    translation.Value.x += deltaTime;
                if (input.horizontal < 0)
                    translation.Value.x -= deltaTime;
                if (input.vertical > 0)
                    translation.Value.z += deltaTime;
                if (input.vertical < 0)
                    translation.Value.z -= deltaTime;
            }).Run();
        }
    }
}