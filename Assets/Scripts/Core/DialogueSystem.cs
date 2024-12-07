using UnityEngine;
using System;
using System.Collections.Generic;
using Forever.Characters;
using Forever.UI;
using Forever.Audio;

namespace Forever.Core
{
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        [Header("Dialogue Settings")]
        public float typingSpeed = 0.05f;
        public float autoProgressDelay = 2f;
        public float choiceTimeout = 10f;
        
        [Header("Audio")]
        public AudioClip[] typingSounds;
        public AudioClip dialogueStart;
        public AudioClip dialogueEnd;
        public AudioClip choiceSelect;
        
        [Header("Visual Effects")]
        public ParticleSystem emotionVFX;
        public float emotionIntensity = 1f;
        
        private Dictionary<string, DialogueTree> dialogueTrees;
        private DialogueNode currentNode;
        private Character currentSpeaker;
        private Character currentListener;
        private bool isDialogueActive;
        private float choiceTimer;
        
        private UIManager uiManager;
        private AudioManager audioManager;
        private QuestSystem questSystem;
        private GameManager gameManager;
        
        public event Action<DialogueNode> OnDialogueNodeStart;
        public event Action<DialogueNode> OnDialogueNodeEnd;
        public event Action<string> OnDialogueChoice;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSystem()
        {
            dialogueTrees = new Dictionary<string, DialogueTree>();
            
            // Get system references
            uiManager = FindObjectOfType<UIManager>();
            audioManager = FindObjectOfType<AudioManager>();
            questSystem = FindObjectOfType<QuestSystem>();
            gameManager = FindObjectOfType<GameManager>();
            
            // Load dialogue data
            LoadDialogueTrees();
        }
        
        private void LoadDialogueTrees()
        {
            // TODO: Load dialogue trees from data files
        }
        
        private void Update()
        {
            if (isDialogueActive)
            {
                UpdateDialogue();
            }
        }
        
        private void UpdateDialogue()
        {
            if (currentNode == null) return;
            
            // Update choice timer if choices are present
            if (currentNode.choices != null && currentNode.choices.Length > 0)
            {
                choiceTimer -= Time.deltaTime;
                if (choiceTimer <= 0)
                {
                    // Auto-select first choice or default choice if time runs out
                    MakeChoice(0);
                }
            }
        }
        
        public void StartDialogue(string dialogueId, Character speaker, Character listener)
        {
            if (!dialogueTrees.ContainsKey(dialogueId))
            {
                Debug.LogWarning($"Dialogue tree {dialogueId} not found");
                return;
            }
            
            DialogueTree tree = dialogueTrees[dialogueId];
            if (!CanStartDialogue(tree, speaker, listener))
            {
                Debug.Log($"Cannot start dialogue {dialogueId}: conditions not met");
                return;
            }
            
            // Set up dialogue state
            currentSpeaker = speaker;
            currentListener = listener;
            isDialogueActive = true;
            
            // Start with first node
            currentNode = tree.startNode;
            
            // Show dialogue UI
            uiManager?.ShowDialogueUI(true);
            
            // Play start effects
            PlayDialogueEffects(DialogueEffectType.Start);
            
            // Process first node
            ProcessDialogueNode(currentNode);
        }
        
        private bool CanStartDialogue(DialogueTree tree, Character speaker, Character listener)
        {
            if (isDialogueActive) return false;
            
            // Check quest prerequisites
            if (tree.requiredQuests != null)
            {
                foreach (string questId in tree.requiredQuests)
                {
                    QuestState state = questSystem?.GetQuestState(questId);
                    if (state == null || !state.isCompleted)
                        return false;
                }
            }
            
            // Check relationship level
            if (tree.requiredRelationshipLevel > 0)
            {
                float relationship = GetRelationshipLevel(speaker, listener);
                if (relationship < tree.requiredRelationshipLevel)
                    return false;
            }
            
            return true;
        }
        
        private float GetRelationshipLevel(Character character1, Character character2)
        {
            // TODO: Implement relationship system
            return 0f;
        }
        
        private void ProcessDialogueNode(DialogueNode node)
        {
            if (node == null)
            {
                EndDialogue();
                return;
            }
            
            OnDialogueNodeStart?.Invoke(node);
            
            // Display text
            uiManager?.ShowDialogueText(node.text, currentSpeaker.characterName);
            
            // Play character animation
            PlayCharacterAnimation(node.emotion);
            
            // Show choices if any
            if (node.choices != null && node.choices.Length > 0)
            {
                uiManager?.ShowDialogueChoices(node.choices);
                choiceTimer = choiceTimeout;
            }
            
            // Execute node actions
            ExecuteNodeActions(node);
        }
        
        private void PlayCharacterAnimation(EmotionType emotion)
        {
            if (currentSpeaker != null)
            {
                // Play emotion animation
                string animTrigger = GetEmotionAnimationTrigger(emotion);
                currentSpeaker.GetComponent<Animator>()?.SetTrigger(animTrigger);
                
                // Spawn emotion VFX
                if (emotionVFX != null)
                {
                    ParticleSystem vfx = Instantiate(emotionVFX, 
                        currentSpeaker.transform.position + Vector3.up * 2f, 
                        Quaternion.identity);
                    
                    var main = vfx.main;
                    main.startColor = GetEmotionColor(emotion);
                    
                    var emission = vfx.emission;
                    emission.rateOverTime = emotionIntensity;
                }
            }
        }
        
        private string GetEmotionAnimationTrigger(EmotionType emotion)
        {
            switch (emotion)
            {
                case EmotionType.Happy: return "EmoteHappy";
                case EmotionType.Sad: return "EmoteSad";
                case EmotionType.Angry: return "EmoteAngry";
                case EmotionType.Surprised: return "EmoteSurprised";
                case EmotionType.Neutral: return "EmoteNeutral";
                default: return "EmoteNeutral";
            }
        }
        
        private Color GetEmotionColor(EmotionType emotion)
        {
            switch (emotion)
            {
                case EmotionType.Happy: return Color.yellow;
                case EmotionType.Sad: return Color.blue;
                case EmotionType.Angry: return Color.red;
                case EmotionType.Surprised: return Color.magenta;
                case EmotionType.Neutral: return Color.white;
                default: return Color.white;
            }
        }
        
        private void ExecuteNodeActions(DialogueNode node)
        {
            if (node.actions == null) return;
            
            foreach (var action in node.actions)
            {
                switch (action.type)
                {
                    case DialogueActionType.StartQuest:
                        questSystem?.StartQuest(action.parameter);
                        break;
                        
                    case DialogueActionType.GiveItem:
                        // TODO: Implement inventory system
                        break;
                        
                    case DialogueActionType.ChangeRelationship:
                        if (float.TryParse(action.parameter, out float change))
                        {
                            // TODO: Implement relationship change
                        }
                        break;
                        
                    case DialogueActionType.TriggerEvent:
                        gameManager?.TriggerGameEvent(action.parameter);
                        break;
                }
            }
        }
        
        public void MakeChoice(int choiceIndex)
        {
            if (!isDialogueActive || currentNode == null || 
                currentNode.choices == null || choiceIndex >= currentNode.choices.Length)
                return;
                
            DialogueChoice choice = currentNode.choices[choiceIndex];
            OnDialogueChoice?.Invoke(choice.text);
            
            // Play choice sound
            if (audioManager != null && choiceSelect != null)
            {
                audioManager.PlaySound(choiceSelect);
            }
            
            // Process choice consequences
            if (choice.consequences != null)
            {
                foreach (var consequence in choice.consequences)
                {
                    ExecuteNodeActions(consequence);
                }
            }
            
            // Move to next node
            currentNode = choice.nextNode;
            ProcessDialogueNode(currentNode);
        }
        
        public void AdvanceDialogue()
        {
            if (!isDialogueActive || currentNode == null)
                return;
                
            // If there are choices, wait for player input
            if (currentNode.choices != null && currentNode.choices.Length > 0)
                return;
                
            OnDialogueNodeEnd?.Invoke(currentNode);
            
            // Move to next node
            currentNode = currentNode.nextNode;
            ProcessDialogueNode(currentNode);
        }
        
        private void EndDialogue()
        {
            isDialogueActive = false;
            currentNode = null;
            currentSpeaker = null;
            currentListener = null;
            
            // Hide dialogue UI
            uiManager?.ShowDialogueUI(false);
            
            // Play end effects
            PlayDialogueEffects(DialogueEffectType.End);
        }
        
        private void PlayDialogueEffects(DialogueEffectType effectType)
        {
            if (audioManager == null) return;
            
            switch (effectType)
            {
                case DialogueEffectType.Start:
                    if (dialogueStart != null)
                        audioManager.PlaySound(dialogueStart);
                    break;
                    
                case DialogueEffectType.End:
                    if (dialogueEnd != null)
                        audioManager.PlaySound(dialogueEnd);
                    break;
                    
                case DialogueEffectType.Typing:
                    if (typingSounds != null && typingSounds.Length > 0)
                    {
                        AudioClip clip = typingSounds[UnityEngine.Random.Range(0, typingSounds.Length)];
                        audioManager.PlaySound(clip, 0.5f);
                    }
                    break;
            }
        }
    }
    
    [System.Serializable]
    public class DialogueTree
    {
        public string dialogueId;
        public string description;
        public DialogueNode startNode;
        public string[] requiredQuests;
        public float requiredRelationshipLevel;
        public bool repeatable;
    }
    
    [System.Serializable]
    public class DialogueNode
    {
        public string text;
        public EmotionType emotion;
        public DialogueChoice[] choices;
        public DialogueAction[] actions;
        public DialogueNode nextNode;
    }
    
    [System.Serializable]
    public class DialogueChoice
    {
        public string text;
        public DialogueNode nextNode;
        public DialogueAction[] consequences;
    }
    
    [System.Serializable]
    public class DialogueAction
    {
        public DialogueActionType type;
        public string parameter;
    }
    
    public enum EmotionType
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Surprised
    }
    
    public enum DialogueActionType
    {
        StartQuest,
        GiveItem,
        ChangeRelationship,
        TriggerEvent
    }
    
    public enum DialogueEffectType
    {
        Start,
        End,
        Typing
    }
} 