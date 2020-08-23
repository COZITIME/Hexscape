﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public sealed class GameStateEndless : GameStateBase
{
    private const int energyIncreaseHexDig = 2;
    private const int energyIncreaseLevelDigBelow = 15;
    private const int energyIncreaseLevelDigAbove = 40;
    private float tier = 0;

    private TierSpeedLogCurve speedCurve = new TierSpeedLogCurve();

    [SerializeField] public Level[] levels;

    [Space(5f)]
    [Header("Options:")]
    [Space(1f)]
    [Header("   - Map Options:")]
    public bool useRandomLevels = false;
    private bool editMode = false; // TEMP - TODO: Create 'EditModeState'

    [Space(3f)]
    [Header("   - Player Options:")]
    //public float initialPlayerSpeed = 30f; // Replaced with initial Energy value
    public float playerSpeedIncreaseLogBase = 20f;
    public float playerSpeedIncreaseLogMultiplyer = 45f;

    public float playerKillzoneOffset = 40f;

    float initialEnergy = 30.0f;
    float initalEnergyAcceleration = 0.5f;

    #region PostProcessingAttributes
    // NOTE: could be moved to a struct - could permit the manager to update levels on behalf of state?
    float minSaturation = -70;
    float maxSaturation = 20;

    float minBrightness = -50;
    float maxBrightness = 50;
    #endregion


    HexTunnelEnergy energyMetreTunnel;

    PlayerController playerController;

    EnergyMetre energyMetre;

    public GameStateEndless()
    {
        InitialiseStateTransitions();
        GameManager.instance.SetIngameHudActive(true);
        energyMetre = new EnergyMetre(initialEnergy);

        energyMetre.maxEnergyReached.AddListener(() =>
        {
            MaxEnergyReachedListener();
            //HandleRegisterClick();
            //GameManager.instance.ProcessCommand(commandToCall);
        });

        InitialiseClickSounds();
    }

    public override void StateUpdate()
    {
        energyMetre.DrainEmergy();

        //GameManager.instance.scoreUI.SetFillValue(  energyMetre.GetCurrentEnergyNormalised());
        energyMetreTunnel.SetEnergyFill(energyMetre.GetBarFill(), energyMetre.GetBarFill());


        UpdatePostProcesser();

        if (((currentSessionData.levelScore < currentSessionData.passScore)) && playerController.gameObject.transform.position.y < MapSpawner.Instance.GetCurrentMapHolder().transform.position.y)
        {
            ColourManager.instance.SetGrayPallet();
        } else
        {
            ColourManager.instance.SetGrayPallet(false);
        }
        if (playerController.gameObject.transform.position.y < MapSpawner.Instance.GetCurrentMapHolder().transform.position.y - playerKillzoneOffset)
        {
            LoadNextLevel();
            //GameManager.instance.ProcessCommand(Command.End);
        }


        //   playerController.moveSpeed = /*initialPlayerSpeed +*/ (energyMetre.GetCurrentEnergy() * (playerSpeedIncreaseLogMultiplyer * Mathf.Log(playerSpeedIncreaseLogBase)));
        float nplayerSpeed = /*playerSpeedIncreaseLogBase + */speedCurve.GetY(energyMetre.currentEnergyTier) * playerSpeedIncreaseLogMultiplyer;
        if (playerController.moveSpeed != nplayerSpeed)
        {
            playerController.moveSpeed = nplayerSpeed;
        }
        if (energyMetre.GetCurrentEnergy() <= 0) GameManager.instance.StartCoroutine(EndGame());
    }

    float currentColourBoost = 0;
    float colourBoostToAddOnDig = 40;
    float colourBoostDeclineExponent = 0.4f;

    float colourBoostMin = -70;
    float colourBoostMax = 30;

    private void UpdatePostProcesser()
    {
        if (currentColourBoost > 0)
            currentColourBoost -= Mathf.Pow(currentColourBoost, colourBoostDeclineExponent);
        else currentColourBoost = 0;

        float currentEnergy = energyMetre.GetCurrentEnergy() - 50 + currentColourBoost;

        PostProcessingManager.instance.ModifyColourGrading(currentEnergy, currentEnergy);
    }

    private void AddToColourBoost(float amountToAdd)
    {
        currentColourBoost += amountToAdd;
    }

    private void MaxEnergyReachedListener()
    {

        energyMetreTunnel.PlayTierUpEffect();
        ColourManager.instance.ChangePalette();
    }



    protected override void InitialiseStateTransitions()
    {
        stateTransitions = new Dictionary<Command, TransitionData<GameStateBase>>
        {
            { Command.Begin, new TransitionData<GameStateBase>(typeof(GameStateEndlessScoreboard))  },
            { Command.End, new TransitionData<GameStateBase>(typeof(GameStateEndlessScoreboard))  },
            //{ Command.Pause, new TransitionData<GameStateBase>(typeof(GameStatePauseMenu), true) } // Example of loading a pause menu - Endless game state is set to be preserved. TODO: Consider how best to decide when to destroy a preserved state.
        };
    }

    public override void StartGameState()
    {
        Debug.Log("GameStateEndless: Start Game State ");
        currentSessionData = new GameSessionData();
        PopulateLevelsArray();

        InitialiseClickSounds();


        Vector3 mapPosition = MapSpawner.Instance.GetCurrentMapHolder().transform.position;
        mapPosition += new Vector3(0, 10, 0);
        //GameManager.instance.GetPlayerBall().transform.position = mapPosition; // ballPosition;
        GameManager.instance.GetPlayerBall().SetActive(true);


        energyMetreTunnel = GameManager.instance.gameObject.AddComponent<HexTunnelEnergy>();
        energyMetreTunnel.Initialise(8, 30, 9);
        //energyMetreTunnel.edgeValue = energyMetre.GetEnergyMax() / 6;



        editMode = PlayerPrefs.GetInt("Edit Mode") == 1;
        if (editMode == true) // level edit on start... if the ball does not spawn we go into edit mode because of a helpfull bug thingo :)
        {
            Object.Destroy(Object.FindObjectOfType<PlayerController>().gameObject);
        }
        else
        {
            playerController = GameManager.instance.GetPlayerBall().GetComponent<PlayerController>();
        }

        LoadNextLevel();

    }

    public override bool CleanupGameState()
    {
        // ...

        UnityEngine.Object.Destroy(energyMetreTunnel);
        Debug.Log("CLEAN GAME STATE!");

        GameManager.instance.SetIngameHudActive(false);

        return true;
    }

    IEnumerator EndGame()
    {
        //DROP RINGS
        //RESET POST PROC (could be handled by state change on manager?)
        //END STATE

        PostProcessingManager.instance.ResetPostProcessor();
        GameManager.instance.ProcessCommand(Command.End);

        energyMetreTunnel.FallDestroy();
        yield return null;
    }



    protected void InitialiseClickSounds()
    {
        hexClickSounds = new SoundEffectEnum[5]
        {
            SoundEffectEnum.ES_01,
            SoundEffectEnum.ES_02,
            SoundEffectEnum.ES_03,
            SoundEffectEnum.ES_04,
            SoundEffectEnum.ES_05
        };
    }

    public override void PlayClickSound()
    {
        if (hexClickSounds.Length > 0)
        {
            int scaleIndex = currentSessionData.levelScore % hexClickSounds.Length;
            int pitch = (currentSessionData.levelScore / hexClickSounds.Length) + 1;

            AudioManager.instance.PlaySoundEffect(hexClickSounds[scaleIndex], pitch);
        }
    }


    public override void LoadNextLevel()
    {
    
        //ColourManager.instance.ChangePalette();

        Level newLevel = useRandomLevels ? levels[Random.Range(0, levels.Length)] : levels[currentSessionData.levelIndex % levels.Length];

        ++currentSessionData.levelIndex;

        currentSessionData.levelScore = 0;
        currentSessionData.passScore = 0;
        //newLevel.hexs.ToList().ForEach(x => currentSessionData.passScore += x.GetHex().destroyPoints);

        if (playerController != null)
        {
            playerController.SetDestination(playerController.transform.position);
            //playerController.moveSpeed = initialPlayerSpeed + ( currentSessionData.levelIndex * (playerSpeedIncreaseLogMultiplyer * Mathf.Log(playerSpeedIncreaseLogBase)));
        }

        MapSpawner.Instance.SpawnHexs(newLevel, playerController.transform.position);

        UpdateScore();

    }

    public override void HexDigEvent()
    {
        currentSessionData.levelScore += 1;
        int scoreToAdd = 10 * energyMetre.currentEnergyTier;
        currentSessionData.totalScore += scoreToAdd;
        GameManager.instance.scoreUI.ShowScoreIncreaseText(scoreToAdd);
        energyMetre.AddEnergy(energyIncreaseHexDig);

        AddToColourBoost(colourBoostToAddOnDig);

        if (currentSessionData.levelScore >= currentSessionData.passScore) // Level Cleared
        {
            if (playerController.transform.position.y >= MapSpawner.Instance.grid.transform.position.y)
            {
                energyMetre.AddEnergy(energyIncreaseLevelDigAbove);


            } else
            {
                energyMetre.AddEnergy(energyIncreaseLevelDigBelow);
            }
           
            LoadNextLevel();

        }
        UpdateScore();

    }


    public override void PlayGroundThud()
    {
        AudioManager.instance.PlaySoundEffect(SoundEffectEnum.Ground_Hit_Thud);
        RippleManager.instance.CreateRippleThud(playerController.gameObject.transform.position, 5f, 100);
    }

    public override void Pause()
    {
        throw new System.NotImplementedException();
    }

    public override void Resume()
    {
        throw new System.NotImplementedException();
    }


    private void PopulateLevelsArray()
    {
        //levels = Resources.LoadAll<Level>("Levels/Endless") as Level;
        //Debug.Log("Loaded Level count = " + levels.Length);

        levels = LevelLoader.Instance.GetLevelsFrom("Levels/Endless");
    }


    private void UpdateScore()
    {
        GameManager.instance.scoreUI.SetScore(currentSessionData.totalScore, energyMetre.currentEnergyTier);
    }

    internal class TierSpeedLogCurve
    {

        public TierSpeedLogCurve(float a = 2.7f, float b = 4.5f, float c = -3.1f)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
        private float a, b, c;

        public float GetY(float x)
        {
            float y = (a * (Mathf.Log(x + b))) + c;

            return (y);
        }
    }
}


