using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private Scene _currentScene;
    public PlayerMovement player;
    public Transform respawnPoint;
    private void Awake()
    {
        // currentScene = SceneManager.GetActiveScene();
        //
        // player.enabled = false;
        // NewGame();
    }

    private void NewGame()
    {
        Time.timeScale = 1;
        StartCoroutine(StartGame());
    }



    private IEnumerator StartGame()
    {
        yield return new WaitForSecondsRealtime(1f);
        player.enabled = true;

        yield return new WaitUntil(() => player.horizontalMovement > 0);
    }

    public void EndGame()
    { 
        player.transform.position = respawnPoint.position;
        player.rb.linearVelocity = Vector3.zero;
        player.enabled = false;
        
        NewGame();
    }
}
