using UnityEngine;

public class DragonController : MonoBehaviour
{
    [SerializeField] private Animator AnimatorRef;
    [SerializeField] private SetWingSlider SetWingSliderRef;
    private Vector2 BoundsX = new Vector2(-95, 95), BoundsZ = new Vector2(1, 1900);

    public Vector3 GetRandomTargetPosition()
    {
        float randomX = Random.Range(BoundsX.x, BoundsX.y);
        float randomY = Random.Range(0,10);
        float randomZ = Random.Range(BoundsZ.x, BoundsZ.y);
        return new Vector3();
    }
    public void SetFlapSpeed(float Speed)
    {
        float speed = Mathf.Clamp(Speed,0.5f,2);
        AnimatorRef.speed = speed;
    }

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
