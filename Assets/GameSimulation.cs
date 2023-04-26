using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class GameSimulation: IGameSimulation
{
  /// <summary>
  /// Value is 6 (3 pairs) element array in the following order Hung,Happiness,Temperature first element is for "on" applience state second is for "off"
  /// </summary>
  public static Dictionary<Applience, int[]> BalanceTable = new()
  {
    {Applience.Fridge, new int[]  {-5, 5, 0, 0, 0, 0 } },
    {Applience.Oven, new int[]    {-30, 10, 30, 0, 0, 0 } },
    {Applience.AirCond, new int[] {0, 0, 15, -5, 20, -5 } },
    {Applience.Washer, new int[]  {0, 0, 15, -10, 0, 0 } },
    {Applience.Dryer, new int[]   {0, 0, 15, -10, 0, 0 } },
    {Applience.Boiler, new int[]  {0, 0, 15, -10, 0, 0 } },
    {Applience.Lights, new int[]  {0, 0, 15, -5, 0, 0 } },
    {Applience.TV, new int[]      {0, 0, 30, 0, 0, 0 } },
    {Applience.Ev, new int[]      {0, 0, 0, 0, 0, 0 } },
  };

  private IApplienceManager _applienceManager;
  private int _defaultBattery;

  /// <summary>
  /// if TimeScale == 10 than 1 sec = 10 minutes
  /// </summary>
  public float TimeScale { get; set; }

  public float Battery { get; set; } //W

  public float Temperature { get; private set; } //F
  public float Hungriness { get; private set; }
  public float Happiness { get; private set; }
  public float DayTime { get; private set; } //minutes

  private float _totalGameTime;

  private bool _internalGameOver;

  public bool IsGameOver { get => Temperature <= 50 || Happiness == 0 || Hungriness == 100 || Battery == 0 || _internalGameOver; }

  public bool IsGameWon { get => Outage * 60 < _totalGameTime; }

  public int Outage { get; }

  public GameSimulation(IApplienceManager applienceManager,int outage,  Dictionary<Applience, int[]> applBalanceTable = null, int battery= 10000)
  {
    if (applBalanceTable != null)
      BalanceTable = applBalanceTable;

    _defaultBattery = battery;
    Battery = _defaultBattery;
    Temperature = 70;
    Hungriness = 0;
    Happiness = 100;
    DayTime = 660;
    TimeScale = 1;
    _applienceManager = applienceManager;
    Outage = outage;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="powerConsumed"></param>
  /// <param name="timeFromLastIteration">in seconds</param>
  public void Iterate(float powerConsumed, float timeFromLastIteration)
  {
    //6 sec = 1 h if TimeScale = 10; 1s = 10m

    var tempDelta = 0;
    var hungDelta = 0;
    var happinDelta = 0;

    DayTime += timeFromLastIteration * TimeScale;
    DayTime = DayTime % 1440;
    _totalGameTime += timeFromLastIteration * TimeScale;

    var appliences = ApplienceHelper.All();
    for (int i = 0; i < appliences.Length; i++)
    {
      var appl = appliences[i];
      bool isOn = _applienceManager.IsApplienceOn(appl);

      hungDelta += BalanceTable[appl][isOn ? 0 : 1];
      happinDelta += BalanceTable[appl][isOn ? 2 : 3];
      tempDelta += BalanceTable[appl][isOn ? 4 : 5];

    }
    Happiness += happinDelta * timeFromLastIteration * TimeScale / 60;
    Happiness = Math.Clamp(Happiness, 0, 100);
    Hungriness += hungDelta * timeFromLastIteration * TimeScale / 60;
    Hungriness = Math.Clamp(Hungriness, 0, 100);
    Temperature += tempDelta * timeFromLastIteration * TimeScale / 60;
    Temperature = Math.Clamp(Temperature, 50, 70);
    Battery -= powerConsumed * timeFromLastIteration * TimeScale / 60;
    Battery = Math.Clamp(Battery, 0, _defaultBattery);

  }
    
  public void SetGameOver()
  {
    _internalGameOver = true;
  }
}
