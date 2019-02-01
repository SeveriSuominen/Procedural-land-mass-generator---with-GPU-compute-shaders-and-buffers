using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TransformMemoryToken {

    const int max = 32;
    public readonly int scaler;

    public Transform owner { get; private set; }

    public readonly List<Vector3> positions;


    public TransformMemoryToken(Transform owner, int scaler)
    {
        this.owner = owner;
        this.scaler = scaler;

        this.positions   = new List<Vector3>();

        Update();
    }

    public bool IsChanged()
    {
        if (positions.Count < 2)
            return false;

        return positions[positions.Count - 1] != positions[positions.Count - 2];
    }


    public Vector3 Latest()
    {
        if (positions.Count < 1)
            return Vector3.zero;

        return positions[positions.Count - 1];
    }

    public void Update()
    {
        if (positions.Count > max)
            positions.RemoveAt(0);

        this.positions.Add(
            new Vector3Int(
                Mathf.FloorToInt(owner.position.x / scaler),
                Mathf.FloorToInt(owner.position.y / scaler),
                Mathf.FloorToInt(owner.position.z / scaler)
            )
        );
    }
}
