# Runtime subscene GUID
 
The repository includes two builds - One that works using a hardcoded scene GUID and one that does not work, attempting to use sceneSystem.GetSceneGUID.

The difference in this behaviour is found in the ReadyHandler.cs script at line 169-181

I recommend opening the project in the unity editor and using Server Only mode, whilst joining as Client Only with the builds.
Alternatively, Server&Client Only works for the builds as well.

Use the Lobby Sample button and click ready.