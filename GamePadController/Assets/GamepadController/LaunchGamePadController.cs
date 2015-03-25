using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using XInputDotNetPure;
using System.Reflection;

/*--> Additional Objectives
// Use the controller on a different thread if possible
//add dualstick for 2d games
//add deadzone function


//Check if button is axis and transform axis values to button
//Use custom editor for "enable controller"
//Save/Load preconfigured inputs in a file
//allow change of key or button or axis at runtime (public class and variables plus custom functions)
//First controllers values get copied in all other controllers (so as not to rewrite them)
//make some deafult parameters (Ex : Right Stick is always axis, if button is axis, then only one direction)
//Add other default Parameters (Ex : LeftStick X&Y already have Horizontal and Vertical written, etc...)
//Add smart autocomplete
*/

[System.Serializable]
public class LaunchGamePadController : MonoBehaviour
{
    GamePadController gamePadController = new GamePadController();
    public bool DebugMode = true;

#if UNITY_EDITOR
    public GamePadController.DebugGamePad DebugParameters;
#endif

    private static float[] _vibrationTimers = new float[4];
    private bool[] _controllersConnected = new bool[4];

    public bool useComputerInputs = true;
    public bool[] enableController = new bool[4] { true, false, false, false };
    [HideInInspector]
    [SerializeField]
    public GamePadController.ControllerComputerInputs[] computerControllers = new GamePadController.ControllerComputerInputs[4] { 
        new GamePadController.ControllerComputerInputs(0), 
        new GamePadController.ControllerComputerInputs(1), 
        new GamePadController.ControllerComputerInputs(2), 
        new GamePadController.ControllerComputerInputs(3)};

    void Awake()
    {
        GamePadController.computerInputs = computerControllers;
        gamePadController.useComputerInputs = useComputerInputs;
        gamePadController.Start();
        _controllersConnected = GamePadController.ControllerConnected;
    }

    void Update()
    {
#if UNITY_EDITOR
        GamePadController.DebugMode = DebugMode;        
#endif

        gamePadController.Update();

        _vibrationTimers = GamePadController.VibrationTimers;
        for (int i = 0; i < _vibrationTimers.Length; i++)
        {
            if (_vibrationTimers[i] > 0 && _vibrationTimers != null)
            {
                var timer = _vibrationTimers[i];
                StartCoroutine(ResetTimer(i, timer));
                _vibrationTimers[i] = -1f;
                GamePadController.VibrationTimers = _vibrationTimers;
            }
        }

#if UNITY_EDITOR
        if (DebugMode)
            DebugParameters.Update();  
#endif
    }

    IEnumerator ResetTimer(int controllerId, float timer)
    {
        yield return new WaitForSeconds(timer);

        gamePadController.StopControllerVibration(controllerId);
    }

}

#if UNITY_EDITOR
[System.Serializable]
[CanEditMultipleObjects]
[CustomEditor(typeof(LaunchGamePadController))]
public class LaunchGamePadControllerInspector : Editor
{

    [SerializeField]
    public GamePadController.ControllerComputerInputs[] controllers = new GamePadController.ControllerComputerInputs[4];

    private bool[] foldOutStates = new bool[4];
    bool guiCreated = false;
    public bool editorDebug = false;

    LaunchGamePadController mainControllerScript;

    public void OnEnable()
    {
        mainControllerScript = (LaunchGamePadController)target;
        for (int i = 0; i < controllers.Length; i++)
        {
            controllers[i] = mainControllerScript.computerControllers[i];            
#if UNITY_EDITOR
            controllers[i].targetInspector = this;
            mainControllerScript.computerControllers[i].targetInspector = this;
#endif
            foldOutStates[i] = false;
        }
        guiCreated = true;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.BeginVertical();
        //GUILayout.BeginHorizontal();
        //EditorGUILayout.LabelField("Live Editor Debug :", GUILayout.MaxWidth(120));
        //editorDebug = EditorGUILayout.Toggle(editorDebug);
        //GUILayout.EndHorizontal();
        if (guiCreated)
        {
            for (int i = 0; i < controllers.Length; i++)
            {
                controllers[i].inputsEnabled = mainControllerScript.enableController[i];
                if (foldOutStates[i])
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("-", GUILayout.MinWidth(20), GUILayout.MaxWidth(30)))
                    {
                        foldOutStates[i] = false;
                    }
                    EditorGUILayout.LabelField("Controller N°" + (i + 1));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    controllers[i].CreateControllerInputs();
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (mainControllerScript.enableController[i])
                    {
                        if (GUILayout.Button("+", GUILayout.MinWidth(20), GUILayout.MaxWidth(30)))
                        {
                            foldOutStates[i] = true;
                        }
                    }
                    EditorGUILayout.LabelField("Controller N°" + (i + 1));
                    GUILayout.EndHorizontal();
                }
                if (!mainControllerScript.enableController[i])
                {
                    foldOutStates[i] = false;
                }
                controllers[i].UpdateControllerInputs();
            }
        }
        GUILayout.EndVertical();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(mainControllerScript);
        }
        serializedObject.ApplyModifiedProperties();
        serializedObject.Update();

        //Debug.Log(controllers[0].ButtonA.buttonName);

        if (editorDebug)
        {
            for (int i = 0; i < controllers.Length; i++)
            {
                controllers[i].UpdateControllerInputs();
            }
            this.Repaint();
        }
    }
}
#endif

public class GamePadController
{

    [System.Serializable]
    public enum typeInput
    {
        Axis,
        Button,
        Key
    }
    public enum axisDir
    {
        plus,
        minus,
        both
    }
    public enum ButtonStates
    {
        Released,
        Pressed,
        Held,
        Zero
    }

    public static Controller GamePadOne;
    public static Controller GamePadTwo;
    public static Controller GamePadThree;
    public static Controller GamePadFour;

    public bool useComputerInputs;

    public static bool DebugMode { private get; set; }

    public static float[] VibrationTimers = new float[4];
    public static bool[] ControllerConnected = new bool[4];

    protected static GamePadState[] GamePadStateArray = new GamePadState[4];
    protected static PlayerIndex[] PlayerIndexArray = new PlayerIndex[4];
    protected static Controller[] GamePads = new Controller[4];

    //private static string[] _controllerNames = new string[] {"One", "Two", "Three", "Four"};
    private string[] _abxy = new string[] { "A", "B", "X", "Y", "L3", "R3", "start", "select", "LB", "RB" };
    private string[] _abxyXinput = new string[] { "A", "B", "X", "Y", "LeftStick", "RightStick", "Start", "Back", "LeftShoulder", "RightShoulder" };
    private string[] _dPad = new string[] { "Up", "Down", "Left", "Right" };
    private string[] _controllerClassButtons = new string[] { "A", "B", "X", "Y", "LB", "RB", "Start", "Select", "R3", "L3", "UP", "DOWN", "LEFT", "RIGHT" };
    private string[] _computerClassButtons = new string[] { "ButtonA", "ButtonB", "ButtonX", "ButtonY", "LeftButton", "RightButton", "start", "select", "ButtonR3", "ButtonL3", "ButtonUP", "ButtonDOWN", "ButtonLEFT", "ButtonRIGHT" };

    #region Failed attempt to cache reflection values
    //private Controller.ButtonsStates[] statesArrayCache = new Controller.ButtonsStates[14];
    //private ButtonState[] xinputStatesCache = new ButtonState[14];
    //private bool[] cachedStates = new bool[] { false, false, false, false };
    #endregion

    public static ControllerComputerInputs[] computerInputs = new ControllerComputerInputs[4];

    public void Start()
    {
        PlayerIndexArray[0] = PlayerIndex.One;
        PlayerIndexArray[1] = PlayerIndex.Two;
        PlayerIndexArray[2] = PlayerIndex.Three;
        PlayerIndexArray[3] = PlayerIndex.Four;

        for (int i = 0; i < 4; i++)
        {
            GamePadStateArray[i] = GamePad.GetState(PlayerIndexArray[i]);
            GamePads[i] = new Controller(i);
        }
        GamePadOne = GamePads[0];
        GamePadTwo = GamePads[1];
        GamePadThree = GamePads[2];
        GamePadFour = GamePads[3];
    }

    public void Update()
    {
        for (int i = 0; i < 4; i++)
        {
            GamePadStateArray[i] = GamePad.GetState(PlayerIndexArray[i]);
            if (GamePadStateArray[i].IsConnected && !ControllerConnected[i])
            {
                ControllerConnected[i] = true;
                if (DebugMode)
                    Debug.Log("<color=green>Controller N°" + (i + 1) + " connected.</color>");
            }
            else if (GamePadStateArray[i].IsConnected && ControllerConnected[i])
            {
                UpdateGamePad(i);
                UpdateUnityInputs(i);
            }
            else if (!GamePadStateArray[i].IsConnected && !ControllerConnected[i] && useComputerInputs)
            {
                UpdateUnityInputs(i);
            }
            else if (!GamePadStateArray[i].IsConnected && ControllerConnected[i])
            {
                ControllerConnected[i] = false;
                if (DebugMode)
                {
                    if (useComputerInputs && computerInputs[i].inputsEnabled)
                        Debug.Log("<color=teal>Controller N°" + (i + 1) + " not connected, computer inputs enabled.</color>");
                    Debug.Log("<color=brown>Controller N°" + (i + 1) + " disconnected.</color>");
                }
            }
        }
    }

    private void UpdateGamePad(int controllerId)
    {
        UpdateGamePadAxis(controllerId);
        UpdateGamePadButtons(controllerId);
    }

    private void UpdateGamePadAxis(int controllerId)
    {
        GamePads[controllerId].LeftTrigger = GamePadStateArray[controllerId].Triggers.Left;
        GamePads[controllerId].RightTrigger = GamePadStateArray[controllerId].Triggers.Right;

        GamePads[controllerId].LeftStick.X = GamePadStateArray[controllerId].ThumbSticks.Left.X;
        GamePads[controllerId].LeftStick.Y = GamePadStateArray[controllerId].ThumbSticks.Left.Y;
        GamePads[controllerId].RightStick.X = GamePadStateArray[controllerId].ThumbSticks.Right.X;
        GamePads[controllerId].RightStick.Y = GamePadStateArray[controllerId].ThumbSticks.Right.Y;
    }

    private void UpdateGamePadButtons(int controllerId)
    {
        var curGamePad = GamePadStateArray[controllerId];
        for (int i = 0; i < 14; i++)
        {
            ButtonState curStateButton = ButtonState.Released;
            Controller.ButtonsStates curButton = new Controller.ButtonsStates();
            if (i <= _abxy.Length - 1)
            {
                curButton = (Controller.ButtonsStates)GamePads[controllerId].GetType().GetField(_abxy[i]).GetValue(GamePads[controllerId]);
                curStateButton = (ButtonState)typeof(GamePadButtons).GetProperty(_abxyXinput[i]).GetValue(GamePadStateArray[controllerId].Buttons, null);
            }
            else
            {
                int iValue = i - 10;
                curButton = (Controller.ButtonsStates)GamePads[controllerId].GetType().GetField(_dPad[iValue].ToUpper()).GetValue(GamePads[controllerId]);
                curStateButton = (ButtonState)typeof(GamePadDPad).GetProperty(_dPad[iValue]).GetValue(GamePadStateArray[controllerId].DPad, null);
            }

            #region Failed attempt to cache reflection values
            /* --> Failed attempt to cache reflection values
                if(!cachedStates[controllerId]) {
                if (i <= _abxy.Length - 1)
                {
                    statesArrayCache[i] = (Controller.ButtonsStates)GamePads[controllerId].GetType().GetField(_abxy[i]).GetValue(GamePads[controllerId]);
                    xinputStatesCache[i] = (ButtonState)typeof(GamePadButtons).GetProperty(_abxyXinput[i]).GetValue(GamePadStateArray[controllerId].Buttons, null);
                }
                else
                {
                    int iValue = i - 10;
                    statesArrayCache[i] = (Controller.ButtonsStates)GamePads[controllerId].GetType().GetField(_dPad[iValue].ToUpper()).GetValue(GamePads[controllerId]);
                    xinputStatesCache[i] = (ButtonState)typeof(GamePadDPad).GetProperty(_dPad[iValue]).GetValue(GamePadStateArray[controllerId].DPad, null);
                }
                cachedStates[controllerId] = true;
            }
            curButton = statesArrayCache[i]; 
            curStateButton = xinputStatesCache[i]; 
             */
            #endregion

            #region Old code, stopped working for some reason
            //-----> Old code, stopped working for some reason
            //Check Button States
            //if (curButton.Pressed && curStateButton == ButtonState.Released)
            //{
            //    curButton.Pressed = false;
            //    //Or use reflection again curButton.GetType().GetField("Pressed").SetValue(curButton, false);
            //}
            //if (!curButton.Pressed && (curButton.Released || curButton.Held) && curStateButton == ButtonState.Released)
            //{
            //    curButton.Pressed = true;
            //    Debug.Log("Pressed");
            //    curButton.Held = false;
            //    curButton.Released = false;
            //}
            //if (!curButton.Pressed && curButton.Released && curStateButton == ButtonState.Pressed)
            //{
            //    curButton.Held = true;
            //    curButton.Released = false;
            //    Debug.Log("Held");
            //}
            //if (!curButton.Pressed && !curButton.Released && !curButton.Held && curStateButton == ButtonState.Pressed)
            //{
            //    curButton.Released = true;
            //    Debug.Log("Released");
            //}
            #endregion

            #region Check Button States (New code, working for now, order of conditions is primordial)
            //Held
            if (curButton.Pressed && (!curButton.Released || !curButton.Held) && curStateButton == ButtonState.Pressed)
            {
                curButton.Pressed = false;
                //Debug.Log("Held");
                curButton.Held = true;
                curButton.Released = false;
                curButton.Zero = false;
                //Or use reflection again curButton.GetType().GetField("Pressed").SetValue(curButton, false);
            }
            else if (!curButton.Pressed && !curButton.Released && !curButton.Zero && curButton.Held && curStateButton == ButtonState.Pressed)
            {
                curButton.Pressed = false;
                //Debug.Log("Held");
                curButton.Held = true;
                curButton.Released = false;
                curButton.Zero = false;
            }

            //Pressed
            if (curButton.Pressed && curStateButton == ButtonState.Pressed)
            {
                curButton.Pressed = false;
                curButton.Zero = false;
            }
            else if (!curButton.Pressed && curButton.Zero && curStateButton == ButtonState.Pressed)
            {
                curButton.Pressed = true;
                //Debug.Log("Pressed");
                curButton.Held = false;
                curButton.Released = false;
                curButton.Zero = false;
            }

            //Zero
            if (!curButton.Pressed && !curButton.Held && !curButton.Zero && curButton.Released && curStateButton == ButtonState.Released)
            {
                curButton.Released = false;
                curButton.Held = false;
                curButton.Pressed = false;
                curButton.Zero = true;
                //Debug.Log("Zero1");
            }
            else if (!curButton.Pressed && !curButton.Held && !curButton.Released && !curButton.Zero && curStateButton == ButtonState.Released)
            {
                curButton.Released = false;
                curButton.Held = false;
                curButton.Pressed = false;
                curButton.Zero = true;
                //Debug.Log("Zero2");
            }

            //Released
            if (curButton.Released && curStateButton == ButtonState.Released)
            {
                curButton.Pressed = false;
                curButton.Held = false;
                curButton.Released = false;
                curButton.Zero = true;
                //Debug.Log("Released2");
            }
            else if ((curButton.Pressed || curButton.Held) && !curButton.Released && !curButton.Zero && curStateButton == ButtonState.Released)
            {
                curButton.Released = true;
                //Debug.Log("Released 1");
            }
            #endregion

            #region Failed attempt to cache reflection values
            /*--> Failed attempt to cache reflection values
             * statesArrayCache[i] = curButton; 
            xinputStatesCache[i] = curStateButton; */
            #endregion

            //Finished Checking, Set Values
            if (i <= _abxy.Length - 1)
            {
                GamePads[controllerId].GetType().GetField(_abxy[i]).SetValue(GamePads[controllerId], curButton);
            }
            else
            {
                int iValue = i - 10;
                GamePads[controllerId].GetType().GetField(_dPad[iValue].ToUpper()).SetValue(GamePads[controllerId], curButton);
            }
        }
    }

    private void UpdateUnityInputs(int controllerId)
    {
        if (computerInputs[controllerId].inputsEnabled)
        {
            computerInputs[controllerId].UpdateControllerInputs();
            SetUnityInputs(controllerId);
        }
        else
            return;
    }

    private void SetUnityInputs(int controllerId)
    {
        SetUnityAxis(controllerId);
        SetUnityButtons(controllerId);
    }

    private void SetUnityAxis(int controllerId)
    {
        ControllerComputerInputs curComputerInputs = computerInputs[controllerId];

        GamePads[controllerId].LeftTrigger = SetAndCheckIfUnityAxisIsSetup(controllerId, curComputerInputs.LeftTrigger, GamePads[controllerId].LeftTrigger);
        GamePads[controllerId].RightTrigger = SetAndCheckIfUnityAxisIsSetup(controllerId, curComputerInputs.RightTrigger, GamePads[controllerId].RightTrigger);

        GamePads[controllerId].LeftStick.X = SetAndCheckIfUnityAxisIsSetup(controllerId, curComputerInputs.LeftStickX, GamePads[controllerId].LeftStick.X);
        GamePads[controllerId].LeftStick.Y = SetAndCheckIfUnityAxisIsSetup(controllerId, curComputerInputs.LeftStickY, GamePads[controllerId].LeftStick.Y);
        GamePads[controllerId].RightStick.X = SetAndCheckIfUnityAxisIsSetup(controllerId, curComputerInputs.RightStickX, GamePads[controllerId].RightStick.X);
        GamePads[controllerId].RightStick.Y = SetAndCheckIfUnityAxisIsSetup(controllerId, curComputerInputs.RightStickY, GamePads[controllerId].RightStick.Y);
    }

    private float SetAndCheckIfUnityAxisIsSetup(int controllerId, ControllerComputerInputs.ButtonInput computerInput, float gamePadVal)
    {
        if (ControllerConnected[controllerId])
        {
            if (computerInput.inputSetup && gamePadVal == 0)
                return computerInput.axisValue;
            else
                return gamePadVal;
        }
        else
        {
            {
                if (computerInput.inputSetup)
                    return computerInput.axisValue;
                else
                    return 0f;
            }
        }
    }

    private void SetUnityButtons(int controllerId)
    {
        for (int i = 0; i < _controllerClassButtons.Length; i++)
        {
            Controller.ButtonsStates curGamePadButton = new Controller.ButtonsStates();
            ControllerComputerInputs.ButtonInput curcomputerButton = new ControllerComputerInputs.ButtonInput();

            string gamePadButton = _controllerClassButtons[i];
            string computerButton = _computerClassButtons[i];
            if (gamePadButton == "Start" || gamePadButton == "Select")
                gamePadButton = gamePadButton.ToLower();

            curGamePadButton = (Controller.ButtonsStates)GamePads[controllerId].GetType().GetField(gamePadButton).GetValue(GamePadController.GamePads[controllerId]);
            curcomputerButton = (ControllerComputerInputs.ButtonInput)computerInputs[controllerId].GetType().GetField(computerButton).GetValue(GamePadController.computerInputs[controllerId]);

            if (curcomputerButton.inputSetup)
            {
                curGamePadButton.Held = SetAndCheckIfUnityButtonIsSetup(controllerId, curcomputerButton, curGamePadButton.Held, curcomputerButton.Held);
                curGamePadButton.Pressed = SetAndCheckIfUnityButtonIsSetup(controllerId, curcomputerButton, curGamePadButton.Pressed, curcomputerButton.Pressed);
                curGamePadButton.Released = SetAndCheckIfUnityButtonIsSetup(controllerId, curcomputerButton, curGamePadButton.Released, curcomputerButton.Released);
                curGamePadButton.Zero = SetAndCheckIfUnityButtonIsSetup(controllerId, curcomputerButton, curGamePadButton.Zero, curcomputerButton.Zero);

                GamePads[controllerId].GetType().GetField(gamePadButton).SetValue(GamePads[controllerId], curGamePadButton);
            }
        }
    }

    private bool SetAndCheckIfUnityButtonIsSetup(int controllerId, ControllerComputerInputs.ButtonInput computerInput, bool gamePadVal, bool computerVal)
    {
        if (ControllerConnected[controllerId])
        {
            if (!gamePadVal && computerVal && computerInput.inputSetup)
                return gamePadVal;
            else if (computerInput.inputSetup && !gamePadVal)
                return computerVal;
            else
                return gamePadVal;
        }
        else
        {
            if (computerInput.inputSetup)
                return computerVal;
            else
                return false;
        }
    }

    public void StopControllerVibration(int controllerId)
    {
        GamePads[controllerId].StopVibration();
    }

    public class Controller
    {
        public ButtonsStates
            A, B, X, Y, LB, RB, start, select, L3, R3, UP, DOWN, LEFT, RIGHT = new ButtonsStates();
        //A, B, X, Y, LB, RB, start, select, L3, R3, UP, DOWN, LEFT, RIGHT = new ButtonsStates();

        public float
            LeftTrigger, RightTrigger;

        public StickStates
            LeftStick, RightStick;


        private int _controllerId;

        public Controller(int controllerId)
        {
            _controllerId = controllerId;

            LeftStick = new StickStates();
            RightStick = new StickStates();

            #region old code, non need to instantiate every class now
            //A = new ButtonsStates();
            //B = new ButtonsStates();
            //X = new ButtonsStates();
            //Y = new ButtonsStates();
            //LB = new ButtonsStates();
            //RB = new ButtonsStates();
            //start = new ButtonsStates();
            //select = new ButtonsStates();
            //L3 = new ButtonsStates();
            //R3 = new ButtonsStates();
            //UP = new ButtonsStates();
            //DOWN = new ButtonsStates();
            //LEFT = new ButtonsStates();
            //RIGHT = new ButtonsStates();
            #endregion

        }

        public void SetVibration(float leftMotor, float rightMotor)
        {
            if (GamePadController.GamePadStateArray[_controllerId].IsConnected)
            {
                PlayerIndex curPlayerIndex = GamePadController.PlayerIndexArray[_controllerId];

                leftMotor /= 100f;
                rightMotor /= 100f;

                GamePad.SetVibration(curPlayerIndex, leftMotor, rightMotor);
            }
        }

        public void SetVibration(float intensity)
        {
            if (GamePadController.GamePadStateArray[_controllerId].IsConnected)
            {
                PlayerIndex curPlayerIndex = GamePadController.PlayerIndexArray[_controllerId];

                intensity /= 100f;

                GamePad.SetVibration(curPlayerIndex, intensity, intensity);
            }
        }

        public void SetVibration(float leftMotor, float rightMotor, float timer)
        {
            if (GamePadController.GamePadStateArray[_controllerId].IsConnected)
            {
                PlayerIndex curPlayerIndex = GamePadController.PlayerIndexArray[_controllerId];

                leftMotor /= 100f;
                rightMotor /= 100f;

                GamePad.SetVibration(curPlayerIndex, leftMotor, rightMotor);

                GamePadController.VibrationTimers[_controllerId] = timer;
            }
        }

        //public void SetVibration(float intensity, float timer)
        //{
        //    if (GamePadController.GamePadStateArray[_controllerId].IsConnected)
        //    {
        //        PlayerIndex curPlayerIndex = GamePadController.PlayerIndexArray[_controllerId];

        //        intensity /= 100f;

        //        GamePad.SetVibration(curPlayerIndex, intensity, intensity);

        //        GamePadController.VibrationTimers[_controllerId] = timer;
        //    }
        //}

        public void StopVibration()
        {
            PlayerIndex curPlayerIndex = GamePadController.PlayerIndexArray[_controllerId];
            GamePad.SetVibration(curPlayerIndex, 0, 0);
        }

        public struct ButtonsStates
        {
            public bool
                Pressed, Released, Held, Zero;
        }

        public struct StickStates
        {
            public float
                X, Y;
        }
    }

    [System.Serializable]
    public class ControllerComputerInputs
    {
        public int controllerId;
        public bool inputsEnabled = false;

        #region Controller Buttons
        public ButtonInput ButtonA = new ButtonInput("Button A");
        public ButtonInput ButtonB = new ButtonInput("Button B");
        public ButtonInput ButtonX = new ButtonInput("Button X");
        public ButtonInput ButtonY = new ButtonInput("Button Y");

        public ButtonInput ButtonUP = new ButtonInput("UP");
        public ButtonInput ButtonDOWN = new ButtonInput("Down");
        public ButtonInput ButtonLEFT = new ButtonInput("Left");
        public ButtonInput ButtonRIGHT = new ButtonInput("Right");

        public ButtonInput ButtonR3 = new ButtonInput("Button R3");
        public ButtonInput ButtonL3 = new ButtonInput("Button L3");

        public ButtonInput RightButton = new ButtonInput("Button RB");
        public ButtonInput LeftButton = new ButtonInput("Button LB");

        public ButtonInput start = new ButtonInput("Start");
        public ButtonInput select = new ButtonInput("Select");

        public ButtonInput RightTrigger = new ButtonInput("RightTrigger");
        public ButtonInput LeftTrigger = new ButtonInput("LeftTrigger");

        public ButtonInput RightStickX = new ButtonInput("RightStickX");
        public ButtonInput RightStickY = new ButtonInput("RightStickY");
        public ButtonInput LeftStickX = new ButtonInput("LeftStickX");
        public ButtonInput LeftStickY = new ButtonInput("LeftStickY");
        #endregion

        public bool debugString = false;

#if UNITY_EDITOR
        public LaunchGamePadControllerInspector targetInspector;
#endif

        private string[] _computerClassButtons = new string[] { "ButtonA", "ButtonB", "ButtonX", "ButtonY", "ButtonUP", "ButtonDOWN", "ButtonLEFT", "ButtonRIGHT", "ButtonR3", "ButtonL3", "LeftButton", "RightButton", "start", "select" };
        private string[] _computerClassAxis = new string[] { "RightTrigger", "LeftTrigger", "RightStickX", "RightStickY", "LeftStickX", "LeftStickY" };

        public ControllerComputerInputs(int _controllerId)
        {
            controllerId = _controllerId;
        }

        public void CreateControllerInputs()
        {
            GUILayout.BeginVertical();

            //GUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("Debug Input Setup (Alpha) : ");
            //debugString = EditorGUILayout.Toggle(debugString);
            //GUILayout.EndHorizontal();

            UpdateControllerInputs();

            for (int i = 0; i < (_computerClassButtons.Length + _computerClassAxis.Length); i++)
            {
                ControllerComputerInputs.ButtonInput curcomputerButton = new ControllerComputerInputs.ButtonInput();
                string computerButton = "";
                if (i < _computerClassButtons.Length)
                {
                    computerButton = _computerClassButtons[i];
                }
                else
                {
                    int ival = i - _computerClassButtons.Length;
                    computerButton = _computerClassAxis[ival];
                }

                curcomputerButton = (ControllerComputerInputs.ButtonInput)this.GetType().GetField(computerButton).GetValue(this);

                CreateInput(curcomputerButton);
                this.GetType().GetField(computerButton).SetValue(this, curcomputerButton);
            }

            #region old code
            //Buttons
            //ABXY
            //CreateInput(ButtonA);
            //CreateInput(ButtonB);
            //CreateInput(ButtonX);
            //CreateInput(ButtonY);

            ////UP-DOWN-LEFT-RIGHT
            //CreateInput(ButtonUP);
            //CreateInput(ButtonDOWN);
            //CreateInput(ButtonLEFT);
            //CreateInput(ButtonRIGHT);

            ////R3-L3
            //CreateInput(ButtonR3);
            //CreateInput(ButtonL3);

            ////LB-RB
            //CreateInput(RightButton);
            //CreateInput(LeftButton);

            ////STart-Select
            //CreateInput(start);
            //CreateInput(select);

            ////Axis
            ////LT-RT
            //CreateInput(RightTrigger);
            //CreateInput(LeftTrigger);

            ////LS-RS
            //CreateInput(RightStickX);
            //CreateInput(RightStickY);
            //CreateInput(LeftStickX);
            //CreateInput(LeftStickY);
            #endregion
            //EditorGUILayout.Space();
            GUILayout.EndVertical();
        }

        public void UpdateControllerInputs()
        {
            UpdateAxisInputs();
            UpdateButtonInputs();
        }

        public void UpdateAxisInputs()
        {
            //LT-RT
            UpdateInput(RightTrigger);
            UpdateInput(LeftTrigger);

            //LS-RS
            UpdateInput(RightStickX);
            UpdateInput(RightStickY);
            UpdateInput(LeftStickX);
            UpdateInput(LeftStickY);
        }

        public void UpdateButtonInputs()
        {
            for (int i = 0; i < _computerClassButtons.Length; i++)
            {
                ControllerComputerInputs.ButtonInput curcomputerButton = new ControllerComputerInputs.ButtonInput();
                string computerButton = _computerClassButtons[i];

                curcomputerButton = (ControllerComputerInputs.ButtonInput)this.GetType().GetField(computerButton).GetValue(this);

                UpdateInput(curcomputerButton);
                this.GetType().GetField(computerButton).SetValue(this, curcomputerButton);
            }
        }

        public void CreateInput(ButtonInput newButton)
        {
#if UNITY_EDITOR
            GUILayout.BeginHorizontal();

            //GUILayout.Label(newButton.buttonName, GUILayout.MaxWidth(80));
            EditorGUILayout.LabelField(newButton.buttonName, GUILayout.MaxWidth(80));
            newButton.inputType = (typeInput)EditorGUILayout.EnumPopup(newButton.inputType, GUILayout.MaxWidth(60));
            GUIStyle textFieldSetup = new GUIStyle(GUI.skin.textField);
            GUIStyle buttonSetup = new GUIStyle(GUI.skin.button);
            if (newButton.inputSetup && newButton.inputName != "")
            {
                textFieldSetup.normal.textColor = Color.green;
                textFieldSetup.focused.textColor = Color.green;
            }
            else if (!newButton.inputSetup && newButton.inputName != "")
            {
                textFieldSetup.normal.textColor = Color.red;
                textFieldSetup.focused.textColor = Color.red;
            }
            newButton.inputName = EditorGUILayout.TextField(newButton.inputName, textFieldSetup);
            if (newButton.inputType == typeInput.Axis)
            {
                newButton.inputDir = (axisDir)EditorGUILayout.EnumPopup(newButton.inputDir, GUILayout.MaxWidth(50));
                if (newButton.inputDir == axisDir.minus || newButton.inputDir == axisDir.plus)
                {
                    if (newButton.inputDir == axisDir.minus)
                    {
                        if (newButton.absoluteVal)
                        {
                            buttonSetup.normal.textColor = Color.green;
                            buttonSetup.focused.textColor = Color.green;
                            if (GUILayout.Button("A", buttonSetup, GUILayout.MinWidth(20), GUILayout.MaxWidth(30)))
                            {
                                newButton.absoluteVal = false;
                            }
                        }
                        else
                        {
                            buttonSetup.normal.textColor = Color.red;
                            buttonSetup.focused.textColor = Color.red;
                            if (GUILayout.Button("A", buttonSetup, GUILayout.MinWidth(20), GUILayout.MaxWidth(30)))
                            {
                                newButton.absoluteVal = true;
                            }
                        }
                    }

                    if (!newButton.showSlider)
                    {
                        if (GUILayout.Button("+", GUILayout.MinWidth(20), GUILayout.MaxWidth(30)))
                        {
                            newButton.showSlider = true;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("-", GUILayout.MinWidth(20), GUILayout.MaxWidth(30)))
                        {
                            newButton.showSlider = false;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginVertical();
                        newButton.axisValue = EditorGUILayout.Slider(newButton.axisValue, -1, 1);
                        ProgressBar(Mathf.Abs(newButton.axisValue), "Axis");
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                    }
                }
                EditorGUILayout.FloatField(newButton.axisValue);
            }
            else
            {
                newButton.buttonState = (ButtonStates)EditorGUILayout.EnumPopup(newButton.buttonState, GUILayout.MaxWidth(90));
            }
            GUILayout.EndHorizontal();
#endif
        }

        public void UpdateInput(ButtonInput newButton)
        {
            if (newButton.inputType == typeInput.Axis)
            {
                float axisVal = UpdateUnityAxis(newButton.inputName, newButton);
                if (newButton.inputDir == axisDir.plus)
                {
                    if (axisVal >= 0)
                        newButton.axisValue = axisVal;
                    else
                        newButton.axisValue = 0;
                }
                else if (newButton.inputDir == axisDir.minus)
                {
                    if (axisVal <= 0)
                    {
                        if (newButton.absoluteVal)
                            newButton.axisValue = Mathf.Abs(axisVal);
                        else
                            newButton.axisValue = axisVal;
                    }
                    else
                        newButton.axisValue = 0;
                }
                else
                    newButton.axisValue = axisVal;
            }
            else
            {
                int inputResult = 0;
                if (newButton.inputType == typeInput.Button)
                {
                    inputResult = UpdateUnityButtons(newButton.inputName, newButton);
                }
                if (newButton.inputType == typeInput.Key)
                {
                    inputResult = UpdateUnityKeys(newButton.inputName, newButton);
                }
                if (inputResult == 1)
                {
                    newButton.Pressed = true;
                    newButton.Held = false;
                    newButton.Released = false;
                    newButton.Zero = false;
                    //Debug.Log("Pressed B");
                    newButton.buttonState = ButtonStates.Pressed;
                }
                else if (inputResult == 2)
                {
                    newButton.Pressed = false;
                    newButton.Held = true;
                    newButton.Released = false;
                    newButton.Zero = false;
                    //Debug.Log("Held B");
                    newButton.buttonState = ButtonStates.Held;
                }
                else if (inputResult == 3)
                {
                    newButton.Pressed = false;
                    newButton.Held = false;
                    newButton.Released = true;
                    newButton.Zero = false;
                    //Debug.Log("Released B");
                    newButton.buttonState = ButtonStates.Released;
                }
                else
                {
                    newButton.Pressed = false;
                    newButton.Held = false;
                    newButton.Released = false;
                    newButton.Zero = true;
                    newButton.buttonState = ButtonStates.Zero;
                }
            }

#if UNITY_EDITOR
            if (targetInspector)
                targetInspector.Repaint();
#endif
        }

        #region Update Unity Inputs
        float UpdateUnityAxis(string axis, ButtonInput button, bool debug = false)
        {
            if (axis != "" && IsAxisAvailable(axis))
            {
                button.inputSetup = true;
                if (debug && debugString)
                    Debug.Log("Axis " + axis + " setup.");
                return Input.GetAxis(axis);
            }
            else
            {
                button.inputSetup = false;
                return 0;
            }
        }

        int UpdateUnityButtons(string button, ButtonInput buttonInstance, bool debug = false)
        {
            if (button != "" && IsButtonAvailable(button))
            {
                buttonInstance.inputSetup = true;
                if (debug && debugString)
                    Debug.Log("button " + button + " setup.");
                if (Input.GetButtonDown(button))
                    return 1;
                if (Input.GetButton(button))
                    return 2;
                if (Input.GetButtonUp(button))
                    return 3;
                if (!(Input.GetButtonUp(button) && Input.GetButton(button) && Input.GetButtonDown(button)))
                    return 0;
                else
                    return 0;
            }
            else
            {
                buttonInstance.inputSetup = false;
                return 0;
            }
        }

        int UpdateUnityKeys(string key, ButtonInput button, bool debug = false)
        {
            if (key != "")
            {
                if (key.Length == 1)
                    key = key.ToUpper();
                if (IsKeycodeAvailable(key))
                {
                    button.inputSetup = true;
                    if (debug && debugString)
                        Debug.Log("key " + key + " setup.");
                    KeyCode buttonKeyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), key);

                    if (Input.GetKeyDown(buttonKeyCode))
                        return 1;
                    if (Input.GetKey(buttonKeyCode))
                        return 2;
                    if (Input.GetKeyUp(buttonKeyCode))
                        return 3;
                    if (!(Input.GetKeyUp(buttonKeyCode) && Input.GetKey(buttonKeyCode) && Input.GetKeyDown(buttonKeyCode)))
                        return 0;
                    else
                        return 0;
                }
                else
                {
                    button.inputSetup = false;
                    return 0;
                }
            }
            else
            {
                button.inputSetup = false;
                return 0;
            }
        }
        #endregion

        #region Inputs Availability
        bool IsAxisAvailable(string axisName)
        {
            try
            {
                Input.GetAxis(axisName);
                return true;
            }
            catch (UnityException exc)
            {
                return false;
            }
        }

        bool IsButtonAvailable(string btnName)
        {
            try
            {
                Input.GetButton(btnName);
                return true;
            }
            catch (UnityException exc)
            {
                return false;
            }
        }

        bool IsKeycodeAvailable(string key)
        {
            try
            {
                KeyCode buttonKeyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), key);
                return true;
            }
            catch (UnityException exc)
            {
                return false;
            }
        }
        #endregion

        public void ProgressBar(float value, string label)
        {
            // Get a rect for the progress bar using the same margins as a textfield:
            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");

#if UNITY_EDITOR
            EditorGUI.ProgressBar(rect, value, label);
            EditorGUILayout.Space();
#endif
        }

        [System.Serializable]
        public class ButtonInput
        {

            public ButtonInput(string instanceName = "Button", typeInput buttonType = typeInput.Button, string nameInput = "")
            {
                buttonName = instanceName;
                inputType = buttonType;
                inputName = nameInput;
            }

            public string buttonName = "test";
            public typeInput inputType;
            public string inputName = "";
            public axisDir inputDir;
            public float axisValue = 0;
            public bool showSlider = false;

            public bool inputSetup = false;
            public bool absoluteVal = true;

            public ButtonStates buttonState;

            public bool
                Pressed, Released, Held, Zero;

        }
    }


#if UNITY_EDITOR
    [System.Serializable]
    public class DebugGamePad
    {
        [ReadOnly]
        public bool[] ControllersConnected = new bool[] { false, false, false, false };
        public enum DebugForGamePad
        {
            One,
            Two,
            Three,
            Four
        }
        public DebugForGamePad DebugGamePad_N;

        [SerializeField]
        [ReadOnly]
        private string Messages = "Debug not enabled.";
        [SerializeField]
        [ReadOnly]
        private string CurrentButton = "No input detected.";

        [ReadOnly]
        public Vector2 LeftStick, RightStick;
        [ReadOnly]
        public float LeftTrigger, RightTrigger;

        public enum ButtonStates
        {
            Released,
            Pressed,
            Held,
            Zero
        }
        [ReadOnly]
        public ButtonStates A, B, X, Y, LB, RB, Start, Select, R3, L3, UP, DOWN, LEFT, RIGHT = ButtonStates.Zero;
        private string[] Buttons = new string[] { "A", "B", "X", "Y", "LB", "RB", "Start", "Select", "R3", "L3", "UP", "DOWN", "LEFT", "RIGHT" };

        public void Update()
        {
            ControllersConnected = new bool[] { false, false, false, false };
            ControllersConnected = GamePadController.ControllerConnected;
            if (GamePadController.DebugMode)
            {
                for (int i = 0; i < (int)DebugForGamePad.Four; i++)
                {
                    if (DebugGamePad_N == (DebugForGamePad)i)
                    {
                        if (ControllersConnected[i])
                        {
                            Messages = "GamePad \"" + (i + 1) + "\" detected.";
                            ShowDebug(i);
                        }
                        else if (!ControllersConnected[i] && GamePadController.computerInputs[i].inputsEnabled)
                        {
                            Messages = "GamePad \"" + (i + 1) + "\" not detected, computer inputs \"" + (i + 1) + "\" enabled.";
                            ShowDebug(i);
                        }
                        else
                            Messages = "GamePad \"" + (i + 1) + "\" not connected.";
                    }
                }
            }
            else
                Messages = "Debug not enabled.";
        }

        private void ShowDebug(int controllerId)
        {

            LeftTrigger = GamePadController.GamePads[controllerId].LeftTrigger;
            RightTrigger = GamePadController.GamePads[controllerId].RightTrigger;

            LeftStick = new Vector2(GamePadController.GamePads[controllerId].LeftStick.X, GamePadController.GamePads[controllerId].LeftStick.Y);
            RightStick = new Vector2(GamePadController.GamePads[controllerId].RightStick.X, GamePadController.GamePads[controllerId].RightStick.Y);
            
            for (int i = 0; i < Buttons.Length; i++)
            {
                ButtonStates curButton = (ButtonStates)this.GetType().GetField(Buttons[i]).GetValue(this);
                string gamePadButton = Buttons[i];
                if (gamePadButton == "Start" || gamePadButton == "Select")
                    gamePadButton = gamePadButton.ToLower();
                Controller.ButtonsStates curGamePadButton = (Controller.ButtonsStates)GamePadController.GamePads[controllerId].GetType().GetField(gamePadButton).GetValue(GamePadController.GamePads[controllerId]);
                
                curButton = ButtonStates.Zero;
                this.GetType().GetField(Buttons[i]).SetValue(this, curButton);
                if (curGamePadButton.Pressed)
                {
                    curButton = ButtonStates.Pressed;
                    this.GetType().GetField(Buttons[i]).SetValue(this, curButton);
                    CurrentButton = Buttons[i] + " Pressed.";
                }
                if (curGamePadButton.Held)
                {
                    curButton = ButtonStates.Held;
                    this.GetType().GetField(Buttons[i]).SetValue(this, curButton);
                    CurrentButton = Buttons[i] + " Held.";
                }
                if (curGamePadButton.Released)
                {
                    curButton = ButtonStates.Released;
                    this.GetType().GetField(Buttons[i]).SetValue(this, curButton);
                    CurrentButton = Buttons[i] + " Released.";
                }
            }
        }
    }
#endif
}


#if UNITY_EDITOR
public class ReadOnlyAttribute : PropertyAttribute
{
}

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
    GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
    public override void OnGUI(Rect position,
    SerializedProperty property,
    GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif

/* Originale options :
Triggers = GamePadStateArray.Triggers.Left, GamePadStateArray.Triggers.Right
DPad = GamePadStateArray.DPad.Up, GamePadStateArray.DPad.Right, GamePadStateArray.DPad.Down, GamePadStateArray.DPad.Left
Start/Select = GamePadStateArray.Buttons.Start, GamePadStateArray.Buttons.Back
L3/R3 = GamePadStateArray.Buttons.LeftStick, GamePadStateArray.Buttons.RightStick
Shoulder Buttons = GamePadStateArray.Buttons.LeftShoulder, GamePadStateArray.Buttons.RightShoulder
ABXY = GamePadStateArray.Buttons.A, GamePadStateArray.Buttons.B, GamePadStateArray.Buttons.X, GamePadStateArray.Buttons.Y);
LeftStick Axis = GamePadStateArray.ThumbSticks.Left.X, GamePadStateArray.ThumbSticks.Left.Y
RightStick Axis = GamePadStateArray.ThumbSticks.Right.X, GamePadStateArray.ThumbSticks.Right.Y
Vibrations (function) = GamePad.SetVibration(playerIndex, LeftMotor, RightMotor) -> any value between 0-65535 
*/