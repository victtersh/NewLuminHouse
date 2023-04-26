using System;

public interface IGameSimulation
{
  public float Temperature { get;  } //F
  public float Hungriness { get;  }
  public float Happiness { get; }
  public float DayTime { get;  } //minutes
  public bool IsGameOver { get; }

  public bool IsGameWon { get; }
  float TimeScale { get; set; }
  float Battery { get; set; }

  void Iterate(float powerCons, float deltaTime);
    
  void SetGameOver();
}