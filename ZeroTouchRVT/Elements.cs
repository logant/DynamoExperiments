using System.Collections.Generic;               // This gives us access to Lists
using Autodesk.Revit.DB;                        // This is from the RevitAPI.dll
using dsGeo = Autodesk.DesignScript.Geometry;   // This is from the ProtoGeometry.dll
using rElems = Revit.Elements;                  // This is from the RevitNodes.dll
using Revit.GeometryConversion;                 // This is coming from the RevitNodes.dll
using RevitServices.Persistence;                // This is from RevitServices.dll

namespace LINE.Dynamo.ZeroTouchRVT
{
    public static class Elements
    {
        // Parameters to store data
        static Document rDoc = null; // Revit document
        static Options geoOpt;       // Geometry Options.

        /// <summary>
        /// This is a public static method, so Dynamo will find it while searching and make it available
        /// as a node that can be used within the Dynamo script. It assumes the input will be a simple list of
        /// revit elements that it will then evaluate to return a two dimensional list of Dynamo meshes, one representing the
        /// Revit element, the next the solids within it.
        /// </summary>
        /// <param name="elems">A List of Revit Elements (Autodesk.Revit.DB.Elements wrapped into a Revit.Elements.Element)</param>
        /// <param name="currentView">Optional: Get the mesh as visible in the current view. This can allow you to get a specific 
        /// version of the mesh, assuming different detail levels have different geometry, or get a mesh that's cut by a scope box.</param>
        /// <returns>A list of Autodesk.DesignScript.Geometry.Mesh objects that correlate back to the list of Revit Elements.</returns>
        public static List<List<dsGeo.Mesh>> MeshFromElement(List<rElems.Element> elems, bool currentView = false)
        {
            // Get the current Revit document by using the RevitServices reference. This is similar
            // to how you would get the Revit document in a Python node within Dynamo.
            rDoc = DocumentManager.Instance.CurrentDBDocument;

            // Set up the geometry references. This will be used for extracting the geometry from an element.
            geoOpt = rDoc.Application.Create.NewGeometryOptions();
            geoOpt.ComputeReferences = true;
            geoOpt.IncludeNonVisibleObjects = false;

            List<int> elemsInView = new List<int>();
            if (currentView)
            {
                // First get a list of Element Ids that represent objects in the active view.
                View view = rDoc.ActiveView;
                ICollection<ElementId> viewElems = new FilteredElementCollector(rDoc, view.Id).ToElementIds();
                foreach (ElementId eid in viewElems)
                {
                    elemsInView.Add(eid.IntegerValue);
                }

                // Because this is view specific, specify the view and it's detail level in the geometry options.
                geoOpt.View = view;
                geoOpt.DetailLevel = view.DetailLevel;
            }

            // There's several steps to go through, so we'll break it down into separate methods to make it easier
            // to encapsulate what we're trying to write. The first one we'll call is GetMeshes. Notice how the GetMeshes
            // method occurs after this initial method, so unlike Python the order of method or functions in C# doesn't matter.
            List<List<dsGeo.Mesh>> retrievedMeshes = GetMeshes(elems, currentView, elemsInView);

            return retrievedMeshes;
        }

        /// <summary>
        /// Iterate through the elements to 
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="currentViewOnly"></param>
        /// <param name="elemsInView"></param>
        /// <returns></returns>
        private static List<List<dsGeo.Mesh>> GetMeshes(List<rElems.Element> elements, bool currentViewOnly, List<int> elemsInView)
        {
            List<List<dsGeo.Mesh>> dynamoMeshes = new List<List<dsGeo.Mesh>>();
            foreach (rElems.Element rElem in elements)
            {
                List<dsGeo.Mesh> elemMeshes = new List<dsGeo.Mesh>();

                if (rElem != null)
                {
                    // If the optional CurrentViewOnly input for the node is true, check to make
                    // sure the element that we're looking for is actually visible in the view.
                    // There's no sense in getting it's geometry if it's not there to begin with.
                    if (currentViewOnly && !elemsInView.Contains(rElem.Id))
                        elemMeshes.Add(null);
                    else
                    {
                        // Get an Autodesk.Revit.DB.Element from the Revit.Elements.Element's Id property.
                        Element revElem = rDoc.GetElement(new ElementId(rElem.Id));

                        // Get the GeometryElement for the current revElem
                        GeometryElement geoElement = revElem.get_Geometry(geoOpt);

                        // Check to see if the geoElement is null, and if so return add a list with a single null object and continue.
                        if(null == geoElement)
                        {
                            elemMeshes.Add(null);
                            dynamoMeshes.Add(elemMeshes);
                            continue;
                        }

                        // If the GeometryElement is not null, lets continue by grabbing the objects in it.
                        foreach(GeometryObject geoObj in geoElement)
                        {
                            // The GeometryObject could be a GeometryInstasnce if we're dealing with a typical family or even an import
                            // but will just start returning the geometry objects (Solid, Mesh, Line, etc.) if it's a System Family.
                            // Check if it's a geometry object we'll need to go deeper, otherwise we'll just need to check for the right geometry.
                            if(geoObj is GeometryInstance)
                            {
                                // Iterate through the objects in the instance to get the appropriate geometry.
                                GeometryInstance geoInst = geoObj as GeometryInstance;
                                foreach(GeometryObject instObj in geoInst.GetInstanceGeometry())
                                {
                                    if(instObj is Solid)
                                    {
                                        Solid solid = instObj as Solid;
                                        if (null == solid || 0 == solid.Faces.Size || 0 == solid.Edges.Size)
                                            continue;
                                        else
                                        {
                                            // Convert the Solid to a Dynamo Mesh
                                            dsGeo.Mesh dMesh = SolidToMesh(solid);
                                            elemMeshes.Add(dMesh);
                                        }
                                    }
                                    else if(instObj is Mesh)
                                    {
                                        Mesh rMesh = instObj as Mesh;
                                        if (null == rMesh || 0 == rMesh.Vertices.Count)
                                            continue;
                                        else
                                        {
                                            dsGeo.Mesh dMesh = RevitToProtoMesh.ToProtoType(rMesh, true);
                                            elemMeshes.Add(dMesh);
                                        }

                                    }
                                }
                            }
                            else if (geoObj is Solid)
                            {
                                Solid solid = geoObj as Solid;
                                if (null == solid || 0 == solid.Faces.Size || 0 == solid.Edges.Size)
                                    continue;
                                else
                                {
                                    // Convert the Solid to a Dynamo Mesh
                                    dsGeo.Mesh dMesh = SolidToMesh(solid);
                                    elemMeshes.Add(dMesh);
                                }
                            }
                            else if (geoObj is Mesh)
                            {
                                Mesh rMesh = geoObj as Mesh;
                                if (null == rMesh || 0 == rMesh.Vertices.Count)
                                    continue;
                                else
                                {
                                    dsGeo.Mesh dMesh = RevitToProtoMesh.ToProtoType(rMesh, true);
                                    elemMeshes.Add(dMesh);
                                }

                            }
                        }
                    }
                }
                if (elemMeshes.Count == 0)
                    elemMeshes.Add(null);

                dynamoMeshes.Add(elemMeshes);
            }

            return dynamoMeshes;
        }

        /// <summary>
        /// This is another private method, so Dynamo won't show it when it loads the library as a Zero Touch plugin.
        /// This method takes an Autodesk.Revit.DB.Solid from the Revit element and tessellates it to get an
        /// Autodesk.Revit.DB.Mesh from it. This mesh is then converted using the Revit.GeometryConversion referance
        /// to go from the Autodesk.Revit.DB.Mesh to an Autodesk.DesignScript.Geometry.Mesh.
        /// </summary>
        /// <param name="solid">Autodesk.Revit.DB.Solid geometry object to mesh.</param>
        /// <returns>An Autodesk.DesignScript.Geometry.Mesh from the solid.</returns>
        private static dsGeo.Mesh SolidToMesh(Solid solid)
        {
            dsGeo.Mesh mesh = null;

            // Each face of a solid is tessellated separately into a unique mesh, so we first
            // Collect all of the meshes that represent a solid's face.
            List<dsGeo.Mesh> unjoinedMeshes = new List<dsGeo.Mesh>();
            foreach (Face f in solid.Faces)
            {
                // call the Triangulate method for the Autodesk.Revit.DB.Face
                Mesh rMesh = f.Triangulate();

                // Use Revit.GeometryConversion.RevitToProtoMesh.ToProtoType method to convert the 
                // Autodesk.Revit.DB.Mesh to an Autodesk.DesignScript.Geometry.Mesh
                dsGeo.Mesh dMesh = RevitToProtoMesh.ToProtoType(rMesh, true);

                unjoinedMeshes.Add(dMesh);
            }

            // Join meshes
            if (unjoinedMeshes.Count == 1)
            {
                mesh = unjoinedMeshes[0];
            }
            else
            {
                // Join all of the meshes into a single mesh representing the input solid.
                List<dsGeo.Point> vertices = new List<dsGeo.Point>();
                List<dsGeo.IndexGroup> indexGroups = new List<dsGeo.IndexGroup>();

                foreach (dsGeo.Mesh m in unjoinedMeshes)
                {
                    if (m == null)
                        continue;
                    // A count of the verticies that exist at the start of adding the current mesh.
                    // This is the base index for specifying the vertex index for each mesh face.
                    int baseCount = vertices.Count;

                    // Then add the vertices of this current mesh to the list.
                    vertices.AddRange(m.VertexPositions);

                    // Build the Mesh Faces (Autodesk.DesignScript.Geometry.IndexGroup) for this mesh and add it to
                    // the list of Autodesk.DesignScript.Geometry.IndexGroups.
                    foreach (dsGeo.IndexGroup ig in m.FaceIndices)
                    {
                        // Three vertices for the face
                        if (ig.Count == 3)
                        {
                            // The current meshes vertex indices starts at 0, but the new one will start at baseCount.
                            dsGeo.IndexGroup iGroup = dsGeo.IndexGroup.ByIndices((uint)(ig.A + baseCount), (uint)(ig.B + baseCount), (uint)(ig.C + baseCount));
                            indexGroups.Add(iGroup);
                        }
                        // Four vertices for the face.
                        else
                        {
                            // The current meshes vertex indices starts at 0, but the new one will start at baseCount.
                            dsGeo.IndexGroup iGroup = dsGeo.IndexGroup.ByIndices((uint)(ig.A + baseCount), (uint)(ig.B + baseCount), (uint)(ig.C + baseCount), (uint)(ig.D + baseCount));
                            indexGroups.Add(iGroup);
                        }
                    }
                }

                // Create a new Autodesk.DesignScript.Geometry.Mesh based on the new list of Vertices and IndexGroups (mesh faces)
                dsGeo.Mesh joinedMesh = dsGeo.Mesh.ByPointsFaceIndices(vertices, indexGroups);
                if (joinedMesh != null)
                {
                    mesh = joinedMesh;
                }
            }

            return mesh;
        }
    }
}
