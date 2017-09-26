using UnityEngine;
using System.Collections;

public class CameraObstacle : MonoBehaviour 
{
    //This tells the camera if the obstacle should fade out when obstructing.
    //If not the camera will move forward
    public bool FadeOutIfObstructing = true; 

    public float FadeEaseSpeed = 1.0f;
    public float MinFadeAlpha = 0.25f;

	void Start () 
    {
	
	}
	
	void Update () 
    {
	    UpdateFade();
	}

    //Causes the obstacle to fade out.  Call this for every frame that you want the fade out to happen.
    public void FadeOutThisFrame()
    {
        m_GoalFadeAlpha = MinFadeAlpha;
    }

    //Updates the fading
    private void UpdateFade()
    {
        m_CurrentFadeAlpha = MathUtils.ExponentialEase(FadeEaseSpeed, m_CurrentFadeAlpha, m_GoalFadeAlpha, Time.deltaTime);

        GameUtils.SetAlpha(m_CurrentFadeAlpha, gameObject);

        //Clear goal fade.  If FadeOutThisFrame stops getting called every frame, this will cause
        //the fade to go back to normal
        m_GoalFadeAlpha = 1.0f;
    }

    float m_GoalFadeAlpha;
    float m_CurrentFadeAlpha;
}
