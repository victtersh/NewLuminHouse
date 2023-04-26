using System;
using System.Collections.Generic;
using UnityEngine;

internal class SmartGameSimulation : IGameSimulation
{

  public Dictionary<Applience, int[]> BalanceTable = GameSimulation.BalanceTable;
  private IApplienceManager _applienceManager;

  public SmartGameSimulation(IApplienceManager applienceManager, int outage, Dictionary<Applience, int[]> applBalanceTable = null, int battery = 10000)
  {
    _applienceManager = applienceManager;
    Outage = outage;
    BalanceTable = applBalanceTable;
    Battery = battery;
    _defaultBattery = battery;

    Temperature = 70;
    Hungriness = 0;
    Happiness = 100;
    DayTime = 660;
    TimeScale = 1;

  }

  public float Temperature { get; private set; }

  public float Hungriness { get; private set; }

  public float Happiness { get; private set; }

  public float DayTime { get; private set; }

  private float _totalGameTime;

  public bool IsGameOver { get => Temperature <= 50 || Happiness == 0 || Hungriness == 100 || Battery == 0 || _internalGameOver; }
  
  public float TimeScale { get; set; }
  public int Outage { get; }
  public float Battery { get; set; }

  private int _defaultBattery;
  private Tuple<Applience, int>[] _applienceOrder;
  private bool _internalGameOver;

  public bool IsGameWon { get => Outage * 60 < _totalGameTime; }

  public void Iterate(float powerConsumed, float timeFromLastIteration)
  {
    //6 sec = 1 h if TimeScale = 10; 1s = 10m

    DayTime += timeFromLastIteration * TimeScale;
    DayTime = DayTime % 1440;
    _totalGameTime += timeFromLastIteration * TimeScale;

    Battery -= powerConsumed * timeFromLastIteration * TimeScale / 60;
    Battery = Mathf.Clamp(Battery, 0, _defaultBattery);
  }
    

  public void SetGameOver()
  {
    _internalGameOver = true;
  }
}