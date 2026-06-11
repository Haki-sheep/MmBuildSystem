using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mm_Budier
{
    public interface ICubeBehaviour
    {
        void OnPlaced(PlacedCube placedCube);
        void OnRemoved();
        void OnInteract(PlacedCube placedCube);
    }
}