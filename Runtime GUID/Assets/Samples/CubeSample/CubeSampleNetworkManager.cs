using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = Unity.Mathematics.Random;

namespace CubeSample
{
    public struct GoInGameRequest : IRpcCommand
    {
        public float4 color;
    }

    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class GoInGameClientSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableCubeSample>();
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            uint seed = (uint)System.DateTime.Now.Millisecond;
            Entities.WithStructuralChanges().WithNone<NetworkStreamInGame>().ForEach((Entity entity, ref NetworkIdComponent id) =>
            {
                Debug.Log("ClientGame");
                ecb.AddComponent<NetworkStreamInGame>(entity);
                var req = EntityManager.CreateEntity();
                Random r = Random.CreateFromIndex(seed);
                float4 color = new float4(0, 0, 0, 1);
                color.xyz = r.NextFloat3();
                ecb.AddComponent(req, new GoInGameRequest{color = color});
                ecb.AddComponent(req, new SendRpcCommandRequestComponent { TargetConnection = entity });
            }).Run();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }

    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public class GoInGameServerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<CubePrefab>();
            RequireSingletonForUpdate<EnableCubeSample>();
        }

        protected override void OnUpdate()
        {
            Entity prefab = GetSingleton<CubePrefab>().Prefab;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            uint seed = (uint)System.DateTime.Now.Millisecond;
            
            Entities.WithStructuralChanges().WithNone<SendRpcCommandRequestComponent>().ForEach((Entity reqEnt, int entityInQueryIndex, ref GoInGameRequest req, ref ReceiveRpcCommandRequestComponent reqSrc) =>
                {
                    Debug.Log(prefab);
                    ecb.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);
                    int networkId = GetComponent<NetworkIdComponent>(reqSrc.SourceConnection).Value;
                    Debug.Log($"Server setting connection {networkId} to in game");

                    var player = ecb.Instantiate(prefab);
                    ecb.SetComponent(player, new GhostOwnerComponent{NetworkId = networkId});
                    ecb.AddBuffer<PlayerInput>(player);
                    ecb.SetComponent(reqSrc.SourceConnection, new CommandTargetComponent{targetEntity = player});

                    ecb.SetComponent(player, new CubeSampleMovementData{color = req.color});
                    ecb.DestroyEntity(reqEnt);
                }).Run();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
