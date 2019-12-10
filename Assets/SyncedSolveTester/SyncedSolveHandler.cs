using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncedSolveHandler : MonoBehaviour {

	public KMBombModule ModSelf;
	public KMSelectable selectable;
	public TextMesh textMesh;


	private static int delay = 60;
	private static bool startcd = false;

	private readonly string[] possibleNames = new string[]
	{"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
	"N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
	"0","1","2","3","4","5","6","7","8","9"};

	private static int modID = 1;
	private int curmodID;

	void Awake()
	{
		curmodID = modID++;
	}

	// Use this for initialization
	void Start () {
		ModSelf.ModuleDisplayName = possibleNames[Random.Range(0, possibleNames.Length)];
		if (Random.value < .5)
		{
			ModSelf.ModuleDisplayName += " " + possibleNames[Random.Range(0, possibleNames.Length)];
		}
		else
		{
			ModSelf.ModuleDisplayName += possibleNames[Random.Range(0, possibleNames.Length)];
		}
		Debug.LogFormat("[Synced Solve Module Tester {0}]: The module's actual disaplayed name is {1}",curmodID,ModSelf.ModuleDisplayName);
		textMesh.text = ModSelf.ModuleDisplayName;
		selectable.OnInteract += delegate
		{
			startcd = true;
			return false;
		};

	}

	// Update is called once per frame
	void Update () {
		if (startcd)
		{
			if (delay > 0)
			{
				delay--;
			}
			else
			{
				ModSelf.HandlePass();
			}
		}
	}
}
