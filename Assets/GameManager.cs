using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;


public enum PriorityType
{
    Essential,
    Optional,
    Undesired
}

public struct ApplienceRecord
{
    public Applience Applience;
    public PriorityType Type;
    public bool IsEnabled;

    public ApplienceRecord(Applience app, PriorityType type, bool isEnabled = true)
    {
        IsEnabled = isEnabled;
        Applience = app;
        Type = type;
    }
}

public class ApplienceOrder
{
    public int GameMode;
    public ApplienceRecord[] Order;

    public ApplienceOrder(int gameMode)
    {
        GameMode = gameMode;
        Order = new ApplienceRecord[9] {
            new(Applience.Fridge,PriorityType.Essential),
            new(Applience.AirCond,PriorityType.Optional),
            new(Applience.Oven,PriorityType.Optional),
            new(Applience.Boiler,PriorityType.Undesired),
            new(Applience.Dryer,PriorityType.Undesired),
            new(Applience.Washer,PriorityType.Undesired),
            new(Applience.Lights,PriorityType.Undesired),
            new(Applience.Ev,PriorityType.Undesired),
            new(Applience.TV,PriorityType.Undesired),
        };

        if (gameMode == 3)
        {
            Order = new ApplienceRecord[9] {
                new(Applience.Ev,PriorityType.Essential,true),
                new(Applience.Fridge,PriorityType.Optional,true),
                new(Applience.AirCond,PriorityType.Undesired,false),
                new(Applience.Oven,PriorityType.Undesired,false),
                new(Applience.Boiler,PriorityType.Undesired,false),
                new(Applience.Dryer,PriorityType.Undesired,false),
                new(Applience.Washer,PriorityType.Undesired,false),
                new(Applience.Lights,PriorityType.Undesired,false),
                new(Applience.TV,PriorityType.Undesired,false),
            };
        }
    }

}

public class BalanceConfiguration
{
    public Dictionary<Applience, int[]> BalanceTable;

    public int Battery = 10000;

    public Dictionary<Applience, float> PowerConsumption = new()
    {
        {Applience.AirCond, 3500},
        {Applience.Boiler, 4500},
        {Applience.Dryer, 5000},
        {Applience.Ev, 9600},
        {Applience.Fridge, 225},
        {Applience.Lights, 150},
        {Applience.Oven, 2300},
        {Applience.TV, 100},
        {Applience.Washer, 800},
    };

    public int PowerMomentLimit = 6800;
}

public class GameManager : MonoBehaviour
{
    private const int EV_MODE_BATTERY = 19200;
    private int _powerLimitDefault = 6800;

    private Dictionary<int, ApplienceOrder> _orders = new();

   

    [FormerlySerializedAs("RoofObjects")] [SerializeField] private GameObject[] roofObjects;

    [SerializeField] private CameraController cameraController;

    [SerializeField] private UIManager uiManager;

    [SerializeField] private ApplienceManager applienceManager;

    [SerializeField] private Material nightSkybox;

    [SerializeField] private DayAndNightControl dayNightController;

    private IGameSimulation _gameSimulation;
    private BalanceConfiguration _config;

    private Dictionary<Applience, int> _optionalBatteryThreshold = new();


    // Start is called before the first frame update
    void Awake()
    {
        Application.targetFrameRate = 60;

        uiManager.ApplienceClicked += OnApplienceOnOffClicked;
        uiManager.NavigateClicked += OnNavigateClicked;
        uiManager.NewGameClicked += OnNewGameClicked;
        uiManager.AppliencePriorityChanged += AppOrderChanged;
        uiManager.OptionalBatteryChanged += OptionalBatteryThreeshold;
        uiManager.ApplienceStateChanged += OnApplienceSwitched;
        uiManager.StopSimulation += OnStopSimulation;
        uiManager.TimeScaleChanged += timeScale =>
        {
            if (_gameSimulation != null) _gameSimulation.TimeScale = timeScale;
        };

        try
        {
             var path = "config.json";
            var balanceString = File.ReadAllText(path);
            _config = JsonConvert.DeserializeObject<BalanceConfiguration>(balanceString);
            _powerLimitDefault = _config.PowerMomentLimit;
            
       
        }
        catch (Exception)
        {
            Debug.Log("Can't find the balance config file");
            _config = new BalanceConfiguration();
        }
        uiManager.applienceManager = applienceManager;
        applienceManager.Config = _config;

        _orders.Add(0, new ApplienceOrder(0));
        _orders.Add(1, new ApplienceOrder(1));
        _orders.Add(2, new ApplienceOrder(2));
        _orders.Add(3, new ApplienceOrder(3));
    }

    private void Start()
    {
        uiManager.GameMode.RegisterValueChangedCallback(OnGameModeChanged);
        applienceManager.SwitchApplience(Applience.Lights, applienceManager.ApplienceStates[Applience.Lights]);
        applienceManager.SwitchApplience(Applience.TV, applienceManager.ApplienceStates[Applience.TV]);
        foreach (var (app, power) in _config.PowerConsumption)
        {
            uiManager.SetPower(app, power);
        }
        uiManager.SetTotalPower(_config?.Battery ?? 10000);

        float powerCons = applienceManager.CalculatePowerConsumptionPerHour();

        uiManager.SetPowerMoment(powerCons);
        uiManager.SetPowerLimit(_powerLimitDefault);  
        uiManager.SetBatteryPowerLeft(_config?.Battery ?? 10000);
        cameraController.SwitchBetweenOrigins(Applience.Lights);

    }

    private void OnGameModeChanged(ChangeEvent<int> evt)
    {
        for (int i = 0; i < _orders[_gameMode].Order.Length; i++)
        {
            ApplienceOrder order = _orders[_gameMode];
            order.Order[i].IsEnabled = applienceManager.IsApplienceOn(order.Order[i].Applience);
            Debug.Log($"{order.Order[i].Applience} is {order.Order[i].IsEnabled}");
        }

        _gameMode = evt.newValue;

        for (int i = 0; i < _orders[_gameMode].Order.Length; i++)
        {
            ApplienceOrder order = _orders[_gameMode];
            SwitchApplienceAndUpdateUI(order.Order[i].Applience, order.Order[i].IsEnabled);
            uiManager.SetApplienceToogle(order.Order[i].Applience, order.Order[i].IsEnabled);
        }

        var applienceOrder = _orders[_gameMode].Order;
        _powerLimitDefault = _config?.PowerMomentLimit ?? 6000;
        if (evt.newValue == 3)
        {
            _powerLimitDefault = EV_MODE_BATTERY;
            for (int i = 0; i < applienceOrder.Length; i++)
            {
                Applience appl = applienceOrder[i].Applience;
                if (appl == Applience.Ev || appl == Applience.Fridge)
                    continue;
                SwitchApplienceAndUpdateUI(appl, false);
                uiManager.SetApplienceToogle(applienceOrder[i].Applience, false);
            }
        }
        uiManager.OrderAppliences(_orders[_gameMode]);
        uiManager.SetPowerLimit(_powerLimitDefault);
        SetPowers();
    }

    private void EnableOtherOptional(Applience id)
    {
        var applienceOrder = _orders[_gameMode].Order;

        var powerLimit = _powerLimitDefault;
        for (int i = 0; i < applienceOrder.Length; i++)
        {
            bool byUser = _disabledByUser.ContainsKey(applienceOrder[i].Applience) && _disabledByUser[applienceOrder[i].Applience];
            if (id == applienceOrder[i].Applience || applienceOrder[i].Type == PriorityType.Undesired || byUser)
                continue;

            SwitchApplienceAndUpdateUI(applienceOrder[i].Applience, true);
            uiManager.SetApplienceToogle(applienceOrder[i].Applience, true);
            if (applienceManager.CalculatePowerConsumptionPerHour() > powerLimit)
            {
                SwitchApplienceAndUpdateUI(applienceOrder[i].Applience, false);
                uiManager.SetApplienceToogle(applienceOrder[i].Applience, false);
                break;
            }
        }
    }

    private void OnNavigateClicked()
    {
        cameraController.SwitchBetweenOrigins(Applience.Lights);
    }

    private void OnStopSimulation()
    {
        foreach (var applienceState in _config.PowerConsumption)
        {
            SwitchApplienceAndUpdateUI(applienceState.Key, true);
            uiManager.SetApplienceToogle(applienceState.Key, true);
        }
        _gameSimulation = null;
    }

    private void AppOrderChanged(ApplienceRecord[] records)
    {
        
        if (_gameMode == 3)
        {
            int evIndex = Int32.MaxValue;
            for (int i = 0; i < records.Length; i++)
            {
                if (records[i].Applience == Applience.Ev)
                    evIndex = i;
                if (records[i].Type == PriorityType.Essential && i > evIndex)
                {
                    (records[i], records[evIndex]) = (records[evIndex], records[i]);
                    evIndex = i;
                }
            }
        }
        _orders[uiManager.GameMode.value].Order = records;
        uiManager.OrderAppliences(_orders[uiManager.GameMode.value]);
    }

    private void OnNewGameClicked(int gameMode, int outage)
    {
        _gameMode = gameMode;
        int outageHours = 6;
        switch (outage)
        {
            case 0:
                outageHours = 6;
                break;
            case 1:
                outageHours = 24;
                break;
            case 2:
                outageHours = 72;
                break;
        }

        if (_config != null)
        {
            _gameSimulation = new SmartGameSimulation(applienceManager, outageHours, _config.BalanceTable, _config.Battery);
        }
        else
            _gameSimulation = new SmartGameSimulation(applienceManager, outageHours);

        uiManager.SwitchScreen(gameMode != 0);
        _gameSimulation.TimeScale = uiManager.TimeScale;
        uiManager.SetMessage("Outage started!" + Environment.NewLine + "Try to survive!");

        var applienceOrder = _orders[_gameMode].Order;
        if (_gameSimulation is SmartGameSimulation)
        {
            for (int i = 0; i < applienceOrder.Length; i++)
            {
                if ((applienceOrder[i].Type == PriorityType.Undesired))
                {
                    uiManager.SetApplienceToogle(applienceOrder[i].Applience, false);
                    SwitchApplienceAndUpdateUI(applienceOrder[i].Applience, false);
                    uiManager.SetMessage($"Have disabled {applienceOrder[i].Applience} as not essential during an outage");
                }
            }
        }
    }

    public void OptionalBatteryThreeshold(Applience applience, int threeshold)
    {
        _optionalBatteryThreshold[applience] = threeshold;
    }

    private int _gameMode = 1;

    Dictionary<Applience, bool> _disabledByUser = new();

    private void OnApplienceSwitched(Applience applience, bool state)
    {
        _disabledByUser[applience] = !state;
        SwitchApplienceAndUpdateUI(applience, state);

      
        bool isSecurrityEnabled = (_gameSimulation is SmartGameSimulation) || _gameMode == 3;
        if (isSecurrityEnabled && _powerLimitDefault < applienceManager.CalculatePowerConsumptionPerHour())
        {
            SwitchApplienceAndUpdateUI(applience, false);
            uiManager.SetApplienceToogle(applience, false);
            uiManager.SetMessage($"Have disabled  {applience}, exceeding the moment power limit");
        }

    }
    private void OnApplienceOnOffClicked(Applience applience, bool isOn)
    {
        bool byUser = _disabledByUser.ContainsKey(applience) && _disabledByUser[applience];
        if (!isOn && !byUser)
        {
            SwitchApplienceAndUpdateUI(applience, true);
            uiManager.SetApplienceToogle(applience, true);
        }
        applienceManager.AppliencePowerOn(applience, isOn);
        uiManager.SetPower(applience, applienceManager.GetPower(applience));
       
        
        float powerCons = applienceManager.CalculatePowerConsumptionPerHour();
        uiManager.SetPowerMoment(powerCons);
        SetPowers();
        
        if(!isOn && (_gameMode == 3 || _gameSimulation is {IsGameOver: false}))
            EnableOtherOptional(applience);
        
        if (isOn &&  _gameMode == 3)
        {
            DisableFromTheBottom(_orders[_gameMode].Order, applienceManager.CalculatePowerConsumptionPerHour());
        }
    }

    private void SwitchApplienceAndUpdateUI(Applience applience, bool state)
    {
        if (state == applienceManager.IsApplienceOn(applience))
            return;
        applienceManager.SwitchApplience(applience, state);
        SwitchBetweenOriginsAnimated(applience);
        uiManager.SetPower(applience, applienceManager.GetPower(applience));

        float powerCons = applienceManager.CalculatePowerConsumptionPerHour();
        uiManager.SetPowerMoment(powerCons);

        SetPowers();
    }


    // Update is called once per frame
    void Update()
    {
        bool isVisible = cameraController.ZoomDistance > 10;
        for (int i = 0; i < roofObjects.Length; i++)
        {
            roofObjects[i].SetActive(isVisible);
        }
        uiManager.SetBatteryPercentagePower(100);
        uiManager.SetDayTime("12:00");

        var applienceOrder = _orders[_gameMode].Order;
        SetPowers();
        if (_gameSimulation != null && !_gameSimulation.IsGameOver && !_gameSimulation.IsGameWon)
        {
            float powerCons = applienceManager.CalculatePowerConsumptionPerHour();

            uiManager.SetPowerMoment(powerCons);
            DisableFromTheBottom(applienceOrder, powerCons);

            _gameSimulation.Iterate(powerCons, Time.deltaTime);
            uiManager.SetBatteryPercentagePower(Mathf.Ceil(_gameSimulation.Battery / _config.Battery * 100));
            uiManager.SetBatteryPowerLeft(_gameSimulation.Battery);

            var timeSpan = TimeSpan.FromMinutes(_gameSimulation.DayTime);
            uiManager.SetDayTime($"{timeSpan.Hours}:{timeSpan.Minutes} ");

            float batteryPercentage = _gameSimulation.Battery / _config.Battery * 100;


            if (_gameMode == 2)
                foreach (var pair in _optionalBatteryThreshold)
                {
                    if (pair.Value >= batteryPercentage)
                    {
                        if (applienceManager.IsApplienceOn(pair.Key))
                            uiManager.SetMessage($"Disabled {pair.Key}. Reached battery threshold limit {MathF.Round(pair.Value, 1)}%");
                        uiManager.SetApplienceToogle(pair.Key, false);
                        SwitchApplienceAndUpdateUI(pair.Key, false);
                    }
                }

            dayNightController.currentTime = _gameSimulation.DayTime / (24 * 60);

            if (_gameSimulation.IsGameOver)
            {
                string gameOverMessage = _gameSimulation.Battery == 0 ? "Your battery has run out of power" : "";
                uiManager.SetMessage("Simulation has been ended. " + gameOverMessage);
            }

            if (_gameSimulation.IsGameWon)
                uiManager.SetMessage("You survived an outage!");
        }

    }
    
    public void SwitchBetweenOriginsAnimated(Applience app)
    {
        _cameraSwitchList.Add(app);
    }


    private HashSet<Applience> _cameraSwitchList = new();
    private void LateUpdate()
    {
        _cameraSwitchList.Remove(Applience.Lights);
        if (_cameraSwitchList.Count == 0)
            return;
        
        List<List<Applience>> final = new();
        while (_cameraSwitchList.Count != 0)
        {
            List<Applience> cluster = new List<Applience>();
            foreach (var app in  _cameraSwitchList)
            {
                Transform r1 = cameraController._orbitsDict[_cameraSwitchList.First()].transform;
                Transform r2 = cameraController._orbitsDict[app].transform;

                if (Quaternion.Angle(r1.rotation, r2.rotation) < 60 &&
                    Vector3.Distance(r1.position, r2.position) < 0.2f)
                {
                    cluster.Add(app);
                }
            }

            for (int i = 0; i < cluster.Count; i++)
            {
                _cameraSwitchList.Remove(cluster[i]);
            }
            final.Add(cluster);
        }
        
        Sequence mySequence = DOTween.Sequence();
        if (_currentOnOffTwin != null && _currentOnOffTwin.IsPlaying())
        {
            _currentOnOffTwin.Complete(true);
        }

        for (int i = 0; i < final.Count; i++)
        {
            var tween = cameraController.SwitchBetweenOriginInternal(final[i].First(),0.5f);
            mySequence.Append(tween);
            for (int j = 0; j < final[i].Count; j++)
            {
                var colorTween = applienceManager.GetAnimatedOnOff(final[i][j], 2f);
                mySequence.Join(colorTween);
            }
        }
        mySequence.Append(cameraController.SwitchBetweenOriginInternal(Applience.Lights,0.5f));
        mySequence.Play();
        _currentOnOffTwin = mySequence;
        _cameraSwitchList.Clear();
    }

    private Tween _currentOnOffTwin;

    private void DisableFromTheBottom(ApplienceRecord[] applienceOrder, float powerCons)
    {
        var powerLimit = _powerLimitDefault;
        if (powerCons > powerLimit)
        {
            for (int i = applienceOrder.Length - 1; i >= 0; i--)
            {
                float powerConsumption = applienceManager.CalculatePowerConsumptionPerHour();
                if (applienceManager.CalculatePowerConsumptionPerHour() > powerLimit ||
                    applienceOrder[i].Type == PriorityType.Undesired)
                {
                    SwitchApplienceAndUpdateUI(applienceOrder[i].Applience, false);
                    uiManager.SetApplienceToogle(applienceOrder[i].Applience, false);
                }
                else break;
            }
        }
    }

    private void SetPowers()
    {
        var applienceOrder = _orders[_gameMode].Order;
        float essential = 0, optional = 0, undesired = 0;
        if (applienceOrder != null)
        {
            for (int i = 0; i < applienceOrder.Length; i++)
            {
                var appId = applienceOrder[i].Applience;
                switch (applienceOrder[i].Type)
                {
                    case PriorityType.Essential:
                        essential += applienceManager.GetPower(appId);
                        break;
                    case PriorityType.Optional:
                        optional += applienceManager.GetPower(appId);
                        break;
                    case PriorityType.Undesired:
                        undesired += applienceManager.GetPower(appId);
                        break;
                }
            }

            uiManager.SetSectionPowers(essential, optional, undesired);
        }
    }
}