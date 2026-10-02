#nullable enable
using System.Xml.Linq;

namespace Archarina64.Core;

public static class DemoRoom
{
    // Original synthetic geometry, not extracted game content.
    public static string Create()
    {
        int[][] points = [[-200,0,-200],[200,0,-200],[200,0,200],[-200,0,200],[-200,160,-200],[200,160,-200],[-200,160,200]];
        int[][] faces = [[0,2,1],[0,3,2],[0,1,5],[0,5,4],[0,4,6],[0,6,3]];
        var vertices = new XElement("Vertices");
        foreach (var p in points)
            vertices.Add(new XElement("Vertex", new XElement("X", p[0]), new XElement("Y", p[1]), new XElement("Z", p[2])));
        var triangles = new XElement("Triangles");
        foreach (var f in faces)
            triangles.Add(new XElement("Triangle", new XElement("VertIndex", f.Select(i => new XElement("int", i)))));
        return new XElement("SectionTile", new XElement("Version", 1), new XElement("Name", "Demo room"),
            new XElement("Grid", 100), new XElement("RomFingerprint", "synthetic-demo"), new XElement("SourceScene", 0),
            new XElement("Visual", vertices, new XElement("Groups", new XElement("Group", triangles))))
            .ToString();
    }
}
