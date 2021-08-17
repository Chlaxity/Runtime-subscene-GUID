using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace LobbySample
{
    // This could be a buffer as well, but having these as components will allow them to be added
    // directly to the connection, which means it will also be destroyed when a player disconnect.
    // Having it as a buffer would make it easier to iterate over all the players.
    [GenerateAuthoringComponent]
    public struct LobbyScenePlayer : IComponentData
    {
        public FixedString32 name;
        public int id;
        public bool ready;
    }
}