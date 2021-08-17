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

[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
public class ConnectSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireSingletonForUpdate<ConnectData>();
    }

    protected override void OnUpdate()
    {
        var connectData = GetSingleton<ConnectData>();
        EntityManager.DestroyEntity(GetSingletonEntity<ConnectData>());
        var subScenes = GameObject.FindObjectsOfType<SubScene>();
        foreach (World world in World.All)
        {
            var network = world.GetExistingSystem<NetworkStreamReceiveSystem>();
            bool didConnect = false;
            if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
            {
                Debug.Log("Client Connect");
                NetworkEndPoint ep = NetworkEndPoint.Parse(connectData.IPAddress.Value, connectData.Port);
                network.Connect(ep);
                didConnect = true;
            }
            else if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
            {
                Debug.Log("Server Connect");
                NetworkEndPoint ep = NetworkEndPoint.AnyIpv4;
                ep.Port = connectData.Port;
                network.Listen(ep);
                didConnect = true;
            }

            if (didConnect)
            {
                var sceneSystem = world.GetExistingSystem<SceneSystem>();
                foreach (SubScene subScene in subScenes)
                {
                    sceneSystem.LoadSceneAsync(subScene.SceneGUID);
                }
            }
        }
    }
}
