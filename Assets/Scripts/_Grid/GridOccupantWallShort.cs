using UnityEngine;
using System.Collections.Generic;

// Para objetos de 1x1
public class GridOccupantWallShort : GridOccupant 
{
    public Vector2 growthDirection = new Vector2(1, -1); 

    public override List<Vector3> GetOccupiedWorldCenters()
    {
        // Usamos el método GetBoard() heredado de la clase padre
        var board = GetBoard();
        if (board == null) return new List<Vector3>();

        List<Vector3> bluePoints = new List<Vector3>();
        float halfTile = board.tileSize / 2f;

        // Vectores de dirección
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        
        // Calculamos los offsets basándonos en la dirección que elegiste
        float xOffset = halfTile * growthDirection.x;
        float zOffset = halfTile * growthDirection.y;

        // Matemáticas: Pivote + (Derecha * medioTile) + (Adelante * medioTile)
        Vector3 centerPoint = transform.position + (right * xOffset) + (forward * zOffset);
        
        // Mantenemos altura
        centerPoint.y = transform.position.y;

        bluePoints.Add(centerPoint);

        return bluePoints;
    }

    // Dibujamos un Gizmo especial para que se vea la esquina y hacia dónde crece
    protected override void OnDrawGizmosSelected()
    {
        // Llamamos al base para que dibuje el punto azul y la caja cian
        base.OnDrawGizmosSelected();

        var board = GetBoard();
        if (board == null) return;

        //  Una línea amarilla desde la esquina (Pivote) hasta el centro
        if (growthDirection != Vector2.zero)
        {
            List<Vector3> points = GetOccupiedWorldCenters();
            if (points.Count > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, points[0]);
                
                // Dibuja la esquina (Pivote real)
                Gizmos.color = new Color(1f, 0.6f, 0f);
                Gizmos.DrawSphere(transform.position, 0.1f);
            }
        }
    }
}