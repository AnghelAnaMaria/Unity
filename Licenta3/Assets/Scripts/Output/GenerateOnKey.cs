using UnityEngine;

public class GenerateOnKey : MonoBehaviour
{
    public Final wfcFinal;

    void Update()
    {
        //Only listen if this scene is active
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (wfcFinal != null)
            {
                wfcFinal.CreateWFC();
                wfcFinal.CreateTilemap();
            }
        }
    }
}
