using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class DummyAgent : Agent
{
    private int stepCount;

    //Test functions to see if a base agent would be functional Agent 

    public override void OnEpisodeBegin()
    {
        stepCount = 0;
        Debug.Log("EPISODE BEGIN");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(0f);
        Debug.Log("OBSERVATION SENT");
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];

        Debug.Log("ACTION RECEIVED: " + action);

        stepCount++;

        AddReward(0.01f);

        if (stepCount >= 10)
        {
            Debug.Log("ENDING EPISODE");
            EndEpisode();
        }
    }
}
