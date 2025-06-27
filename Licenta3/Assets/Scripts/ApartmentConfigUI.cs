using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ApartmentConfigUI : MonoBehaviour
{
    public ApartmentConfig apartmentConfig;
    public IntegerField roomsCount;
    public VisualTreeAsset roomEditorTemplate;

    private ScrollView roomsScroll;
    private Button setParametersButton;
    private VisualElement root;

    private bool isLocked = false;
    private Toggle kitchenToggle;
    private Toggle livingRoomToggle;
    private Button goToMainMenu;


    void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        roomsScroll = root.Q<ScrollView>("RoomsScroll");
        setParametersButton = root.Q<Button>("ApplyButton");
        roomsCount = root.Q<IntegerField>("RoomsCountField");
        kitchenToggle = root.Q<Toggle>("KitchenOpenSpaceToggle");
        livingRoomToggle = root.Q<Toggle>("LivingRoomOpenSpaceToggle");

        goToMainMenu = root.Q<Button>("GoToMainMenu");

        if (goToMainMenu != null)
        {
            goToMainMenu.clicked += OnGoToOtherPressed;
        }

        roomsCount.RegisterValueChangedCallback(evt =>
        {
            if (isLocked) return;
            AdjustRoomsList(evt.newValue);
            RefreshUI();
            UpdateApplyButtonState();
        });

        kitchenToggle.RegisterValueChangedCallback(evt => //RegisterValueChangedCallback este built-in UI Toolkit API method (pentru value-change notifications)
        {
            apartmentConfig.IncludeOpenSpaceKitchen = evt.newValue;
            UpdateApplyButtonState();//gray or not the applyButton button
        });
        livingRoomToggle.RegisterValueChangedCallback(evt =>
        {
            apartmentConfig.IncludeOpenSpaceLivingRoom = evt.newValue;
            UpdateApplyButtonState();//gray or not the applyButton button
        });

        if (setParametersButton != null)
            setParametersButton.clicked += OnApplyPressed;

        RefreshUI();
        UpdateApplyButtonState();
    }

    void AdjustRoomsList(int newCount)
    {
        var rooms = apartmentConfig.GetRooms();
        while (rooms.Count < newCount)
            apartmentConfig.AddRoom();
        while (rooms.Count > newCount)
            apartmentConfig.RemoveRoomAt(rooms.Count - 1);
    }

    void RefreshUI()
    {
        roomsScroll.Clear();
        var rooms = apartmentConfig.GetRooms();

        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            var roomItem = roomEditorTemplate.Instantiate();//sablon uxml pt camera noastra

            var dropdown = roomItem.Q<DropdownField>("RoomTypeDropdown");//atribute camera
            var xField = roomItem.Q<IntegerField>("XField");
            var yField = roomItem.Q<IntegerField>("YField");

            dropdown.choices = new List<string>(System.Enum.GetNames(typeof(RoomType)));//optiuni în dropdown
            dropdown.value = room.GetRoomType().ToString();//valoarea din Inspector
            dropdown.RegisterValueChangedCallback(evt => //eveniment (daca s-a schimbat valoarea in meniu)
            {
                if (System.Enum.TryParse(evt.newValue, out RoomType t))
                    room.SetRoomType(t);
                UpdateApplyButtonState();
            });


            var xInput = xField.Q<TextField>(null, "unity-text-input");//dimensiune x din UI Toolkit
            var yInput = yField.Q<TextField>(null, "unity-text-input");//dimensiune y din UI Toolkit


            if (xInput != null)
            {
                xInput.RegisterCallback<KeyDownEvent>(evt => //RegisterCallback este built-in UI Toolkit method on VisualElement; asigura subscribe la evenimente
                {
                    if (evt.keyCode == KeyCode.Backspace && xInput.value.Length <= 1)//fara dimensiune x<=1 cand folosim Backspace pt a sterge valoarea x
                    {
                        xInput.value = "";
                        evt.StopPropagation();//not responding to this change
                    }
                });
            }
            if (yInput != null)
            {
                yInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Backspace && yInput.value.Length <= 1)//fara dimensiune y<=1 cand folosim Backspace pt a sterge valoarea y
                    {
                        yInput.value = "";
                        evt.StopPropagation();//not responding to this change
                    }
                });
            }


            xField.RegisterValueChangedCallback(evt =>  //fires when the field’s parsed value actually changes—after the user commits a new number, toggles a checkbox, or selects a new dropdown entry
            {
                var dims = room.GetRoomDimensions();
                dims.x = evt.newValue;
                room.SetRoomDimensions(dims);
                UpdateApplyButtonState();
            });
            yField.RegisterValueChangedCallback(evt =>
            {
                var dims = room.GetRoomDimensions();
                dims.y = evt.newValue;
                room.SetRoomDimensions(dims);
                UpdateApplyButtonState();
            });

            // regula >=2 
            xField.RegisterCallback<FocusOutEvent>(evt =>
            {
                int parsed = 0;
                if (!int.TryParse(xInput.value, out parsed))
                    parsed = room.GetRoomDimensions().x;
                int clamped = Mathf.Max(2, parsed);
                xField.SetValueWithoutNotify(clamped);
                var dims = room.GetRoomDimensions();
                dims.x = clamped;
                room.SetRoomDimensions(dims);
                UpdateApplyButtonState();
            });
            yField.RegisterCallback<FocusOutEvent>(evt =>
            {
                int parsed = 0;
                if (!int.TryParse(yInput.value, out parsed))
                    parsed = room.GetRoomDimensions().y;
                int clamped = Mathf.Max(2, parsed);
                yField.SetValueWithoutNotify(clamped);
                var dims = room.GetRoomDimensions();
                dims.y = clamped;
                room.SetRoomDimensions(dims);
                UpdateApplyButtonState();
            });

            //valorile vizibile
            xField.SetValueWithoutNotify(room.GetRoomDimensions().x);
            yField.SetValueWithoutNotify(room.GetRoomDimensions().y);

            if (isLocked)//pe perioada executiei nu schimbam valorile
            {
                dropdown.SetEnabled(false);
                xField.SetEnabled(false);
                yField.SetEnabled(false);
            }

            roomsScroll.Add(roomItem);
        }

        bool hasKitchen = rooms.Exists(r => r.GetRoomType() == RoomType.Bucatarie);
        bool hasLivingRoom = rooms.Exists(r => r.GetRoomType() == RoomType.Sufragerie);

        kitchenToggle.SetEnabled(hasKitchen);
        livingRoomToggle.SetEnabled(hasLivingRoom);

        roomsCount.SetValueWithoutNotify(rooms.Count);
        kitchenToggle.SetValueWithoutNotify(apartmentConfig.IncludeOpenSpaceKitchen && hasKitchen);
        livingRoomToggle.SetValueWithoutNotify(apartmentConfig.IncludeOpenSpaceLivingRoom && hasLivingRoom);

    }


    void UpdateApplyButtonState()//gestionare buton setParametersButton
    {
        if (setParametersButton == null) return;
        setParametersButton.SetEnabled(CanApply());//to gray (or not) the button
    }

    bool CanApply()//conditii pentru apartament
    {
        var rooms = apartmentConfig.GetRooms();
        if (rooms.Count < 3) return false;

        bool hasDorm = false, hasBuc = false, hasBaie = false;
        foreach (var room in rooms)
        {
            var type = room.GetRoomType();
            if (type == RoomType.Dormitor) hasDorm = true;
            else if (type == RoomType.Bucatarie) hasBuc = true;
            else if (type == RoomType.Baie) hasBaie = true;

            var dim = room.GetRoomDimensions();
            if (dim.x < 2 || dim.y < 2) return false;
        }
        return hasDorm && hasBuc && hasBaie;
    }

    void OnApplyPressed()//Meniul dispare si incepe generarea
    {
        if (!CanApply()) return;

        isLocked = true;
        root.style.display = DisplayStyle.None;
        GenerateApartment();
    }

    void GenerateApartment()
    {
        Debug.Log("Generare apartament:");
        foreach (var room in apartmentConfig.GetRooms())
        {
            Debug.Log($"• {room.GetRoomType()} ({room.GetRoomDimensions().x}×{room.GetRoomDimensions().y})");
        }
    }

    void Update()//La apasarea tastei M reapare meniul
    {
        if (isLocked && Input.GetKeyDown(KeyCode.M))
        {
            isLocked = false;
            root.style.display = DisplayStyle.Flex;

            RefreshUI();
            UpdateApplyButtonState();
        }
    }

    private void OnGoToOtherPressed()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
