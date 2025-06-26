using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ApartmentConfigUI : MonoBehaviour
{
    public ApartmentConfig apartmentConfig;
    public VisualTreeAsset roomEditorTemplate;

    private ScrollView roomsScroll;
    private IntegerField roomsCountField;
    private Button applyButton;
    private VisualElement root;
    private bool isLocked = false;

    void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        roomsScroll = root.Q<ScrollView>("RoomsScroll");
        roomsCountField = root.Q<IntegerField>("RoomsCountField");
        applyButton = root.Q<Button>("ApplyButton");

        RefreshUI();
        UpdateApplyButtonState();
    }

    void RefreshUI()
    {
        roomsScroll.Clear();
        var rooms = apartmentConfig.GetRooms();

        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            var roomItem = roomEditorTemplate.Instantiate();//sablon uxml pt camera noastra

            var dropdown = roomItem.Q<DropdownField>("RoomTypeDropdown");
            var xField = roomItem.Q<IntegerField>("XField");
            var yField = roomItem.Q<IntegerField>("YField");


            dropdown.choices = new List<string>(System.Enum.GetNames(typeof(RoomType)));//optiuni în dropdown
            dropdown.value = room.GetRoomType().ToString();//valoarea din Inspector
            dropdown.RegisterValueChangedCallback(evt => //eveniment
            {
                if (System.Enum.TryParse(evt.newValue, out RoomType t))
                    room.SetRoomType(t);
                UpdateApplyButtonState();
            });


            var xInput = xField.Q<TextField>(null, "unity-text-input");//dimensiuni din UI Toolkit
            var yInput = yField.Q<TextField>(null, "unity-text-input");
            if (xInput != null)
            {
                xInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Backspace && xInput.value.Length <= 1)//fara dimensiune x<=1
                    {
                        xInput.value = "";
                        evt.StopPropagation();
                    }
                });
            }
            if (yInput != null)
            {
                yInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Backspace && yInput.value.Length <= 1)//fara dimensiune y<=1
                    {
                        yInput.value = "";
                        evt.StopPropagation();
                    }
                });
            }


            xField.RegisterValueChangedCallback(evt =>  //eveniment
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

            if (isLocked)
            {
                dropdown.SetEnabled(false);
                xField.SetEnabled(false);
                yField.SetEnabled(false);
            }

            roomsScroll.Add(roomItem);
        }
    }


    void UpdateApplyButtonState()
    {
        if (applyButton == null) return;
        applyButton.SetEnabled(CanApply());
    }

    bool CanApply()
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

    void OnApplyPressed()
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
}
