using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Field:MonoBehaviour {

    [SerializeField]
    private Color32 fillColor;
    [SerializeField]
    private Color32 pathColor;
    [SerializeField]
    private Color32 clearedColor;

    private int width = 608;
    private int height = 352;

    private int maxIndex; //Maximum pixel index
    private Texture2D fieldTexture; //Image of the field
    private NativeArray<Color32> pixels;

    private bool pathStarted = false;
    private Vector2Int[] pathEnterPixels;
    private Vector2Int lastPathPixel; //Last pixel of the cut-out path we have drawn sow e could fill it later
    private int claimed = 0;

    public void Initialize() {
        maxIndex = width * height - 1;

        Renderer mr = GetComponent<Renderer>();
        Material mat = mr.sharedMaterials[0];

        //Just in case, check if a texture is already assigned and destroy it
        if(mat.mainTexture != null) {
            Destroy(mat.mainTexture);
        }

        //Create field texture and assign it to material
        fieldTexture = new Texture2D(width,height);
        mat.mainTexture = fieldTexture;

        //Get pixel array which can be modified to draw on texture
        pixels = fieldTexture.GetPixelData<Color32>(0);
    }

    #region Game level methods

    public void Restart() {
        Fill();
        pathStarted = false;
        claimed = 0;
    }

    //Sets the field to its initial filled state
    public void Fill() {
        for(int i = 0;i < pixels.Length;i++) {
            pixels[i] = fillColor;
        }
        fieldTexture.Apply(false); //Apply pixel data
    }

    //Get random point on the field in world coordinates
    public Vector3 GetRandomPointOnField(int extents=8) {
        Vector2Int pos = Vector2Int.zero;
        while(!IsRegionFilled(pos,extents)) { 
            pos = new Vector2Int(
                Random.Range(extents,width-extents),
                Random.Range(extents,height-extents)
            );
        }
        Vector3 worldPos = FieldToWorld(pos);
        worldPos.z = 0;
        return worldPos;
    }

    private bool IsRegionFilled(Vector2Int pos,int extents) { 
        Vector2Int min = new Vector2Int(pos.x - extents,pos.y - extents);
        Vector2Int max = new Vector2Int(pos.x + extents,pos.y + extents);
        for(int x = min.x;x < max.x;x++) {
            for(int y = min.y;y < max.y;y++) {
                if(!PixelHasColor(x,y,ref fillColor)) {
                    return false;
                }
            }
        }
        return true;
    }

    //Cuts path when player moves across the field
    public void CutPath(Vector2 center,int extents) {
        Vector2Int pixelPosition = WorldToField(center);
        Vector2Int min = new Vector2Int(pixelPosition.x - extents,pixelPosition.y - extents);
        Vector2Int max = new Vector2Int(pixelPosition.x + extents,pixelPosition.y + extents);
        bool drawnAny = false;
        for(int x = min.x;x < max.x;x++) {
            for(int y = min.y;y < max.y;y++) {
                if(PixelHasColor(x,y,ref fillColor) || PixelHasColor(x,y,ref pathColor)) {
                    if(SetPixel(x,y,pathColor)) {
                        drawnAny = true;
                        lastPathPixel = new Vector2Int(x,y);
                    }
                }
            }
        }
        if(drawnAny && !pathStarted) {
            pathStarted = true; //We started drawing a path
            pathEnterPixels=new Vector2Int[4] {
                new Vector2Int(pixelPosition.x+extents,pixelPosition.y+extents),
                new Vector2Int(pixelPosition.x-extents-1,pixelPosition.y+extents),
                new Vector2Int(pixelPosition.x+extents,pixelPosition.y-extents-1),
                new Vector2Int(pixelPosition.x-extents-1,pixelPosition.y-extents-1)
            };
        }
        if(!drawnAny && pathStarted) {
            pathStarted = false; //We exited the field and stopped drawing the path

            int claimedPixels = 0;

            //Start flood fill from the last drawn path pixel
            claimedPixels+=FloodFill(lastPathPixel);

            //float startTime = Time.realtimeSinceStartup;


            //Check a pixel beyond every corner for a fillable area
            foreach(Vector2Int testPoint in pathEnterPixels) {
                if(PixelHasColor(testPoint,ref fillColor)) {
                    if(TestAreaForEnemies(testPoint)) {
                        claimedPixels+=FloodFill(testPoint);
                    }
                }
            }
            Vector2Int[] pathExitPixels = new Vector2Int[4] {
                new Vector2Int(pixelPosition.x+extents,pixelPosition.y+extents),
                new Vector2Int(pixelPosition.x-extents-1,pixelPosition.y+extents),
                new Vector2Int(pixelPosition.x+extents,pixelPosition.y-extents-1),
                new Vector2Int(pixelPosition.x-extents-1,pixelPosition.y-extents-1)
            };
            foreach(Vector2Int testPoint in pathExitPixels) {
                if(PixelHasColor(testPoint,ref fillColor)) {
                    if(TestAreaForEnemies(testPoint)) {
                        claimedPixels+=FloodFill(testPoint);
                    }
                }
            }

            //Debug.Log(Time.realtimeSinceStartup - startTime);
            //Debug.Break();

            claimed += claimedPixels;
            GameManager.Instance.AddScore(claimedPixels);
            GameManager.Instance.UpdateClaimed((claimed/((float)maxIndex-1))*100f);
            GameManager.Instance.UpdateUI();
            GameManager.Instance.StopPlayer();
        }
        fieldTexture.Apply(false); //Apply pixel data
    }

    public void ResetPath() {
        if(pathStarted) { 
            FloodFill(lastPathPixel,false);
            pathStarted = false;
        }
    }

    /// <summary>
    /// The most simple of flood fill algorithms.
    /// I would implement something more efficient if given
    /// more time to figure it out.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="step"></param>
    private int FloodFill(Vector2Int start,bool claimed = true) {
        int claimedPixels = 0;
        Color32 startColor = GetPixel(start);
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        while(queue.Count > 0) {
            Vector2Int pos = queue.Dequeue();
            if(IsPosWithinBounds(pos) && PixelHasColor(pos,ref startColor)) {
                if(claimed) {
                    SetPixel(pos,clearedColor);
                    claimedPixels++;
                } else { 
                    SetPixel(pos,fillColor);
                }
                queue.Enqueue(new Vector2Int(pos.x - 1,pos.y));
                queue.Enqueue(new Vector2Int(pos.x,pos.y + 1));
                queue.Enqueue(new Vector2Int(pos.x + 1,pos.y));
                queue.Enqueue(new Vector2Int(pos.x,pos.y - 1));
            }
        }
        return claimedPixels;
    }

    /// <summary>
    /// Tests is there are enemies enclosed within a given area.
    /// It is a modified version of flood fill, but with a bigger step
    /// so it doesnt test every pixel.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    private bool TestAreaForEnemies(Vector2Int start,int step = 4) {
        Color32 startColor = GetPixel(start);
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<int> checkedIndices = new HashSet<int>(); //To keep indices checked pixels so we'll only check them once
        queue.Enqueue(start);
        while(queue.Count > 0) {
            Vector2Int pos = queue.Dequeue();
            if(IsPosWithinBounds(pos)) {
                if(PixelHasColor(pos,ref startColor) && !checkedIndices.Contains(PosToInt(pos))) {
                    RaycastHit2D hit = Physics2D.GetRayIntersection(new Ray(FieldToWorld(pos),Vector3.forward),Mathf.Infinity);
                    if(hit.collider != null && hit.collider.tag == "Enemy") {
                        return false;
                    }
                    checkedIndices.Add(PosToInt(pos));
                    Vector2Int[] newPoints = new Vector2Int[4] {
                        new Vector2Int(pos.x - step,pos.y),
                        new Vector2Int(pos.x,pos.y + step),
                        new Vector2Int(pos.x + step,pos.y),
                        new Vector2Int(pos.x,pos.y - step),
                    };
                    foreach(Vector2Int newPoint in newPoints) {
                        if(newPoint.x >= 0 && newPoint.y >= 0 && newPoint.x < width && newPoint.y < height) {
                            if(!checkedIndices.Contains(PosToInt(newPoint))) {
                                queue.Enqueue(newPoint);
                            }
                        }
                    }
                }
            }
        }
        return true;
    }

    public Vector2Int TestForBorders(Vector3 worldPosition,Vector2Int direction,int extents,bool inside=true) {
        Vector2Int pixelPosition = WorldToField(worldPosition);
        Vector2Int borderDirection = Vector2Int.zero;
        //Will hold pixel coordinates for each side we need to test
        Vector2Int[] testPoints = new Vector2Int[extents * 2];
        //X direction check
        if(direction.x != 0) {
            for(int i = 0;i < testPoints.Length;i++) {
                testPoints[i] = new Vector2Int(pixelPosition.x + extents * direction.x + (direction.x < 0 ? -1 : 0),pixelPosition.y + i - extents);
            }
            foreach(Vector2Int testPoint in testPoints) {
                if(inside) {
                    //SetPixel(testPoint,Color.red);
                    if(!PixelHasColor(testPoint,ref fillColor)) {
                        if(PixelHasColor(testPoint,ref pathColor)) {
                            borderDirection.x = direction.x * 2;
                        } else {
                            borderDirection.x = direction.x;
                        }
                    }
                } else {
                    if(PixelHasColor(testPoint,ref fillColor)) {
                        borderDirection.x = direction.x;
                    }
                }
            }
        }
        //Y direction check
        if(direction.y != 0) {
            for(int i = 0;i < testPoints.Length;i++) {
                testPoints[i] = new Vector2Int(pixelPosition.x + i - extents,pixelPosition.y + extents * direction.y + (direction.y < 0 ? -1 : 0));
            }
            foreach(Vector2Int testPoint in testPoints) {
                //SetPixel(testPoint,Color.red);
                if(inside) {
                    if(!PixelHasColor(testPoint,ref fillColor)) {
                        if(PixelHasColor(testPoint,ref pathColor)) {
                            borderDirection.y = direction.y * 2;
                        } else {
                            borderDirection.y = direction.y;
                        }
                    }
                } else {
                    if(PixelHasColor(testPoint,ref fillColor)) {
                        borderDirection.y = direction.y;
                    }
                }
            }
        }
        return borderDirection;
    }

    #endregion

    #region Convert coordinates to texture pixels and vice-versa

    public Vector2Int WorldToField(Vector2 worldPosition) {
        Vector2 normalizedPosition = (Vector2)transform.InverseTransformPoint(worldPosition) + (Vector2.one / 2f);
        return new Vector2Int(
            Mathf.RoundToInt(normalizedPosition.x * width),
            Mathf.RoundToInt(normalizedPosition.y * height)
        );
    }

    public Vector3 FieldToWorld(Vector2Int fieldPosition) {
        Vector2 normalizedPosition = new Vector2(
            ((float)fieldPosition.x) / width,
            ((float)fieldPosition.y) / height
        );
        return transform.TransformPoint(normalizedPosition - (Vector2.one / 2f)) - Vector3.forward;
    }

    #endregion

    #region Pixel level methods

    private bool PixelHasColor(Vector2Int position,ref Color32 color) {
        return PixelHasColor(position.x,position.y,ref color);
    }

    private bool PixelHasColor(int x,int y,ref Color32 color) {
        if(!IsPosWithinBounds(x,y))
            return false;
        int index = PosToInt(x,y);
        if(index > maxIndex)
            return false;
        return pixels[index].Equals(color);
    }

    private Color32 GetPixel(Vector2Int position) {
        return GetPixel(position.x,position.y);
    }

    private Color32 GetPixel(int x,int y) {
        if(!IsPosWithinBounds(x,y))
            return Color.clear;
        int index = PosToInt(x,y);
        if(index > maxIndex)
            return Color.clear;
        return pixels[index];
    }

    private bool SetPixel(Vector2Int position,Color32 color) {
        return SetPixel(position.x,position.y,color);
    }

    private bool SetPixel(int x,int y,Color32 color) {
        if(!IsPosWithinBounds(x,y))
            return false;
        int index = PosToInt(x,y);
        if(index > maxIndex)
            return false;
        pixels[index] = color;
        return true;
    }

    private bool IsPosWithinBounds(Vector2Int position) { 
        return IsPosWithinBounds(position.x,position.y);
    }

    private bool IsPosWithinBounds(int x,int y) { 
        return (x >= 0 && y >= 0 && x < width && y < width);
    }

    private int PosToInt(Vector2Int position) {
        return PosToInt(position.x,position.y);
    }

    private int PosToInt(int x, int y) {
        return x + (width * y);
    }

    #endregion

}
