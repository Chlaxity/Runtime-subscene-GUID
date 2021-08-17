using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

[Preserve]
public class NetcodeBootstrap : ClientServerBootstrap
{
    public static World defaultWorld;
    public static World clientWorld;
    public static World serverWorld;
    
    public override bool Initialize(string defaultWorldName)
    {
        // if (SceneManager.GetActiveScene().name != "Menu")
        // {
        //     return base.Initialize(defaultWorldName);
        // }
        
        // The default world must be created before generating the system list in order to have a valid TypeManager instance.
        // The TypeManage is initialised the first time we create a world.
        var world = new World(defaultWorldName);
        World.DefaultGameObjectInjectionWorld = world;

        var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
        GenerateSystemLists(systems);

        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, ExplicitDefaultWorldSystems);
#if !UNITY_DOTSRUNTIME
        ScriptBehaviourUpdateOrder.AddWorldToCurrentPlayerLoop(world);
#endif

        if (SceneManager.GetActiveScene().name != "Menu")
        {
            var bootstrapEntity = world.EntityManager.CreateEntity(typeof(BootstrapData));
            
#if UNITY_EDITOR
            var ip = RequestedAutoConnect;
            var numThinClients = RequestedNumThinClients;
#else
            // This path probably won't be reached, as the standalone build will always launch the menu first,
            // however we might in the future create builds where that is not the case.
            var ip = "127.0.0.1";
            var numThinClients = 0;
#endif

            world.EntityManager.SetComponentData(bootstrapEntity, new BootstrapData
            {
                Port = 7979,
                IPAddress = ip,
                PlayType = (global::PlayType) RequestedPlayType,
                NumThinClients = numThinClients
            });
        }

        defaultWorld = world;
        return true;
    }
}

public struct BootstrapData : IComponentData
{
    public FixedString32 IPAddress;
    public ushort Port;
    public PlayType PlayType;
    public int NumThinClients;
    public FixedList512<FixedString128> SubScenePaths;
}

public struct ConnectData : IComponentData
{
    public FixedString32 IPAddress;
    public ushort Port;
    public FixedList512<FixedString128> SubScenePaths;
}

[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
public class CreateNetCodeWorldsSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireSingletonForUpdate<BootstrapData>();
    }

    protected override void OnUpdate()
    {
        BootstrapData bootstrapData = GetSingleton<BootstrapData>();
        EntityManager.DestroyEntity(GetSingletonEntity<BootstrapData>());

        if (bootstrapData.PlayType == PlayType.Client ||
            bootstrapData.PlayType == PlayType.ClientAndServer)
            NetcodeBootstrap.clientWorld = ClientServerBootstrap.CreateClientWorld(NetcodeBootstrap.defaultWorld, "ClientWorld");
        if (bootstrapData.PlayType == PlayType.Server ||
            bootstrapData.PlayType == PlayType.ClientAndServer)
            NetcodeBootstrap.serverWorld = ClientServerBootstrap.CreateServerWorld(NetcodeBootstrap.defaultWorld, "ServerWorld");
        for (int i = 0; i < bootstrapData.NumThinClients; i++)
        {
            var thin = ClientServerBootstrap.CreateClientWorld(NetcodeBootstrap.defaultWorld, $"ThinClient{i}");
            var thinPlayer = thin.EntityManager.CreateEntity(typeof(ThinClientComponent));
        }

        var connectEntity = EntityManager.CreateEntity(typeof(ConnectData));
        EntityManager.SetComponentData(connectEntity, new ConnectData
        {
            Port = bootstrapData.Port,
            IPAddress = bootstrapData.IPAddress
        });
    }
}