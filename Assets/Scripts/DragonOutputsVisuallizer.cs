using UnityEngine;

public class DragonOutputsVisuallizer : MonoBehaviour
{
    [SerializeField] private Animator AnimatorRef;
    [SerializeField] private SetWingSlider SetWingSliderRef;

    //Animation
    public void SetFlapSpeed(float Speed)
    {
        float speed = Mathf.Clamp(Speed,0.5f,2);
        AnimatorRef.speed = speed;
    }

    ///Sliders
    public void SetLeftWingValue(float newValue)
    {
        SetWingSliderRef.SetValueLeftWing(newValue);
    }

    public void SetRightWingValue(float NewValue)
    {
        SetWingSliderRef.SetValueRightWing(NewValue);
    }

    public void SetTailValue(float newValue)
    {
        SetWingSliderRef.SetValueTail(newValue);
    }

}
