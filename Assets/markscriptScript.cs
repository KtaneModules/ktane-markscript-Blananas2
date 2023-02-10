using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class markscriptScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable ModuleSelectable;

    public KMSelectable Power;
    public KMSelectable[] Keyboard;
    public TextMesh Screen;
    public TextMesh OtherScreen;
    public GameObject Line;
    public GameObject StatusLight;

    private KeyCode[] TheKeys =
	{
        KeyCode.Space, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.Backspace, KeyCode.Return,
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M,
		KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
		KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9, KeyCode.Keypad0,		
	};

    private bool Focused = false;
    private string Task = "";
    List<string> Program = new List<string> {""};
    int CursorIndex = 0;
    string Keys = "QWERTYUIOP789ASDFGHJKL▲456ZXCVBNM  ▼123«√◊∩₪☼♣♫  ←0☺ ";
    string NShep = " ▲▼←▼QWERTYUIOPASDFGHJKLZXCVBNM12345678901234567890";
    string NShift = " ▲▼←☺QWERTYUIOPASDFGHJKLZXCVBNM«√◊∩₪☼♣♫~~«√◊∩₪☼♣♫~~";
    bool TaskShown = false;
    bool ProgramRunning = false;
    bool CursorFlicker = false;
    int ScreenScroll = 0;
    int? CorrectAnswer = null;
    int NumberOfRuns = 0;
    int PuzzleIndex = 0;
    int NumberOfStartLines = 0;
    string DebugLog = "";
    int[] RetroNumbers = { 2, 2, 2, 2, 2, 2, 2 };
    string[] RetroLines = { "", "", "", "", "", "", "" };

    int CurrentLine = 0;
    List<string> ProgramComments = new List<string> {};
    List<string> VarNames = new List<string> {};
    List<int> VarValues = new List<int> {};

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;

        if (Application.isEditor) { Focused = true; }
        ModuleSelectable.OnFocus += delegate () { Focused = true; };
        ModuleSelectable.OnDefocus += delegate () { Focused = false; };

        Power.OnInteract += delegate () { PowerPress(); return false; };
        foreach (KMSelectable Key in Keyboard) {
            Key.OnInteract += delegate () { KeyPress(Key); return false; };
        }
    }

    // Use this for initialization
    void Start () {
        StatusLight.SetActive(false);
        Line.SetActive(false);
        GeneratePuzzle();
        DrawScreen(CursorIndex, ScreenScroll);
        StartCoroutine(CursorFlip());
        OtherScreen.color = new Color(1f, 1f, 1f, 0.2f);
    }

    void Update () {
        if (!Focused) { return; }
        for (int j = 0; j < TheKeys.Count(); j++) {
            if (Input.GetKeyDown(TheKeys[j])) {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                    if (NShift[j] == '~') { return; }
                    KeyPress(Keyboard[Keys.IndexOf(NShift[j])]);
                } else {
                    KeyPress(Keyboard[Keys.IndexOf(NShep[j])]);
                }
            }
        }
	}

    void GeneratePuzzle () {
        string[] Puzzles = {
            "Create a program which returns √P times √Q.                                                                        | √P ?;√Q ?      ",
            "Create a program which returns 1 if √X is divisible by 5, and 0 otherwise.                                         | √X ?           ",
            "Create a program which returns the larger value minus the smaller value when given numbers √D and √F.              | √D ?;√F ?      ",
            "Create a program which returns what you would need to add to √N to get 0.                                          | √N ?           ",
            "Create a program which returns the √Tth triangular number (sequence is 1, 3, 6, 10, 15, 21 etc.).                  | √T ?           ",
            "Create a program which returns the √Fth Fibonacci number (sequence is 1, 1, 2, 3, 5, 8, 13 etc.).                  | √F ?           ",
            "Create a program which returns the √M modulo √D (the remainder after a division).                                  | √M ?;√D ?      ",
            "Create a program which returns the average between √A and √V (add them then divide by 2).                          | √A ?;√V ?      ",
            "Create a program which returns (2 times √A) minus √C.                                                              | √A ?;√C ?      ",
            "Create a program which returns √Z divided by 2 if √Z is even, and √Z times 3 plus 1 otherwise.                     | √Z ?           ",
            "Create a program which returns the digital root of √R (adding up all digits until you end up with just one digit). | √R ?           ",
            "Create a program which returns √S, times itself.                                                                   | √S ?           ",
            "Create a program which returns the value 1-4 missing when given numbers √X, √Y, and √Z.                            | √X ?;√Y ?;√Z ? ",
            "Create a program which returns the closest multiple of 9 to given number √I.                                       | √I ?           ",
            "Create a program which returns the middle value when given numbers √M, √I, and √D.                                 | √M ?;√I ?;√D ? ",
        };
        PuzzleIndex = UnityEngine.Random.Range(0, Puzzles.Count());
        Task = Puzzles[PuzzleIndex].Split('|')[0].Trim(); Program = Puzzles[PuzzleIndex].Split('|')[1].Trim().Split(';').ToList<string>();
        Debug.LogFormat("[Markscript #{0}] Task: {1}", moduleId, Task);
    }
    
    void RandomizeStart() {
        if (moduleSolved) { return; }
        List<int> unk = new List<int> {};
        switch (PuzzleIndex) {
             case 0: NumberOfStartLines = 2; unk.AddRange(new List<int> {UnityEngine.Random.Range(3,8), UnityEngine.Random.Range(3,8)}); CorrectAnswer = unk[0] * unk[1]; break;
             case 1: NumberOfStartLines = 1; unk.Add(UnityEngine.Random.Range(2,5)*5 + (UnityEngine.Random.Range(0,2) == 0 ? 0 : UnityEngine.Random.Range(1,5))); CorrectAnswer = (unk[0] % 5 == 0 ? 1 : 0); break;
             case 2: NumberOfStartLines = 2; unk.AddRange(new List<int> {UnityEngine.Random.Range(6,13), UnityEngine.Random.Range(6,13)}); CorrectAnswer = Math.Abs(unk[0] - unk[1]); break;
             case 3: NumberOfStartLines = 1; unk.Add(UnityEngine.Random.Range(-11,11)); CorrectAnswer = -unk[0]; break;
             case 4: NumberOfStartLines = 1; unk.Add(UnityEngine.Random.Range(4,14)); CorrectAnswer = ComplicatedScenario(PuzzleIndex, unk); break;
             case 5: NumberOfStartLines = 1; unk.Add(UnityEngine.Random.Range(5,13)); CorrectAnswer = ComplicatedScenario(PuzzleIndex, unk); break;
             case 6: NumberOfStartLines = 2; unk.AddRange(new List<int> {UnityEngine.Random.Range(10,30), UnityEngine.Random.Range(3,9)}); CorrectAnswer = (unk[0] % unk[1]); break;
             case 7: NumberOfStartLines = 2; unk.Add(UnityEngine.Random.Range(5,13)); unk.Add(unk[0]+2*UnityEngine.Random.Range(2,7)); CorrectAnswer = (unk[0] + unk[1])/2; break;
             case 8: NumberOfStartLines = 2; unk.Add(UnityEngine.Random.Range(5,13)); unk.Add(unk[0]+UnityEngine.Random.Range(2,5)); CorrectAnswer = 2*unk[0] - unk[1]; break;
             case 9: NumberOfStartLines = 1; unk.Add(UnityEngine.Random.Range(5,27)); CorrectAnswer = (unk[0]%2 == 0 ? unk[0]/2 : unk[0]*3+1); break;
            case 10: NumberOfStartLines = 1; unk.Add(UnityEngine.Random.Range(11,50)); CorrectAnswer = ((unk[0]-1)%9)+1; break;
            case 11: NumberOfStartLines = 1; unk.Add(UnityEngine.Random.Range(2,14)); CorrectAnswer = unk[0]*unk[0]; break;
            case 12: NumberOfStartLines = 3; unk.AddRange(Enumerable.Range(1, 4)); unk.Shuffle(); CorrectAnswer = unk[3]; unk.Remove(unk[3]); break;
            case 13: NumberOfStartLines = 1; unk.Add(UnityEngine.Random.Range(10,42)); CorrectAnswer = ComplicatedScenario(PuzzleIndex, unk); break;
            case 14: NumberOfStartLines = 3; unk.Add(UnityEngine.Random.Range(10,20)); unk.Add(unk[0] - UnityEngine.Random.Range(2,9)); unk.Add(unk[0] + UnityEngine.Random.Range(2,9)); unk.Shuffle(); CorrectAnswer = ComplicatedScenario(PuzzleIndex, unk); break;
        }
        Debug.LogFormat("[Markscript #{0}] Given starting value(s): {1}", moduleId, unk.Join(", "));
        for (int u = 0; u < NumberOfStartLines; u++) {
            Program[u] = Program[u].Replace("?", unk[u].ToString().Replace("-", "♣"));
        }
    }

    int ComplicatedScenario(int z, List<int> o) {
        switch (z) {
            case 4: int ta = o[0]; int tb = 0; for (int tc = ta; tc >= 0; tc--) { tb += tc; } return tb;
            case 5: int td = o[0]; int te = 1; int tf = 1; int tg = 0; int th = 2; while (th < td) { tg = te + tf; te = tf; tf = tg; th += 1; } return tf;
            case 13: int ti = o[0]; int tj = ti % 9; if (tj == 0) { return ti; } else { if (tj < 5) { while ((tj % 9) != 0) { tj -= 1; } } else { while ((tj % 9) != 0) { tj += 1; } } } return tj;
            case 14: List<int> tk = o; tk.Sort(); return tk[1];
        }
        return -1;
    }

    void UndoStartRandomization () {
        if (moduleSolved) { return; }
        for (int w = 0; w < NumberOfStartLines; w++) {
            string[] ww = Program[w].Split(' ');
            ww[1] = "?";
            Program[w] = ww.Join(" ");
        }
    }

    void PowerPress() {
        Power.AddInteractionPunch();
        if (TaskShown) { return; }
        ProgramRunning = !ProgramRunning;
        Line.SetActive(ProgramRunning);
        Line.transform.localPosition = new Vector3(0f, 0.0149f, 0.0343f);
        if (ProgramRunning) {
            NumberOfRuns = 0;
            OtherScreen.color = new Color(1f, 1f, 1f, 0.2f);
            OtherScreen.gameObject.SetActive(true);
            StartProgram(0.25f);
        } else {
            Debug.LogFormat("[Markscript #{0}] Events: {1}", moduleId, DebugLog);
            Debug.LogFormat("[Markscript #{0}] The program was halted by the user.", moduleId);
            OtherScreen.gameObject.SetActive(false);
            CursorIndex = ScreenScroll+1;
            UndoStartRandomization();
            DrawScreen(CursorIndex, ScreenScroll);
        }
    }

    void StartProgram(float f) {
        ScreenScroll = 0;
        CurrentLine = 0;
        DrawScreen(CursorIndex, ScreenScroll);
        VarNames.Clear();
        VarValues.Clear();
        ProgramComments.Clear();
        for (int rtro = 0; rtro < 7; rtro++) {
            RetroNumbers[rtro] = 2;
            RetroLines[rtro] = "";
        }
        if (NumberOfRuns == 0) {
            Debug.LogFormat("[Markscript #{0}] Program: {1}", moduleId, Program.Join(";"));
        }
        DebugLog = "";
        RandomizeStart();
        for (int l = 0; l < Program.Count; l++) {
            string[] spl = Program[l].Split('«');
            if (spl.Length == 2) { 
                ProgramComments.Add(spl[1].Trim());
            } else {
                ProgramComments.Add("!");
            }
        }
        StartCoroutine(RunProgram(f));
    }

    private IEnumerator RunProgram(float f) {
        while (ProgramRunning) {
            yield return new WaitForSeconds(f);
            if (CurrentLine < Program.Count) {
                Audio.PlaySoundAtTransform("run", transform);
                RunLine(CurrentLine);
                if (ProgramRunning) { RetroScroll(); }
                if (CurrentLine < 4) {
                    ScreenScroll = 0;
                    Line.transform.localPosition = new Vector3(0f, 0.0149f, 0.0343f + (CurrentLine*-0.0132f));
                } else if (Program.Count <= 7 || CurrentLine > Program.Count - 3) {
                    ScreenScroll = (Program.Count <= 7 ? 0 : Program.Count - 6);
                    Line.transform.localPosition = new Vector3(0f, 0.0149f, 0.0343f + ((Program.Count <= 7 ? CurrentLine : (6-(Program.Count-CurrentLine)))*-0.0132f));
                } else {
                    ScreenScroll = CurrentLine - 3;
                    Line.transform.localPosition = new Vector3(0f, 0.0149f, -0.0053f);
                }
                DrawScreen(CursorIndex, ScreenScroll);
            } else {
                //End of program
                ProgramRunning = false;
                Line.SetActive(false);
                CursorIndex = Program.Count - 1;
                OtherScreen.gameObject.SetActive(false);
                DrawScreen(CursorIndex, ScreenScroll);
                UndoStartRandomization();
            }
            yield return null;
        }
    }

    void RunLine (int cur) {
        if (!ProgramRunning) { return; }
        string line = Program[cur].Split('«')[0].Trim();
        while (line.Contains("  ")) {
            line = line.Replace("  ", " ");
        }
        string[] split = line.Split(' ');
        string[] reg = { @"^√[A-Z]+ (♣?[0-9]+|[A-Z]+)$", @"^[A-Z]+√ (♣?[0-9]+|[A-Z]+)$", @"^◊[A-Z]+ (♣|∩)?∩(♣?[0-9]+|[A-Z]+)$", @"^₪[A-Z]+$", @"^☼[A-Z]+ (♣?[0-9]+|[A-Z]+)$", @"^♫(♣?[0-9]+|[A-Z]+)$"};
        string name = ""; string value = ""; int numeric = 0;
        for (int x = 0; x < reg.Length; x++) {
            if (Regex.IsMatch(line, reg[x])) {
                switch (x) {
                    case 0: 
                        name = split[0].Substring(1, split[0].Length-1);
                        if (VarNames.Contains(name)) {
                            DrawError("KNOWN √ " + name);
                            return;
                        }
                        value = split[1];
                        if (Regex.IsMatch(value, @"^♣?[0-9]+$")) {
                            numeric = Int32.Parse(value.Replace('♣','-'));
                        } else {
                            if (!VarNames.Contains(value)) {
                                DrawError("UNKNOWN √ " + value);
                                return;
                            } else {
                                numeric = VarValues[VarNames.IndexOf(value)];
                            }
                        }
                        VarNames.Add(name);
                        VarValues.Add(numeric);
                        CurrentLine += 1;
                        DebugLog += ("√ Made " + name + " which equals " + numeric + ";");
                        return;
                    case 1: 
                        name = split[0].Substring(0, split[0].Length-1);
                        if (!VarNames.Contains(name)) {
                            DrawError("UNKNOWN √ " + name);
                            return;
                        }
                        value = split[1];
                        if (Regex.IsMatch(value, @"^♣?[0-9]+$")) {
                            numeric = Int32.Parse(value.Replace('♣','-'));
                        } else {
                            if (!VarNames.Contains(value)) {
                                DrawError("UNKNOWN √ " + value);
                                return;
                            } else {
                                numeric = VarValues[VarNames.IndexOf(value)];
                            }
                        }
                        VarValues[VarNames.IndexOf(name)] = numeric;
                        CurrentLine += 1;
                        DebugLog += ("√ " + name + " now equals " + numeric + ";");
                        return;
                    case 2: 
                        name = split[0].Substring(1, split[0].Length-1);
                        if (!VarNames.Contains(name)) {
                            DrawError("UNKNOWN √ " + name);
                            return;
                        }
                        if (split[1][0] == '♣') { //GREATER THAN
                            value = split[1].Substring(2, split[1].Length-2);
                            if (Regex.IsMatch(value, @"^♣?[0-9]+$")) {
                                numeric = Int32.Parse(value.Replace('♣','-'));
                            } else {
                                if (!VarNames.Contains(value)) {
                                    DrawError("UNKNOWN √ " + value);
                                    return;
                                } else {
                                    numeric = VarValues[VarNames.IndexOf(value)];
                                }
                            }
                            if (VarValues[VarNames.IndexOf(name)] > numeric) {
                                CurrentLine += 2;
                                DebugLog += ("◊♣∩ " + name + " is greater than " + numeric + ", skipping a line;");
                            } else {
                                CurrentLine += 1;
                                DebugLog += ("◊♣∩ " + name + " is not greater than " + numeric + ";");
                            }
                            return;
                        } else if (split[1][0] == '∩' && split[1][1] == '∩') { //EQUAL TO
                            value = split[1].Substring(2, split[1].Length-2);
                            if (Regex.IsMatch(value, @"^♣?[0-9]+$")) {
                                numeric = Int32.Parse(value.Replace('♣','-'));
                            } else {
                                if (!VarNames.Contains(value)) {
                                    DrawError("UNKNOWN √ " + value);
                                    return;
                                } else {
                                    numeric = VarValues[VarNames.IndexOf(value)];
                                }
                            }
                            if (VarValues[VarNames.IndexOf(name)] == numeric) {
                                CurrentLine += 2;
                                DebugLog += ("◊∩∩ " + name + " is equal to " + numeric + ", skipping a line;");
                            } else {
                                CurrentLine += 1;
                                DebugLog += ("◊∩∩ " + name + " is not equal to " + numeric + ";");
                            }
                            return;
                        } else { //LESS THAN
                            value = split[1].Substring(1, split[1].Length-1);
                            if (Regex.IsMatch(value, @"^♣?[0-9]+$")) {
                                numeric = Int32.Parse(value.Replace('♣','-'));
                            } else {
                                if (!VarNames.Contains(value)) {
                                    DrawError("UNKNOWN √ " + value);
                                    return;
                                } else {
                                    numeric = VarValues[VarNames.IndexOf(value)];
                                }
                            }
                            if (VarValues[VarNames.IndexOf(name)] < numeric) {
                                CurrentLine += 2;
                                DebugLog += ("◊∩ " + name + " is less than " + numeric + ", skipping a line;");
                            } else {
                                CurrentLine += 1;
                                DebugLog += ("◊∩ " + name + " is not less than " + numeric + ";");
                            }
                        }
                        return;
                    case 3: 
                        name = split[0].Substring(1, split[0].Length-1);
                        if (!ProgramComments.Contains(name)) {
                            DrawError("UNKNOWN « " + name);
                            return;
                        }
                        CurrentLine = ProgramComments.IndexOf(name);
                        DebugLog += ("₪ " + name + " jump;");
                        return;
                    case 4: 
                        name = split[0].Substring(1, split[0].Length-1);
                        if (!VarNames.Contains(name)) {
                            DrawError("UNKNOWN √ " + value);
                            return;
                        }
                        value = split[1];
                        if (Regex.IsMatch(value, @"^♣?[0-9]+$")) {
                            numeric = Int32.Parse(value.Replace('♣','-'));
                        } else {
                            if (!VarNames.Contains(value)) {
                                DrawError("UNKNOWN √ " + value);
                                return;
                            } else {
                                numeric = VarValues[VarNames.IndexOf(value)];
                            }
                        }
                        VarValues[VarNames.IndexOf(name)] += numeric;
                        CurrentLine += 1;
                        DebugLog += ("☼ " + name + " now equals " + VarValues[VarNames.IndexOf(name)] + ";");
                        return;
                    case 5:
                        value = split[0].Substring(1, split[0].Length-1);
                        if (Regex.IsMatch(value, @"^♣?[0-9]+$")) {
                            numeric = Int32.Parse(value.Replace('♣','-'));
                        } else {
                            if (!VarNames.Contains(value)) {
                                DrawError("UNKNOWN √ " + value);
                                return;
                            } else {
                                numeric = VarValues[VarNames.IndexOf(value)];
                            }
                        }
                        DebugLog += ("♫ " + numeric + " returned");
                        if (numeric == CorrectAnswer) {
                            ThatWasRight();
                        } else {
                            if (moduleSolved) {
                                DrawError("UNAUTHORIZED DATA DETECTED, ♫ " + numeric);
                                return;
                            } else {
                                DrawError("INCORRECT ♫ " + numeric);
                                return;
                            }
                        }
                        return; //just in case...
                }
            }
        }
        //down here is default don't question it
        if (line != "") {
            DrawError("WHAT SYNTAX");
            return;
        }
        CurrentLine += 1;
        return;
    }

    void ThatWasRight () {
        Debug.LogFormat("[Markscript #{0}] Events: {1}", moduleId, DebugLog);
        NumberOfRuns += 1;
        Debug.LogFormat("[Markscript #{0}] Correct value returned. Total so far: {1}", moduleId, NumberOfRuns);
        switch (NumberOfRuns) {
            case 1: UndoStartRandomization(); StartProgram(0.2f); return;
            case 2: case 3: case 4: //i'm debating on how many times are needed to consider the module solved, may change later
            UndoStartRandomization(); StartProgram(0.15f); return;
            default: 
                Audio.PlaySoundAtTransform("success", transform);
                ProgramRunning = false;
                Line.SetActive(false);
                CursorIndex = ScreenScroll+1;
                DrawScreen(CursorIndex, ScreenScroll);
                StatusLight.SetActive(true);
                GetComponent<KMBombModule>().HandlePass();
                moduleSolved = true;
                CorrectAnswer = null;
                OtherScreen.text = "";
                StopAllCoroutines();
                StartCoroutine(CursorFlip());
                Debug.LogFormat("[Markscript #{0}] That's 5 correct values in a row, module solved!", moduleId);
            return;
        }
    }

    void DrawError (string err) {
        Audio.PlaySoundAtTransform("denied", transform);
        Debug.LogFormat("[Markscript #{0}] Events: {1}", moduleId, DebugLog);
        Debug.LogFormat("[Markscript #{0}] An error occured. Error: {1}", moduleId, err);
        TaskShown = true;
        ProgramRunning = false;
        OtherScreen.color = Color.white;
        OtherScreen.gameObject.SetActive(true);
        Screen.gameObject.SetActive(false);
        Line.SetActive(false);
        OtherScreen.text = "<color=maroon>" + WordWrap("ERROR: " + err, 24) + "</color>";
        CursorIndex = ScreenScroll+1;
        UndoStartRandomization();
        DrawScreen(CursorIndex, ScreenScroll);
    }

    void KeyPress(KMSelectable Key) {
        Audio.PlaySoundAtTransform("key", transform);
        if (ProgramRunning) { return; }
        for (int k = 0; k < 53; k++) {
            if (Key == Keyboard[k]) {
                switch (Keys[k].ToString()) {
                    case "▲":
                        if (CursorIndex == 0 || TaskShown) { return; }
                        if (Program[CursorIndex] == "" && CursorIndex+1 == Program.Count) {
                            Program.RemoveAt(CursorIndex);
                            if (ScreenScroll != 0) { ScreenScroll -= 1; }
                        }
                        CursorIndex -= 1;
                        if (CursorIndex == ScreenScroll-1) {
                            ScreenScroll -= 1;
                        }
                    break;
                    case "▼": 
                        if (TaskShown) { return; }
                        if ("" == Program[CursorIndex] && CursorIndex+1 == Program.Count) { return; }
                        CursorIndex += 1;
                        if (CursorIndex == Program.Count) {
                            Program.Add("");
                        }
                        if (CursorIndex == ScreenScroll+7) {
                            ScreenScroll += 1;
                        }
                    break;
                    case "←":
                        if (Program[CursorIndex].Length == 0) { return; } //these have to be seperate to avoid an index out of range exception
                        if (TaskShown || Program[CursorIndex][Program[CursorIndex].Length - 1] == '?') { return; }
                        Program[CursorIndex] = Program[CursorIndex].Substring(0, Program[CursorIndex].Length - 1);
                    break;
                    case "☺":
                        Screen.gameObject.SetActive(true);
                        OtherScreen.text = "";
                        OtherScreen.gameObject.SetActive(false);
                        TaskShown = !TaskShown;
                    break;
                    default:
                        if (TaskShown) { return; }
                        if (Program[CursorIndex].Length != 24) {
                            Program[CursorIndex] += Keys[k];
                        }
                    break;
                }
                DrawScreen(CursorIndex, ScreenScroll);
            }
        }
    }

    void DrawScreen (int c, int s) {
        if (!TaskShown) {
            string progString = "";
            for (int d = 0; d < 7; d++) {
                if (Program.Count <= d+s) {
                    progString += ((d+s == c && Program[c].Length != 24 && !ProgramRunning) ? "§\n" : "\n");
                } else {
                    progString += (Program[d+s] + ((d+s == c && Program[c].Length != 24 && !ProgramRunning) ? "§\n" : "\n"));
                }
            }
            Screen.text = Style(progString.Replace("§", (CursorFlicker ? "■" : "□")));
        } else {
            Screen.text = Style(WordWrap(Task, 24));
        }
    }

    void RetroScroll () {
        for (int p = 0; p < Math.Min(VarNames.Count, 7); p++) {
            RetroNumbers[p] = (RetroNumbers[p] + 23)%24;
            RetroLines[p] = RetroStuff(p, RetroNumbers[p]);
        }
        OtherScreen.text = RetroLines.Join("\n").Replace("-", "♣");
    }

    string RetroStuff (int v, int o) {
        string va = VarNames[v] + "≈" + VarValues[v];
        string st = "".PadLeft(o);
        va = st + va;
        int ijic = 0;
        string sjic = "";
        if (va.Length < 24) {
            return va;
        } else {
            ijic = va.Length - 24;
            sjic = va.Substring(va.Length - ijic);
            va = sjic + va.Substring(ijic, 24 - sjic.Length);
            return va;
        }
    }

    string Style (string t) {
        string st = t; //might need to add a \n
        if (TaskShown) { 
            string[] sp = st.Split(' ');
            for (int s = 0; s < sp.Length; s++) {
                if (sp[s].Contains('√')) { 
                    sp[s] = "<color=magenta>" + sp[s] + "</color>";
                    sp[s] = sp[s].Replace(".</color>", "</color>.");
                    sp[s] = sp[s].Replace(",</color>", "</color>,"); 
                    sp[s] = sp[s].Replace(")</color>", "</color>)"); 
                    sp[s] = sp[s].Replace("th</color>", "</color>th"); 
                    sp[s] = sp[s].Replace("(the</color>", "</color>(the"); 
                }
            }
            st = "<i><color=teal>Mark</color><color=lightblue>script</color></i>\n\n<color=lime>" + sp.JoinString(" ") + "</color>";
        } else {
            st = st.Replace("■", " ■").Replace("□", " □");
            string[] sm = st.Split('\n');
            string[] reg = { @"^♫(♣?[0-9]+|[A-Z]+)$", @"^₪[A-Z]+$", @"^☼[A-Z]+$", @"^♣?[0-9]+$", @"^(√[A-Z]+|[A-Z]+√)$", @"^◊[A-Z]+$", @"^«[A-Z]+$", @"^(♣|∩)?∩(♣?[0-9]+|[A-Z]+)$", @"^[A-Z]+$"};
            string[] col = { "red", "lime", "blue", "cyan", "magenta", "yellow", "silver", "orange", "purple"};
            for (int l = 0; l < sm.Length; l++) {
                string[] sl = sm[l].Split(' ');
                for (int e = 0; e < sl.Length; e++) {
                    for (int r = 0; r < reg.Length; r++) {
                        if (Regex.IsMatch(sl[e], reg[r])) {
                            sl[e] = "<color=" + col[r] + ">" + sl[e] + "</color>";
                        }
                    }
                    if (!sl[e].Contains("/") && sl[e] != "■" && sl[e] != "□") {
                        sl[e] = "<color=maroon>" + sl[e] + "</color>";
                    }
                }
                sm[l] = sl.JoinString(" ");
            }
            st = sm.JoinString("\n");
            st = st.Replace(" ■", "<color=white>■</color>").Replace(" □", "<color=white>□</color>").Replace("?", "<color=darkblue>?</color>");
        }
        return st;
    }

    private IEnumerator CursorFlip () {
        while (true) {
            yield return new WaitForSeconds(0.75f);
            CursorFlicker = !CursorFlicker;
            DrawScreen(CursorIndex, ScreenScroll);
            yield return null;
        }
    }

    /// WORDWRAP CODE, Credit goes to ICR and Rapptz on StackExchange
    static char[] splitChars = new char[] { ' ', '-', '\t' };
    private static string WordWrap(string str, int width)
    {
        string[] words = Exp(str, splitChars);
        int curLineLength = 0;
        StringBuilder strBuilder = new StringBuilder();
        for(int i = 0; i < words.Length; i += 1) {
            string word = words[i];
            if (curLineLength + word.Length > width) {
                if (curLineLength > 0) {
                    strBuilder.Append(Environment.NewLine);
                    curLineLength = 0;
                }
                while (word.Length > width) {
                    strBuilder.Append(word.Substring(0, width - 1) + "-");
                    word = word.Substring(width - 1);
                    strBuilder.Append(Environment.NewLine);
                }
                word = word.TrimStart();
            }
            strBuilder.Append(word);
            curLineLength += word.Length;
        }
        return strBuilder.ToString();
    }
    private static string[] Exp(string str, char[] splitChars){
        List<string> parts = new List<string>();
        int startIndex = 0;
        while (true){
            int index = str.IndexOfAny(splitChars, startIndex);
            if (index == -1) { parts.Add(str.Substring(startIndex)); return parts.ToArray(); }
            string word = str.Substring(startIndex, index - startIndex);
            char nextChar = str.Substring(index, 1)[0];
            if (char.IsWhiteSpace(nextChar))
            { parts.Add(word); parts.Add(nextChar.ToString());
            } else { parts.Add(word + nextChar); }
            startIndex = index + 1;
        }
    }

}
