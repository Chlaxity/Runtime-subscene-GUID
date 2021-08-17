//THIS FILE IS AUTOGENERATED BY GHOSTCOMPILER. DON'T MODIFY OR ALTER.
using AOT;
using Unity.Burst;
using Unity.Networking.Transport;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using CubeSample;


namespace Assembly_CSharp.Generated
{
    [BurstCompile]
    public struct CubeSampleGoInGameRequestSerializer : IComponentData, IRpcCommandSerializer<CubeSample.GoInGameRequest>
    {
        public void Serialize(ref DataStreamWriter writer, in RpcSerializerState state, in CubeSample.GoInGameRequest data)
        {
            writer.WriteFloat(data.color.x);
            writer.WriteFloat(data.color.y);
            writer.WriteFloat(data.color.z);
            writer.WriteFloat(data.color.w);
        }

        public void Deserialize(ref DataStreamReader reader, in RpcDeserializerState state,  ref CubeSample.GoInGameRequest data)
        {
            data.color.x = reader.ReadFloat();
            data.color.y = reader.ReadFloat();
            data.color.z = reader.ReadFloat();
            data.color.w = reader.ReadFloat();
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(RpcExecutor.ExecuteDelegate))]
        private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
        {
            RpcExecutor.ExecuteCreateRequestComponent<CubeSampleGoInGameRequestSerializer, CubeSample.GoInGameRequest>(ref parameters);
        }

        static PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
            new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
        public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
        {
            return InvokeExecuteFunctionPointer;
        }
    }
    class CubeSampleGoInGameRequestRpcCommandRequestSystem : RpcCommandRequestSystem<CubeSampleGoInGameRequestSerializer, CubeSample.GoInGameRequest>
    {
        [BurstCompile]
        protected struct SendRpc : IJobEntityBatch
        {
            public SendRpcData data;
            public void Execute(ArchetypeChunk chunk, int orderIndex)
            {
                data.Execute(chunk, orderIndex);
            }
        }
        protected override void OnUpdate()
        {
            var sendJob = new SendRpc{data = InitJobData()};
            ScheduleJobData(sendJob);
        }
    }
}
