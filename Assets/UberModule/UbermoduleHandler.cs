using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using KModkit;
using System;
//using System.Text;
using System.Text.RegularExpressions;

public class UbermoduleHandler : MonoBehaviour {
	public static string[] ignores = null;
	public GameObject button;
	public Renderer screen;
	public TextMesh text;
	private List<string> solvedModules = new List<string>();
	public KMBombModule ModSelf;
	public KMBombInfo Info;
	public KMAudio sound;
	public KMSelectable selectable;
	private static int _moduleIdCounter = 1;
	private int _moduleId = 0;
	private bool isFinal = false;
	private int stagesToGenerate = 0;
	private int[] stagesNum;        // A set of stages to get the xth solved module.
	private string[] InputMethod;	// String determining the input method necessary. "" will be used if neither matches.
	private int currentStage = -1;
	private bool started = false;

	public Material[] materials = new Material[2];

	private int animationLength = 30;
	private List<string> solvables = new List<string>();

	private bool isHolding = false;
	private double timeHeld = 0;
	private List<double> timesHeld = new List<double>();

	public int timerdashthres = 30;
	private bool isplayAnim = false;
	private bool stateduringHold = false;

	private string[] startupStrings = new string[]
	{
		"Are you sure\nthis works?",
		"Attempt 11\nand counting...",
		"No, This is\nnot Souvenir.",
		"No, I did not\nmake a mistake.",
		"No, This contains\nTap Code.",
		"Just no.",
		"Yes. This\nis a thing.",
		"Oh no!\nNot again!",
		"Bright idea!",
		"...",
		"Simple,\nright?",
		"Just yes.",
		"Yes.",
		"Yes, this\nexists."
	};

	private string cStageModName = "";

	private bool solved = false;

	public IEnumerator currentlyRunning;

	// Use this for initialization
	void Start () {
		currentlyRunning = PlaySolveState ();
		_moduleId = _moduleIdCounter++;
		selectable.OnInteract += delegate {
			selectable.AddInteractionPunch((float)0.5);
			if (!solved&&!isplayAnim){
			isHolding = true;
			if (currentlyRunning!=null)
				StopCoroutine(currentlyRunning);
			text.color = new Color(text.color.r,text.color.g,text.color.b,(float)1.0);
			if ( (currentStage>=0 && currentStage<stagesNum.Count()) && InputMethod[currentStage].Equals("Morse"))
			{
				StartCoroutine(ShowMorseInput());
			}
			}
			stateduringHold = isplayAnim;
			return false;
		};
		selectable.OnInteractEnded += delegate {
			if (!solved&&!isplayAnim&&(!stateduringHold))// Detect if the module is solved, playing an animation, or being held while the animation is playing
			{
				isHolding = false;
				//print (timeHeld);
				timesHeld.Add(timeHeld);
				timeHeld = 0;
				if (isFinal)
				{
					if (timesHeld.Count()>=10)
					{
						if (stagesNum[currentStage]<0)
						{
							Debug.LogFormat("[Übermodule #{0}] Override detected! The module will now solve itself.",_moduleId);
							StopCoroutine(currentlyRunning);
							StartCoroutine(PlaySolveState());
						}
						else
						{
							Debug.LogFormat("[Übermodule #{0}] Override detected! However stage is valid to input. Strike!",_moduleId);
							StopCoroutine(currentlyRunning);
							text.color = new Color(text.color.r,text.color.g,text.color.b,(float)1.0);
							timesHeld.Clear();
							ModSelf.HandleStrike();
							StartCoroutine(PlayStrikeAnim(-1));
							TapCodeInput1 = 0;
							Debug.LogFormat("[Übermodule #{0}] Your input has been cleared.",_moduleId);
						}
					}
					else
					if(InputMethod[currentStage].Equals("Morse"))
					{
							currentlyRunning = CheckMorse();
							StartCoroutine(currentlyRunning);
					}
					else
					if (InputMethod[currentStage].Equals("Tap Code"))
					{
							currentlyRunning = CheckTapCode();
							StartCoroutine(currentlyRunning);
					}
					}
				else
				{
					Debug.LogFormat("[Übermodule #{0}] Strike! You cannot interact with the module until the module is in it's \"finale\" phase.",_moduleId);
					ModSelf.HandleStrike();
					StartCoroutine(PlayStrikeAnim(-1));
					timesHeld.Clear();
				}
			}

			return;
		};
		Debug.LogFormat("[Ubermodule #{0}] Entering Startup Phase...",_moduleId);
		UpdateScreen (startupStrings [UnityEngine.Random.Range (0, startupStrings.Length)]);
		if (ignores == null) {
			ignores = GetComponent<KMBossModule> ().GetIgnoredModules ("Ubermodule", new string[] {
				"Cookie Jars",
                "Cruel Purgatory",
				"Divided Squares",
				"Forget Enigma",
				"Forget Everything",
                "Forget Me Later",
				"Forget Me Not",
				"Forget Perspective",
                "Forget Them All",
				"Forget This",
                "Forget Us Not",
				"Hogwarts",
				"Organization",
				"Purgatory",
				"Simon's Stages",
				"Souvenir",
				"The Swan",
				"Tallordered Keys",
				"The Time Keeper",
				"Timing is Everything",
                "The Troll",
                "Turn The Key",
				"Übermodule",
				"The Very Annoying Button"
				
                
            });
		}
		Debug.LogFormat ("[Übermodule #{0}] Ignored Module List: {1}", _moduleId, FomatterDebugList (ignores)); // Prints ENTIRE list of Ignored Modules. Can be commented out later upon final release
		// Übermodule: Don't hang bombs with duplicates of THIS
		// Timing is Everything, Time Keeper, Turn The Key: Bomb Timer sensitive.
		// The Swan, The Very Annoying Button: RT Sensitive, would make sense to ignore?
		// Forget Everything, Forget Enigma, Forget Me Not, Forget Perspective, Forget This, Forget Them All, Forget Us Not: Relies on this module to be solved otherwise without Boss Module Manager detecting this.
		// Tallordered Keys: See "Forget" Modules
		// Hogwarts, Divided, Cookie, Forget Me Later: Something, something, bomb hanging...
		// Souvenir: Can eat up a lot of time for some reason from Ubermodule?
		// Purgatory + Cruel variant: Rare "last" condtion can hang bombs.
		Info.OnBombExploded += delegate {
			if (solved) return;
			Debug.LogFormat ("[Übermodule #{0}] Upon bomb detonation:", _moduleId);
            if (stagesNum.Length <= 0)
            {
                Debug.LogFormat("[Übermodule #{0}] Bomb detonated before stages were generated.", _moduleId);
                return;
            }
			for (int x=currentStage+1;x<stagesNum.Count();x++)
			{
				if (stagesNum[x]<0||stagesNum[x]>=solvedModules.Count())
				{
					Debug.LogFormat ("[Übermodule #{0}] Stage {1} would not be accessible.", _moduleId,x+1);
				}
				else
				{
					Debug.LogFormat ("[Übermodule #{0}] For stage {2}, the number {1} would be visible.", _moduleId,stagesNum[x]+1,x+1);
					Debug.LogFormat ("[Übermodule #{0}] The module that was solved for that stage would be {1}.", _moduleId,solvedModules[stagesNum[x]]);
					Debug.LogFormat ("[Übermodule #{0}] The defuser would have to input the correct letter in {1}.", _moduleId,InputMethod[x]);
				}
			}
			return;
		};
		ModSelf.OnActivate += delegate {
			UpdateScreen("0");
			started = true;
			// Section used for debugging ignored modules start here.
			solvables = Info.GetSolvableModuleNames ().Where (a => !ignores.Contains (a)).ToList ();
				if (solvables.Count () != 0)
				Debug.LogFormat ("[Übermodule #{0}] Non-ignored Modules: {1}", _moduleId, FomatterDebugList (solvables.ToArray ())); // Prints ENTIRE list of modules not ignored.
				else
				Debug.LogFormat ("[Übermodule #{0}] There are 0 non-ignored modules.", _moduleId);
			
			var ignored = Info.GetSolvableModuleNames().Where(a=>ignores.Contains(a)).ToList();
			Debug.LogFormat ("[Übermodule #{0}] Ignored Modules present (including itself): {1}", _moduleId,FomatterDebugList(ignored.ToArray())); // Prints ENTIRE list of modules ignored.
			// Section used for debugging ignored modules end here.

			stagesToGenerate = UnityEngine.Random.Range (3, 5);
			stagesNum = new int[stagesToGenerate];
			InputMethod = new string[stagesToGenerate];

			var numbers = new int[solvables.Count ()];
			for (int p = 0; p < solvables.Count (); p++) {
				numbers [p] = p;
			}
			for (int p = 0; p < solvables.Count (); p++) {
				var temp = -1;
				var toreplace = UnityEngine.Random.Range (p, solvables.Count ());
				temp = numbers[p];
				numbers [p] = numbers [toreplace];
				numbers [toreplace] = temp;
			}
			for (int x = 0; x < stagesToGenerate; x++) {
				var pickState = new string[] { "Tap Code","Morse" };
				var RandomState = "";
				if (x < solvables.Count ()){
					stagesNum [x] = numbers [x];
					RandomState = pickState[UnityEngine.Random.Range (0, pickState.Count())];
				} else {
					stagesNum [x] = -1;
				}
				InputMethod [x] = RandomState;
				if (stagesNum [x] >= 0) {
					if (RandomState.Equals("Morse"))
						Debug.LogFormat ("[Übermodule #{0}] Generated manditory stage {1} requiring Morse input.", _moduleId, numbers [x] + 1);
					else if (RandomState.Equals("Tap Code"))
						Debug.LogFormat ("[Übermodule #{0}] Generated manditory stage {1} requiring Tap Code input.", _moduleId, numbers [x] + 1);
				}
			}
		};
	}
	string FomatterDebugList(string[] list) // This one is more used compared to the one underneath.
	{
		string output = "";
		for (int o = 0; o < list.Count(); o++) {
			if (o != 0)
				output += ", ";
			output += list[o];
			}
		return output;
	}
	string FomatterDebugList(List<String> list)
	{
		string output = "";
		for (int o = 0; o < list.Count(); o++) {
			if (o != 0)
				output += ", ";
			output += list[o];
		}
		return output;
	}
	void UpdateScreen(string value) // Update to the given text
	{

		var lowervalue = value.ToLower ();
		var largestLength = 0;
		var clength = 0;
		for (int x = 0; x < lowervalue.Length; x++) {
			if (lowervalue.Substring (x, 1).RegexMatch (".")) {
				clength++;
			} else {
				if (clength > largestLength)
					largestLength = clength;
				clength = 0;
			}
		}
		if (clength > largestLength)
            largestLength = clength;


		if (value.Length == 0) {
			text.fontSize = 375;
		} else {
			text.fontSize = (int)((375 / Mathf.Pow(largestLength,(float)0.9)));
		}
		text.text = value;
	}
	string SplitTextSpecial (string input)
	{
		var checker = input;
		//checker = "Boolean Venn Diagram"; // Used for Testing, 
		var largest = 0;
		var words = checker.Split(new[] {' ','|',',',' '}).ToList();
		var splits = new List<int>();
		for (int x = 0; x < words.Count ()-1; x++) {
			if (Math.Abs ((words [x + 1].Length + words [x].Length) - largest) <= 2) {
				splits.Add (x+1);
				x++;
			}
			else if (words[x].Length>=largest||words[x+1].Length>=largest){
				splits.Add (x);
			}
		}	
		var output = "";
		if (words.Count > 0) {
			for (int x = 0; x < words.Count () - 1; x++) {
				output += words [x];
				if (splits.Contains(x)) {
					output += "\n";
				} else {
					output += " ";
				}
			}
			output += words [words.Count() - 1];
		}
		return output;
	}
	// Update is called once per frame
	void Update () {
		if (!solved) {
			if (isHolding) {
				timeHeld++;
			}
			if (started) {
				if (CanUpdateCounterNonBoss ()) {
					var list1 = Info.GetSolvedModuleNames ().Where (a => !ignores.Contains (a)).ToList();
					if (list1.Count () != solvedModules.Count()) {
						foreach (String A in solvedModules) {
							list1.Remove (A);
						}
						solvedModules.AddRange (list1);
                        Debug.LogFormat("[Übermodule #{0}] ---------- {1} Solved ----------", _moduleId, Info.GetSolvedModuleNames().Count());
                        Debug.LogFormat("[Übermodule #{0}] Non-ignored Modules Currently Solved: {1}", _moduleId, FomatterDebugList(solvedModules));
                    }
					string value = solvedModules.Count().ToString();
					if (!isFinal) {
						UpdateScreen (value);
						if (solvedModules.Count () >= solvables.Count ()) {
                            StartCoroutine(PlayFinaleState());
						}
                    }
				}
            }
		}
	}
	IEnumerator GetStage(int cstage)
	{
		isplayAnim = true;
		sound.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.MenuButtonPressed, transform);
		for(int cnt=0;cnt<animationLength+1;cnt++)
		{
			if (cnt == 0) {
				if (cstage>=0&&stagesNum[cstage]>=0) {
					Debug.LogFormat ("[Übermodule #{0}] You need to input stage {1}.", _moduleId, stagesNum[cstage]+1);
					if (InputMethod[currentStage].Equals("Morse")) {
						Debug.LogFormat ("[Übermodule #{0}] You need to input the correct letter in Morse.", _moduleId);
					}
					else if (InputMethod[currentStage].Equals("Tap Code")) {
						Debug.LogFormat ("[Übermodule #{0}] You need to input the correct letter in Tap Code.", _moduleId);
					}
					UpdateScreen ((stagesNum[cstage]+1).ToString());
					Debug.LogFormat("[Übermodule #{0}] The solved module for that stage was: {1}",_moduleId,solvedModules [stagesNum[cstage]]);
					//Debug.LogFormat ("[Ubermodule #{0}] For reference, the module name is {1}", _moduleId,solvedModules[stagesNum[cstage]]);
				} else {
					Debug.LogFormat ("[Übermodule #{0}] The modules has ran out of stages to input.", _moduleId);
					Debug.LogFormat ("[Übermodule #{0}] Enforce a solve by clicking on this module 10 times.", _moduleId);
					UpdateScreen ("?");
				}
			}
			if (InputMethod [currentStage].Equals ("Morse")) {
				text.color = new Color (1, 0, 0, (float)cnt / animationLength);
			} else if (InputMethod [currentStage].Equals ("Tap Code")) {
				text.color = new Color (0, 0, 1, (float)cnt / animationLength);
			} else {
				text.color = new Color (1, 1, 1, (float)cnt / animationLength);
			}

			yield return new WaitForSeconds(0);
		}
		isplayAnim = false;
	}

	IEnumerator PlayStrikeAnim(int cstage)
	{
		isplayAnim = true;
		for(int cnt=0;cnt<animationLength;cnt++)
		{
			text.transform.Rotate (Vector3.back*6);
			text.color = new Color(1,0,0,(float)(1.0-(float)cnt/animationLength));
			yield return new WaitForSeconds(0);
		}
        if (isFinal && cstage >= 0 && cstage < stagesNum.Count())
        {
            Debug.LogFormat("[Übermodule #{0}] Revealing module name that was solved that advanced the counter to {1}", _moduleId, stagesNum[cstage] + 1);
            UpdateScreen(SplitTextSpecial(solvedModules[stagesNum[cstage]]));
            //Debug.LogFormat("[Ubermodule #{0}] The solved module for that stage was: {1}",_moduleId,solvedModules [stagesNum[cstage]]);
        }
        for (int cnt=0;cnt<animationLength+1;cnt++)
		{
            if (cnt<animationLength)
			text.transform.Rotate (Vector3.back*6);
			if (!isFinal) {
				text.color = new Color (0, 0, 0, (float)cnt / animationLength);
			}
			else
			{
				if (InputMethod[currentStage].Equals("Morse")) {
					text.color = new Color (1, 0, 0, (float)cnt / animationLength);
				}
				else if (InputMethod [currentStage].Equals ("Tap Code")){
					text.color = new Color (0, 0, 1, (float)cnt / animationLength);
				}
			}
			yield return new WaitForSeconds(0);
		}
		isplayAnim = false;
	}
	IEnumerator PlayFinaleState()
	{
		isplayAnim = true;
		Debug.LogFormat ("[Übermodule #{0}] All non-ignored modules have been solved, activating \"finale\" phase.", _moduleId);
		isFinal = true;
		sound.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.LightBuzz, transform);
		for (int cnt = 0; cnt < 4*animationLength; cnt++) {
			text.color = new Color(text.color.r,text.color.g,text.color.b,(float)(1.0-(float)cnt/animationLength/4));

			yield return new WaitForSeconds(0);
			if ((cnt%30>=15||cnt<=2*animationLength)&&cnt%30<25)
				screen.material = materials[0];
			else
				screen.material = materials[1];
		}
		screen.material = materials[1];
		AdvanceStage ();
		isplayAnim = false;
	}
	IEnumerator PlaySolveState()
	{
		solved = true;
		ModSelf.HandlePass ();
		sound.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.CorrectChime, transform);
		Debug.LogFormat("[Übermodule #{0}] Module solved.",_moduleId);
		string[] characters = new string[26] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
		int randomstartB = UnityEngine.Random.Range(0,characters.Count());
		int randomstartA = UnityEngine.Random.Range(0,characters.Count());
		for (int cnt = 0; cnt < animationLength; cnt++) {
			randomstartA++;
			randomstartB++;
            text.color = new Color(1, 1, 1, (float)cnt / animationLength);
            UpdateScreen(characters[(randomstartA + cnt) % characters.Count()] + characters[(randomstartB + cnt) % characters.Count()]);
			yield return new WaitForSeconds(0);
		}
		while (randomstartA % 26 != 6||randomstartA<52) {
			randomstartA++;
			randomstartB++;
            UpdateScreen(characters[randomstartA % characters.Count()] + characters[randomstartB % characters.Count()]);
			yield return new WaitForSeconds(0);
		}
		while (randomstartB % 26 != 6||randomstartB<104) {
			randomstartB++;
            UpdateScreen(characters[randomstartA % characters.Count()] + characters[randomstartB % characters.Count()]);
			yield return new WaitForSeconds(0);
		}

	}
    bool CanUpdateCounterNonBoss()
	{
		var list1 = Info.GetSolvedModuleNames().Where(a => !ignores.Contains(a));
		return list1.Count () >= solvedModules.Count ();
	}
	void AdvanceStage()
	{
		currentStage++;
		if (currentStage >= stagesToGenerate) {
			Debug.LogFormat ("[Übermodule #{0}] No more stages to go.", _moduleId);
			StartCoroutine (PlaySolveState());
		} else {
			if (currentStage > 0) {
				Debug.LogFormat ("[Übermodule #{0}] Correct character inputted. Moving on to next stage.", _moduleId);
			}
			StartCoroutine (GetStage (currentStage));
		}
	}
	IEnumerator ShowMorseInput()
	{
		while (isHolding) {
			if (timeHeld > timerdashthres) {
				UpdateScreen ("\uFE58");
			} else {
				UpdateScreen ("\u2022");
			}
			yield return new WaitForSeconds (0);
		}
		//UpdateScreen ((stagesNum[currentStage]+1).ToString());
		yield return null;
	}
    string GetLetterFromMorse(string input)
	{
		switch (input) {
			case ".":
				return "E";
			case "-":
				return "T";
			case ".-":
				return "A";
			case "-.":
				return "N";
			case "--":
				return "M";
			case "..":
				return "I";
			case "...":
				return "S";
			case ".-.":
				return "R";
			case "..-":
				return "U";
			case "-..":
				return "D";
			case ".--":
				return "W";
			case "-.-":
				return "K";
			case "--.":
				return "G";
			case "---":
				return "O";
			case "-.-.":
				return "C";
			case "..-.":
				return "F";
			case "-...":
				return "B";
			case ".--.":
				return "P";
			case "-.--":
				return "Y";
			case "--..":
				return "Z";
			case "...-":
				return "V";
			case "--.-":
				return "Q";
			case ".-..":
				return "L";
			case ".---":
				return "J";
			case "....":
				return "H";
			case "-..-":
				return "X";
			case ".----":
				return "1";
			case "..---":
				return "2";
			case "...--":
				return "3";
			case "....-":
				return "4";
			case ".....":
				return "5";
			case "-....":
				return "6";
			case "--...":
				return "7";
			case "---..":
				return "8";
			case "----.":
				return "9";
			case "-----":
				return "0";
			default:
				return "?";
		}
	}
    string GetFirstValidCharacter(string module)
    {
        var input = module.ToUpper();
        var output = "";
        for (var currentindex = 0; currentindex<input.Length&&output.Length==0; currentindex++)
        {
            var currentLetter = input.Substring(currentindex, 1);
            if (currentLetter.RegexMatch(@"\w"))
            {
                output = currentLetter;
            }
        }
        return output;
    }
    bool IsCorrect(string input)
	{
		cStageModName = solvedModules[stagesNum[currentStage]];
        if (Regex.IsMatch(cStageModName, @"^The\s"))// Filter out the word "The " at the start of the module name, if present
        {
			cStageModName = cStageModName.Substring (4);;
		}
        var letterRequired = GetFirstValidCharacter(cStageModName);
        if (letterRequired.Length != 0)
        {
            Debug.LogFormat("[Übermodule #{0}] Checking \"{1}\" with \"{2}\"...", _moduleId, letterRequired, input);
            return input.EqualsIgnoreCase(letterRequired);
        }
        Debug.LogFormat("[Übermodule #{0}] There is no valid detectable character from this. Skipping check...", _moduleId, letterRequired, input);
        return true;
	}
	IEnumerator CheckMorse()
	{
        for (int cnt = 0; cnt < animationLength * 4; cnt++)
        {
			text.color = new Color(text.color.r,text.color.g,text.color.b,(float)(1.0-(float)cnt/animationLength));

			yield return new WaitForSeconds(0);
		}
		var morseIn = "";
		for (int x = 0; x < timesHeld.Count (); x++) {
			if (timesHeld [x] > timerdashthres) {
				morseIn += "-";
			} else {
				morseIn += ".";
			}
		}
		cStageModName = solvedModules[currentStage];
		var letterInputted = GetLetterFromMorse(morseIn);
		timesHeld.Clear ();
		if (IsCorrect(letterInputted)) {
			AdvanceStage ();
		} else {
			UpdateScreen (letterInputted);
			if (letterInputted.Equals ("?")) {
				Debug.LogFormat("[Übermodule #{0}] Strike! The module could NOT reference a valid letter or digit for Morse!",_moduleId);
				Debug.LogFormat("[Übermodule #{0}] The recorded input: {1} is not valid for Morse.",_moduleId,morseIn);
			}
			else{
				Debug.LogFormat("[Übermodule #{0}] Strike! \"{1}\" was inputted which is not correct!",_moduleId,letterInputted);
			}
			ModSelf.HandleStrike ();
			StartCoroutine (PlayStrikeAnim (currentStage));
		}
		yield return null;
	}
	private int TapCodeInput1 = 0;
	IEnumerator CheckTapCode()
	{
		var GridLetters = new[] {
			new[] {"A","B","C","D","E","1"},
			new[] {"F","G","H","I","J","2"},
			new[] {"L","M","N","O","P","3"},
			new[] {"Q","R","S","T","U","4"},
			new[] {"V","W","X","Y","Z","5"},
			new[] {"6","7","8","9","0","K"}
		};// Grid for Tap Code, not a lot of use otherwise.
        for (int cnt = 0; cnt < animationLength * 4; cnt++)
        {
			text.color = new Color(text.color.r,text.color.g,text.color.b,(float)(1.0-(float)cnt/animationLength));

			yield return new WaitForSeconds(0);
		}
		sound.PlaySoundAtTransform ("MiniTap",transform);
		if (TapCodeInput1 == 0) {
			TapCodeInput1 = timesHeld.Count ();
			timesHeld.Clear ();
			text.color = new Color(text.color.r,text.color.g,text.color.b,(float)1.0);
		}
			else
		{
			var TapCodeInput2 = timesHeld.Count ();
			timesHeld.Clear ();
			var letterInputted = "?";
			if ((TapCodeInput1 >= 1 && TapCodeInput1 <= 6) && (TapCodeInput2 >= 1 && TapCodeInput2 <= 6)) {
				letterInputted = GridLetters [TapCodeInput1 - 1] [TapCodeInput2 - 1];
			}
			if (IsCorrect(letterInputted)) {
				AdvanceStage ();
			} else {
				UpdateScreen (letterInputted);
				if (letterInputted.Equals ("?")) {
					Debug.LogFormat("[Übermodule #{0}] Strike! The module could NOT reference a valid letter or digit for Tap Code!",_moduleId);
					Debug.LogFormat("[Übermodule #{0}] The recorded input: {1}, {2} is not a valid for Tap Code.",_moduleId,TapCodeInput1,TapCodeInput2);
				} else {
					Debug.LogFormat("[Übermodule #{0}] Strike! \"{1}\" was inputted which is not correct!",_moduleId,letterInputted);
				}
				ModSelf.HandleStrike ();
				StartCoroutine (PlayStrikeAnim (currentStage));
			}
			TapCodeInput1 = 0;
		}
		yield return null;
	}
}
