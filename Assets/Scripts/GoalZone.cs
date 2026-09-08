using UnityEngine;

public class GoalZone : MonoBehaviour
{
    public int ScoringPlayer = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        var ball = other.GetComponent<Ball>();
        if (ball == null)
            return;

        if (PongGame.Instance != null)
            PongGame.Instance.OnGoal(ScoringPlayer);
    }
}
