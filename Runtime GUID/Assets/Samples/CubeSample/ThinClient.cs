using Unity.Entities;
using Unity.NetCode;

namespace CubeSample
{
    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class ThinClientInputSystem : SystemBase
    {
        private BufferFromEntity<PlayerInput> playerInputGroup;
        private ClientSimulationSystemGroup clientSimulationSystemGroup;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<ThinClientComponent>();
            RequireSingletonForUpdate<CommandTargetComponent>();
            RequireSingletonForUpdate<EnableCubeSample>();
            clientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();
        }

        protected override void OnUpdate()
        {
            Entity localInput = GetSingleton<CommandTargetComponent>().targetEntity;

            if (localInput == Entity.Null)
            {
                var thinPlayer = GetSingletonEntity<ThinClientComponent>();
                EntityManager.AddBuffer<PlayerInput>(thinPlayer);
                EntityManager.SetComponentData(GetSingletonEntity<CommandTargetComponent>(), new CommandTargetComponent {targetEntity = thinPlayer});
                return;
            }
            
            playerInputGroup = GetBufferFromEntity<PlayerInput>(false);
            if (!playerInputGroup.HasComponent(localInput))
                return;

            PlayerInput input = default;    
            input.Tick = clientSimulationSystemGroup.ServerTick;

            input.horizontal = UnityEngine.Random.Range(-1, 2);
            input.vertical = UnityEngine.Random.Range(-1, 2);

            var inputBuffer = EntityManager.GetBuffer<PlayerInput>(localInput);
            inputBuffer.AddCommandData(input);
        }
    }
}