using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn;
using Yarn.Unity;

public class LineViewCustom : DialogueViewBase
{
    public static Dictionary<string, NodeType> nodeTypeDict = new Dictionary<string, NodeType>()
    {
        {"Joke", NodeType.Joke },
        {"Lifestyle", NodeType.Lifestyle },
        {"Work", NodeType.Work },
        {"Relationship", NodeType.Relationship }
    };

    public enum NodeType
    {
        NULL = -1,
        Joke = 0,
        Lifestyle,
        Work,
        Relationship
    }

    private struct NodeStruct
    {
        public Node node;
        public NodeType type;
        public bool isPositive;

        public NodeStruct(Node node)
        {
            this.node = node;
            if (node.Tags.Contains("Joke"))
            {
                type = NodeType.Joke;
            }
            else if (node.Tags.Contains("Work"))
            {
                type = NodeType.Work;
            }
            else if (node.Tags.Contains("Lifestyle"))
            {
                type = NodeType.Lifestyle;
            }
            else if (node.Tags.Contains("Relationship"))
            {
                type = NodeType.Relationship;
            }
            else
            {
                Debug.LogError("No type tag found.");
                type = NodeType.Work;
            }

            isPositive = node.Tags.Contains("Positive");
        }
    }

    internal enum ContinueActionType
    {
        None,
        KeyCode,
        InputSystemAction,
        InputSystemActionFromAsset,
    }

    [SerializeField]
    internal CanvasGroup canvasGroup;

    [SerializeField]
    internal bool useFadeEffect = true;

    [SerializeField]
    [Min(0)]
    internal float fadeInTime = 0.25f;

    [SerializeField]
    [Min(0)]
    internal float fadeOutTime = 0.05f;

    [SerializeField]
    internal TextMeshProUGUI lineText = null;

    [SerializeField]
    internal bool showCharacterNameInLineView = true;

    [SerializeField]
    internal TextMeshProUGUI characterNameText = null;

    [SerializeField]
    internal bool useTypewriterEffect = false;

    [SerializeField]
    [Min(0)]
    internal float typewriterEffectSpeed = 0f;

    [SerializeField]
    internal GameObject continueButton = null;

    [SerializeField]
    internal ContinueActionType continueActionType;

    [SerializeField]
    internal KeyCode continueActionKeyCode = KeyCode.Escape;


#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        [SerializeField]
        internal InputActionReference continueActionReference = null;

        [SerializeField]
        internal InputAction continueAction = new InputAction("Skip", InputActionType.Button, CommonUsages.Cancel);
#endif

    private InterruptionFlag interruptionFlag = new InterruptionFlag();

    LocalizedLine currentLine = null;

    [SerializeField]
    private GameObject textBoxPrefab = null;

    private bool signal = false;
    private NodeType signalType = NodeType.NULL;

    private List<Node> jokeNodes = new List<Node>(),
        lifestyleNodes = new List<Node>(),
        workNodes = new List<Node>(),
        relationshipNodes = new List<Node>();

    private Dictionary<string, NodeStruct> nodeDict = new Dictionary<string, NodeStruct>();

    private DialogueRunner runner = null;
    private string nextNode = null;

    public void Start()
    {
        canvasGroup.alpha = 0;

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            // If we are using an action reference, and it's not null,
            // configure it
            if (continueActionType == ContinueActionType.InputSystemActionFromAsset && continueActionReference != null)
            {
                continueActionReference.action.performed += UserPerformedSkipAction;
            }

            // The custom skip action always starts disabled
            continueAction?.Disable();
            continueAction.performed += UserPerformedSkipAction;
#endif
        runner = FindObjectOfType<DialogueRunner>();
        List<Node> nodes = new List<Node>(runner.yarnProject.GetProgram().Nodes.Values);
        runner.onNodeComplete.AddListener(NodeComplete);
        for (int i = 0; i < nodes.Count; ++i)
        {
            NodeStruct node = new NodeStruct(nodes[i]);
            switch (node.type)
            {
                case NodeType.Joke:
                    jokeNodes.Add(nodes[i]);
                    break;
                case NodeType.Work:
                    workNodes.Add(nodes[i]);
                    break;
                case NodeType.Lifestyle:
                    lifestyleNodes.Add(nodes[i]);
                    break;
                case NodeType.Relationship:
                    relationshipNodes.Add(nodes[i]);
                    break;
            }
            nodeDict[node.node.Name] = node;
        }

        runner.AddCommandHandler<string>("WaitForType", WaitForSignal);

        runner.StartDialogue("Node");
    }

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        private void UserPerformedSkipAction(InputAction.CallbackContext obj)
        {
            OnContinueClicked();
        }
#endif

    public void Reset()
    {
        canvasGroup = GetComponentInParent<CanvasGroup>();
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    public void Update()
    {
        // If the legacy input system is available, we are configured
        // to use a keycode to skip lines, AND the skip keycode was
        // just pressed, then skip
        if (continueActionType == ContinueActionType.KeyCode)
        {
            if (UnityEngine.Input.GetKeyDown(continueActionKeyCode))
            {
                OnContinueClicked();
            }
        }
    }
#endif

    public override void DismissLine(Action onDismissalComplete)
    {
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            continueAction?.Disable();
            continueActionReference?.action?.Disable();
#endif

        currentLine = null;

        if (useFadeEffect)
        {
            StartCoroutine(Effects.FadeAlpha(canvasGroup, 1, 0, fadeOutTime, onDismissalComplete));
        }
        else
        {
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            onDismissalComplete();
        }
    }

    public override void OnLineStatusChanged(LocalizedLine dialogueLine)
    {
        switch (dialogueLine.Status)
        {
            case LineStatus.Presenting:
                break;
            case LineStatus.Interrupted:
                // We have been interrupted. Set our interruption flag,
                // so that any animations get skipped.
                interruptionFlag.Set();
                break;
            case LineStatus.FinishedPresenting:
                // The line has finished being delivered by all views.
                StartCoroutine(WaitForSeconds(1f, ReadyForNextLine));
                break;
            case LineStatus.Dismissed:
                break;
        }
    }

    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        currentLine = dialogueLine;

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            // If we are using a custom Unity Input System action, enable
            // it now.
            if (continueActionType == ContinueActionType.InputSystemAction)
            {
                continueAction?.Enable();
            }
            else if (continueActionType == ContinueActionType.InputSystemActionFromAsset)
            {
                continueActionReference?.action.Enable();
            }
#endif

        if(textBoxPrefab)
        {
            lineText = Instantiate(textBoxPrefab, transform).GetComponent<TextMeshProUGUI>();
        }

        lineText.gameObject.SetActive(true);
        canvasGroup.gameObject.SetActive(true);

        if (continueButton != null)
        {
            continueButton.SetActive(false);
        }

        interruptionFlag.Clear();

        if (characterNameText == null)
        {
            if (showCharacterNameInLineView)
            {
                lineText.text = dialogueLine.Text.Text;
            }
            else
            {
                lineText.text = dialogueLine.TextWithoutCharacterName.Text;
            }
        }
        else
        {
            characterNameText.text = dialogueLine.CharacterName;
            lineText.text = dialogueLine.TextWithoutCharacterName.Text;
        }

        if (useFadeEffect)
        {
            if (useTypewriterEffect)
            {
                // If we're also using a typewriter effect, ensure that
                // there are no visible characters so that we don't
                // fade in on the text fully visible
                lineText.maxVisibleCharacters = 0;
            }
            else
            {
                // Ensure that the max visible characters is effectively unlimited.
                lineText.maxVisibleCharacters = int.MaxValue;
            }

            // Fade up and then call FadeComplete when done
            StartCoroutine(Effects.FadeAlpha(canvasGroup, 0, 1, fadeInTime, () => FadeComplete(onDialogueLineFinished), interruptionFlag));
        }
        else
        {
            // Immediately appear 
            canvasGroup.interactable = true;
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;

            if (useTypewriterEffect)
            {
                // Start the typewriter
                StartCoroutine(Effects.Typewriter(lineText, typewriterEffectSpeed, onDialogueLineFinished, interruptionFlag));
            }
            else
            {
                onDialogueLineFinished();
            }
        }
    }

    private void FadeComplete(Action onDialogueLineFinished)
    {
        if (useTypewriterEffect)
        {
            StartCoroutine(Effects.Typewriter(lineText, typewriterEffectSpeed, onDialogueLineFinished, interruptionFlag));
        }
        else
        {
            onDialogueLineFinished();
        }
    }

    public void OnContinueClicked()
    {
        if (currentLine == null)
        {
            // We're not actually displaying a line. No-op.
            return;
        }
        ReadyForNextLine();
    }

    private IEnumerator WaitForSeconds(float f, Action action)
    {
        yield return new WaitForSeconds(f);
        action.Invoke();
    }

    //private void SignalCheck()
    //{
    //    signal = true;
    //}

    private void SignalCheck(NodeType type)
    {
        if (type == signalType)
            signal = true;
    }

    private IEnumerator WaitForSignalCoroutine(string signalName)
    {
        //if (!GameSignals.NodeTypeSignals.ContainsKey(signalName))
        //{
        //    Debug.LogError($"Can't find signal named: {signalName}.");
        //    yield break;
        //}
        if (!nodeTypeDict.ContainsKey(signalName))
        {
            Debug.LogError($"Can't find signal named: {signalName}.");
            yield break;
        }
        signalType = nodeTypeDict[signalName];
        signal = false;

        GameSignals.typedSignal.AddListener(SignalCheck);
        //GameSignals.NodeTypeSignals[signalName].AddListener(SignalCheck);
        while (!signal)
        {
            yield return null;
        }

        GameSignals.typedSignal.RemoveListener(SignalCheck);
        //GameSignals.NodeTypeSignals[signalName].RemoveListener(SignalCheck);
    }

    private Coroutine WaitForSignal(string signalName)
    {
        return StartCoroutine(WaitForSignalCoroutine(signalName));
    }

    private void NodeComplete(string nodeName)
    {
        Debug.Log(nodeDict[nodeName].type);

        int curType = (int)nodeDict[nodeName].type;

        int nextType = UnityEngine.Random.Range(0, 3);
        if (nextType == curType)
            ++nextType;

        switch ((NodeType)nextType)
        {
            case NodeType.Joke:
                nextNode = jokeNodes[UnityEngine.Random.Range(0, jokeNodes.Count)].Name;
                break;
            case NodeType.Lifestyle:
                nextNode = lifestyleNodes[UnityEngine.Random.Range(0, lifestyleNodes.Count)].Name;
                break;
            case NodeType.Work:
                nextNode = workNodes[UnityEngine.Random.Range(0, workNodes.Count)].Name;
                break;
            case NodeType.Relationship:
                nextNode = relationshipNodes[UnityEngine.Random.Range(0, relationshipNodes.Count)].Name;
                break;
        }
        StartCoroutine(WaitForSeconds(1f, NextNode));
    }

    private void NextNode()
    {
        if(nextNode != null)
        {
            runner.StartDialogue(nextNode);
            nextNode = null;
        }
    }
}
