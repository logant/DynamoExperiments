using System.Collections.Generic;
using Autodesk.DesignScript.Geometry;

namespace LINE.Dynamo.ZeroTouch
{
    public static class Meshes
    {
        /// <summary>
        /// Join a list of meshes into a single mesh            
        /// </summary>
        /// <param name="meshes">Meshes to Join</param>
        /// <returns name="Mesh">Returns a single joined mesh </returns>
        public static Mesh MeshJoin(List<Mesh> meshes)
        {
           // Create lists to store the vertex points and index groups (faces)
            List<Point> vertices = new List<Point>();
            List<IndexGroup> faces = new List<IndexGroup>();

            foreach (Mesh m in meshes)
            {
                // Get the number of vertices before adding the new set to them
                // This count will be what we add to the mesh face indices to shift
                // it to the growing list of vertices.
                uint currentVertCount = (uint)vertices.Count;

                // Add the current meshes vertices to the list
                vertices.AddRange(m.VertexPositions);

                // Iterate through the mesh faces, creating new ones with shifted indices.
                foreach (IndexGroup f in m.FaceIndices)
                {
                    // Create a new Mesh Face (IndexGroup) by adding the vertex count to each index
                    IndexGroup updatedFace = null;
                    if (f.Count == 3)
                        updatedFace = IndexGroup.ByIndices(f.A + currentVertCount, f.B + currentVertCount, f.C + currentVertCount);
                    else
                        updatedFace = IndexGroup.ByIndices(f.A + currentVertCount, f.B + currentVertCount, f.C + currentVertCount, f.D + currentVertCount);

                    // Add the mesh face to our growing list of mesh faces.
                    faces.Add(updatedFace);
                }
            }

            // Make sure we retrieved data before trying to build a new mesh
            if (vertices.Count > 0 && faces.Count > 0)
            {
                // Build a new mesh with all of the vertices and faces.
                Mesh mesh = Mesh.ByPointsFaceIndices(vertices, faces);
                return mesh;
            }
            return null;
        }
    }
}
