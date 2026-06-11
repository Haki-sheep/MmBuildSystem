
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Mm_Budier
{
    public class TestCube : CubeBehaviour
    {
        public override void OnPlaced(PlacedCube placedCube)
        {
            base.OnPlaced(placedCube);
            Debug.Log("OnPlaced: " + placedCube.data.CubeType);
        }
        public override void OnRemoved()
        {   
            base.OnRemoved();
            Debug.Log("OnRemoved: " + this.curCubeData.data.CubeType);
            this.curCubeData = null;
        }
        public override void OnInteract(PlacedCube placedCube)
        {
            base.OnInteract(placedCube);
            Debug.Log("OnInteract: " + placedCube.data.CubeType);
        }
    }
}