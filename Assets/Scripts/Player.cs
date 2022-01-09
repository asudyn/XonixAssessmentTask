using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    private float inputThreshold = 0.1f;

    private void Update() {
        if(Time.timeScale == 0)
            return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if(horizontal > inputThreshold) {
            direction = Vector2Int.right;
        } else if(horizontal < -inputThreshold) {
            direction = Vector2Int.left;
        } else if(vertical > inputThreshold) {
            direction = Vector2Int.up;
        } else if(vertical < -inputThreshold) { 
            direction = Vector2Int.down;
        }

        //Stop at screen edges
        Bounds screenBounds = new Bounds(
            Camera.main.WorldToScreenPoint(transform.position),
            Vector2.one*8f
        );
        if((screenBounds.max.x >= Screen.width && direction.x > 0) || (screenBounds.min.x <= 0 && direction.x<0)) {
            direction.x = 0;
        }
        if((screenBounds.max.y >= Screen.height && direction.y > 0) || (screenBounds.min.y <= 16 && direction.y<0)) {
            direction.y = 0;
        }

        if(direction != Vector2Int.zero) {
            this.transform.position += (Vector3)(Vector2)direction * speed * Time.deltaTime;
        }

        field.CutPath(transform.position,4);

        Vector2Int borders = field.TestForBorders(this.transform.position,direction,4);
        if(borders != Vector2Int.zero) {
            //If player touched the path
            if(Mathf.Abs(borders.x) > 1 || Mathf.Abs(borders.y) > 1) {
                GameManager.Instance.PlayerDeath();
            }
        }

    }

}
