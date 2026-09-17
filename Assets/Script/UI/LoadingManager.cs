using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
  //  public static string NEXT_SCENE = "PlayScene";

    public GameObject progressbar;
    public TMP_Text text;
    private float fixerloadingtime = 3f;

    private void Start()
    {
       // StartCoroutine(LoadSceneAsync(NEXT_SCENE));
    }

    public IEnumerator LoadSceneAsync(string Scenename)
    {
        AsyncOperation operetion = SceneManager.LoadSceneAsync(Scenename);

        while (!operetion.isDone)
        {
            float progress = Mathf.Clamp01(operetion.progress / 0.9f);
            progressbar.GetComponent<Image>().fillAmount = progress;
            text.text = (progress * 100).ToString(format: "0") + "%";

            yield return new WaitForSeconds(fixerloadingtime);
        }
    }
}
