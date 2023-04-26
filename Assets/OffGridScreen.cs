using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

public class OffGridScreen : IScreen
{

    public class BottomMenu
    {
        private bool _isShown = false;
        private readonly VisualElement _root;
        private readonly VisualElement _energyInfo;
        private readonly Toggle _enagleAppToggle;
        private readonly Label _applienceName;
        private readonly Label _powerConsumption;
        private readonly Label _onOffStatus;
        private readonly Label _appCategory;
        private int _pageIndex = 0;
        private Dictionary<Applience, Texture2D[]> _energyImages = new();
        private string _powerConsString;

        public BottomMenu(VisualElement root)
        {
            _root = root;
            _enagleAppToggle = root.Q<Toggle>("toggle");
            _applienceName = root.Q<Label>("applienceName");
            _powerConsumption = root.Q<Label>("powerCons");
            _energyInfo = root.Q<VisualElement>("distribution");
            _energyInfo.RegisterCallback<MouseUpEvent>(evt =>CarouselDistribution());
            _onOffStatus = root.Q<Label>("on_off_status");
            _appCategory = root.Q<Label>("app_category");
            _enagleAppToggle.RegisterValueChangedCallback((evt) =>
            {
                ToggleChanged?.Invoke(evt.newValue);
                if (!evt.newValue)
                    _powerConsumption.text = "0W";
                else
                    _powerConsumption.text = _powerConsString;
            });

            _root.RegisterCallback<MouseUpEvent>((evt) =>
            {
                bool isIgnored = evt.target == _energyInfo;
                if(isIgnored)
                    return;
                ShowHide(!_isShown, false);
            });

            LoadImages();


            CarouselDistribution();
        }

        public bool IsShown => _isShown;

        private void LoadImages()
        {
            _energyImages[Applience.Oven] = new Texture2D[3];
            _energyImages[Applience.Oven][0] = Resources.Load<Texture2D>(@"Images\power_distribution\range_1");
            _energyImages[Applience.Oven][1] = Resources.Load<Texture2D>(@"Images\power_distribution\range_2");
            _energyImages[Applience.Oven][2] = Resources.Load<Texture2D>(@"Images\power_distribution\range_3");
            
            
            _energyImages[Applience.Boiler] = new Texture2D[3];
            _energyImages[Applience.Boiler][0] = Resources.Load<Texture2D>(@"Images\power_distribution\water_heater_1");
            _energyImages[Applience.Boiler][1] = Resources.Load<Texture2D>(@"Images\power_distribution\water_heater_2");
            _energyImages[Applience.Boiler][2] = Resources.Load<Texture2D>(@"Images\power_distribution\water_heater_3");
            
            
            _energyImages[Applience.Dryer] = new Texture2D[3];
            _energyImages[Applience.Dryer][0] = Resources.Load<Texture2D>(@"Images\power_distribution\dryer_1");
            _energyImages[Applience.Dryer][1] = Resources.Load<Texture2D>(@"Images\power_distribution\dryer_2");
            _energyImages[Applience.Dryer][2] = Resources.Load<Texture2D>(@"Images\power_distribution\dryer_3");
            
            _energyImages[Applience.Ev] = new Texture2D[3];
            _energyImages[Applience.Ev][0] = Resources.Load<Texture2D>(@"Images\power_distribution\ev_1");
            _energyImages[Applience.Ev][1] = Resources.Load<Texture2D>(@"Images\power_distribution\ev_2");
            _energyImages[Applience.Ev][2] = Resources.Load<Texture2D>(@"Images\power_distribution\ev_3");
            
            _energyImages[Applience.Fridge] = new Texture2D[3];
            _energyImages[Applience.Fridge][0] = Resources.Load<Texture2D>(@"Images\power_distribution\fridge_1");
            _energyImages[Applience.Fridge][1] = Resources.Load<Texture2D>(@"Images\power_distribution\fridge_2");
            _energyImages[Applience.Fridge][2] = Resources.Load<Texture2D>(@"Images\power_distribution\fridge_3");
            
            _energyImages[Applience.Lights] = new Texture2D[3];
            _energyImages[Applience.Lights][0] = Resources.Load<Texture2D>(@"Images\power_distribution\lights_1");
            _energyImages[Applience.Lights][1] = Resources.Load<Texture2D>(@"Images\power_distribution\lights_2");
            _energyImages[Applience.Lights][2] = Resources.Load<Texture2D>(@"Images\power_distribution\lights_3");
            
            _energyImages[Applience.Washer] = new Texture2D[3];
            _energyImages[Applience.Washer][0] = Resources.Load<Texture2D>(@"Images\power_distribution\washer_1");
            _energyImages[Applience.Washer][1] = Resources.Load<Texture2D>(@"Images\power_distribution\washer_2");
            _energyImages[Applience.Washer][2] = Resources.Load<Texture2D>(@"Images\power_distribution\washer_3");
            
            _energyImages[Applience.AirCond] = new Texture2D[3];
            _energyImages[Applience.AirCond][0] = Resources.Load<Texture2D>(@"Images\power_distribution\heat_pump_1");
            _energyImages[Applience.AirCond][1] = Resources.Load<Texture2D>(@"Images\power_distribution\heat_pump_2");
            _energyImages[Applience.AirCond][2] = Resources.Load<Texture2D>(@"Images\power_distribution\heat_pump_3");

            _energyImages[Applience.TV] = new Texture2D[3];
            _energyImages[Applience.TV][0] = Resources.Load<Texture2D>(@"Images\power_distribution\tv_1");
            _energyImages[Applience.TV][1] = Resources.Load<Texture2D>(@"Images\power_distribution\tv_2");
            _energyImages[Applience.TV][2] = Resources.Load<Texture2D>(@"Images\power_distribution\tv_3");
            
        }

        private void CarouselDistribution()
        {
            VisualElement prevImage = GetPointVisual(_pageIndex);
            prevImage.style.backgroundColor = new StyleColor(HexToColorConverter("565B63"));
            
            _pageIndex = ++_pageIndex % 3;
            _energyInfo.style.backgroundImage = new StyleBackground(_energyImages[_currentApplience][_pageIndex]);
            
            VisualElement activeImage = GetPointVisual(_pageIndex);
            activeImage.style.backgroundColor = new StyleColor(HexToColorConverter("9F9F9F"));
        }
        
        private Color HexToColorConverter(string hex)
        {
            float r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
            float g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
            float b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;

            return new Color(r, g, b);
        }

        private VisualElement GetPointVisual(int index)
        {
            string pointVisualName = $"bottom_point_{index+1}";
            return _root.Q<VisualElement>(pointVisualName);
        }

        public void SetApplience(Applience app, string powerConsumption, bool isOn, string category)
        {
            _currentApplience = app;
            _applienceName.text = GetName(app);
            _powerConsString = powerConsumption;
            _powerConsumption.text = powerConsumption;
            _enagleAppToggle.value = isOn;
            _onOffStatus.text = isOn ? "On" : "Off";
            _appCategory.text = category;
            _energyInfo.style.backgroundImage = new StyleBackground(_energyImages[_currentApplience][_pageIndex]);
        }

        private string GetName(Applience app)
        {
            if (app == Applience.Fridge)
                return "Refrigerator";
            if (app == Applience.Ev)
                return "EV Charger";
            if (app == Applience.AirCond)
                return "A/C";

            if (app == Applience.Boiler)
                return "Water heater";
            
            return app.ToString();
        }

        public void ShowHide(bool isShown, bool isAnimated)
        {
            _isShown = isShown;
            if (!isAnimated)
            {
                _root.style.visibility = isShown ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public event Action<bool> ToggleChanged;
    }
    
    private VisualElement _screen;

    private BottomMenu _bottomMenu;

    private Label _ovenLabel;
    private Label _tvLabel;
    private Label _washerLabel;
    private Label _dryerLabel;
    private Label _evLabel;
    private Label _fridgeLabel;
    private Label _heaterLabel;
    private Label _airCondLabel;
    private Label _lightsLabel;

    private VisualElement _fridgeVisual;
    private VisualElement _airCondVisual;
    private VisualElement _tvVisual;
    private VisualElement _ovenVisual;
    private VisualElement _dryerVisual;
    private VisualElement _washerVisual;
    private VisualElement _evVisual;
    private VisualElement _lightsVisual;
    private VisualElement _boilerVisual;

    private Label _towerSupply;
    private Label _powerMoment;

    private Label _batteryLifeLabel;
    private VisualElement _offgridCircle;
    private Label _offgridStatus;
    private Label _offgridMessage;
    private VisualElement _towerArrow;
    private Label _totalPower;

    private Dictionary<Applience, bool> _states = new()
  {
    {Applience.AirCond, true },
    {Applience.Boiler,true },
    {Applience.Dryer, true },
    {Applience.Ev, true},
    {Applience.Fridge,true },
    {Applience.Lights,true },
    {Applience.Oven,true },
    {Applience.TV,true },
    {Applience.Washer, true },
  };

    private static Applience _currentApplience;
    private readonly Action<Applience, bool> _applienceStateChanged;
    private Dictionary<Applience, string> _powers = new();

    public OffGridScreen(VisualElement screen, Action<Applience, bool> applienceStateChanged)
    {
        _screen = screen;
        _applienceStateChanged = applienceStateChanged;

        _fridgeVisual = _screen.Q<VisualElement>("fridge");
        _airCondVisual = _screen.Q<VisualElement>("air_cond");
        _tvVisual = _screen.Q<VisualElement>("tv");
        _ovenVisual = _screen.Q<VisualElement>("oven");
        _dryerVisual = _screen.Q<VisualElement>("dryer");
        _washerVisual = _screen.Q<VisualElement>("washer");
        _evVisual = _screen.Q<VisualElement>("ev_charger");
        _lightsVisual = _screen.Q<VisualElement>("lights");
        _boilerVisual = _screen.Q<VisualElement>("water_heater");

        _ovenLabel = _ovenVisual.Q<Label>("power");
        _tvLabel = _tvVisual.Q<Label>("power");
        _washerLabel = _washerVisual.Q<Label>("power");
        _dryerLabel = _dryerVisual.Q<Label>("power");
        _evLabel = _evVisual.Q<Label>("power");
        _fridgeLabel = _fridgeVisual.Q<Label>("power");
        _heaterLabel = _boilerVisual.Q<Label>("power");
        _airCondLabel = _airCondVisual.Q<Label>("power");
        _lightsLabel = _lightsVisual.Q<Label>("power");

        // _powerMoment = _screen.Q<Label>("power_moment");
        _totalPower = _screen.Q<Label>("total_power");
        _batteryLifeLabel = _screen.Q<Label>("battery_left");
        _powerMoment = _screen.Q<Label>("power_moment");
        _towerSupply = _screen.Q<Label>("tower_supply");

        _offgridCircle = _screen.Q<VisualElement>("offgrid_status_circle");
        _offgridStatus = _screen.Q<Label>("offgrid_status_label");
        _offgridMessage = _screen.Q<Label>("offgrid_status_message");
        _towerArrow = _screen.Q<VisualElement>("tower_arrow");

        _bottomMenu = new BottomMenu(_screen.Q<VisualElement>("bottom_menu"));
        _bottomMenu.ShowHide(false, false);
        _bottomMenu.ToggleChanged += OnBottomMenuToggleChanged;

        _ovenVisual.RegisterCallback<MouseUpEvent>((evt) => { _currentApplience = Applience.Oven; ShowHideBottomMenu(true,false); });
        _tvVisual.RegisterCallback<MouseUpEvent>((evt) => { _currentApplience = Applience.TV; ShowHideBottomMenu(true,false); });
        _washerVisual.RegisterCallback<MouseUpEvent>((evt) => { _currentApplience = Applience.Washer; ShowHideBottomMenu(true,false); });
        _dryerVisual.RegisterCallback<MouseUpEvent>((evt) =>  { _currentApplience = Applience.Dryer; ShowHideBottomMenu(true,false); });
        _evVisual.RegisterCallback<MouseUpEvent>((evt) =>  { _currentApplience = Applience.Ev; ShowHideBottomMenu(true,false); });
        _fridgeVisual.RegisterCallback<MouseUpEvent>((evt) =>  { _currentApplience = Applience.Fridge; ShowHideBottomMenu(true,false); });
        _boilerVisual.RegisterCallback<MouseUpEvent>((evt) =>  { _currentApplience = Applience.Boiler; ShowHideBottomMenu(true,false); });
        _airCondVisual.RegisterCallback<MouseUpEvent>((evt) =>  { _currentApplience = Applience.AirCond; ShowHideBottomMenu(true,false); });
        _lightsVisual.RegisterCallback<MouseUpEvent>((evt) =>  { _currentApplience = Applience.Lights; ShowHideBottomMenu(true,false); });
    }

    private void OnBottomMenuToggleChanged(bool state)
    {
        SetApplienceToogle(_currentApplience, !_states[_currentApplience]); _applienceStateChanged(_currentApplience, _states[_currentApplience]);
        
        
    }


    public void SetOffgrid(bool state)
    {
        if (state)
        {
            _offgridCircle.style.backgroundColor = new StyleColor(new UnityEngine.Color(111 / 255f, 179 / 255f, 132 / 255f, 1));
            _offgridStatus.text = "On grid";
            _offgridMessage.text = "System normal";
            _offgridMessage.style.color = new StyleColor(new UnityEngine.Color(128 / 255f, 134 / 255f, 139 / 255f, 1));
            _towerArrow.visible = true;
        }
        else
        {
            _offgridCircle.style.backgroundColor = new StyleColor(new UnityEngine.Color(196 / 255f, 196 / 255f, 196 / 255f, 1));
            _offgridStatus.text = "Off grid";
            _offgridMessage.text = "Battery powering home";
            _offgridMessage.style.color = new StyleColor(new UnityEngine.Color(244 / 255f, 181 / 255f, 74 / 255f, 1));
            _towerArrow.visible = false;
        }
    }

    public void ShowHideBottomMenu(bool isShown, bool isAnimated)
    {
        _bottomMenu.ShowHide(isShown, isAnimated);
        if (_powers.ContainsKey(_currentApplience))
        {
            string sPower = _powers[_currentApplience];
            _bottomMenu.SetApplience(_currentApplience, sPower, _states[_currentApplience], "");
        }
    }

    public void SetApplienceToogle(Applience id, bool state)
    {
        _states[id] = state;
        SwitchOffgridItemStyle(id);
    }

    private void SwitchOffgridItemStyle(Applience appl)
    {
        string className = _states[appl] ? "offgrid-item-on" : "offgrid-item-off";
        switch (appl)
        {
            case Applience.AirCond:
                _airCondVisual.ClearClassList();
                _airCondVisual.AddToClassList(className);
                break;
            case Applience.Boiler:
                _boilerVisual.ClearClassList();
                _boilerVisual.AddToClassList(className);
                break;
            case Applience.Dryer:
                _dryerVisual.ClearClassList();
                _dryerVisual.AddToClassList(className);
                break;
            case Applience.Ev:
                _evVisual.ClearClassList();
                _evVisual.AddToClassList(className);
                break;
            case Applience.Fridge:
                _fridgeVisual.ClearClassList();
                _fridgeVisual.AddToClassList(className);
                break;
            case Applience.Lights:
                _lightsVisual.ClearClassList();
                _lightsVisual.AddToClassList(className);
                break;
            case Applience.Oven:
                _ovenVisual.ClearClassList();
                _ovenVisual.AddToClassList(className);
                break;
            case Applience.TV:
                _tvVisual.ClearClassList();
                _tvVisual.AddToClassList(className);
                break;
            case Applience.Washer:
                _washerVisual.ClearClassList();
                _washerVisual.AddToClassList(className);
                break;
        }
    }

    public void SetPower(Applience appl, float power)
    {
        string sPower = $"{power} W";
        _powers[appl] = sPower;
        switch (appl)
        {
            case Applience.AirCond:
                _airCondLabel.text = sPower;
                break;
            case Applience.Boiler:
                _heaterLabel.text = sPower;
                break;
            case Applience.Dryer:
                _dryerLabel.text = sPower;
                break;
            case Applience.Ev:
                _evLabel.text = sPower;
                break;
            case Applience.Fridge:
                _fridgeLabel.text = sPower;
                break;
            case Applience.Lights:
                _lightsLabel.text = sPower;
                break;
            case Applience.Oven:
                _ovenLabel.text = sPower;
                break;
            case Applience.TV:
                _tvLabel.text = sPower;
                break;
            case Applience.Washer:
                _washerLabel.text = sPower;
                break;
        }

    }
    public void SetBatteryPowerLeftPercentage(float powerLeft)
    {
        _batteryLifeLabel.text = $"{(int)(powerLeft)}%";
    }

    public void SetOptionalPower(float power)
    {
    }

    public void SetEssentialPower(float power)
    {
    }

    public void SetUndesiredPower(float power)
    {
    }


    public void SetPowerMoment(float powerMoment)
    {
        float powerM = powerMoment / 1000;
        _powerMoment.text = $"{powerM:0.0} kWh";
        _towerSupply.text = $"{powerM - 1.3:0.0} kWh";

    }

    public void SetTotalPower(float power)
    {
        _totalPower.text = ((int)power).ToString();
    }

    public void SetBatteryPowerLeft(float powerLeft)
    {
    }
}
