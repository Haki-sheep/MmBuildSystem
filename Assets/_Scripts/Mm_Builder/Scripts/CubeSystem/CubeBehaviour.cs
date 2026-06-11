using Mm_Budier;
using UnityEngine;

public abstract class CubeBehaviour : MonoBehaviour, ICubeBehaviour
{
    protected PlacedCube curCubeData;
    public virtual void OnPlaced(PlacedCube placedCube){
        this.curCubeData = placedCube;
    }
    public virtual void OnRemoved(){
    }  
    public virtual void OnUpdated(PlacedCube placedCube){}
    public virtual void OnInteract(PlacedCube placedCube){}
}
