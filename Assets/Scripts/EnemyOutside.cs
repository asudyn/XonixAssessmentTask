using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyOutside : Character
{
    private void Awake() {
        int choice = Random.Range(0,4);
        if(choice == 0) {
            direction = new Vector2Int(1,1);
        } else if(choice==1) { 
            direction = new Vector2Int(-1,1);
        } else if(choice==2) { 
            direction = new Vector2Int(-1,-1);
        } else if(choice==3) { 
            direction = new Vector2Int(1,-1);
        }
    }

    private void Update() {
        if(Time.timeScale == 0)
            return;
        if(direction != Vector2Int.zero) {
            //Bounce from screen edges
            Bounds screenBounds = new Bounds(
                Camera.main.WorldToScreenPoint(transform.position),
                Vector2.one*8f
            );
            if((screenBounds.max.x >= Screen.width && direction.x > 0) || (screenBounds.min.x <= 0 && direction.x<0)) {
                direction.x = - direction.x;
            }
            if((screenBounds.max.y >= Screen.height && direction.y > 0) || (screenBounds.min.y <= 16 && direction.y<0)) {
                direction.y = -direction.y;
            }
            //Bounce from field edges
            Vector2Int borders = field.TestForBorders(this.transform.position,direction,4,false);
            if(borders != Vector2Int.zero) {
                if(Mathf.Abs(borders.x) > 0) { 
                    direction.x = -direction.x;
                }
                if(Mathf.Abs(borders.y) > 0) {
                    direction.y = -direction.y;
                } 
            }

            this.transform.position += (Vector3)(Vector2)direction * speed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if(Time.timeScale == 0)
            return;
        if(collision.tag == "Player") {
            GameManager.Instance.PlayerDeath();
        }
    }

    private void OnTriggerStay2D(Collider2D collision) {
        if(Time.timeScale == 0)
            return;
        if(collision.tag == "Player") {
            GameManager.Instance.PlayerDeath();
        }
    }

}
