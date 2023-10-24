using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LabJack.LabJackUD;
//Notes and Questions:
// How do I launch Eye-tracker calibration? 
// How to quit experiment after X blocks?
// How to make controller buzz when sphere destroyed?
// How to import a trial matrix from a csv or another script?
// Note: chat-gpt prompt: Write a function in c# that returns an array.  The array will consist of the (x,y) coordinates for six equally spaced points on a circle.  The six points will be 30, 90, 150, 210, 270, 330 degrees.


public class MyStudy : MonoBehaviour
{
    // select experiment version [1=centered, 2=spatial] - perhaps this should be a "phase" in sXR  (not working yet)
    int expVersion = 2;

    // input participant height (doesn't work yet)
    float ph = 1f;

    // create some colors
    Color blueColor = new Color(0f, 0f, 1f, 1f);
    Color redColor = new Color(1f, 0f, 0f, 1f);
    Color greenColor = new Color(0f, 1f, 0f, 1f);
    Color blackColor = new Color(0f, 0f, 0f, 1f);
    Color whiteColor = new Color(1f, 1f, 1f, 1f);

    // experiment settings
    Vector3 fixationPointSize = new Vector3(0.25f, 0.25f, 0.25f); // set fixation sphere size
    Vector3 targetSphereScale = new Vector3(0.4f, 0.4f, 0.4f); // set target sphere size

    // set target locs float and location index
    //float[,] locs = { };
    float[,] locs = { { 0.5f, 1.5f, 0 }, { -0.5f, 1.5f, 0 }, { 0.5f, 0.5f, 0 }, { -0.5f,  0.5f, 0 }, { 0.75f, 1f, 0 }, { -0.75f, 1f, 0 } }; // equal locs around central fixation sphere
    //float[,] locs = { { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 } }; // all at the same (centered) location
    int locIndex;

    // set trial matrix (need to import from elsewhere ideally - this would be three blocks of 30 pseudorandom trials - all same order for now)
    int[,] trialTypeIdx = {
        { 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 0, 2, 2, 2, 1, 2, 2, 2, 0, 2, 2, 2, 2, 1 },
        { 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 0, 2, 2, 2, 1, 2, 2, 2, 0, 2, 2, 2, 2, 1 },
        { 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 0, 2, 2, 2, 1, 2, 2, 2, 0, 2, 2, 2, 2, 1 }};

   

    // init an instance of LabJack
    private U3 u3;
    bool trigger = true;


    // Start is called before the first frame update
    void Start()
    {
        // init the LabJack and set port to zero
        u3 = new U3(LJUD.CONNECTION.USB, "0", true);
        LJUD.ePut(u3.ljhandle, LJUD.IO.PUT_DIGITAL_PORT, 8, 0, 12);
    }

    IEnumerator Waiter() //??? why is this not working???
    {
        yield return new WaitForSecondsRealtime(1f);
    }


    // Update is called once per frame
    void Update()
    {

        // init some experiment stuff (why can't I do this outside of loop?)
        int nTrials = trialTypeIdx.GetLength(1);
        int nBlocks = trialTypeIdx.GetLength(0);


        // if experiment has been started with start button
        if (sxr.GetPhase() > 0) 
        {


            // switch between steps in trial
            switch (sxr.GetStepInTrial()) 
            {
                // press trigger to start block, also sets up logging 
                case 0: 
                   
                    // if blocks finished, quit study
                    if (sxr.GetBlock() > 1)
                    {
                        Application.Quit();
                    }
                    
                    if (sxr.GetTrial() == 0)
                    {
                        sxr.DisplayImage("trigger"); //displays press trigger to continue, start study on press and write headers to datafile
                        if (sxr.GetTrigger())
                        {
                            sxr.WriteHeaderToTaggedFile("mainFile", "Target_Location" + "," + "Target_Duration"); // write headers to datafile
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
                  
                    // draw a fixation point for the spatial version of the task only
                    if (expVersion == 2) 
                    {
                        if (!sxr.ObjectExists("Fixation_Point"))
                        {
                            sxr.SpawnObject(PrimitiveType.Sphere, "Fixation_Point", 0f, 1f, 0f); // spawn a fixation sphere at center
                            var fixationPoint = sxr.GetObject("Fixation_Point");
                            sxr.ResizeObject(fixationPoint, fixationPointSize, 0); // resize object
                            var fixationPointRenderer = fixationPoint.GetComponent<Renderer>(); // set sphere color to black
                            fixationPointRenderer.material.SetColor("_Color", whiteColor);
                        }
                    }

                    // spawn a target sphere at location (x,y,z)
                    locIndex = Random.Range(0, 6); // location index for this trial (currently set to random, but will need to pseudorandomize)
                    sxr.SpawnObject(PrimitiveType.Sphere, "Target_Sphere", locs[locIndex, 0], locs[locIndex, 1], locs[locIndex, 2]); // spawns
                    var targetSphere = sxr.GetObject("Target_Sphere"); // creates "Target_Sphere" gameObject from ^
                    sxr.ResizeObject(targetSphere, targetSphereScale, 0); // resize targetSphere
                    var targetSphereRenderer = targetSphere.GetComponent<Renderer>(); // set targetSphere color 

                    // assign color to target sphere
                    //int targetColorIdx = Random.Range(0, 3); // color index for this trial (currently set to random but will need to set according to trial matrix)
                    int targetColorIdx = trialTypeIdx[sxr.GetBlock(),sxr.GetTrial()]; // get stimulus color for this trial [row, col] 

                    if (targetColorIdx == 0)
                    {
                        targetSphereRenderer.material.SetColor("_Color", redColor);
                    }
                    else if (targetColorIdx == 1)
                    {
                        targetSphereRenderer.material.SetColor("_Color", blueColor);
                    }
                    else if (targetColorIdx == 2)
                    {
                        targetSphereRenderer.material.SetColor("_Color", greenColor);
                    }
                   
                    // send event marker for stimulus onset (just actual trial count number)
                    LJUD.ePut(u3.ljhandle, LJUD.IO.PUT_DIGITAL_PORT, 8, sxr.GetTrial()+1, 12); 

                    break;

                // keep target stimulus on screen for XX ms AND check for collision of saber and sphere 
                case 2: 
                    
                    float t = sxr.TimePassed(); // t is the time delta since start of study
                    sxr.GetFullGazeInfo(); // get gaze data from eyetracker

                    // if collision then destroy sphere and move to next trial [may need a pause here to prevent prepotent responses]
                    if (sxr.CheckCollision(sxr.GetObject("RightController"), sxr.GetObject("Target_Sphere"))){
                        Object.Destroy(sxr.GetObject("Target_Sphere"));
                        
                        // send event code 200 to mark collision
                        if (trigger)
                            {
                                LJUD.ePut(u3.ljhandle, LJUD.IO.PUT_DIGITAL_PORT, 8, 200, 12);
                                trigger = false;
                            }                    
                        sxr.NextStep(); // move to ISI 
                    }

                    // if no collision within specified time, then destroy block and move to next trial
                    if (sxr.TimePassed() > 1.5f)
                    {
                        Object.Destroy(sxr.GetObject("Target_Sphere"));
                        sxr.NextStep();
                    }
  
                    break;
                
                // Wait until XX secs passed before showing next stimulus OR moving to next block
                case 3:
                    if (sxr.TimePassed() > 2f)
                    {
                        // pause recordings and write all trial/eye/camera data to files
                        sxr.PauseRecordingEyeTrackerInfo();
                        sxr.PauseRecordingCameraPos();
                        sxr.WriteToTaggedFile("mainFile", locIndex.ToString() + "," + sxr.TimePassed()); // log data

                        if (sxr.GetTrial() < nTrials-1) // move to next trial
                        {
                            sxr.NextTrial();
                        }
                        else
                        {
                            sxr.NextBlock(); // move to next block
                        }
                    }
                        
                    break;

            }
        }
    }
}




// JUSTIN's FRAMEWORK
// self-paced task template (see case 0)

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class MyStudy : MonoBehaviour
//{

//    Vector3 fixationPointSize = new Vector3(0.5f, 0.5f, 0.5f); // set fixation cube size
//    Vector3 targetSphereScale = new Vector3(1f, 1f, 1f); // set target sphere size

//    // set up locations for target spheres [ {0,0,0} is center]
//    int[,] locs = { { 1, 1, 0 }, { -1, 1, 0 }, { 1, -1, 0 }, { -1, -1, 0 } };
//    int locIndex;



//    // Start is called before the first frame update
//    void Start()
//    {

//    }

//    // Update is called once per frame
//    void Update()
//    {
//        if (sxr.GetPhase() > 0) // if experiment has been started with start buttong
//        {
//            switch (sxr.GetStepInTrial()) // switch between steps in trial
//            {
//                case 0: // waiting for trial start
//                    sxr.DisplayImage("trigger"); // diplays a "press trigger to continue" image
//                    if (sxr.GetTrigger())
//                    {
//                        sxr.NextStep(); // instruction to move to next step of trial (case 1)
//                        sxr.StartTimer(); // start the timer for this trial
//                        sxr.StartRecordingEyeTrackerInfo(); // start the eye-tracker recording


//                        locIndex = Random.Range(0, 4); // location index for this trial (currently set to random, but will need to pseudorandomize)

//                        sxr.SpawnObject(PrimitiveType.Sphere, "Fixation_Point", 0f, 0f, 0f); // spawn a target sphere at location (x,y,z)
//                        sxr.ResizeObject(sxr.GetObject("Fixation_Point"), fixationPointSize, 0); // resize object

//                        sxr.SpawnObject(PrimitiveType.Sphere, "Target_Sphere", locs[locIndex,0], locs[locIndex,1], locs[locIndex,2]); // spawn a target sphere at location (x,y,z)
//                        sxr.ResizeObject(sxr.GetObject("Target_Sphere"), targetSphereScale, 0); // resize object

//                        // how do i programatically change the color of gameObjects?
//                        // I'm respawning many fixation points.  
//                    }
//                    break;

//                case 1: // check if laser from controller has collided with sphere

//                    if (sxr.TimePassed() > 2)
//                    {
//                        Object.Destroy(sxr.GetObject("Target_Sphere"));
//                        Object.Destroy(sxr.GetObject("Fixation_Point"));

//                        sxr.NextTrial();                       
//                    }
//                    break;

//            }
//        }
//    }
//}
