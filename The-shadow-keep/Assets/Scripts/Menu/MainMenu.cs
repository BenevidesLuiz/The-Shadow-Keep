using UnityEngine;

public class MainMenu : MonoBehaviour {

    // MainMenu
    public void OnNewGameClick() => GameManager.Instance.GoToScene("Fase1", loadSave: false);
    public void OnContinueClick() => GameManager.Instance.GoToScene("Fase1", loadSave: true);
    public void GoToFase1() => GameManager.Instance.GoToScene("Fase1");
    public void GoToFase2() => GameManager.Instance.GoToScene("Fase2");
    public void GoToFase3() => GameManager.Instance.GoToScene("Fase3");

    // Instantâneas — sem loading
    public void GoToMorte() => GameManager.Instance.GoToSceneInstant("Morte");
    public void GoToMenu() => GameManager.Instance.GoToSceneInstant("Menu");
}