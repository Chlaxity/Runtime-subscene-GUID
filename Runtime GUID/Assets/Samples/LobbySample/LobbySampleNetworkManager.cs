using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LobbySample
{
    public struct SetupLobbyRequest : IRpcCommand
    {
        public FixedString32 name;
    }

    public struct ThinClientReadyTimerData : IComponentData
    {
        public float timer;
    }

    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class SetupLobbyClientSystem : SystemBase
    {
        string[] thinClientNames = {"Mathias", "Chloe", "Jonathan", "Josep", "Per Kristian", "Jamie", "Daisy"};
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableLobbySample>();
            RequireSingletonForUpdate<NetworkIdComponent>();
        }

        // OnStartRunning might cause a bug, since we don't know exactly when it's run.
        protected override void OnStartRunning()
        {
            var req = EntityManager.CreateEntity(typeof(SendRpcCommandRequestComponent));
            var isThinClient = HasSingleton<ThinClientComponent>();
            string name = "Onur";
            if (isThinClient)
            {
                name = thinClientNames[UnityEngine.Random.Range(0, thinClientNames.Length)];
                var readyTimer = EntityManager.CreateEntity();
                EntityManager.AddComponentData(readyTimer, new ThinClientReadyTimerData {timer = 3f});
            }
            else
                name = ConnectionManager.Name;
            EntityManager.AddComponentData(req, new SetupLobbyRequest {name = name});
        }

        protected override void OnUpdate()
        {
        }
    }

    //public struct LobbyManager : IComponentData {}

    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public class SetupLobbyServerSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        private int joinOrder;
        
        protected override void OnCreate()
        {
            ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
            RequireSingletonForUpdate<EnableLobbySample>();
        }

        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer();
            //int frame = UnityEngine.Time.frameCount;

            Entities.WithoutBurst().WithNone<SendRpcCommandRequestComponent>().ForEach(
                (Entity reqEnt, ref SetupLobbyRequest req, ref ReceiveRpcCommandRequestComponent reqSrc) =>
                {
                    //ecb.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);
                    int networkId = GetComponent<NetworkIdComponent>(reqSrc.SourceConnection).Value;
                    Debug.Log($"Player {req.name} joined the game");

                    var player = ecb.CreateEntity();
                    ecb.AddComponent(player, new LobbyScenePlayer{name = req.name, id = joinOrder});
                    
                    var BroadcastRpc = ecb.CreateEntity();
                    ecb.AddComponent(BroadcastRpc, new PlayerJoinedRpc{name = req.name, id = joinOrder++});
                    ecb.AddComponent<SendRpcCommandRequestComponent>(BroadcastRpc);

                    ecb.SetComponent(reqSrc.SourceConnection, new CommandTargetComponent{targetEntity = player});
                    
                    ecb.DestroyEntity(reqEnt);
                }).Run();
            
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
    
    public struct PlayerJoinedRpc : IRpcCommand
    {
        public FixedString32 name;
        public int id;
    }

    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class PlayerJoinedClientSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        
        protected override void OnCreate()
        {
            ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
            RequireSingletonForUpdate<EnableLobbySample>();
        }

        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer();

            Entities.WithNone<SendRpcCommandRequestComponent>().ForEach(
                (Entity reqEnt, ref PlayerJoinedRpc req, ref ReceiveRpcCommandRequestComponent reqSrc) =>
                {
                    var player = ecb.CreateEntity();
                    ecb.AddComponent(player, new LobbyScenePlayer{name = req.name, id = req.id});
                    ecb.SetComponent(reqSrc.SourceConnection, new CommandTargetComponent{targetEntity = player});
                    ecb.DestroyEntity(reqEnt);
                }).Schedule();
            
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}