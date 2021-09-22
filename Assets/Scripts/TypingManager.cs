using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class TypingManager : MonoBehaviour
{
    public TextMeshProUGUI typingText;

    public List<Word> words;

    public List<Word> currentTricks = new List<Word>();
    public Animator playerAnimator;
    public GameObject sound;
    public GameObject unsecuredScorePrefab;
    private TextMeshProUGUI unsecuredScoreText;
    private Animator unsecuredScoreAnimator;

    public GameObject completedTextPrefab;

    private int grindCount;
    
    private void Start() {
        typingText.text = "";
        Player.Instance.onJump += () => {
            // Debug.Log("onJump");
            GameObject unsecuredScore = Instantiate(unsecuredScorePrefab, Score.Instance.scoreDisplay.transform, false);
            unsecuredScoreText = unsecuredScore.GetComponent<TextMeshProUGUI>();
            unsecuredScoreAnimator = unsecuredScore.GetComponent<Animator>();
        };
        Player.Instance.onLand += () => {
            // calculate score
            int scoreAdded = 0;
            foreach (Word trick in currentTricks) {
                scoreAdded += trick.trickScore;
            }
            // if safe landing
            if (Player.Instance.safe) {
                // add score
                sound.GetComponent<SFX>().playVoice();
                Score.Instance.AddScore(scoreAdded);
                // push
                float multiplier = Mathf.Lerp(0.7f, 2.0f, scoreAdded/10.0f);
                Player.Instance.Push(multiplier);
                // animate score
                if (unsecuredScoreAnimator != null) {
                    unsecuredScoreAnimator.SetBool("secured", true);
                    unsecuredScoreText = null;
                    unsecuredScoreAnimator = null;
                }
                
            }
            // if crash landing
            else {
                sound.GetComponent<SFX>().playScratch();
                // screen shake
                StartCoroutine(CameraShake.Instance.Shake(0.2f + scoreAdded*0.1f));
                // slow to min speed
                Player.Instance.Slow();
                if (unsecuredScoreAnimator != null) {
                    Destroy(unsecuredScoreAnimator.gameObject);
                    unsecuredScoreAnimator = null;
                    unsecuredScoreText = null;
                }
            }
            // securedTricks = false;
            currentTricks.Clear();
            typingText.text = "";
        };
    }

    // Update is called once per frame
    void Update()
    {
        string input = Input.inputString;
        if (input.Equals("")) return;
        
        char c = input[0];
        Word currentWord = null;
        foreach (Word w in words) {
            // skip tricks that you've already done, but not grind
            if (currentTricks.Contains(w) && w.text != "grind") continue;
            // skip trick if not in correct state
            if (!w.availableInStates.Contains(Player.Instance.state)) continue;

            // if the current input matches a word
            if (w.ContinueText(c)) {
                if (currentWord == null) {
                    currentWord = w;
                }
                else if(w.GetTyped().Length > currentWord.GetTyped().Length) {
                    currentWord.Clear();
                    currentWord = w;
                }
                // if user typed the whole word
                if (w.GetTyped().Equals(w.text))
                {
                    // if word is "grind"
                    if (w.text.Equals("grind")) Player.Instance.grindCount++;
                    // add to current tricks
                    if (Player.Instance.state != Player.State.OnGround)
                    {
                        currentTricks.Add(w);
                        // update unsecured score text
                        int score = 0;
                        foreach (Word trick in currentTricks) {
                            score += trick.trickScore;
                        }
                        unsecuredScoreText.text = score.ToString();
                        // do player animation
                        if(w.trickScore > 0) playerAnimator.SetTrigger("trick");
                    }
                    // animate completed text
                    TextMeshProUGUI completedText = Instantiate(completedTextPrefab, typingText.transform.parent, false).GetComponent<TextMeshProUGUI>();
                    completedText.text = w.text;
                    // clear current typing
                    typingText.text = "";
                    currentWord = null;
                    break;
                }
            }
        }
        if (currentWord == null)
        {
            typingText.text = "";
        }
        else
        {
            typingText.text = currentWord.GetTyped();
        }
    }
}

[Serializable]
public class Word
{
    public string text;
    public int trickScore;
    public List<Player.State> availableInStates;
    public UnityEvent onTyped;

    private string hasTyped;
    private int curChar;

    public Word(string t)
    {
        text = t;
        hasTyped = "";
        curChar = 0;
    }

    public bool ContinueText(char c)
    {
        // if c matches
        if (c.Equals(text[curChar]))
        {
            curChar++;
            hasTyped = text.Substring(0, curChar);

            // if we typed the whole word
            if (curChar >= text.Length)
            {
                onTyped?.Invoke();
                curChar = 0;
            }
            return true;
        }
        // if c doesn't match
        Clear();
        return false;
    }

    public void Clear()
    {
        curChar = 0;
        hasTyped = "";
    }

    public string GetTyped()
    {
        return hasTyped;
    }
}
