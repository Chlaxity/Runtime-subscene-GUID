using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    public void LoadSceneNow(string sceneName)
    {
        var bootstrapEntity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity(typeof(BootstrapData));
        
        World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(bootstrapEntity, new BootstrapData
        {
            Port = ConnectionManager.port,
            IPAddress = ConnectionManager.IPAddress,
            PlayType = ConnectionManager.RequestedPlayType,
            NumThinClients = ConnectionManager.NumThinClients,
        });
        SceneManager.LoadScene(sceneName);
    }
}
