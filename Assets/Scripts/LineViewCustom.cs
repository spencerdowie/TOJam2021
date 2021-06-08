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

    private InterruptionFlag interruptionFlag = new InterruptionFlag();

    [SerializeField]
    private GameObject textBoxPrefab = null;
    private CanvasGroup currentLineCanvasGroup = null;

    private bool signal = false;
    private NodeType signalType = NodeType.NULL;

    private List<Node> jokeNodes = new List<Node>(),
        lifestyleNodes = new List<Node>(),
        workNodes = new List<Node>(),
        relationshipNodes = new List<Node>();

    private Dictionary<string, NodeStruct> nodeDict = new Dictionary<string, NodeStruct>();

    private DialogueRunner runner = null;

    public void Start()
    {
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
        //runner.onNodeComplete.AddListener(NodeComplete);
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

        StartCoroutine(WaitForSeconds(0.1f, () => runner.StartDialogue("Node")));
    }

    public void Reset()
    {
        canvasGroup = GetComponentInParent<CanvasGroup>();
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
                StartCoroutine(WaitForSeconds(2f, ReadyForNextLine));
                break;
            case LineStatus.Dismissed:
                break;
        }
    }

    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        if(textBoxPrefab)
        {
            Transform textboxTransform = Instantiate(textBoxPrefab, transform).transform;
            if (dialogueLine.CharacterName == "Skelly")
            {
                textboxTransform.localScale = new Vector3(-1, 1, 1);
                textboxTransform.GetChild(0).localScale = new Vector3(-1, 1, 1);
            }
            lineText = textboxTransform.GetComponentInChildren<TextMeshProUGUI>();
            currentLineCanvasGroup = textboxTransform.GetComponent<CanvasGroup>();
        }

        canvasGroup.gameObject.SetActive(true);

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
            StartCoroutine(Effects.FadeAlpha(currentLineCanvasGroup, 0, 1, fadeInTime, () => FadeComplete(onDialogueLineFinished), interruptionFlag));
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

    private IEnumerator WaitForSeconds(float f, Action action)
    {
        yield return new WaitForSeconds(f);
        action.Invoke();
    }

    private void SignalCheck(NodeType type)
    {
        if (type == signalType)
            signal = true;
    }

    private IEnumerator WaitForSignalCoroutine(string signalName, Action<string> action)
    {
        string currentNode = runner.CurrentNodeName;
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
        yield return new WaitUntil(() => signal);

        GameSignals.typedSignal.RemoveListener(SignalCheck);
        //GameSignals.NodeTypeSignals[signalName].RemoveListener(SignalCheck);
        action(currentNode);
    }

    private void WaitForSignal(string signalName)
    {
        StartCoroutine(WaitForSignalCoroutine(signalName, NodeComplete));
    }

    private void NodeComplete(string nodeName)
    {
        Debug.Log(nodeDict[nodeName].type);

        int curType = (int)nodeDict[nodeName].type;

        int nextType = UnityEngine.Random.Range(0, 3);
        if (nextType == curType)
            ++nextType;

        string nextNode = null;
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
        NextNode(nextNode);

        StartCoroutine(Effects.FadeAlpha(canvasGroup, 1, 0, fadeOutTime));
    }

    private void NextNode(string nextNode)
    {
        if(nextNode != null)
        {
            runner.StartDialogue(nextNode);
        }
    }


}
