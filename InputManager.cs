using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Invoke each UnityEvent when corresponding key pushed
/// </summary>
public class InputManager : MonoBehaviour
{
    //Simply push
    [SerializeField] private KeyAction[] simpleActions;

    //Actions require several pushes (including single)
    [SerializeField] private KeyActionSeveralPushes[] severalPushesActions;

    [Tooltip("longest interval of several pushes(s)")]
    [SerializeField] private float pushesInterval = 0.3f;

    private List<KeyTimer> keyTimers = new List<KeyTimer>();

    //contains infomations of several push keys
    private class KeyTimer
    {
        public string keyName;
        public int pushedNum = 0;

        //time passed after the latest push
        public float timer = 0f;

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

    private void Start()
    {
        InitializeSeveralPushesKeys();
    }

    private void Update()
    {
        CheckKeys();        
    }

    /// <summary>
    /// Check key is pushed and process it if pushed
    /// </summary>
    private void CheckKeys()
    {
        CheckSimpleActions();
        CheckSeveralPushesIncreases();

        CheckTimers();

        CheckSeveralPushes();
    }

    /// <summary>
    /// check if pushed and invoke of simpleActions
    /// </summary>
    private void CheckSimpleActions()
    {
        foreach(KeyAction keyAction in simpleActions)
        {
            //correspond to input type
            switch (keyAction.inputType)
            {
                case 0:
                    //Invoke during pushed
                    if(Input.GetKey(keyAction.keyName)){
                        keyAction.Invoke();
                    }
                    break;

                case 1:
                    //Invoke right on the time of pushed
                    if(Input.GetKeyDown(keyAction.keyName)){
                        keyAction.Invoke();
                    }
                    break;

                case 2:
                    //Invoke right on the time key up
                    if(Input.GetKeyUp(keyAction.keyName)){
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
            if (Input.GetKeyDown(keyName))
            {
                //counter increases
                KeyTimer keyTimer = GetTimer(keyName);
                keyTimer.pushedNum++;

                //reset timer
                keyTimer.timer = 0f;
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
    private void CheckSeveralPushes()
    {
        List<KeyActionSeveralPushes> _runningActions
            = new List<KeyActionSeveralPushes>(runningActions);
        foreach(KeyActionSeveralPushes action in _runningActions)
        {
            switch (action.inputType)
            {
                case 0:
                    //invoke while pushed
                    if (Input.GetKey(action.keyName))
                    {
                        action.Invoke();
                    }
                    else
                    {
                        //not push => delete
                        runningActions.Remove(action);
                    }
                    break;

                case 1:
                    //run immedietly
                    action.Invoke();
                    runningActions.Remove(action);
                    break;

                case 2:
                    //run when up
                    //not using GetKeyUp because of possibility of
                    //key up during waiting for keyTimer
                    if (!Input.GetKey(action.keyName))
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
    /// Actions and its key
    /// </summary>
    [System.Serializable] public class KeyAction
    {
        [Tooltip("Methods called when key pushed")]
        [SerializeField] private UnityEvent actions;

        [Tooltip("Key name. Google 'Unity Input key name'")]
        [SerializeField] private string _keyName;

        [Tooltip("0:GetKey, 1:GetKeyDown, 2:GetKeyUp")]
        [SerializeField] private byte _inputType = 0;
        
        //getters. These are necessary for SerializeField
        public string keyName
        {
            get
            {
                return _keyName;
            }
        }

        public byte inputType
        {
            get
            {
                return _inputType;
            }
        }

        /// <summary>
        /// Simply invoke the registered UnityActions
        /// </summary>
        public void Invoke()
        {
            actions.Invoke();
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

        //getters
        public int pushesNum
        {
            get
            {
                return _pushesNum;
            }
        }
    }
}
