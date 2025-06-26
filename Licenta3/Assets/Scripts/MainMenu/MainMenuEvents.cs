using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    private UIDocument uiDocument;

    void OnEnable()
    {
        //Find the UIDocument component automatically
        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            uiDocument = FindObjectOfType<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("No UIDocument found in the scene!");
                return;
            }
        }

        //Get the root visual element
        var root = uiDocument.rootVisualElement;

        var PCGButton = root.Q<Button>("PCG");
        var WFCButton = root.Q<Button>("WFC");

        if (PCGButton != null)
        {
            PCGButton.clicked += () =>
            {
                SceneManager.LoadScene("DungeonScene");
            };
        }

        if (WFCButton != null)
        {
            WFCButton.clicked += () =>
            {
                SceneManager.LoadScene("WFCApartmentScene");
            };
        }

    }
}
