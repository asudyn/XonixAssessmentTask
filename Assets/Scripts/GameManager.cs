using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	protected static GameManager _instance;

    [SerializeField]
    private int initialLives = 3;
    [SerializeField]
    private int enemyInsideCount = 3;
    [SerializeField]
    private int enemyOutsideCount = 1;
    [SerializeField]
    private int winPercentage = 70;


    [Space(10)]
    [SerializeField]
    private Field field;

    [Space(10)]
    [SerializeField]
    private GameObject playerPrefab;
    [SerializeField]
    private GameObject enemyInsidePrefab;
    [SerializeField]
    private GameObject enemyOutsidePrefab;

    [Space(10)]
    [SerializeField]
    private TextMesh scoreText;
    [SerializeField]
    private TextMesh livesText;
    [SerializeField]
    private TextMesh claimedText;

    [Space(10)]
    [SerializeField]
    private GameObject getReadyMessage;
    [SerializeField]
    private GameObject deathMessage;
    [SerializeField]
    private GameObject gameOverMessage;
    [SerializeField]
    private GameObject victoryMessage;

    private Player player;
    private EnemyInside[] enemiesInside;
    private EnemyOutside[] enemiesOutside;
    private int score = 0;
    private int lives = 0;
    private float claimedPercentage = 0;

	public static GameManager Instance
	{
		get {
			if ((object)_instance == null) {
				_instance = FindObjectOfType<GameManager> ();
				if (_instance == null) {
					GameObject obj = new GameObject ();
					_instance = obj.AddComponent<GameManager> ();
				}
			}
			return _instance;
		}
	}

	protected virtual void Awake ()
	{
		if (!Application.isPlaying){
			return;
		}
		_instance = this;

        Screen.SetResolution(648,400,false);
        Initialize();
	}

    private void Initialize() {
        score = 0;
        lives = initialLives;
        claimedPercentage = 0;
        //Initialize and fill the field
        field.Initialize();
        field.Fill();
        Time.timeScale = 0;
        //Instantiate and initialize the player
        player = Instantiate(playerPrefab,new Vector3(0f,6.125f,0f),Quaternion.identity).GetComponent<Player>();
        player.Initialize(field);
        //Instantiate and initialize the enemies
        enemiesInside = new EnemyInside[enemyInsideCount];
        for(int i = 0;i < enemyInsideCount;i++) {
            enemiesInside[i] = Instantiate(enemyInsidePrefab).GetComponent<EnemyInside>();
            enemiesInside[i].Initialize(field);
        }
        enemiesOutside = new EnemyOutside[enemyOutsideCount];
        for(int i = 0;i < enemyOutsideCount;i++) {
            enemiesOutside[i] = Instantiate(enemyOutsidePrefab).GetComponent<EnemyOutside>();
            enemiesOutside[i].Initialize(field);
        }
        RepositionEnemies();
        StartCoroutine(StartTime());
    }

    public void StopPlayer() {
        player.Stop();
    }

    public void Restart() {
        score = 0;
        lives = initialLives;
        claimedPercentage = 0;
        field.Restart();
        RepositionPlayer();
        RepositionEnemies();
        UpdateUI();
        StartCoroutine(StartTime());
    }

    public void RepositionPlayer() {
        player.Restart();
        player.transform.position = new Vector3(0f,6.125f,0f);
    }

    public void RepositionEnemies() {
        for(int i = 0;i < enemyInsideCount;i++) {
            enemiesInside[i].transform.position=field.GetRandomPointOnField();
            enemiesInside[i].ChooseRandomDiagonalDirection();
        }
        for(int i = 0;i < enemyOutsideCount;i++) {
            enemiesOutside[i].transform.position = new Vector3(Random.Range(-9f,9f),-5.5f,0);
            enemiesOutside[i].ChooseRandomDiagonalDirection();
        }
    }

    public void AddScore(int points) {
        score += points;
    }

    public void UpdateClaimed(float percentage) {
        claimedPercentage = percentage;
        if(claimedPercentage >= winPercentage) { 
            StartCoroutine(YouHaveWon());
        }
    }

    public void UpdateUI() {
        scoreText.text = score.ToString();
        livesText.text = lives.ToString();
        claimedText.text = claimedPercentage.ToString("F1") + "%";
    }

    public void PlayerDeath() {
        lives--;
        if(lives > 0) {
            StartCoroutine(YouHaveDied());
        } else { 
            StartCoroutine(GameOver());
        }
        UpdateUI();
    }

    private IEnumerator StartTime() {
        getReadyMessage.SetActive(true);
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(3);
        Time.timeScale = 1;
        getReadyMessage.SetActive(false);
    }

    private IEnumerator YouHaveDied() {
        deathMessage.SetActive(true);
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(3);
        Time.timeScale = 1;
        deathMessage.SetActive(false);
        field.ResetPath();
        RepositionPlayer();
    }

    private IEnumerator GameOver() { 
        gameOverMessage.SetActive(true);
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(3);
        Time.timeScale = 1;
        gameOverMessage.SetActive(false);
        Restart();
    }

    private IEnumerator YouHaveWon() {
        victoryMessage.SetActive(true);
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(3);
        Time.timeScale = 1;
        victoryMessage.SetActive(false);
        Restart();
    }

}
