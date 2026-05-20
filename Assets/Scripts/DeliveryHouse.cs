using UnityEngine;

/// Marks a GameObject as a customer delivery house.
/// Placed on house root by CityGenerator. Stores door world position for
/// interaction range checks so the game manager doesn't need to guess.
public class DeliveryHouse : MonoBehaviour
{
    public string houseName;
    public Vector3 doorWorldPos;   // set by CityGenerator after placement
    public bool isActiveTarget;    // toggled by DeliveryGameManager
}
