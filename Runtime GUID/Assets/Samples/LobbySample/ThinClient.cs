using Unity.Entities;
using Unity.NetCode;

namespace LobbySample
{
    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class ThinClientReadySystem : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            ecbSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var dt = UnityEngine.Time.deltaTime;
            var ecb = ecbSystem.CreateCommandBuffer();
            
            Entities.ForEach((Entity entity, ref ThinClientReadyTimerData timer) =>
            {
                timer.timer -= dt;

                if (timer.timer <= 0)
                {
                    Entity req = ecb.CreateEntity();
                    ecb.AddComponent<SendRpcCommandRequestComponent>(req);
                    ecb.AddComponent<ReadyRPCClient>(req);
                    ecb.DestroyEntity(entity);
                }
            }).Schedule();
            
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}