using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class UiGameScript : MonoBehaviour
{
    public CinemachineCamera camera;
    public GameObject car;
    public GameObject diaper;
    public float waitTime = 2f;
    public Animator Animator;
    public Material Material;

    void Start()
    {
        //car.SetActive(false);
        //diaper.SetActive(false);
        //camera.Follow = null;
    }

    public void Startgame()
    {
        StartCoroutine(setGame());
    }

    private IEnumerator setGame()
    {
        Animator.Play("Start");
        car.SetActive(true);
        yield return new WaitForSeconds(waitTime);
        camera.Follow = car.transform;
        diaper.SetActive(true);
    }
    public void StopGame()
    {
        Animator.Play("End");
    }
}
