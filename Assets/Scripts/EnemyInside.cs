using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyInside : Character
{
    private void Update() {
        if(Time.timeScale == 0)
            return;
        if(direction != Vector2Int.zero) {
            Vector2Int borders = field.TestForBorders(transform.position,direction,4);
            if(borders != Vector2Int.zero) {
                //Special condition when objects find player's path
                if(Mathf.Abs(borders.x) > 1 || Mathf.Abs(borders.y) > 1) {
                    GameManager.Instance.PlayerDeath();
                } else {
                    //Colliding on y axis
                    if(Mathf.Abs(borders.y) > 0) {
                        direction.y = -direction.y;
                    }
                    //Colliding on x axis
                    if(Mathf.Abs(borders.x) > 0) {
                        direction.x = -direction.x;
                    }
                }
            }
            this.transform.position += (Vector3)(Vector2)direction * speed * Time.deltaTime;
        }
    }

}
