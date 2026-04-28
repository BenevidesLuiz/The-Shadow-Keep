using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour {
    public Text nameText;
    public Text dialogueText;
    public Animator animator; 
    private Queue<string> sentences = new Queue<string>();

    private void Start() {
    }

    public void StartDialogue(Dialogue dialogue) {
        Debug.Log("Start conversaation " + dialogue.name);
        animator.SetBool("isOpen", true);
        nameText.text = dialogue.name;
        sentences.Clear();

        foreach (string sentence in dialogue.sentences) { 
            sentences.Enqueue(sentence);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence() {
        if (sentences.Count == 0) {
            EndDialogue();
            return;
        }

        string sentence = sentences.Dequeue(); 
        dialogueText.text = sentence;
    }

    void EndDialogue() {
        animator.SetBool("isOpen", false);
    }
}