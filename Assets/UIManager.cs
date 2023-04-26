using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public interface IScreen
{
    void SetPower(Applience appl, float power);
    void SetApplienceToogle(Applience id, bool state);
    void SetPowerMoment(float powerMoment);
    void SetTotalPower(float power);
    void SetBatteryPowerLeftPercentage(float batteryPower);
    void SetOptionalPower(float power);
    void SetEssentialPower(float power);
    void SetUndesiredPower(float power);

}

public class UIManager : MonoBehaviour
{
    public IScreen Screen { get; set; }

    [FormerlySerializedAs("_UXMLTree")] [SerializeField]
    private VisualTreeAsset uxmlTree;

    [FormerlySerializedAs("_optionalTemplate")] [SerializeField]
    private VisualTreeAsset optionalTemplate;

    public ApplienceManager applienceManager;
    private UIDocument _uiDocument;
    private SmartPowerScreen _smartPowerScreen;
    private OffGridScreen _offGridScreen;

    private Label _batteryLifeLabel;
    private Label _totalPower;

    private Label _dialogLabel;
    private Button _newGameButton;
    private VisualElement _offgridRibbon;
    private VisualElement _dialogWindow;
    private ScrollView _dragDropArea;
    private Label _temperature;
    private Label _dayTime;
    private Label _powerMoment;
    private Button _startGameButton;
    private Button _navigetaButton;
    public RadioButtonGroup GameMode;
    private VisualElement _newGameWindow;

    private Button _stopSimulation;

    private Dictionary<Applience, Label> _sliderLabels = new();
    private Dictionary<Slider, Applience> _batterySliders = new();
    private Dictionary<Applience, bool> _optionalStates = new();
    private Dictionary<VisualElement, Applience> _visualsDic = new();
    private Dictionary<Applience, bool> _applienceClickedStates = new();

    public event Action<Applience, int> OptionalBatteryChanged;

    public event Action<Applience, bool> ApplienceStateChanged = (app, state) => { };
    public event Action<Applience, bool> ApplienceClicked;
    public event Action<int, int> NewGameClicked;
    public event Action<float> TimeScaleChanged = (v) => { };
    public event Action<ApplienceRecord[]> AppliencePriorityChanged;
    public event Action StopSimulation;

    Coroutine _clearMessageCoroutine;
    private VisualElement _draggingVisual;
    private Toggle _smartToggle;
    private Toggle _screenSwitch;
    private int _messageCount;
    private bool _hideOptional = true;

    private Slider _timeScale;
    public float TimeScale { get => _timeScale.value; }

    private bool _isDragging;
    private int _originalIndex;
    private int _currentIndex;

    // Start is called before the first frame update
    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _smartPowerScreen = new SmartPowerScreen(_uiDocument.rootVisualElement.Q<VisualElement>("Screen"), ApplienceStateChanged);
        _offGridScreen = new OffGridScreen(_uiDocument.rootVisualElement.Q<VisualElement>("Offgrid"), ApplienceStateChanged);
        _offGridScreen.SetOffgrid(true);
        SwitchScreen(true);

        _dialogLabel = _uiDocument.rootVisualElement.Q<Label>("dialog_label");
        _newGameButton = _uiDocument.rootVisualElement.Q<Button>("new_game");

        GameMode = _uiDocument.rootVisualElement.Q<RadioButtonGroup>("game_mode");
        GameMode.RegisterValueChangedCallback(OnGameModeChanged);

        _newGameWindow = _uiDocument.rootVisualElement.Q<VisualElement>("new_game_window_1");
        _navigetaButton = _uiDocument.rootVisualElement.Q<Button>("home_icon");
        _navigetaButton.clicked += OnNavigationClicked;
        _stopSimulation = _uiDocument.rootVisualElement.Q<Button>("stop_simulation");
        _stopSimulation.clicked += () =>
        {
            _offgridRibbon.visible = false;
            StopSimulation?.Invoke();
            _stopSimulation.visible = false;
            _newGameWindow.visible = true;
            _offGridScreen.SetOffgrid(true);
            _smartToggle.value = false;
        };
        _stopSimulation.visible = false;

        _dialogWindow = _uiDocument.rootVisualElement.Q<VisualElement>("dialog_window");
        _dialogWindow.visible = false;

        _dragDropArea = _uiDocument.rootVisualElement.Q<ScrollView>("drag_drop_area");

        _dragDropArea.RegisterCallback<MouseMoveEvent>(OnDragMove);
        _dragDropArea.RegisterCallback<WheelEvent>(OnScreenMouseWheel);
        _dragDropArea.RegisterCallback<MouseLeaveEvent>(OnDragLeave);
        _smartToggle = _uiDocument.rootVisualElement.Q<Toggle>("smart_toggle");

        _screenSwitch = _uiDocument.rootVisualElement.Q<Toggle>("screen_switch");
        _screenSwitch.RegisterValueChangedCallback(OnScreenChanged);

        _offgridRibbon = _uiDocument.rootVisualElement.Q<VisualElement>("top_ribbon");
        _offgridRibbon.visible = false;

        _reset = _uiDocument.rootVisualElement.Q<Button>("reset");
        _reset.clicked += OnReset;
        

        var fridgeVisual = _uiDocument.rootVisualElement.Q<VisualElement>("fridge_visual");
        var airCondVisual = _uiDocument.rootVisualElement.Q<VisualElement>("aircond_visual");
        var tvVisual = _uiDocument.rootVisualElement.Q<VisualElement>("tv_visual");
        var ovenVisual = _uiDocument.rootVisualElement.Q<VisualElement>("oven_visual");
        var dryerVisual = _uiDocument.rootVisualElement.Q<VisualElement>("dryer_visual");
        var washerVisual = _uiDocument.rootVisualElement.Q<VisualElement>("washer_visual");
        var evVisual = _uiDocument.rootVisualElement.Q<VisualElement>("ev_visual");
        var lightsVisual = _uiDocument.rootVisualElement.Q<VisualElement>("lights_visual");
        var boilerVisual = _uiDocument.rootVisualElement.Q<VisualElement>("boiler_visual");

        fridgeVisual.RegisterCallback<MouseDownEvent>((evt) => OnApplienceMouseDown(fridgeVisual, evt, Applience.Fridge));
        airCondVisual.RegisterCallback<MouseDownEvent>((evt) => OnApplienceMouseDown(airCondVisual, evt, Applience.AirCond));
        tvVisual.RegisterCallback<MouseDownEvent>((evt) => OnApplienceMouseDown(tvVisual, evt, Applience.TV));
        ovenVisual.RegisterCallback<MouseDownEvent>((evt) => OnApplienceMouseDown(ovenVisual, evt, Applience.Oven));
        dryerVisual.RegisterCallback<MouseDownEvent>((evt) => OnApplienceMouseDown(dryerVisual, evt, Applience.Dryer));
        washerVisual.RegisterCallback<MouseDownEvent>((evt) => OnApplienceMouseDown(washerVisual, evt, Applience.Washer));
        evVisual.RegisterCallback<MouseDownEvent>((evt) => OnApplienceMouseDown(evVisual, evt, Applience.Ev));
        lightsVisual.RegisterCallback<MouseDownEvent>((evt) => OnApplienceMouseDown(lightsVisual, evt, Applience.Lights));
        boilerVisual.RegisterCallback<MouseDownEvent>((evt) => OnApplienceMouseDown(boilerVisual, evt, Applience.Boiler));

        fridgeVisual.RegisterCallback<MouseUpEvent>((evt) => OnApplienceMouseUp(fridgeVisual, evt, Applience.Fridge));
        airCondVisual.RegisterCallback<MouseUpEvent>((evt) => OnApplienceMouseUp(airCondVisual, evt, Applience.AirCond));
        tvVisual.RegisterCallback<MouseUpEvent>((evt) => OnApplienceMouseUp(tvVisual, evt, Applience.TV));
        ovenVisual.RegisterCallback<MouseUpEvent>((evt) => OnApplienceMouseUp(ovenVisual, evt, Applience.Oven));
        dryerVisual.RegisterCallback<MouseUpEvent>((evt) => OnApplienceMouseUp(dryerVisual, evt, Applience.Dryer));
        washerVisual.RegisterCallback<MouseUpEvent>((evt) => OnApplienceMouseUp(washerVisual, evt, Applience.Washer));
        evVisual.RegisterCallback<MouseUpEvent>((evt) => OnApplienceMouseUp(evVisual, evt, Applience.Ev));
        lightsVisual.RegisterCallback<MouseUpEvent>((evt) => OnApplienceMouseUp(lightsVisual, evt, Applience.Lights));
        boilerVisual.RegisterCallback<MouseUpEvent>((evt) => OnApplienceMouseUp(boilerVisual, evt, Applience.Boiler));

        var slider = fridgeVisual.Q<Slider>();
        slider.RegisterValueChangedCallback(OptionalValueChanged);
        _batterySliders.Add(slider, Applience.Fridge);
        _sliderLabels.Add(Applience.Fridge, fridgeVisual.Q<Label>("percentage"));
        var batterThreshold = fridgeVisual.Q<Button>("set_threshold");
        batterThreshold.clicked += () => OpenOptionalSection(fridgeVisual, Applience.Fridge);
        batterThreshold.visible = false;
        var cancel = fridgeVisual.Q<Button>("cancel");
        cancel.clicked += () =>
        {
            fridgeVisual.Q<Slider>().value = 0;
            CloseOptional(fridgeVisual, Applience.Fridge);
        };
        var save = fridgeVisual.Q<Button>("save");
        save.clicked += () => CloseOptional(fridgeVisual, Applience.Fridge);
        var navigationButton = fridgeVisual.Q<Button>("applience_link");
        navigationButton.text += "-(on)";
        navigationButton.clicked += () => ApplienceNameClicked(fridgeVisual, Applience.Fridge);

        slider = airCondVisual.Q<Slider>();
        slider.RegisterValueChangedCallback(OptionalValueChanged);
        _batterySliders.Add(slider, Applience.AirCond);
        _sliderLabels.Add(Applience.AirCond, airCondVisual.Q<Label>("percentage"));
        batterThreshold = airCondVisual.Q<Button>("set_threshold");
        batterThreshold.visible = false;
        batterThreshold.clicked += () => OpenOptionalSection(airCondVisual, Applience.AirCond);
        cancel = airCondVisual.Q<Button>("cancel");
        cancel.clicked += () =>
        {
            airCondVisual.Q<Slider>().value = 0;
            CloseOptional(airCondVisual, Applience.AirCond);
        };
        save = airCondVisual.Q<Button>("save");
        save.clicked += () => CloseOptional(airCondVisual, Applience.AirCond);
        navigationButton = airCondVisual.Q<Button>("applience_link");
        navigationButton.text += "-(on)";
        navigationButton.clicked += () => ApplienceNameClicked(airCondVisual, Applience.AirCond);

        slider = tvVisual.Q<Slider>();
        slider.RegisterValueChangedCallback(OptionalValueChanged);
        _batterySliders.Add(slider, Applience.TV);
        _sliderLabels.Add(Applience.TV, tvVisual.Q<Label>("percentage"));
        batterThreshold = tvVisual.Q<Button>("set_threshold");
        batterThreshold.visible = false;
        batterThreshold.clicked += () => OpenOptionalSection(tvVisual, Applience.TV);
        cancel = tvVisual.Q<Button>("cancel");
        cancel.clicked += () =>
        {
            tvVisual.Q<Slider>().value = 0;
            CloseOptional(tvVisual, Applience.TV);
        };
        save = tvVisual.Q<Button>("save");
        save.clicked += () => CloseOptional(tvVisual, Applience.TV);
        navigationButton = tvVisual.Q<Button>("applience_link");
        navigationButton.text += "-(on)";
        navigationButton.clicked += () => ApplienceNameClicked(tvVisual, Applience.TV);

        slider = ovenVisual.Q<Slider>();
        slider.RegisterValueChangedCallback(OptionalValueChanged);
        _batterySliders.Add(slider, Applience.Oven);
        _sliderLabels.Add(Applience.Oven, ovenVisual.Q<Label>("percentage"));
        batterThreshold = ovenVisual.Q<Button>("set_threshold");
        batterThreshold.visible = false;
        batterThreshold.clicked += () => OpenOptionalSection(ovenVisual, Applience.Oven);
        cancel = ovenVisual.Q<Button>("cancel");
        cancel.clicked += () =>
        {
            ovenVisual.Q<Slider>().value = 0;
            CloseOptional(ovenVisual, Applience.Oven);
        };
        save = ovenVisual.Q<Button>("save");
        save.clicked += () => CloseOptional(ovenVisual, Applience.Oven);
        navigationButton = ovenVisual.Q<Button>("applience_link");
        navigationButton.text += "-(on)";
        navigationButton.clicked += () => ApplienceNameClicked(ovenVisual, Applience.Oven);


        slider = dryerVisual.Q<Slider>();
        slider.RegisterValueChangedCallback(OptionalValueChanged);
        _batterySliders.Add(slider, Applience.Dryer);
        _sliderLabels.Add(Applience.Dryer, dryerVisual.Q<Label>("percentage"));
        batterThreshold = dryerVisual.Q<Button>("set_threshold");
        batterThreshold.visible = false;
        batterThreshold.clicked += () => OpenOptionalSection(dryerVisual, Applience.Dryer);
        cancel = dryerVisual.Q<Button>("cancel");
        cancel.clicked += () =>
        {
            dryerVisual.Q<Slider>().value = 0;
            CloseOptional(dryerVisual, Applience.Dryer);
        };
        save = dryerVisual.Q<Button>("save");
        save.clicked += () => CloseOptional(dryerVisual, Applience.Dryer);
        navigationButton = dryerVisual.Q<Button>("applience_link");
        navigationButton.text += "-(on)";
        navigationButton.clicked += () => ApplienceNameClicked(dryerVisual, Applience.Dryer);

        slider = washerVisual.Q<Slider>();
        slider.RegisterValueChangedCallback(OptionalValueChanged);
        _batterySliders.Add(slider, Applience.Washer);
        _sliderLabels.Add(Applience.Washer, washerVisual.Q<Label>("percentage"));
        batterThreshold = washerVisual.Q<Button>("set_threshold");
        batterThreshold.visible = false;
        batterThreshold.clicked += () => OpenOptionalSection(washerVisual, Applience.Washer);
        cancel = washerVisual.Q<Button>("cancel");
        cancel.clicked += () =>
        {
            washerVisual.Q<Slider>().value = 0;
            CloseOptional(washerVisual, Applience.Washer);
        };
        save = washerVisual.Q<Button>("save");
        save.clicked += () => CloseOptional(washerVisual, Applience.Washer);
        navigationButton = washerVisual.Q<Button>("applience_link");
        navigationButton.text += "-(on)";
        navigationButton.clicked += () => ApplienceNameClicked(washerVisual, Applience.Washer);



        slider = evVisual.Q<Slider>();
        slider.RegisterValueChangedCallback(OptionalValueChanged);
        _batterySliders.Add(slider, Applience.Ev);
        _sliderLabels.Add(Applience.Ev, evVisual.Q<Label>("percentage"));
        batterThreshold = evVisual.Q<Button>("set_threshold");
        batterThreshold.visible = false;
        batterThreshold.clicked += () => OpenOptionalSection(evVisual, Applience.Ev);
        cancel = evVisual.Q<Button>("cancel");
        cancel.clicked += () =>
        {
            evVisual.Q<Slider>().value = 0;
            CloseOptional(evVisual, Applience.Ev);
        };
        save = evVisual.Q<Button>("save");
        save.clicked += () => CloseOptional(evVisual, Applience.Ev);
        navigationButton = evVisual.Q<Button>("applience_link");
        navigationButton.text += "-(on)";
        navigationButton.clicked += () => ApplienceNameClicked(evVisual, Applience.Ev);

        slider = lightsVisual.Q<Slider>();
        slider.RegisterValueChangedCallback(OptionalValueChanged);
        _batterySliders.Add(slider, Applience.Lights);
        _sliderLabels.Add(Applience.Lights, lightsVisual.Q<Label>("percentage"));
        batterThreshold = lightsVisual.Q<Button>("set_threshold");
        batterThreshold.visible = false;
        batterThreshold.clicked += () => OpenOptionalSection(lightsVisual, Applience.Lights);
        cancel = lightsVisual.Q<Button>("cancel");
        cancel.clicked += () =>
        {
            lightsVisual.Q<Slider>().value = 0;
            CloseOptional(lightsVisual, Applience.Lights);
        };
        save = lightsVisual.Q<Button>("save");
        save.clicked += () => CloseOptional(lightsVisual, Applience.Lights);
        navigationButton = lightsVisual.Q<Button>("applience_link");
        navigationButton.text += "-(on)";
        navigationButton.clicked += () => ApplienceNameClicked(lightsVisual, Applience.Lights);

        slider = boilerVisual.Q<Slider>();
        slider.RegisterValueChangedCallback(OptionalValueChanged);
        _batterySliders.Add(slider, Applience.Boiler);
        _sliderLabels.Add(Applience.Boiler, boilerVisual.Q<Label>("percentage"));
        batterThreshold = boilerVisual.Q<Button>("set_threshold");
        batterThreshold.visible = false;
        batterThreshold.clicked += () => OpenOptionalSection(boilerVisual, Applience.Boiler);
        cancel = boilerVisual.Q<Button>("cancel");
        cancel.clicked += () =>
        {
            boilerVisual.Q<Slider>().value = 0;
            CloseOptional(boilerVisual, Applience.Boiler);
        };
        save = boilerVisual.Q<Button>("save");
        save.clicked += () => CloseOptional(boilerVisual, Applience.Boiler);
        navigationButton = boilerVisual.Q<Button>("applience_link");
        navigationButton.text += "-(on)";
        navigationButton.clicked += () => ApplienceNameClicked(boilerVisual, Applience.Boiler);

        _dayTime = _uiDocument.rootVisualElement.Q<Label>("day_time");

        _timeScale = _uiDocument.rootVisualElement.Q<Slider>("time_scale");
        _timeScale.RegisterValueChangedCallback((evt) => TimeScaleChanged(evt.newValue));

        _newGameButton.clicked += OpenNewGameWindow;

        HideBattery(GameMode.value == 2);
        HideAddBattery(GameMode.value != 3);
    }


    private void  OnReset()
    {
        SceneManager.LoadScene("SampleScene");
    }

    private void ApplienceNameClicked(VisualElement applienceVisual, Applience applience)
    {
        _applienceClickedStates[applience] = !_applienceClickedStates[applience];
        string status = _applienceClickedStates[applience] ? "-(on)" : "-(off)";
        var linkButton = applienceVisual.Q<Button>("applience_link");
        linkButton.text = linkButton.text.Split("-(")[0] + status;
        ApplienceClicked?.Invoke(applience, _applienceClickedStates[applience]);
    }

    private void OnGameModeChanged(ChangeEvent<int> evt)
    {
        _newGameButton.SetEnabled(true);
        SwitchScreen(evt.newValue != 0);
        SetEvVisualColor(evt.newValue == 3);
        if (evt.newValue == 1 || evt.newValue == 3)
        {
            foreach (var i in _batterySliders)
            {
                OptionalBatteryChanged(i.Value, 0);
            }
            _hideOptional = true;
            HideBattery(false);
        }
        if (evt.newValue == 2)
        {
            HideBattery(true);
            foreach (var i in _batterySliders)
            {
                OptionalBatteryChanged(i.Value, (int)MathF.Round(i.Key.value,0));
            }
            _hideOptional = false;

        }

        if (evt.newValue == 0 || evt.newValue == 3)
        {
            _newGameButton.SetEnabled(false);
        }

        HideAddBattery(evt.newValue != 3);
    }

    public void OrderAppliences( ApplienceOrder order)
    {
        _applienceOrder = order.Order;
        var optionalVis = _dragDropArea.Children().First(i => i.name == "Optional");
        var undesiredVis = _dragDropArea.Children().First(i => i.name == "Undesired");
        var essentialVis = _dragDropArea.Children().First(i => i.name == "Essential");
        var visualReverted = _visualsDic.ToDictionary(i => i.Value, j=> j.Key);
        Dictionary<Applience,ApplienceRecord> orderDic = order.Order.ToDictionary(i => i.Applience);
        _dragDropArea.Clear();
        int ind = 0;
        _dragDropArea.Insert(ind++, essentialVis);
        for(int i = 0; i < order.Order.Length; i++)
        {
            ApplienceRecord record = order.Order[i];
            if (record.Type == PriorityType.Optional && optionalVis != null)
            {
                _dragDropArea.Insert(ind++, optionalVis);
                optionalVis = null;
            }
            if (record.Type == PriorityType.Undesired && undesiredVis != null)
            {
                if(optionalVis!= null) // no optional case
                {
                    _dragDropArea.Insert(ind++, optionalVis);
                    optionalVis = null;
                }

                _dragDropArea.Insert(ind++, undesiredVis);
                undesiredVis = null;
            }

            _dragDropArea.Insert(ind++, visualReverted[record.Applience]);
        }
        if (undesiredVis != null) // no undesired case
        {
            _dragDropArea.Insert(ind++, undesiredVis);
            undesiredVis = null;
        }
        CleanOptional();
    }

    private void SetEvVisualColor(bool isEvMode)
    {
        var evVisual = _dragDropArea.Children().First(i => i.name == "ev_visual");
        var newEvColor = evVisual.style.backgroundColor.value;
        newEvColor.a = isEvMode ? 0.35f : 0;
        evVisual.style.backgroundColor = new StyleColor(newEvColor);
    }

    private void HideBattery(bool isVisible)
    {
        _uiDocument.rootVisualElement.Q<VisualElement>("battery_section").visible = isVisible;

        _uiDocument.rootVisualElement.Q<VisualElement>("power_section").style.left = isVisible ? 0 : 100;
        
    }
    private void HideAddBattery(bool isVisible)
    {
        _uiDocument.rootVisualElement.Q<VisualElement>("add_battery_section").style.display = isVisible ?  DisplayStyle.Flex :  DisplayStyle.None;
        _uiDocument.rootVisualElement.Q<VisualElement>("smart_toggle").style.display = isVisible ?  DisplayStyle.Flex :  DisplayStyle.None;
    }

    public event Action NavigateClicked;
    private void OnNavigationClicked()
    {
        NavigateClicked();
    }

    private void OnScreenChanged(ChangeEvent<bool> evt)
    {
        SwitchScreen(evt.newValue);
    }

    private void CloseOptional(VisualElement vis, Applience es, bool isStartClean = false)
    {
        if (!isStartClean)
            _optionalStates[es] = false;

        var optional = vis.Q<VisualElement>("optional_section");
        if (optional != null)
        {
            optional.visible = false;
            vis.style.height = 55;
            optional.style.display = DisplayStyle.Flex;

            var threshold = vis.Q<Button>("set_threshold");
            string percent = vis.Q<Slider>().value.ToString("0",CultureInfo.InvariantCulture);
            threshold.text = $"Battery threshold: {percent}%";
        }
    }
    private void OptionalValueChanged(ChangeEvent<float> evt)
    {
        var applience = _batterySliders[evt.target as Slider];
        int threshold = (int)MathF.Round(evt.newValue, 0);
        _sliderLabels[applience].text = threshold.ToString("0") + "%";
        OptionalBatteryChanged?.Invoke(applience,threshold);
        
    }

    private void OnScreenMouseWheel(WheelEvent evt)
    {
        var updateContentViewTransform = false;
        var canUseVerticalScroll = _dragDropArea.contentContainer.localBound.height - _dragDropArea.layout.height > 0;
        var canUseHorizontalScroll = _dragDropArea.contentContainer.localBound.width - _dragDropArea.layout.width > 0;
        var horizontalScrollDelta = canUseHorizontalScroll && !canUseVerticalScroll ? evt.delta.y : evt.delta.x;

        if (canUseVerticalScroll)
        {
            var oldVerticalValue = _dragDropArea.verticalScroller.value;

            if (evt.delta.y < 0)
                _dragDropArea.verticalScroller.ScrollPageUp(Mathf.Abs(evt.delta.y) * 50);
            else if (evt.delta.y > 0)
                _dragDropArea.verticalScroller.ScrollPageDown(Mathf.Abs(evt.delta.y) * 50);

            if (_dragDropArea.verticalScroller.value != oldVerticalValue)
            {
                evt.StopPropagation();
                updateContentViewTransform = true;
            }
        }

        if (canUseHorizontalScroll)
        {
            var oldHorizontalValue = _dragDropArea.horizontalScroller.value;

            if (horizontalScrollDelta < 0)
                _dragDropArea.horizontalScroller.ScrollPageUp(Mathf.Abs(horizontalScrollDelta * 10));
            else if (horizontalScrollDelta > 0)
                _dragDropArea.horizontalScroller.ScrollPageDown(Mathf.Abs(horizontalScrollDelta * 10));

            if (_dragDropArea.horizontalScroller.value != oldHorizontalValue)
            {
                evt.StopPropagation();
                updateContentViewTransform = true;
            }
        }

    }

    private void Start()
    {
        var appliences = ApplienceHelper.All();
        foreach (var applience in appliences)
        {
            _optionalStates[applience] = true;
            _applienceClickedStates[applience] = true;
        }
        RaiseApplienceOrderChanged();//  used for initialization

    }

    private bool _isSmartScreen;
    private ApplienceRecord[] _applienceOrder;
    private Button _reset;
    [SerializeField] private Color HIGHLIGHT_CIRCUIT_COLOR;

    public void SwitchScreen(bool isSmartScreen)
    {
        if (isSmartScreen)
        {
            _uiDocument.rootVisualElement.Q<VisualElement>("Screen").visible = true;
            _uiDocument.rootVisualElement.Q<VisualElement>("Offgrid").visible = false;
            
            _offGridScreen.SetOffgrid(false);
            _offGridScreen.ShowHideBottomMenu(false,false);
            Screen = _smartPowerScreen;
        }
        else
        {
            _uiDocument.rootVisualElement.Q<VisualElement>("Screen").visible = false;
            _uiDocument.rootVisualElement.Q<VisualElement>("Offgrid").visible = true;
            Screen = _offGridScreen;
        }
        _isSmartScreen = isSmartScreen;
        foreach (var item in applienceManager.ApplienceStates)
        {
            _offGridScreen.SetApplienceToogle(item.Key, item.Value);
            _smartPowerScreen.SetApplienceToogle(item.Key, item.Value);
        }
    }

    private void OpenNewGameWindow()
    {
        _stopSimulation.visible = true;
        _newGameWindow.visible = false;
        _smartToggle.value = true;
        NewGameClicked(GameMode.value, 1);
        _offgridRibbon.visible = true;
        _offGridScreen.SetOffgrid(false);
    }

    private void OnApplienceMouseDown(VisualElement visual, MouseDownEvent mouseEvent, Applience _)
    {
        if (!_isDragging)
        {
            StartDrag(mouseEvent, visual);
            return;
        }
    }

    private void OnDragMove(MouseMoveEvent mouseEvent)
    {
        if (!_isDragging || _draggingVisual == null)
        {
            return;
        }
        _currentIndex = _dragDropArea.IndexOf(_draggingVisual);
        foreach (var item in _dragDropArea.Children())
        {

            if (item.worldBound.Contains(mouseEvent.mousePosition) && item.worldBound.center.y > mouseEvent.mousePosition.y)
            {
                _currentIndex = _dragDropArea.IndexOf(item);
                SetPosition(_currentIndex, _draggingVisual);
                break;
            }

        }
    }

    private void OnApplienceMouseUp(VisualElement visual, MouseUpEvent mouseEvent, Applience boiler)
    {
        StopDrag();
    }

    private void OnDragLeave(MouseLeaveEvent evt)
    {
        StopDrag();
    }

    private void StartDrag(MouseDownEvent mouseEvent, VisualElement visual)
    {
        if (GameMode.value == 3 && visual.name == "ev_visual")
            return;

        
        _draggingVisual = visual;
        _draggingVisual.style.borderRightColor = _draggingVisual.style.borderLeftColor =
            _draggingVisual.style.borderTopColor =
                _draggingVisual.style.borderBottomColor = new StyleColor(HIGHLIGHT_CIRCUIT_COLOR);
        _draggingVisual.style.borderBottomWidth = _draggingVisual.style.borderTopWidth =
            _draggingVisual.style.borderLeftWidth = _draggingVisual.style.borderRightWidth = new StyleFloat(3f);

        
        _isDragging = true;
        visual.style.left = 0;
        _originalIndex = _dragDropArea.IndexOf(visual);
        _currentIndex = _dragDropArea.IndexOf(visual);
    }
    
    private void StopDrag()
    {
        if (!_isDragging)
            return;

        SetPosition(_currentIndex, _draggingVisual);
        
        _draggingVisual.style.borderRightColor = _draggingVisual.style.borderLeftColor =
            _draggingVisual.style.borderTopColor =
                _draggingVisual.style.borderBottomColor = new StyleColor(new Color(224/255f,224/255f,224/255f));
        _draggingVisual.style.borderBottomWidth = _draggingVisual.style.borderTopWidth =
            _draggingVisual.style.borderLeftWidth = _draggingVisual.style.borderRightWidth = new StyleFloat(1f);
        
        _draggingVisual.style.left = 10;
        _isDragging = false;
        _draggingVisual = null;
        if (_currentIndex != _originalIndex)
            RaiseApplienceOrderChanged();
    }


    private void RaiseApplienceOrderChanged()
    {
        var appliences = ApplienceHelper.All();
        _applienceOrder = new ApplienceRecord[9];
        int k = 0;
        
        PriorityType type = PriorityType.Essential;
        for (int i = 0; i < _dragDropArea.childCount; i++)
        {
            VisualElement vis = _dragDropArea[i];
            for (int j = 0; j < appliences.Length; j++)
            {
                if (vis.name.ToLower().Contains(appliences[j].ToString().ToLower()))
                {
                    _visualsDic[vis] = appliences[j];
                    _applienceOrder[k++] = new ApplienceRecord(appliences[j], type);
                    break;
                }
            }

            if (vis.name.ToLower().Contains("optional"))
            {
                type =  PriorityType.Optional;
            }
            if (vis.name.ToLower().Contains("undesired"))
            {
                type = PriorityType.Undesired;
            }
        }

        CleanOptional();
        AppliencePriorityChanged?.Invoke(_applienceOrder);
    }

    private void CleanOptional()
    {
        var visDictReverted = _visualsDic.ToDictionary(i => i.Value, i => i.Key);
        for (int i = 0; i < _applienceOrder.Length; i++)
        {
            VisualElement appVis = visDictReverted[_applienceOrder[i].Applience];
            if (_applienceOrder[i].Type == PriorityType.Optional && _optionalStates[_applienceOrder[i].Applience] && !_hideOptional)
                OpenOptionalSection(appVis, _applienceOrder[i].Applience);
            else
                CloseOptional(visDictReverted[_applienceOrder[i].Applience], _applienceOrder[i].Applience);

            if (_applienceOrder[i].Type != PriorityType.Optional || _hideOptional)
            {
                var threshold = appVis.Q<Button>("set_threshold");
                threshold.visible = false;
            }
            if (_applienceOrder[i].Type == PriorityType.Optional && !_hideOptional)
            {
                var threshold = appVis.Q<Button>("set_threshold");
                threshold.visible = true;
            }

        }
    }

    private void OpenOptionalSection(VisualElement vis, Applience id)
    {
        if (_hideOptional)
            return;
        _optionalStates[id] = true;
        var threshold = vis.Q<Button>("set_threshold");
        threshold.visible = true;
        threshold.text = "!Set battery threshold";
        vis.style.height = 114;
        var optional = vis.Q<VisualElement>("optional_section");
        if (optional != null)
            optional.visible = true;
    }

    private void SetPosition(int index, VisualElement visual)
    {
        index = index == 0 ? 1 : index >= _dragDropArea.childCount ? _dragDropArea.childCount - 1 : index;
        visual.style.position = new StyleEnum<Position>(Position.Relative);
        visual.style.top = 0;
        var offset = _dragDropArea.scrollOffset;
        _dragDropArea.Insert(index, visual);
        _dragDropArea.scrollOffset = offset;
    }

    public void SetMessage(string message)
    {
        _dialogWindow.visible = true;
        _dialogLabel.text += message + Environment.NewLine;
        _messageCount += 1;
        SetHeightBasedOnText();
        if (_clearMessageCoroutine != null)
        {
            StopCoroutine(_clearMessageCoroutine);
        }
        _clearMessageCoroutine = StartCoroutine(WaitAndClear());
    }

    private void SetHeightBasedOnText()
    {
        var count = Regex.Matches(_dialogLabel.text, Environment.NewLine).Count;

        count = count == 0 ? 2 : count;
        _dialogWindow.style.height = 100 + count * 43;
    }

    private IEnumerator WaitAndClear()
    {
        yield return new WaitForSeconds(2 * _messageCount);
        _dialogLabel.text = string.Empty;
        _clearMessageCoroutine = null;
        _dialogWindow.visible = false;
        _messageCount = 0;
    }

    public void SetApplienceToogle(Applience id, bool state)
    {
        _offGridScreen.SetApplienceToogle(id, state);
        _smartPowerScreen.SetApplienceToogle(id, state);
    }

    public void SetTotalPower(float power)
    {
        _offGridScreen.SetTotalPower(power);
        _smartPowerScreen.SetTotalPower(power);
    }

    public void SetBatteryPercentagePower(float powerLeft)
    {
        _offGridScreen.SetBatteryPowerLeftPercentage(powerLeft);
        _smartPowerScreen.SetBatteryPowerLeftPercentage(powerLeft);
    }
    
    public void SetBatteryPowerLeft(float powerLeft)
    {
        _offGridScreen.SetBatteryPowerLeft(powerLeft);
        _smartPowerScreen.SetBatteryPowerLeft(powerLeft);
    }

    public void SetPowerMoment(float powerMoment)
    {
        _offGridScreen.SetPowerMoment(powerMoment);
        _smartPowerScreen.SetPowerMoment(powerMoment);
    }

    public void SetDayTime(string time)
    {
        _dayTime.text = time;
    }

    public void SetPower(Applience appl, float power)
    {
        _offGridScreen.SetPower(appl, power);
        _smartPowerScreen.SetPower(appl, power);
    }

    public void SetSectionPowers(float essential, float optional, float undesired)
    {
        _offGridScreen.SetEssentialPower(essential);
        _smartPowerScreen.SetEssentialPower(essential);


        _offGridScreen.SetOptionalPower(optional);
        _smartPowerScreen.SetOptionalPower(optional);


        _offGridScreen.SetUndesiredPower(undesired);
        _smartPowerScreen.SetUndesiredPower(undesired);
    }

    public void SetPowerLimit(int powerMomentLimit)
    {
        _smartPowerScreen.SetPowerLimit(powerMomentLimit);
    }
}