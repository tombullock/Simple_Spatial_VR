using System.Collections;
using System.Collections.Generic;
using System.IO; // need this for StreamReader
using UnityEngine;
using LabJack.LabJackUD; // needed for LabJack
using sxr_internal;

// To-Do Lists:
// How to quit experiment after X blocks?  Also how to shut down labjack?
// How to make controller buzz when sphere destroyed?
// Make a proper circle around fixation (chat-gpt prompt: Write a function in c# that returns an array.  The array will consist of the (x,y) coordinates for six equally spaced points on a circle.  The six points will be 30, 90, 150, 210, 270, 330 degrees.
// How to add a warning in case labjack not plugged in and active?  Also create a "no labjack" switch for development/testing.
// How to pull eye-tracking data out of sxr.GetFullGazeData?
// How to pull in sjNum from sxr?  Justin?
// How to stop lots of debug.logs appearing in console?
// Random.range doesn't seem to do a very good job of jittering!  Another method perhaps?
// How to make participant height adjustable



// define class for trial sequences
[System.Serializable]
public class TrialSequences
{
    public int[] oddball_data;
    public int[] location_data;
}

// study class
public class MyStudy : MonoBehaviour
{
    // create instance of trailSequences class here
    public TrialSequences trialSequences;

    // define method to read trial sequence JSON
    public TrialSequences ReadTrialSequences(string path, string filename)
    {
        string json;
        using (StreamReader myFile = new StreamReader(Path.Combine(path, filename)))
        {
            json = myFile.ReadToEnd();
        }
        TrialSequences trialSequences = JsonUtility.FromJson<TrialSequences>(json);
        return trialSequences;
    }

    // set default sjNum (overwritten by sXR screen inputted subject number later in script)
    //int sjNum = 99;

    // input participant height (need to make the location heights scale to this, also make this an option to input on startup, ideally)
    float ph = 1.8f;

    // define some vars
    int targetColorIdx;
    float responseTime;

    // create some colors
    Color blueColor = new Color(0f, 0f, 1f, 1f);
    Color redColor = new Color(1f, 0f, 0f, 1f);
    Color greenColor = new Color(0f, 1f, 0f, 1f);
    Color blackColor = new Color(0f, 0f, 0f, 1f);
    Color whiteColor = new Color(1f, 1f, 1f, 1f);

    // experiment settings
    Vector3 fixationPointSize = new Vector3(0.2f, 0.2f, 0.2f); // set fixation sphere size
    Vector3 targetSphereScale = new Vector3(0.4f, 0.4f, 0.4f); // set target sphere size (GOOD FOR STUDY)

    // set target locs float and location index (need to tie this to experiment phase?)
    float[,] locs = { { } };

    // stimuli appear at sitting height (for development)
    //float[,] locsSpatialFix = { { 0.5f, 1.5f, 0 }, { -0.5f, 1.5f, 0 }, { 0.5f, 0.5f, 0 }, { -0.5f, 0.5f, 0 }, { 0.75f, 1f, 0 }, { -0.75f, 1f, 0 } }; // equal locs around central fixation sphere SITTING HEIGHT
    //float[,] locsCentered = { { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 } }; // all at the same (centered) location SITTING HEIGHT
    //float[] locsFixation = { 0, 1.5f, 0 };

    // stimuli appear at standing height
    float[,] locsSpatialFix = { { 0.5f, 2f, 0 }, { -0.5f, 2f, 0 }, { 0.5f, 1f, 0 }, { -0.5f, 1f, 0 }, { 0.75f, 1.5f, 0 }, { -0.75f, 1.5f, 0 } }; // equal locs around central fixation sphere STANDING HEIGHT
    float[,] locsCentered = { { 0, 1.5f, 0 }, { 0, 1.5f, 0 }, { 0, 1.5f, 0 }, { 0, 1.5f, 0 }, { 0, 1.5f, 0 }, { 0, 1.5f, 0 } }; // all at the same (centered) location STANDING HEIGHT
    float[] locsFixation = { 0, 1.5f, 0 };

    // set trial sequence loader to true (loads new trial sequence at start of each block)
    bool loadTrialSequence = true;

    // init an instance of LabJack
    private U3 u3;
    bool trigger = true;


    // pre-game loop (things in here are called on the first frame update)
    void Start()
    {
        // init the LabJack and set port to zero
        u3 = new U3(LJUD.CONNECTION.USB, "0", true);
        LJUD.ePut(u3.ljhandle, LJUD.IO.PUT_DIGITAL_PORT, 8, 0, 12);

        // do eye-tracker calibration
       // sxr.LaunchEyeCalibration();
    }


    // main game loop (Update is called once per frame)
    void Update()
    {

        // set number of trials and blocks per condition
        int nTrials = trialSequences.location_data.Length;
        int nBlocks = 2; // 6 blocks max currently

        // if experiment has been started with start button (start of sXR functions)
        if (sxr.GetPhase() > 0) 
        {

            // get sjNum from screen input from sXR gui input (defaults to sj00 if nothing entered)
            int sjNum = int.Parse(ExperimentHandler.Instance.subjectID); 
            
            // load trial sequence for this block on first trial
            if (sxr.GetTrial() == 0 && loadTrialSequence == true)
            {
                // read in trial sequence
                int iCond = sxr.GetPhase(); // sxr.phase = condition 
                int iBlock = sxr.GetBlock()+1; // sxr.block starts at zero, hence the +1 
                string path = "C:\\Users\\sloth\\Documents\\Trial_Sequences\\"; // remember trial sequences NOT located in project folder because of silly spaces in directory name!
                string filename = string.Format("sj{0}_cd{1}_bl{2}_trial_seq.json", sjNum.ToString().PadLeft(2, '0'), iCond.ToString().PadLeft(2, '0'), iBlock.ToString().PadLeft(2, '0')); // creates filename
                Debug.Log(filename); // sends message to console 
                trialSequences = ReadTrialSequences(path, filename); // load trial sequence
                loadTrialSequence = false; // once trial sequence loaded, set this false to prevent trial sequence continuously reloading on each update
            }

            // if blocks finished, move to next phase of study 
            if (sxr.GetBlock() > nBlocks-1) 
                sxr.NextPhase();

            // set locations for experiment (counterbalance based on sjNum)
            int counterbalancingOrder = sjNum % 2;
            if (counterbalancingOrder == 0)
            {
                if (sxr.GetPhase() == 2)
                    locs = locsCentered;
                else if (sxr.GetPhase() == 1)
                    locs = locsSpatialFix;
            }
            else if (counterbalancingOrder == 1)
            {
                if (sxr.GetPhase() == 1)
                    locs = locsCentered;
                else if (sxr.GetPhase() == 2)
                    locs = locsSpatialFix;
            }

            // switch between steps in trial
            switch (sxr.GetStepInTrial()) 
            {
                // press trigger to start block, also sets up logging 
                case 0: 
                   
                    if (sxr.GetTrial() == 0)
                    {
                        sxr.DisplayImage("trigger"); //displays press trigger to continue, start study on press and write headers to datafile
                        if (sxr.GetTrigger())
                        {
                            sxr.WriteHeaderToTaggedFile("mainFile", "Stim_Location" + "," + "Stim_Type" + "," + "Response_Time" + "," + "Trial_Time" + "," + "Jitter" + "," + "CB_Order"); // write headers to datafile
                            sxr.HideImagesUI(); // hide loading msg
                            sxr.NextStep();
                        }                
                    }
                    else
                    {
                        sxr.NextStep();
                    }
          
                    break;

                // generate stimuli for each trial
                case 1: 

                    sxr.NextStep(); // instruction to move to next step of trial (case 1)
                    sxr.StartTimer(); // start the timer for this trial
                    sxr.StartRecordingEyeTrackerInfo(); // start the eye-tracker recording [is it good idea to start eye-tracker rec on each trial?]
                    sxr.StartRecordingCameraPos(); // start the world camera position recording
                    trigger = true; // set trigger to true at start of trial

                    // draw a fixation point for the spatial version(s) of the task only [THIS ACTUALLY ONLY NEEDS DRAWING ONCE AT START OF BLOCK.  TRY TO MOVE?]
                    if (sxr.GetPhase() != 0) // CHANGE BACK TO ZERO IF WANT FIX POINT IN ALL CONDITIONS 
                    {
                        if (!sxr.ObjectExists("Fixation_Point"))
                        {
                            sxr.SpawnObject(PrimitiveType.Sphere, "Fixation_Point", locsFixation[0], locsFixation[1], locsFixation[2]); // spawn a fixation sphere at center
                            var fixationPoint = sxr.GetObject("Fixation_Point");
                            sxr.ResizeObject(fixationPoint, fixationPointSize, 0); // resize object
                            var fixationPointRenderer = fixationPoint.GetComponent<Renderer>(); // set sphere color to black
                            fixationPointRenderer.material.SetColor("_Color", whiteColor); // CHANGED TO BLACK FOR TIMING TEST
                        }
                    }

                    // create stimulus
                    sxr.SpawnObject(PrimitiveType.Sphere, "Target_Sphere", locs[trialSequences.location_data[sxr.GetTrial()], 0], locs[trialSequences.location_data[sxr.GetTrial()], 1], locs[trialSequences.location_data[sxr.GetTrial()], 2]); // spawns
                    var targetSphere = sxr.GetObject("Target_Sphere"); // creates "Target_Sphere" gameObject from ^
                    sxr.ResizeObject(targetSphere, targetSphereScale, 0); // resize targetSphere
                    var targetSphereRenderer = targetSphere.GetComponent<Renderer>(); // set targetSphere color 

                    // assign color to target sphere 
                    targetColorIdx = trialSequences.oddball_data[sxr.GetTrial()]; // get stimulus color for this trial
                    if (targetColorIdx == 0)
                    {
                        targetSphereRenderer.material.SetColor("_Color", greenColor);
                    }
                    else if (targetColorIdx == 1)
                    {
                        targetSphereRenderer.material.SetColor("_Color", blueColor);
                    }
                    else if (targetColorIdx == 2)
                    {
                        targetSphereRenderer.material.SetColor("_Color", redColor);
                    }

                    // TIMING TEST - OVERRIDE TO MAKE SPHERE WHITE
                    //targetSphereRenderer.material.SetColor("_Color", whiteColor);

                    // send event trigger to mark stimulus onset (send trial number)
                    LJUD.ePut(u3.ljhandle, LJUD.IO.PUT_DIGITAL_PORT, 8, sxr.GetTrial()+1, 12); 

                    break;

                // keep target stimulus on screen for XX ms AND check for collision of saber and sphere 
                case 2: 
                    
                    float t = sxr.TimePassed(); // t is the time delta since start of study
                    sxr.GetFullGazeInfo(); // get gaze data from eyetracker

                    // attempt to add a real-time gaze marker (failed)
                    //Vector3 gazeVector = GazeHandler.Instance.GazeFixation();
                    //sxr.SpawnObject(PrimitiveType.Sphere, "Gaze_Marker", gazeVector[0], gazeVector[1], gazeVector[2]); // spawns a sphere for eye-gaze
                    //Object.Destroy(sxr.GetObject("Gaze_Marker")); 
                    

                    // if collision then destroy sphere and move to next trial [may need a pause here to prevent prepotent responses]
                    if (sxr.CheckCollision(sxr.GetObject("RightController"), sxr.GetObject("Target_Sphere"))){
                        Object.Destroy(sxr.GetObject("Target_Sphere"));  // destroy the stimulus
                        responseTime = sxr.TimePassed();
                        
                        // send event code 200 to mark collision
                        if (trigger)
                            {
                                LJUD.ePut(u3.ljhandle, LJUD.IO.PUT_DIGITAL_PORT, 8, 200, 12);
                                trigger = false;
                            }

                        // add haptic response if possible here

                        sxr.NextStep(); // move to ISI 
                    }

                    // if no collision within specified time, then destroy block and move to next trial
                    if (sxr.TimePassed() > 1f)
                    {
                        Object.Destroy(sxr.GetObject("Target_Sphere"));
                        responseTime = 99; // assign arb value to responseTime
                        sxr.NextStep();
                    }
  
                    break;
                
                // Wait until XX secs passed before showing next stimulus OR moving to next block
                case 3:

                    // generate ISI jitter
                    float minJitterRange = 1.2f;
                    float maxJitterRange = 1.5f;
                    float jitter = Random.Range(minJitterRange, maxJitterRange);
                    
                    if (sxr.TimePassed() > jitter) 
                    {
                        // pause recordings and write all trial/eye/camera data to files                 
                        sxr.PauseRecordingEyeTrackerInfo();
                        sxr.PauseRecordingCameraPos();
                        sxr.WriteToTaggedFile("mainFile", trialSequences.location_data[sxr.GetTrial()].ToString() + "," + trialSequences.oddball_data[sxr.GetTrial()].ToString() + "," + responseTime.ToString() + "," + sxr.TimePassed().ToString() + "," + jitter.ToString() + "," + counterbalancingOrder.ToString()); // log data

                        if (sxr.GetTrial() < nTrials-1) // move to next trial
                        {
                            sxr.NextTrial();
                        }
                        else
                        {
                            sxr.NextBlock(); // move to next block
                            loadTrialSequence = true; // set to true to load new trial sequence at start of next block
                        }
                    }
                        
                    break;

            }
        }
    }
}