using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Invoke each UnityEvent when corresponding key pushed
/// </summary>
public class InputManager : SingletonMonoBehaviour<InputManager>
{
    //Simply push
    [SerializeField] private List<KeyAction> simpleActions;

    //Actions require several pushes (including single)
    [SerializeField] private List<KeyActionSeveralPushes> severalPushesActions;

    [Tooltip("longest interval of several pushes(s)")]
    [SerializeField] private float pushesInterval = 0.3f;

    //get time of interval of key pushes 
    private List<KeyTimer> keyTimers = new List<KeyTimer>();

    //remember whether the key is pushed
    //keyname -> isPushed
    private Dictionary<string, bool> isPushed = new Dictionary<string, bool>();

    //remember whether the key was pushed 1 frame before
    //keyname -> isPushed
    private Dictionary<string, bool> wasPushed = new Dictionary<string, bool>();

    //pushed by Push() (update each frame)
    //contains keyName
    private List<string> functionPushed = new List<string>();

    //remember corresponding key to avoid duplicated search at PushForAction()
    private Dictionary<UnityAction, string> actionToKey = new Dictionary<UnityAction, string>();

    [Tooltip("Button Name -> Key Name")]
    [SerializeField] private ButtonInterpretation[] buttonsInterpretations;

    //Button Name -> Key Name
    private Dictionary<string, string> buttonsDictionary = new Dictionary<string, string>();

    [System.Serializable]
    public class ButtonInterpretation
    {
        public string keyName;
        public string buttonName;
    }

    //contains infomations of several push keys
    private class KeyTimer
    {
        public string keyName;
        public int pushedNum = 0;

        //time passed after the latest push
        public float timer = 0f;

        //time passed after the first push
        public float totalTimer = 0f;

        //contructor
        public KeyTimer(string keyName)
        {
            this.keyName = keyName;
        }
    }

    //running SeveralPushesAction
    //Action in this list will be runned.
    //Will be deleted when key up.
    //key: key name
    private List<KeyActionSeveralPushes> runningActions
        = new List<KeyActionSeveralPushes>();

    //keys has severalPushesActions
    private List<string> severalPushesKeys = new List<string>();

    //remember non-permanent (registered by script) actions
    public List<NonPermanentAction> nonPermanentActions = new List<NonPermanentAction>();

    //for remember action added by scripts
    public class NonPermanentAction
    {
        public UnityAction action { private set; get; }
        public bool isSimple { private set; get; }
        public KeyAction.InputType inputType { private set; get; }
        public string keyName { private set; get; }
        public int pushesNum { private set; get; } = 1;
        public bool dontWait { private set; get; }

        //constructor
        public NonPermanentAction(string keyName, UnityAction action, KeyAction.InputType inputType = KeyAction.InputType.GetKey, bool isSimple = true, int pushesNum = 1, bool dontWait = false)
        {
            this.action = action;
            this.isSimple = isSimple;
            this.inputType = inputType;
            this.keyName = keyName;
            this.pushesNum = pushesNum;
            this.dontWait = dontWait;
        }
    }

    private void Start()
    {
        InitializeSeveralPushesKeys();

        InitializeButtonInterpretation();

        UpdateIsPushedIndex();
    }

    private void Update()
    {
        UpdateKeys();
        RunActions();        
    }

    /// <summary>
    /// Push by functions
    /// ex) for EventTrigger
    /// </summary>
    public static void Push(string keyName)
    {
        Instance.functionPushed.Add(keyName);
    }

    /// <summary>
    /// Register method by script
    /// </summary>
    /// <returns>return registered action. Use this to unregister</returns>
    public static NonPermanentAction RegisterAction(string keyName, 
        UnityAction action, 
        KeyAction.InputType inputType = KeyAction.InputType.GetKey, 
        bool isSimple = true, 
        int pushesNum = 1,
        bool dontWait = false)
    {
        //make class
        NonPermanentAction nonPermanentAction
            = new NonPermanentAction(keyName, action, inputType, isSimple, pushesNum);

        if (isSimple)
        {
            //simple actions

            //find if already has key
            bool found = false;
            for(int cnt = 0; cnt < Instance.simpleActions.Count(); cnt++)
            {
                if ((Instance.simpleActions[cnt].keyName == keyName)
                    && (Instance.simpleActions[cnt].inputType == inputType))
                {
                    //found
                    //register
                    Instance.simpleActions[cnt].AddListener(action);
                    
                    found = true;
                    break;
                }
            }

            //not found
            if (!found)
            {
                //register new key
                KeyAction newKeyAction = new KeyAction(new UnityEvent(), keyName, inputType);
                newKeyAction.AddListener(action);
                Instance.simpleActions.Add(newKeyAction);

                //add the new key
                Instance.UpdateIsPushedIndex();
            }
        }
        else
        {
            //several pushes

            //find if already has key
            bool found = false;
            for (int cnt = 0; cnt < Instance.severalPushesActions.Count(); cnt++)
            {
                if ((Instance.severalPushesActions[cnt].keyName == keyName)
                        &&(Instance.severalPushesActions[cnt].inputType == inputType)
                        &&(Instance.severalPushesActions[cnt].dontWait == dontWait)
                        &&(Instance.severalPushesActions[cnt].pushesNum == pushesNum))
                {
                    //found
                    //register
                    Instance.severalPushesActions[cnt].AddListener(action);

                    found = true;
                    break;
                }
            }

            //not found
            if (!found)
            {
                //register new key
                KeyAction newKeyAction = new KeyActionSeveralPushes(new UnityEvent(), keyName, pushesNum, inputType, dontWait);
                newKeyAction.AddListener(action);
                Instance.simpleActions.Add(newKeyAction);

                //add the new key
                Instance.UpdateIsPushedIndex();
            }
        }

        //return registered action
        return nonPermanentAction;
    }

    /// <summary>
    /// Unregister action
    /// </summary>
    /// <param name="nonPermanentAction">What you want to remove. You got this when you used RegisterAction()</param>
    /// <returns>false if there was no corresponding action</returns>
    public static bool RemoveAction(NonPermanentAction nonPermanentAction)
    {
        if (nonPermanentAction.isSimple)
        {
            //simple actions

            //find corresponding action
            for (int cnt = 0; cnt < Instance.simpleActions.Count(); cnt++)
            {
                if ((Instance.simpleActions[cnt].keyName == nonPermanentAction.keyName)
                    && (Instance.simpleActions[cnt].inputType == nonPermanentAction.inputType))
                {
                    //found
                    //remove
                    Instance.simpleActions[cnt].RemoveListener(nonPermanentAction.action);

                    return true;
                }
            }

            //not found
            return false;
        }
        else
        {
            //several pushes

            //find corresponding action
            for (int cnt = 0; cnt < Instance.severalPushesActions.Count(); cnt++)
            {
                if ((Instance.severalPushesActions[cnt].keyName == nonPermanentAction.keyName)
                        && (Instance.severalPushesActions[cnt].inputType == nonPermanentAction.inputType)
                        && (Instance.severalPushesActions[cnt].dontWait == nonPermanentAction.dontWait)
                        && (Instance.severalPushesActions[cnt].pushesNum == nonPermanentAction.pushesNum))
                {
                    //found
                    //register
                    Instance.severalPushesActions[cnt].RemoveListener(nonPermanentAction.action);

                    return true;
                }
            }

            //not found
            return false;
        }
    }

    /// <summary>
    /// Update isPushed by GetKey + GetButton + Push functions
    /// </summary>
    private void UpdateKeys()
    {
        //update wasPushed (1 frame before)
        wasPushed = new Dictionary<string, bool>(isPushed);

        //reset
        foreach (string key in wasPushed.Keys)
        {
            isPushed[key] = false;
        }

        //check key
        foreach(string keyName in wasPushed.Keys)
        {
            if (Input.GetKey(keyName))
            {
                isPushed[keyName] = true;
            }
        }

        //check buttons
        foreach(string buttonName in buttonsDictionary.Keys)
        {
            if (Input.GetButton(buttonName))
            {
                isPushed[buttonsDictionary[buttonName]] = true;
            }
        }

        //check function Push
        foreach(string keyName in functionPushed)
        {
            isPushed[keyName] = true;
        }

        //reset
        functionPushed = new List<string>();
    }

    /// <summary>
    /// Run actions if pushed
    /// </summary>
    private void RunActions()
    {
        RunSimpleActions();
        CheckSeveralPushesIncreases();

        CheckTimers();

        RunSeveralPushesActions();
    }

    /// <summary>
    /// check if pushed and invoke of simpleActions
    /// </summary>
    private void RunSimpleActions()
    {
        foreach(KeyAction keyAction in simpleActions)
        {
            //correspond to input type
            switch (keyAction.inputType)
            {
                case KeyAction.InputType.GetKey:
                    //Invoke during pushed
                    if(GetKey(keyAction.keyName)){
                        keyAction.Invoke();
                    }
                    break;

                case KeyAction.InputType.GetKeyDown:
                    //Invoke right on the time of pushed
                    if(GetKeyDown(keyAction.keyName)){
                        keyAction.Invoke();
                    }
                    break;

                case KeyAction.InputType.GetKeyUp:
                    //Invoke right on the time key up
                    if (GetKeyUp(keyAction.keyName))
                    {
                        keyAction.Invoke();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Get GetKeyDown of severalPushActions for increase the push counter
    /// </summary>
    private void CheckSeveralPushesIncreases()
    {
        foreach (string keyName in severalPushesKeys)
        {
            if (GetKeyDown(keyName))
            {
                //counter increases
                KeyTimer keyTimer = GetTimer(keyName);
                keyTimer.pushedNum++;

                //reset timer
                keyTimer.timer = 0f;

                ////run immedietly
                //find corresponding action               
                foreach (KeyActionSeveralPushes keyAction in severalPushesActions)
                {
                    if ((keyAction.keyName == keyName)
                        && (keyAction.pushesNum == keyTimer.pushedNum)
                        && (keyAction.dontWait == true))
                    {
                        //found
                        runningActions.Add(keyAction);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Return reference of corresponding KeyTimer from keyTimers.
    /// Create one if none found.
    /// </summary>
    private KeyTimer GetTimer(string keyName)
    {
        foreach(KeyTimer keyTimer in keyTimers)
        {
            if(keyTimer.keyName == keyName)
            {
                //found
                return keyTimer;
            }
        }

        //not found
        
        //add new timer
        KeyTimer newTimer = new KeyTimer(keyName);
        keyTimers.Add(newTimer);
        return newTimer;
    }

    /// <summary>
    /// Check keyTimers and start running if there is exceeding limit
    /// </summary>
    private void CheckTimers()
    {
        List<KeyTimer> _keyTimers
            = new List<KeyTimer>(keyTimers);
        foreach(KeyTimer keyTimer in _keyTimers)
        {
            //advance timer
            keyTimer.timer += Time.deltaTime;

            //check if exceeding
            if (keyTimer.timer >= pushesInterval)
            {
                //exceeded

                //actoins with more key priors
                //get rid of same key action if exists
                List<KeyActionSeveralPushes> _runningActions
                    = new List<KeyActionSeveralPushes>(runningActions);
                foreach (KeyActionSeveralPushes action in _runningActions)
                {
                    if (action.keyName == keyTimer.keyName)
                    {
                        //found
                        runningActions.Remove(action);
                    }
                }

                ////start running
                //get corresponding action
                foreach (KeyActionSeveralPushes action in severalPushesActions)
                {
                    if ((action.keyName == keyTimer.keyName)
                        && (action.pushesNum == keyTimer.pushedNum))
                    {
                        //corresponded

                        //start running
                        runningActions.Add(action);
                    }
                }

                //get rid of timer
                keyTimers.Remove(keyTimer);
            }
        }
    }

    /// <summary>
    /// run the severalPushes
    /// </summary>
    private void RunSeveralPushesActions()
    {
        List<KeyActionSeveralPushes> _runningActions
            = new List<KeyActionSeveralPushes>(runningActions);
        foreach(KeyActionSeveralPushes action in _runningActions)
        {
            switch (action.inputType)
            {
                case KeyAction.InputType.GetKey:
                    //invoke while pushed
                    if (GetKey(action.keyName))
                    {
                        action.Invoke();
                    }
                    else
                    {
                        //not push => delete
                        runningActions.Remove(action);
                    }
                    break;

                case KeyAction.InputType.GetKeyDown:
                    //run immedietly

                    //dontwait==true => already run
                    if (!action.dontWait)
                    {
                        action.Invoke();
                    }
                    runningActions.Remove(action);
                    break;

                case KeyAction.InputType.GetKeyUp:
                    //run when up
                    //not using GetKeyUp because of possibility of
                    //key up during waiting for keyTimer
                    if (GetKeyUp(action.keyName))
                    {
                        action.Invoke();
                        runningActions.Remove(action);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Call this when actions changed
    /// </summary>
    private void InitializeSeveralPushesKeys()
    {
        List<string> newList = new List<string>();

        foreach(KeyActionSeveralPushes keyAction in severalPushesActions)
        {
            newList.Add(keyAction.keyName);
        }

        //get rid of duplicates
        severalPushesKeys = newList.Distinct().ToList();
    }

    /// <summary>
    /// Call this if interpretation changed
    /// </summary>
    private void InitializeButtonInterpretation()
    {
        //reset
        buttonsDictionary = new Dictionary<string, string>();

        //register all
        foreach(ButtonInterpretation interpretation in buttonsInterpretations)
        {
            buttonsDictionary[interpretation.buttonName] = interpretation.keyName;
        }
    }

    /// <summary>
    /// Check if there is index missing in isPushed, and add the missing index
    /// </summary>
    private void UpdateIsPushedIndex()
    {
        foreach(KeyAction keyAction in simpleActions)
        {
            if (!isPushed.ContainsKey(keyAction.keyName))
            {
                //doesn't contain
                //add the index
                isPushed[keyAction.keyName] = false;
            }

            if (!wasPushed.ContainsKey(keyAction.keyName))
            {
                //doesn't contain
                //add the index
                wasPushed[keyAction.keyName] = false;
            }
        }

        foreach(KeyActionSeveralPushes keyAction in severalPushesActions)
        {
            if (!isPushed.ContainsKey(keyAction.keyName))
            {
                //doesn't contain
                //add the index
                isPushed[keyAction.keyName] = false;
            }

            if (!wasPushed.ContainsKey(keyAction.keyName))
            {
                //doesn't contain
                //add the index
                wasPushed[keyAction.keyName] = false;
            }
        }
    }

    /// <summary>
    /// return true while pushed
    /// </summary>
    private bool GetKey(string keyName)
    {
        if (isPushed[keyName])
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// return true only the first frame pushed
    /// </summary>
    private bool GetKeyDown(string keyName)
    {
        if (!wasPushed[keyName] && isPushed[keyName])
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// return true only the first frame stop pushed
    /// </summary>
    private bool GetKeyUp(string keyName)
    {
        if (wasPushed[keyName] && !isPushed[keyName])
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Actions and its key
    /// </summary>
    [System.Serializable] public class KeyAction
    {
        public enum InputType
        {
            GetKey,
            GetKeyDown,
            GetKeyUp
        }

        [Tooltip("Methods called when key pushed")]
        [SerializeField] private UnityEvent _actions;

        [Tooltip("Key name. Google 'Unity Input key name'")]
        [SerializeField] private string _keyName;

        [Tooltip("0:GetKey, 1:GetKeyDown, 2:GetKeyUp")]
        [SerializeField] private InputType _inputType = 0;
        
        //getters. These are necessary for SerializeField
        public string keyName
        {
            get
            {
                return _keyName;
            }
        }

        public InputType inputType
        {
            get
            {
                return _inputType;
            }
        }

        public UnityEvent actions
        {
            get
            {
                return _actions;
            }
        }

        //constructor
        public KeyAction(UnityEvent actions, string keyName, InputType inputType = InputType.GetKey)
        {
            this._actions = actions;
            this._keyName = keyName;
            this._inputType = inputType;
        }

        /// <summary>
        /// Simply invoke the registered UnityActions
        /// </summary>
        public void Invoke()
        {
            actions.Invoke();
        }

        /// <summary>
        /// AddListener to UnityEvent
        /// </summary>
        public void AddListener(UnityAction action)
        {
            _actions.AddListener(action);
        }

        /// <summary>
        /// RemoveListener to UnityEvent
        /// </summary>
        public void RemoveListener(UnityAction action)
        {
            _actions.RemoveListener(action);
        }
    }

    /// <summary>
    /// Actions and its key. For Several push.
    /// Single push must registered by this when there is also double pushes for the same key.
    /// </summary>
    [System.Serializable] public class KeyActionSeveralPushes : KeyAction
    {
        [Tooltip("How many pushes required")]
        [SerializeField] private int _pushesNum = 1;

        [Tooltip("if true, the action invoked before next push. The invoke will cancel when next push appear")]
        [SerializeField] private bool _dontWait = false;

        //getters
        public int pushesNum
        {
            get
            {
                return _pushesNum;
            }
        }

        public bool dontWait
        {
            get
            {
                return _dontWait;
            }
        }

        //contructor
        public KeyActionSeveralPushes(UnityEvent actions, string keyName, int pushesNum, InputType inputType = InputType.GetKey, bool dontWait = false)
            : base(actions, keyName, inputType)
        {
            this._pushesNum = pushesNum;
            this._dontWait = dontWait;
        }
    }
}
