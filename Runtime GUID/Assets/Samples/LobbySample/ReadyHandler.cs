using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LobbySample
{
    public class ReadyHandler : MonoBehaviour
    {
        public void OnReadyPressed()
        {

            EntityManager em = NetcodeBootstrap.clientWorld.EntityManager;

            EntityQuery query = em.CreateEntityQuery(typeof(NetworkIdComponent));
            if (query.IsEmpty)
                return;
            var entities = query.ToEntityArray(Allocator.Temp);
            Entity connection = entities[0];
            Entity req = em.CreateEntity();
            em.AddComponentData(req, new SendRpcCommandRequestComponent {TargetConnection = connection});
            em.AddComponent<ReadyRPCClient>(req);
        }
    }
    
    public struct ReadyRPCClient : IRpcCommand {}
    
    public struct CheckReadyStateTag : IComponentData {}

    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public class ReadyServerSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        
        protected override void OnCreate()
        {
            ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer();
            
            Entities.WithNone<SendRpcCommandRequestComponent>().ForEach(
                (Entity reqEnt, ref ReadyRPCClient req, ref ReceiveRpcCommandRequestComponent reqSrc) =>
                {
                    var player = GetComponent<CommandTargetComponent>(reqSrc.SourceConnection).targetEntity;
                    var lobbyPlayer = GetComponent<LobbyScenePlayer>(player);
                    lobbyPlayer.ready = !lobbyPlayer.ready;
                    Debug.Log($"Player {lobbyPlayer.name} ready set to {lobbyPlayer.ready}");
                    ecb.SetComponent(player, lobbyPlayer);
                    
                    // Broadcast RPC
                    var broadcastRpc = ecb.CreateEntity();
                    ecb.AddComponent(broadcastRpc, new ReadyBroadcastRpc{readyState = lobbyPlayer.ready, playerId = lobbyPlayer.id});
                    ecb.AddComponent<SendRpcCommandRequestComponent>(broadcastRpc);

                    var readyCheck = ecb.CreateEntity();
                    ecb.AddComponent<CheckReadyStateTag>(readyCheck);
                    
                    ecb.DestroyEntity(reqEnt);
                }).Schedule();
            
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
    
    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public class CheckReadyStateSystem : SystemBase
    {
        private EntityQuery connectionQuery;
        private EntityQuery playerQuery;
        private EntityQuery readyQuery;

        protected override void OnCreate()
        {
            connectionQuery = GetEntityQuery(typeof(NetworkIdComponent));
            playerQuery = GetEntityQuery(typeof(LobbyScenePlayer));
            readyQuery = GetEntityQuery(typeof(CheckReadyStateTag));
        }

        protected override void OnUpdate()
        {
            if (readyQuery.IsEmpty)
                return;
            
            var players = playerQuery.ToComponentDataArray<LobbyScenePlayer>(Allocator.TempJob);
            var connections = connectionQuery.ToEntityArray(Allocator.TempJob);

            var sceneSystem = World.GetExistingSystem<SceneSystem>();
            
            EntityManager.DestroyEntity(readyQuery);
            
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].ready == false)
                {
                    connections.Dispose();
                    players.Dispose();
                    return;
                }
            }

            Debug.Log("Opening CubeSample scene");
            // If the machine is a host, then LoadScene will be run twice, and unload the subscene
            // Commenting this out works, only because we use host mode most of the time as a server, but
            // when we use server-only mode, this could break.
            //SceneManager.LoadScene("CubeSample");
#if UNITY_EDITOR
            var sceneGUID =
                new UnityEditor.GUID(
                    UnityEditor.AssetDatabase.AssetPathToGUID(
                        "Assets/Samples/CubeSample/SubScenes/CubeSampleSubScene.unity"));
#else
                var sceneGUID = new Unity.Entities.Hash128("6c53c57fae4026f4e95ccd7d39eb0572");
                
                //sceneSystem.GetSceneGUID("Assets/Samples/CubeSample/SubScenes/CubeSampleSubScene.unity");
#endif

            sceneSystem.LoadSceneAsync(sceneGUID, new SceneSystem.LoadParameters() { AutoLoad = true });

            var GoInGameRpc = EntityManager.CreateEntity(typeof(GoInGameRpc));
            EntityManager.AddComponent<SendRpcCommandRequestComponent>(GoInGameRpc);

            for (int i = 0; i < connections.Length; i++)
            {
                EntityManager.SetComponentData(connections[i], new CommandTargetComponent{targetEntity = Entity.Null});
            }

            connections.Dispose();
            players.Dispose();
        }
    }
    
    public struct GoInGameRpc : IRpcCommand {}
    
    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class GoInGameClientSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        
        protected override void OnCreate()
        {
            ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
            RequireSingletonForUpdate<EnableLobbySample>();
            RequireSingletonForUpdate<CommandTargetComponent>();
        }

        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer();
            var sceneSystem = World.GetExistingSystem<SceneSystem>();
            var connection = GetSingletonEntity<CommandTargetComponent>();
            bool isThinClient = HasSingleton<ThinClientComponent>();

            Entities.WithStructuralChanges().WithoutBurst().WithNone<SendRpcCommandRequestComponent>().ForEach(
                (Entity reqEnt, ref GoInGameRpc req, ref ReceiveRpcCommandRequestComponent reqSrc) =>
                {
                    var player = ecb.CreateEntity();

                    if (!isThinClient)
                    {
                        Debug.Log("Opening CubeSample scene");
                        SceneManager.LoadScene("CubeSample");
                    }
#if UNITY_EDITOR
                    var sceneGUID =
                        new UnityEditor.GUID(
                            UnityEditor.AssetDatabase.AssetPathToGUID(
                                "Assets/Samples/CubeSample/SubScenes/CubeSampleSubScene.unity"));
#else
                    //var sceneGUID = new Unity.Entities.Hash128("6c53c57fae4026f4e95ccd7d39eb0572");

                    //How we could get the GUID, but this does not work
                    var sceneGUID = sceneSystem.GetSceneGUID("Assets/CubeSample/SubScenes/CubeSampleSubScene.unity");
#endif
                    Debug.Log(sceneGUID);
                    sceneSystem.LoadSceneAsync(sceneGUID);

                    EntityManager.SetComponentData(connection, new CommandTargetComponent{targetEntity = Entity.Null});

                    ecb.DestroyEntity(reqEnt);
                }).Run();
            
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }

    public struct ReadyBroadcastRpc : IRpcCommand
    {
        public int playerId;
        public bool readyState;
    }

    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class ReadyClientSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
            RequireSingletonForUpdate<EnableLobbySample>();
        }

        private struct LobbyPlayerGroup
        {
            public Entity entity;
            public LobbyScenePlayer player;
        }

        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer();
            NativeList<LobbyPlayerGroup> group = new NativeList<LobbyPlayerGroup>(Allocator.TempJob);

            Entities.WithNativeDisableContainerSafetyRestriction(group).ForEach((Entity entity, in LobbyScenePlayer player) =>
            {
                group.Add(new LobbyPlayerGroup
                {
                    entity = entity,
                    player = player
                });
            }).Schedule();
            
            Entities.WithNone<SendRpcCommandRequestComponent>().ForEach(
                (Entity reqEnt, ref ReadyBroadcastRpc req, ref ReceiveRpcCommandRequestComponent reqSrc) =>
                {
                    LobbyPlayerGroup p = default;
                    for (int i = 0; i < group.Length; i++)
                    {
                        if (group[i].player.id == req.playerId)
                        {
                            p = group[i];
                            break;
                        }
                    }

                    p.player.ready = req.readyState;
                    ecb.SetComponent(p.entity, p.player);
                    ecb.DestroyEntity(reqEnt);
                }).Schedule();

            group.Dispose(Dependency);
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}