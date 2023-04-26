using System;
using UnityEngine.UIElements;

public class SmartPowerScreen : IScreen
{
    private Toggle _ovenToggle;
    private Toggle _tvToggle;
    private Toggle _washerToggle;
    private Toggle _dryerToggle;
    private Toggle _evToggle;
    private Toggle _fridgeToggle;
    private Toggle _heaterToggle;
    private Toggle _airCondToggle;
    private Toggle _lightToggle;

    private VisualElement _screen;

    private Label _ovenLabel;
    private Label _tvLabel;
    private Label _washerLabel;
    private Label _dryerLabel;
    private Label _evLabel;
    private Label _fridgeLabel;
    private Label _heaterLabel;
    private Label _airCondLabel;
    private Label _lightsLabel;

    private Label _powerMoment;
    private Label _batteryLifeLabel;
    private Label _powerLimit;
    private readonly Label _undesiredPower;
    private readonly Label _optionalPower;
    private readonly Label _essentialPower;
    private readonly Label _batteryLeftNumber;

    public SmartPowerScreen(VisualElement screen, Action<Applience, bool> applienceStateChanged)
    {
        _screen = screen;

        _essentialPower = _screen.Q<Label>("essential_power");
        _optionalPower = _screen.Q<Label>("optional_power");
        _undesiredPower = _screen.Q<Label>("undesired_power");

        _ovenLabel = _screen.Q<Label>("oven_power");
        _tvLabel = _screen.Q<Label>("tv_power");
        _washerLabel = _screen.Q<Label>("washer_power");
        _dryerLabel = _screen.Q<Label>("dryer_power");
        _evLabel = _screen.Q<Label>("ev_power");
        _fridgeLabel = _screen.Q<Label>("fridge_power");
        _heaterLabel = _screen.Q<Label>("heater_power");
        _airCondLabel = _screen.Q<Label>("air_cond_power");
        _lightsLabel = _screen.Q<Label>("lights_power");

        _powerMoment = _screen.Q<Label>("power_moment");
        _powerLimit = _screen.Q<Label>("power_limit");
        _batteryLifeLabel = _screen.Q<Label>("battery_left");

        _batteryLeftNumber = _screen.Q<Label>("battery_left_number");

        _ovenToggle = _screen.Q<Toggle>("oven_toggle");
        _tvToggle = _screen.Q<Toggle>("tv_toggle");
        _washerToggle = _screen.Q<Toggle>("washer_toggle");
        _dryerToggle = _screen.Q<Toggle>("dryer_toggle");
        _evToggle = _screen.Q<Toggle>("ev_toggle");
        _fridgeToggle = _screen.Q<Toggle>("fridge_toggle");
        _heaterToggle = _screen.Q<Toggle>("heater_toggle");
        _airCondToggle = _screen.Q<Toggle>("air_cond_toggle");
        _lightToggle = _screen.Q<Toggle>("lights_toggle");

        _ovenToggle.RegisterValueChangedCallback((evt) => { applienceStateChanged(Applience.Oven, evt.newValue); });
        _tvToggle.RegisterValueChangedCallback((evt) => { applienceStateChanged(Applience.TV, evt.newValue); });
        _washerToggle.RegisterValueChangedCallback((evt) => { applienceStateChanged(Applience.Washer, evt.newValue); });
        _dryerToggle.RegisterValueChangedCallback((evt) => { applienceStateChanged(Applience.Dryer, evt.newValue); });
        _evToggle.RegisterValueChangedCallback((evt) => { applienceStateChanged(Applience.Ev, evt.newValue); });
        _fridgeToggle.RegisterValueChangedCallback((evt) => { applienceStateChanged(Applience.Fridge, evt.newValue); });
        _heaterToggle.RegisterValueChangedCallback((evt) => { applienceStateChanged(Applience.Boiler, evt.newValue); });
        _airCondToggle.RegisterValueChangedCallback((evt) => { applienceStateChanged(Applience.AirCond, evt.newValue); });
        _lightToggle.RegisterValueChangedCallback((evt) => { applienceStateChanged(Applience.Lights, evt.newValue); });
    }

    public void SetApplienceToogle(Applience id, bool state)
    {
        switch (id)
        {
            case Applience.AirCond:
                _airCondToggle.SetValueWithoutNotify(state);
                break;
            case Applience.Boiler:
                _heaterToggle.SetValueWithoutNotify(state);
                break;
            case Applience.Dryer:
                _dryerToggle.SetValueWithoutNotify(state);
                break;
            case Applience.Ev:
                _evToggle.SetValueWithoutNotify(state);
                break;
            case Applience.Fridge:
                _fridgeToggle.SetValueWithoutNotify(state);
                break;
            case Applience.Lights:
                _lightToggle.SetValueWithoutNotify(state);
                break;
            case Applience.Oven:
                _ovenToggle.SetValueWithoutNotify(state);
                break;
            case Applience.TV:
                _tvToggle.SetValueWithoutNotify(state);
                break;
            case Applience.Washer:
                _washerToggle.SetValueWithoutNotify(state);
                break;
        }
    }

    public void SetPower(Applience appl, float power)
    {
        switch (appl)
        {
            case Applience.AirCond:
                _airCondLabel.text = $"{power} W";
                break;
            case Applience.Boiler:
                _heaterLabel.text = $"{power} W";
                break;
            case Applience.Dryer:
                _dryerLabel.text = $"{power} W";
                break;
            case Applience.Ev:
                _evLabel.text = $"{power} W";
                break;
            case Applience.Fridge:
                _fridgeLabel.text = $"{power} W";
                break;
            case Applience.Lights:
                _lightsLabel.text = $"{power} W";
                break;
            case Applience.Oven:
                _ovenLabel.text = $"{power} W";
                break;
            case Applience.TV:
                _tvLabel.text = $"{power} W";
                break;
            case Applience.Washer:
                _washerLabel.text = $"{power} W";
                break;
        }
    }
    public void SetBatteryPowerLeftPercentage(float powerLeft)
    {
         _batteryLifeLabel.text = $"{(int)(powerLeft)}%";
    }


    public void SetOptionalPower(float power)
    {
        _optionalPower.text = (int)power + "W";
    }

    public void SetEssentialPower(float power)
    {
        _essentialPower.text = (int)power + "W";
    }

    public void SetUndesiredPower(float power)
    {
        _undesiredPower.text = (int)power + "W";
    }

    public void SetPowerMoment(float powerMoment)
    {
        _powerMoment.text = $"{(int)powerMoment}W";
    }

    public void SetTotalPower(float power)
    {
    }

    public void SetPowerLimit(float powerLimit)
    {
        _powerLimit.text = "Limit " + (int)powerLimit + "W";
    }

    public void SetBatteryPowerLeft(float powerLeft)
    {
        _batteryLeftNumber.text = (powerLeft / 1000).ToString("N1") + " kWh remaining";
    }
}
