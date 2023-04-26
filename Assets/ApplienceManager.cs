using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public enum Applience
{
    Fridge,
    TV,
    Washer,
    Dryer,
    Ev,
    Boiler,
    Lights,
    Oven,
    AirCond,
}

public interface IApplienceManager
{
    bool IsApplienceOn(Applience applience);
}

public static class ApplienceHelper
{
    private static Applience[] _applienceIDs;
    public static Applience[] All()
    {
        if (_applienceIDs == null)
            _applienceIDs = Enum.GetValues(typeof(Applience)).Cast<Applience>().Where(i => i >= 0).ToArray();

        return _applienceIDs;
    }
}


public class ApplienceManager : MonoBehaviour, IApplienceManager
{
    private Dictionary<Applience, Material> _materials = new();
    public Dictionary<Applience, bool> ApplienceStates = new();
    private Dictionary<Applience, bool> _appliencePowerOn = new();
    private Dictionary<Applience, float> _outlines = new();
    private Color defaultColor;

    [SerializeField] private Color DISABLE_APPLIENCE_COLOR;
    [SerializeField] private Color ENABLE_APPLIENCE_COLOR; 

    [FormerlySerializedAs("_tvTextures")] [SerializeField] private Texture[] tvTextures;

    [FormerlySerializedAs("Fridge")] public GameObject fridge;
    [FormerlySerializedAs("Oven")] public GameObject oven;
    [FormerlySerializedAs("Lights")] public Light[] lights;
    [FormerlySerializedAs("Washer")] public GameObject washer;
    [FormerlySerializedAs("Dryer")] public GameObject dryer;
    [FormerlySerializedAs("EV")] public GameObject ev;
    [FormerlySerializedAs("TV")] public GameObject tv;
    [FormerlySerializedAs("Boiler")] public GameObject boiler;
    [FormerlySerializedAs("AirCond")] public GameObject airCond;
    
    
    private Applience[] _applienceIDs;
    private Material _lcdMaterial;
    public BalanceConfiguration Config { get; set; }

    // Start is called before the first frame update
    void Awake()
    {
        _applienceIDs = ApplienceHelper.All();
        var fridgeMaterial = fridge.GetComponentsInChildren<MeshRenderer>().SelectMany(i => i.materials).Where(i => i.name.Contains("Outline")).FirstOrDefault();
        _materials.Add(Applience.Fridge, fridgeMaterial);

        var ovenMaterial = oven.GetComponentsInChildren<MeshRenderer>().SelectMany(i => i.materials).Where(i => i.name.Contains("Outline")).FirstOrDefault();
        _materials.Add(Applience.Oven, ovenMaterial);

        var tvMaterial = tv.GetComponentsInChildren<MeshRenderer>().SelectMany(i => i.materials).Where(i => i.name.Contains("Outline")).FirstOrDefault();
        _materials.Add(Applience.TV, tvMaterial);

        _lcdMaterial = tv.GetComponentsInChildren<MeshRenderer>().SelectMany(i => i.materials).Where(i => i.name.Contains("LCD")).FirstOrDefault();

        var dryerMaterial = dryer.GetComponentsInChildren<MeshRenderer>().FirstOrDefault().material;
        _materials.Add(Applience.Dryer, dryerMaterial);

        var washerMaterial = washer.GetComponentsInChildren<MeshRenderer>().FirstOrDefault().material;
        _materials.Add(Applience.Washer, washerMaterial);

        var boilerMaterial = boiler.GetComponentsInChildren<MeshRenderer>().SelectMany(i => i.materials).Where(i => i.name.Contains("Outline")).FirstOrDefault();
        _materials.Add(Applience.Boiler, boilerMaterial);

        var evMaterial = ev.GetComponentsInChildren<MeshRenderer>().SelectMany(i => i.materials).Where(i => i.name.Contains("Outline")).FirstOrDefault();
        _materials.Add(Applience.Ev, evMaterial);

        var airCond = this.airCond.GetComponentsInChildren<MeshRenderer>().SelectMany(i => i.materials).Where(i => i.name.Contains("Outline")).FirstOrDefault();
        _materials.Add(Applience.AirCond, airCond);

        defaultColor = _materials[Applience.Fridge].GetColor("_OutlineColor");

        for (int i = 0; i < _applienceIDs.Length; i++)
        {
            _appliencePowerOn[_applienceIDs[i]] = true;
            ApplienceStates[_applienceIDs[i]] = true;
            _outlines[_applienceIDs[i]] = 0f;
        }

    }

    internal float CalculatePowerConsumptionPerHour()
    {
        float powerConsumed = 0;
        for (int i = 0; i < _applienceIDs.Length; i++)
        {
            Applience applienceID = _applienceIDs[i];
            if (ApplienceStates[_applienceIDs[i]] && _appliencePowerOn[_applienceIDs[i]])
            {
                powerConsumed += Config.PowerConsumption[applienceID];
            }
        }
        return powerConsumed;
    }

    // Update is called once per frame
    void Update()
    {


    }

    void FixedUpdate()
    {
        for (int i = 0; i < _applienceIDs.Length; i++)
        {
            Applience applienceID = _applienceIDs[i];

            if (applienceID == Applience.Lights)
            {
                continue;
            }

            bool appState = ApplienceStates[applienceID] && _appliencePowerOn[applienceID];
            if(appState)
                _materials[applienceID].SetFloat("_OutlineWidth", GetOutlineOverTime(applienceID, appState));
        }
    }

    private Dictionary<Applience, int> _directions = new() {
    {Applience.TV,1 },
    {Applience.AirCond,1 },
    {Applience.Boiler,1 },
    {Applience.Dryer,1 },
    {Applience.Washer,1 },
    {Applience.Ev,1 },
    {Applience.Fridge,1 },
    {Applience.Lights,1 },
    {Applience.Oven,1 }
  };

    private Dictionary<Applience, Tween> _currentTweens = new Dictionary<Applience, Tween>();
    public Tween GetAnimatedOnOff(Applience id,float duration)
    {
        float maxRange = id == Applience.Ev  ?  0.0015f : 0.05f;
        bool appState = ApplienceStates[id] && _appliencePowerOn[id];
        float singleDuretion = 0.5f;  
        int loopCount = Mathf.RoundToInt(duration / singleDuretion);

        _materials[id].SetColor("_OutlineColor", appState ? ENABLE_APPLIENCE_COLOR : DISABLE_APPLIENCE_COLOR);
        var tween  =  _materials[id].DOFloat(maxRange, "_OutlineWidth", singleDuretion).SetLoops(loopCount);
        tween.onComplete += () =>
        {
            _materials[id].SetColor("_OutlineColor", defaultColor);
            _materials[id].SetFloat("_OutlineWidth", 0);
        };
        return tween;
    }
   
    private float GetOutlineOverTime(Applience id, bool state)
    {
        float maxRange = 0.05f;
        float increment = 0.0026f;

        if (id == Applience.Ev)
        {
            maxRange = 0.0015f;
            increment /= 13;
        }

        if (state)
        {
            var direction = _directions[id];
            _outlines[id] += increment * direction;
            if (_outlines[id] > maxRange || _outlines[id] < 0)
            {
                _directions[id] *= -1;
                _outlines[id] = Mathf.Clamp(_outlines[id], 0, maxRange);
            }
            return Mathf.Abs(_outlines[id]);
        }
        else
        {
            _outlines[id] = 0;
            return 0;
        }
    }

    private Coroutine _tvCoroutine;

    public void SwitchApplience(Applience applience, bool state)
    {
        if(applience == Applience.Lights){
            HandleApplienceGo(applience);
            ApplienceStates[applience] = state;
        }
        
        if (!_materials.ContainsKey(applience))
            return;
        ApplienceStates[applience] = state;
        HandleApplienceGo(applience);
        if (!state)
            _materials[applience].SetFloat("_OutlineWidth", 0);
    }

    private void HandleApplienceGo(Applience applience)
    {
        bool state = ApplienceStates[applience] && _appliencePowerOn[applience];
        if (applience == Applience.Lights)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].enabled = !state;
            }
        }

        if (applience == Applience.TV)
        {
            _lcdMaterial.SetColor("_Color", state ? Color.white : Color.black);
            if (!state && _tvCoroutine != null)
                StopCoroutine(_tvCoroutine);
            else
                _tvCoroutine = StartCoroutine(StartTV());
        }
    }

    private IEnumerator StartTV()
    {
        uint i = 0;
        while (true)
        {
            _lcdMaterial.SetTexture("_MainTex", tvTextures[i++ % tvTextures.Length]);
            yield return new WaitForSeconds(1);
        }
    }


    public bool IsApplienceOn(Applience applience)
    {
        return ApplienceStates[applience];
    }

    public void AppliencePowerOn(Applience applience, bool isPower)
    {
        _appliencePowerOn[applience] = isPower;
        HandleApplienceGo(applience);
        if (!isPower)
            _materials[applience].SetFloat("_OutlineWidth", 0);
    }

    public float GetPower(Applience applience)
    {
        bool enabled = _appliencePowerOn[applience] && ApplienceStates[applience];
        return enabled ? Config.PowerConsumption[applience] : 0;
    }
}