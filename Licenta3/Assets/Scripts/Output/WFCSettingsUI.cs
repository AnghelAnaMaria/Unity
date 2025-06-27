using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class WFCSettingsUI : MonoBehaviour
{
    private IntegerField patternSizeField;
    private IntegerField maxIterationField;
    private IntegerField outputWidthField;
    private IntegerField outputHeightField;
    private IntegerField stepsBackField;
    private IntegerField chunkSizeField;
    private IntegerField overlapField;
    private IntegerField gridWidthField;
    private IntegerField gridHeightField;
    private Toggle equalWeightsToggle;
    private DropdownField strategyNameField;
    private Button generateButton;
    private Button mainMenuButton;
    private bool isLocked = false;
    private VisualElement root; // <-- MAKE THIS A FIELD SO IT'S ACCESSIBLE IN Update

    void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        // Query
        patternSizeField = root.Q<IntegerField>("PatternSize");
        maxIterationField = root.Q<IntegerField>("MaxIterations");
        outputWidthField = root.Q<IntegerField>("Width");
        outputHeightField = root.Q<IntegerField>("Height");
        stepsBackField = root.Q<IntegerField>("StepsBack");
        chunkSizeField = root.Q<IntegerField>("ChunkSize");
        overlapField = root.Q<IntegerField>("Overlap");
        gridWidthField = root.Q<IntegerField>("GridWidth");
        gridHeightField = root.Q<IntegerField>("GridHeight");
        equalWeightsToggle = root.Q<Toggle>("EqualWeights");
        strategyNameField = root.Q<DropdownField>("StrategyName");
        generateButton = root.Q<Button>("Parameters");
        mainMenuButton = root.Q<Button>("MainMenu");

        // Seed from model
        var cfg = Final.Instance;
        patternSizeField.SetValueWithoutNotify(cfg.patternSize);
        maxIterationField.SetValueWithoutNotify(cfg.maxIteration);
        outputWidthField.SetValueWithoutNotify(cfg.outputWidth);
        outputHeightField.SetValueWithoutNotify(cfg.outputHeight);
        stepsBackField.SetValueWithoutNotify(cfg.stepsBack);
        chunkSizeField.SetValueWithoutNotify(cfg.chunkSize);
        overlapField.SetValueWithoutNotify(cfg.overlap);
        gridWidthField.SetValueWithoutNotify(cfg.gridWidth);
        gridHeightField.SetValueWithoutNotify(cfg.gridHeight);
        equalWeightsToggle.SetValueWithoutNotify(cfg.equalWeights);

        strategyNameField.choices = new List<string> { "1", "2" };
        strategyNameField.SetValueWithoutNotify(cfg.strategyName);

        // Bind raw changes back into the model
        BindInteger(patternSizeField, v => Final.Instance.patternSize = v);
        BindInteger(maxIterationField, v => Final.Instance.maxIteration = v);
        BindInteger(outputWidthField, v => Final.Instance.outputWidth = v);
        BindInteger(outputHeightField, v => Final.Instance.outputHeight = v);
        BindInteger(stepsBackField, v => Final.Instance.stepsBack = v);
        BindInteger(chunkSizeField, v => Final.Instance.chunkSize = v);
        BindInteger(overlapField, v => Final.Instance.overlap = v);
        BindInteger(gridWidthField, v => Final.Instance.gridWidth = v);
        BindInteger(gridHeightField, v => Final.Instance.gridHeight = v);

        equalWeightsToggle.RegisterValueChangedCallback(evt =>
            Final.Instance.equalWeights = evt.newValue);
        strategyNameField.RegisterValueChangedCallback(evt =>
            Final.Instance.strategyName = evt.newValue);

        // Clamp‐on‐blur for each
        ClampOnBlur(patternSizeField, 1, v => Final.Instance.patternSize = v);
        ClampOnBlur(maxIterationField, 100, v => Final.Instance.maxIteration = v);
        ClampOnBlur(outputWidthField, 3, v => Final.Instance.outputWidth = v);
        ClampOnBlur(outputHeightField, 3, v => Final.Instance.outputHeight = v);
        ClampOnBlur(stepsBackField, 0, v => Final.Instance.stepsBack = v);
        ClampOnBlur(chunkSizeField, 1, v => Final.Instance.chunkSize = v);
        ClampOnBlur(overlapField, 0, v => Final.Instance.overlap = v);
        ClampOnBlur(gridWidthField, 1, v => Final.Instance.gridWidth = v);
        ClampOnBlur(gridHeightField, 1, v => Final.Instance.gridHeight = v);

        // Buttons
        generateButton.clicked += () =>
        {
            isLocked = true;
            root.style.display = DisplayStyle.None;
            Final.Instance.CreateWFC();
            Final.Instance.CreateTilemap();
        };
        mainMenuButton.clicked += () =>
            SceneManager.LoadScene("MainMenu");

        UpdateApplyButtonState();
    }

    void BindInteger(IntegerField f, System.Action<int> setter)
    {
        f.RegisterValueChangedCallback(evt =>
        {
            setter(evt.newValue);
            UpdateApplyButtonState();
        });

        // one‐stroke backspace
        var input = f.Q<TextField>(null, "unity-text-input");
        if (input != null)
            input.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Backspace && input.value.Length <= 1)
                {
                    input.value = "";
                    evt.StopPropagation();
                }
            });
    }

    void ClampOnBlur(IntegerField f, int min, System.Action<int> setter)
    {
        f.RegisterCallback<FocusOutEvent>(evt =>
        {
            var input = f.Q<TextField>(null, "unity-text-input");
            int parsed;
            if (input == null || !int.TryParse(input.value, out parsed))
                parsed = f.value; // last known good
            int clamped = Mathf.Max(min, parsed);
            f.SetValueWithoutNotify(clamped);
            setter(clamped);
            UpdateApplyButtonState();
        });
    }

    void UpdateApplyButtonState()
    {
        generateButton.SetEnabled(
            Final.Instance.patternSize >= 1 &&
            Final.Instance.maxIteration >= 100 &&
            Final.Instance.outputWidth >= 3 &&
            Final.Instance.outputHeight >= 3 &&
            Final.Instance.stepsBack >= 0);
    }

    void Update()
    {
        if (isLocked && Input.GetKeyDown(KeyCode.M))
        {
            isLocked = false;
            root.style.display = DisplayStyle.Flex;
            UpdateApplyButtonState();
        }
    }
}
