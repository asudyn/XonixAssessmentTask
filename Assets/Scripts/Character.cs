using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField]
    protected float speed = 5f;

    protected Field field;
    protected Vector2Int direction=Vector2Int.zero;

    public virtual void Initialize(Field field) {
        this.field = field;
    }

    public virtual void Restart() {
        Stop();
    }

    public virtual void Stop() { 
        direction=Vector2Int.zero;
    }

    public void ChooseRandomDiagonalDirection() { 
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

}
