using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public struct ClientDisconnectCleanupData : IComponentData {}

[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
public class ClientDisconnectSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_Barrier;
    private EntityQuery subSceneQuery;

    protected override void OnCreate()
    {
        m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        subSceneQuery = GetEntityQuery(typeof(SceneReference));
    }

    protected override void OnUpdate()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        double time = Time.ElapsedTime;

        Entities.WithStructuralChanges().WithAny<CommandTargetComponent>().ForEach((Entity entity) =>
        {
            EntityManager.AddComponent<NetworkStreamRequestDisconnect>(entity);
        }).Run();
        
        SceneManager.LoadScene("Menu");
        var ecb = m_Barrier.CreateCommandBuffer();
        var e = ecb.CreateEntity();
        ecb.AddComponent(e, new ClientDisconnectCleanupData());
        m_Barrier.AddJobHandleForProducer(Dependency);
    }
}

[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
public class ClientDisconnectCleanupSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem ecbSystem;
    
    protected override void OnCreate()
    {
        ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        RequireSingletonForUpdate<ClientDisconnectCleanupData>();
    }

    protected override void OnUpdate()
    {
        if (HasSingleton<NetworkStreamInGame>())
            return;
        
        EntityManager.DestroyEntity(GetSingletonEntity<ClientDisconnectCleanupData>());
        World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity(typeof(ClientDisconnectCleanupData));
    }
}

[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
public class CleanupClientWorldsSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem ecbSystem;
    private EntityQuery cleanupQuery;
    
    protected override void OnCreate()
    {
        cleanupQuery = GetEntityQuery(typeof(ClientDisconnectCleanupData));
        RequireForUpdate(cleanupQuery);
    }

    protected override void OnUpdate()
    {
        EntityManager.DestroyEntity(cleanupQuery);
        Debug.Log("Disposing NetCode worlds");
        for (var i = World.All.Count - 1; i >= 0; i--)
        {
            World world = World.All[i];
            if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null ||
                world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
            {
                ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop(world);
                world.Dispose();
            }
        }
    }
}

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class ServerDisconnectSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_Barrier;

    protected override void OnCreate()
    {
        m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_Barrier.CreateCommandBuffer();
        Entities.WithAll<NetworkStreamDisconnected>().ForEach((ref CommandTargetComponent state) =>
        {
            if (state.targetEntity != Entity.Null)
            {
                commandBuffer.DestroyEntity(state.targetEntity);
                state.targetEntity = Entity.Null;
            }
        }).Schedule();
        m_Barrier.AddJobHandleForProducer(Dependency);
    }
}