using UnityEngine;
public class DialogueTrigger : MonoBehaviour {
    public Dialogue dialogue;

    public void TriggerDialogue() {
        DialogueManager dialogManager = FindFirstObjectByType<DialogueManager>();
        if (dialogManager == null) {
            Debug.LogError("DialogManager não encontrado!");
            return;
        }
        dialogManager.StartDialogue(dialogue);
    }
}