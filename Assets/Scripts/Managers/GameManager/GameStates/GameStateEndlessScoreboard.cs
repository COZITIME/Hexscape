﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;


public class GameStateEndlessScoreboard : GameStateBase
{
    //string pathScoreBoardLevel = "Assets/Resources/Levels/Menus/ScoreboardLevel.json";

    string pathScoreBoardLevel = "Levels/Menus/Scoreboard";
    string pathWorldTextPrefab = "Prefabs/GUI/WorldTextCanvas";


    private GameObject levelTextObj;
    private GameObject scoreTextObj;

    private Vector3 scoreboardWorldPos;

    public GameStateEndlessScoreboard()
    {
        InitialiseStateTransitions();
    }

    public override void StartGameState()
    {
        //TODO: Add two new buttons to level - replay and scoreboard

        MapSpawner.Instance.ClearAllGrids();
        
        Level loadedLevel = LevelLoader.Instance.LoadLevelFile(pathScoreBoardLevel);
        if (loadedLevel != null ) { 
        CreateLevel(
            loadedLevel,
            - 30.0f,
            false,
            false
            //new ScoreboardLevelComponent()
            );

        DisplayScores();
        }
        MapSpawner.Instance.PositionMapGrid(PlayerController.instance.transform.position + Vector3.up * MapSpawner.Instance.distanceBetweenMaps, false);
        MapSpawner.Instance.UpdateMapRefence();


        levelTextObj = GameObject.Instantiate(Resources.Load(pathWorldTextPrefab) as GameObject);
        if (levelTextObj != null)
        {
            levelTextObj.GetComponentInChildren<Text>().text = "Levels Cleared";
            levelTextObj.transform.position = MapSpawner.Instance.GetCurrentMapHolder().transform.position + new Vector3(0, 0, 2.6f);
        }

        scoreTextObj = GameObject.Instantiate(Resources.Load(pathWorldTextPrefab) as GameObject);
        if (scoreTextObj != null)
        {
            scoreTextObj.GetComponentInChildren<Text>().text = "Score Total";
            scoreTextObj.transform.position = MapSpawner.Instance.GetCurrentMapHolder().transform.position + new Vector3(0, 0, 0.77f);
            Debug.Log("scoreTextObj.transform.position" + scoreTextObj.transform.position);
            Debug.Log("MapSpawner.Instance.GetCurrentMapHolder().transform.position" + MapSpawner.Instance.GetCurrentMapHolder().transform.position);
        }

        scoreboardWorldPos = MapSpawner.Instance.GetCurrentMapHolder().transform.position;
    }

    protected override void InitialiseStateTransitions()
    {
        stateTransitions = new Dictionary<Command, TransitionData<GameStateBase>>
        {
            { Command.Begin, new TransitionData<GameStateBase>(typeof(GameStateMenuMain))  },
            { Command.End, new TransitionData<GameStateBase>(typeof(GameStateMenuMain))  },
        };
    }

    public override void PlayClickSound()
    {
        base.PlayClickSound();
    }

    public override void HexDigEvent(Hex hex)
    {
        //throw new System.NotImplementedException();
    }

    public override void PassSessionData(GameSessionData data)
    {
        currentSessionData = data;
    }

    public override bool CleanupGameState()
    {
        GameObject.Destroy(scoreTextObj);
        GameObject.Destroy(levelTextObj);


        GameObject textParticleObj = CameraCanvas.instance.GetParticleObject();
        textParticleObj.transform.position = scoreboardWorldPos + new Vector3(0, 0, 200);

        GameManager.instance.StartCoroutine(MoveTo(textParticleObj, (scoreboardWorldPos + new Vector3(0, 200, 0)), 1f));

        if (scoreTextObj != null || levelTextObj != null) return false;
        else return true;
    }

    //public override void NextMenu()
    //{
    //    GameManager.instance.ProcessCommand(Command.End);
    //}

    public override void HandleCommand(Command command)
    {
        switch (command)
        {
            case Command.NextMenu:
                GameManager.instance.ProcessCommand(Command.End);
                break;
        }
    }


    #region Custom State Methods

    //private int scoreToDisplay, levelToDisplay;
    //private int scoreDisplayYPos, levelDisplayYPos;
    private int levelValue, scoreValue;

    Vector2Int[] levelDisplayTilePos = new[] { new Vector2Int(1, 2), new Vector2Int(0, 2), new Vector2Int(-1, 2) };
    Vector2Int[] scoreDisplayTilePos = new[] { new Vector2Int(2, 0), new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0) };

    // Note: Digit array could be stored elsewhere (HexBank?)
    //HexTypeEnum[] hexDigits = { HexTypeEnum.HexTile_Digit0,
    //                            HexTypeEnum.HexTile_Digit1,
    //                            HexTypeEnum.HexTile_Digit2,
    //                            HexTypeEnum.HexTile_Digit3,
    //                            HexTypeEnum.HexTile_Digit4,
    //                            HexTypeEnum.HexTile_Digit5,
    //                            HexTypeEnum.HexTile_Digit6,
    //                            HexTypeEnum.HexTile_Digit7,
    //                            HexTypeEnum.HexTile_Digit8,
    //                            HexTypeEnum.HexTile_Digit9,
    //};

 

    void DisplayScores()
    {
        GetCurrentSessionScore(out levelValue, out scoreValue);

        if (!GameManager.instance.GetIsOffline()) MakeScoreDownloadRequest();
        else GameManager.instance.StartCoroutine( DisplayScoreOffline() );
    }

    void GetCurrentSessionScore(out int returnLevel, out int returnScore)
    {
        GameStateBase.GameSessionData sessionData = GameManager.instance.GetGameSessionData();

        returnLevel = sessionData.levelIndex;
        returnScore = sessionData.totalScore;
    }

    bool doUploadScore = false;
    int downloadedLevelValue;
    int downloadedScoreValue;


    void MakeScoreDownloadRequest()
    {
        // TODO: store scores locally in the event that the server is not accessible, or the player chooses not to host their
        //          scores on teh server. In which case, we should compare the current score to the local score and the server?
        //          Possible issue here is that players could potentially hack their own value into the local score value to be
        //          pushed to the server. 


        DownloadScore scoreDownloader = new DownloadScore();

        if (scoreDownloader != null)
        {
            scoreDownloader.GetScoreForUser(GameManager.instance.loadedProfile.GetPlayerIDasInt(), Callback);
        }
    }

    public void Callback(Leaderboard.EntryData data, DownloadScore.EDownloadStatus status)
    {

        DigitComponent[] digitComps = MapSpawner.Instance.GetCurrentMapHolder().GetComponentsInChildren<DigitComponent>();

        for (int i = 0; i < digitComps.Length; i++) 
        {
            if (i == 0) {
                digitComps[i].UpdateDisplayValue(scoreValue);
            }
            if (i == 1)
            {
                digitComps[i].UpdateDisplayValue(levelValue);
            }
        }


        // Level
        if (data.highLevel < levelValue)
        {
            doUploadScore = true;

            // Display "New Best Level"
            Debug.Log("New Best Level.  Old = " + data.highLevel + "  | New = " + levelValue);

        }


        DisplayHighscoreEffect(); // TEMP
        // Score
        if (data.highScore < scoreValue)
        {
            doUploadScore = true;

            // Display "New Best Score"
            Debug.Log("New Best Score. Old = " + data.highScore + "  | New = " + scoreValue);
            DisplayHighscoreEffect();
        }

        if (doUploadScore)
        {
            UploadUserScore scoreUploader = new UploadUserScore();

            int playerID;
            int.TryParse(GameManager.instance.loadedProfile.GetPlayerID(), out playerID);
            scoreUploader.UploadScore(new Leaderboard.EntryData(playerID, levelValue, scoreValue));
        }
    }

    private IEnumerator DisplayScoreOffline()
    {

        yield return new WaitForSeconds(1.0f);

        DigitComponent[] digitComps = MapSpawner.Instance.GetCurrentMapHolder().GetComponentsInChildren<DigitComponent>();

        for (int i = 0; i < digitComps.Length; i++)
        {
            if (i == 0)
            {
                digitComps[i].UpdateDisplayValue(scoreValue);
            }
            if (i == 1)
            {
                digitComps[i].UpdateDisplayValue(levelValue);
            }
        }

        //...
        //Compare to local high score value
        //Add system to permit player to add to local scoreboard

        int prevHighScore = PlayerPrefs.GetInt("highscore");
        int prevHighLevel = PlayerPrefs.GetInt("highlevel");

        if (PlayerPrefs.HasKey("highscore"))
        {
            if (prevHighScore < scoreValue)
            {
                DisplayHighscoreEffect();
                PlayerPrefs.SetInt("highscore", scoreValue);
                PlayerPrefs.Save();
            }
        }
        else PlayerPrefs.SetInt("highscore", scoreValue);

        if (PlayerPrefs.HasKey("highlevel"))
        {
            if (prevHighLevel < levelValue)
            {
                PlayerPrefs.SetInt("highlevel", levelValue);
                PlayerPrefs.Save();
            }
        }
        else PlayerPrefs.SetInt("highlevel", levelValue);



    }

    void DisplayHighscoreEffect()
    {
        Debug.Log("DisplayHighscoreEffect");
        GameObject textParticleObj = CameraCanvas.instance.GetParticleObject();
        textParticleObj.SetActive(false);
        textParticleObj.transform.position = scoreboardWorldPos + new Vector3(-5, 50, 5);
        textParticleObj.SetActive(true);

        CameraCanvas.instance.ChangeDisplayType(DisplayObjectMap.EDisplayType.HiScoreGlobal);

        //textParticleObj.transform.position = scoreboardWorldPos + new Vector3(2, 0, -0.8f);

        GameManager.instance.StartCoroutine( MoveTo(textParticleObj, (scoreboardWorldPos + new Vector3(2, 0, -0.8f)), 0.5f));
    }

    IEnumerator MoveTo(GameObject obj, Vector3 position, float time)
    {
        Vector3 start = obj.transform.position;
        Vector3 end = position;
        float t = 0;

        while (t < 1)
        {
            yield return null;
            t += Time.deltaTime / time;
            obj.transform.position = Vector3.Lerp(start, end, t);
        }
        obj.transform.position = end;

    }


    int[] ConvertIntToArray(int inValue) // TODO: Move to helper class
    {
        if (inValue == 0) return new int[1] { 0 };

        var digits = new List<int>();

        for (; inValue != 0; inValue /= 10)
            digits.Add(inValue % 10);

        int[] arr = digits.ToArray();
        System.Array.Reverse(arr);
        return arr;
    }
    #endregion
}